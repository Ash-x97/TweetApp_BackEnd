using com.tweetapp.Interfaces;
using com.tweetapp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web;

namespace com.tweetapp.Controllers
{
    [ApiController] 
    [Route("api/v1.0/tweet")]
    public class TweetController : Controller
    {
        private readonly ILogger<TweetController> _logger;
        private readonly IUserService _userService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITweetService _tweetService;
        private readonly IExceptionService _exceptionService;
        private readonly IExternalCommunicationService _exCommService;

        public TweetController(ILogger<TweetController> logger, IUserService userService, ITweetService tweetService, IHttpContextAccessor httpContextAccessor, IExceptionService exceptionService, IExternalCommunicationService exCommService)
        {
            _logger = logger;
            _userService = userService;
            _httpContextAccessor = httpContextAccessor;
            _tweetService = tweetService;
            _exceptionService = exceptionService;
            _exCommService = exCommService;
        }
        
        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel loginData)
        {
            try
            {               
                if (!(loginData is null) && ModelState.IsValid)
                {
                    loginData = await _exCommService.PreventXSS(loginData);

                    string sessionToken = await _userService.Login(loginData.LoginId, loginData.Password);
                    if (sessionToken == "")
                    {
                        UserModel user =await _userService.GetCurrentUserDetails(loginData.LoginId);
                        if (user is null)
                            return new NotFoundObjectResult(new { message= "NO_USER"});

                        return new UnauthorizedObjectResult(new { message= "PASSWORD_INCORRECT" });
                    } 
                    else if (sessionToken == "Inactive")
                    {
                        return new UnauthorizedObjectResult(new { message="INACTIVE" });
                    }
                    else
                    {
                        return new OkObjectResult(new { token = sessionToken, message = "LOGIN_SUCCESS" });

                    }
                }                
                 return new BadRequestObjectResult(new { message = "FAIL_VALIDATION" });                
            }
            catch (Exception e)
            {
                string eId=await _exceptionService.LogExceptionToDB(e);
                return new BadRequestObjectResult(new { message = "EXCEPTION", id= eId });             
            }
        }

        [HttpPost]
        [Route("register")]
        public async  Task<IActionResult> Register([FromBody]UserModel user)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    user=await _exCommService.PreventXSS(user);

                    string otpToken = await _userService.Register(user);

                    if (otpToken == "")
                        return StatusCode(StatusCodes.Status500InternalServerError, new { message = "EMAIL_SENT_FAIL" });
                    else if (otpToken == "IsActive")
                        return new BadRequestObjectResult(new { message = "USER_EXISTS" });
                    else if (otpToken == "Already")
                        return new BadRequestObjectResult(new { message = "LOGIN_EMAIL_EXISTS" });
                    else
                        return new OkObjectResult(new { message = "REGISTRATION_SUCCESS", token = otpToken });                   
                }
                return new BadRequestObjectResult(new { message = "FAIL_VALIDATION" });
            }
            catch (Exception e)
            {
                string eId = await _exceptionService.LogExceptionToDB(e);
                return new BadRequestObjectResult(new { message = "EXCEPTION", id= eId });
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "AuthenticatedUser")]
        [HttpPut]
        [Route("{loginId}/update")]
        public async Task<IActionResult> UpdateNotification(string loginId, [FromBody] List<Notification> notifications)
        {
            try
            {
                string sessionUser = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
                if (sessionUser != loginId)
                    return new ConflictObjectResult(new { message= "SESSION_USER_CONFLICT" });

                if (ModelState.IsValid)
                {                    
                    if(!(notifications is null))
                    {
                        if (await _userService.UpdateNotifications(loginId,notifications))
                            return new OkObjectResult(new { message = "UPDATE_SUCCESS" });
                        else
                            return new BadRequestObjectResult(new { message = "UPDATE_FAIL" });
                    }
                    return new BadRequestObjectResult(new { message = "NOTI_NULL" });
                }
                return new BadRequestObjectResult(new { message = "FAIL_VALIDATION" });
            }
            catch (Exception e)
            {
                string eId = await _exceptionService.LogExceptionToDB(e);
                return new BadRequestObjectResult(new { message = "EXCEPTION", id = eId });
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "InactiveUser")]
        [HttpPut]
        [Route("{loginId}/validate")]
        public async Task<IActionResult> ValidateOtp(string loginId, [FromBody] OTPV otp)
        {
            try
            {
                string sessionOtp = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.Sid).Value;
                if(sessionOtp != otp.otpValue)
                    return new BadRequestObjectResult(new { message = "INVALID_OTP" });
                if (!(string.IsNullOrEmpty(loginId)) && !(string.IsNullOrEmpty(sessionOtp)))
                {
                    if (await _userService.ActivateAccount(loginId, sessionOtp))
                        return new OkObjectResult(new { message = "ACTIVE_SUCCESS" });

                    return new BadRequestObjectResult(new { message = "INVALID_USER" });
                }
                return new BadRequestObjectResult(new { message = "INVALID_OTP" });
            }
            catch (Exception e)
            {
                string eId = await _exceptionService.LogExceptionToDB(e);
                return new BadRequestObjectResult(new { message = "EXCEPTION", id= eId });
            }
        }

        [HttpGet]
        [Route("{loginId}/verify")]
        public async Task<IActionResult> SendVerificationMail(string loginId)
        {
            try
            {
                if (!string.IsNullOrEmpty(loginId))
                {                
                    string otpToken = await _userService.SendVerificationMail(loginId);

                    if (otpToken == "")
                        return StatusCode(StatusCodes.Status500InternalServerError,new { message = "EMAIL_SENT_FAIL" });
                    else if(otpToken == "IsActive")
                        return new BadRequestObjectResult(new { message = "ALREADY_ACTIVE" });
                    else if(otpToken == "UserNil")
                        return new BadRequestObjectResult(new { message = "INVALID_USER" });
                    else
                        return new OkObjectResult(new { message = "OTP_SENT", token=otpToken });                       
                }
                return new BadRequestObjectResult(new { message = "INVALID_USER" });
            }
            catch (Exception e)
            {
                string eId = await _exceptionService.LogExceptionToDB(e);
                return new BadRequestObjectResult(new { message = "EXCEPTION", id= eId });
            }
        }

        [HttpGet]
        [Route("{loginId}/forgot")]
        public async Task<IActionResult> ForgotPassword(string loginId)
        {
            try
            {
                if (!(string.IsNullOrEmpty(loginId)))
                {
                    if (await _userService.ResetPassword(loginId))                    
                        return new OkObjectResult(new {message = "RESET_SUCCESS" });

                    return new NotFoundObjectResult(new { message = "ACC_INACTIVE_OR_NOUSER_FOUND" });                    
                }
                else
                {
                    return new BadRequestObjectResult(new { message = "INVALID_USERNAME" });
                }
            }
            catch (Exception e)
            {
                string eId = await _exceptionService.LogExceptionToDB(e);
                return new BadRequestObjectResult(new { message = "EXCEPTION", id= eId });
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "AuthenticatedUser")]
        [HttpPut]
        [Route("{loginId}/change")]
        public async Task<IActionResult> ChangePassword([FromBody]ForgotPasswordModel password,string loginId)
        {            
            try
            {
                password = await _exCommService.PreventXSS(password);
                string sessionUser;
                sessionUser = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!string.Equals(loginId, sessionUser))
                {
                    return new ConflictObjectResult(new { message="SESSION_USER_CONFLICT"});
                }
                else if (ModelState.IsValid)
                {                                                
                    if (await _userService.ChangePassword(password,loginId))
                        return new OkObjectResult(new { message = "PASSWORD_CHANGED" });
                    else if(!(string.Equals(password.NewPassword,password.ConfirmNewPassword)))
                        return new BadRequestObjectResult(new { message = "PASS_CNFPASS_DIFF" });
                    else 
                        return new BadRequestObjectResult(new { message = "OLDPASS_WRONG" });
                }
                else
                {
                    return new BadRequestObjectResult(new { message = "INVALID" });

                }
            }
            catch(Exception e)
            {
                string eId = await _exceptionService.LogExceptionToDB(e);
                return new BadRequestObjectResult(new { message = "EXCEPTION", id= eId });
            }            
        }
        
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "AuthenticatedUser")]
        [HttpGet]
        [Route("users/all")]
        public async Task<IActionResult> GetAllUsers()
        {            
            try
            {
                List<UserModel> users=await _userService.GetAllUsers();
                if(users.Count>0)
                    return new OkObjectResult(new { users = users });
                return new NotFoundObjectResult(new { message="NO_USERS"});
            }
            catch(Exception e)
            {
                string eId = await _exceptionService.LogExceptionToDB(e);
                return new BadRequestObjectResult(new { message = "EXCEPTION", id= eId });
            }            
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "AuthenticatedUser")]
        [HttpGet]
        [Route("{loginId}/details")]
        public async Task<IActionResult> GetUserDetails(string loginId)
        {
            try
            {                
                if (!string.IsNullOrEmpty(loginId))
                {
                    UserModel user = await _userService.GetCurrentUserDetails(loginId);
                    if (user is null)
                        return new NotFoundObjectResult(new { message="USER_NOT_FOUND" });
                    return new OkObjectResult(new { user = user });
                }                                
                return new UnauthorizedObjectResult(new { message = "INVALID_USERNAME" });
            }
            catch (Exception e)
            {
                string eId = await _exceptionService.LogExceptionToDB(e);
                return new BadRequestObjectResult(new { message = "EXCEPTION", id= eId });
            }
        }       
        
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "AuthenticatedUser")]
        [HttpGet]
        [Route("user/search/{loginIdPattern}")]
        public async Task<IActionResult> SearchUser(string loginIdPattern)
        {
            try
            {                
                if (!string.IsNullOrEmpty(loginIdPattern))
                {
                    List<UserModel> users = await _userService.SearchUsers(loginIdPattern);
                    if (users.Count >0)
                            return new OkObjectResult(new { users = users });
                    return new NotFoundObjectResult(new { message="No Users found with search pattern : "+loginIdPattern });
                    
                }                                
                return new BadRequestObjectResult(new { message = "Search pattern was empty" });
            }
            catch (Exception e)
            {
                string eId = await _exceptionService.LogExceptionToDB(e);
                return new BadRequestObjectResult(new { message = "EXCEPTION", id= eId });
            }
        }

////////////////////////////////////////////////////////////////Actions For Tweets///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "AuthenticatedUser")]
        [HttpGet]
        [Route("all")]
        public async Task<IActionResult> GetAllTweets()
        {
            try
            {
                List<TweetModel> tweets = await _tweetService.GetAllTweets();
                if (tweets.Count > 0)
                    return new OkObjectResult(new { tweets = tweets });
                return new NotFoundObjectResult(new { message = "NO_TWEETS" });
            }
            catch (Exception e)
            {
                string eId = await _exceptionService.LogExceptionToDB(e);
                return new BadRequestObjectResult(new { message = "EXCEPTION", id= eId });
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "AuthenticatedUser")]
        [HttpGet]
        [Route("{loginId}")]
        public async Task<IActionResult> GetTweetsByUser(string loginId)
        {
            try
            {               
                if (!string.IsNullOrEmpty(loginId))
                {
                    List<TweetModel> tweets = await _tweetService.GetTweetsByLoginId(loginId);
                    if (tweets.Count>0)
                        return new OkObjectResult(new { tweets123 = tweets });
                    return new NotFoundObjectResult(new { message = "NO_TWEETS" });
                }
                return new BadRequestObjectResult(new { message = "INVALID_LOGIN_ID" });
            }
            catch (Exception e)
            {
                string eId = await _exceptionService.LogExceptionToDB(e);
                return new BadRequestObjectResult(new { message = "EXCEPTION", id= eId });
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "AuthenticatedUser")]
        [HttpPost]
        [Route("{loginId}/add")]
        public async Task<IActionResult> CreateTweet([FromBody] TweetModel tweet, string loginId)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    tweet = await _exCommService.PreventXSS(tweet);

                    string sessionUser = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
                    if (string.Equals(sessionUser, tweet.LoginId) && string.Equals(loginId, tweet.LoginId))
                    {
                        if (await _tweetService.CreateTweet(tweet))
                            return new OkObjectResult(new { message = "TWEET_SUCCESS" });

                        return new BadRequestObjectResult(new { message = "ALREADY_EXISTS" });
                    }
                    return new ConflictObjectResult(new { message = "SESSION_USER_CONFLICT" });
                }
                return new BadRequestObjectResult(new { message = "VALIDATION_FAIL" });
            }
            catch (Exception e)
            {
                string eId = await _exceptionService.LogExceptionToDB(e);
                return new BadRequestObjectResult(new { message = "EXCEPTION", id= eId });
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "AuthenticatedUser")]
        [HttpPut]
        [Route("{loginId}/update/{tweetId}")]
        public async Task<IActionResult> UpdateTweet([FromBody]TweetModel tweet,string loginId,string tweetId)
        {
            try
            {
                if (!(string.IsNullOrEmpty(loginId)) && !(string.IsNullOrEmpty(tweetId)) && (string.Equals(tweetId,tweet.Id)))
                {
                    tweet = await _exCommService.PreventXSS(tweet);

                    string sessionUser = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
                    if (string.Equals(sessionUser, loginId) && string.Equals(tweet.LoginId,loginId))
                    {
                        if (await _tweetService.UpdateTweet(tweet))
                            return new OkObjectResult(new { message = "Tweet updated successfully" });

                        return new BadRequestObjectResult(new { message = "No tweet exist by specified Id : "+tweetId });
                    }
                    return new ConflictObjectResult(new { message = "Conflict on session user. Cannot Modify tweet for users other than session user" });
                }
                return new BadRequestObjectResult(new { message = "Invalid login Id or Tweet Id" });
            }
            catch (Exception e)
            {
                string eId = await _exceptionService.LogExceptionToDB(e);
                return new BadRequestObjectResult(new { message = "EXCEPTION", id= eId });
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "AuthenticatedUser")]
        [HttpDelete]
        [Route("{loginId}/delete/{tweetId}")]
        public async Task<IActionResult> DeleteTweet(string loginId, string tweetId)
        {
            try
            {
                if (!(string.IsNullOrEmpty(loginId)) && !(string.IsNullOrEmpty(tweetId)))
                {
                    string sessionUser = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
                    if (string.Equals(sessionUser, loginId))
                    {
                        if (await _tweetService.DeleteTweet(tweetId))
                            return new OkObjectResult(new { message = "Tweet deleted successfully" });

                        return new BadRequestObjectResult(new { message = "No tweet exist with specified Id : " + tweetId });
                    }
                    return new ConflictObjectResult(new { message = "Conflict on session user. Cannot Modify tweet for users other than session user" });
                }
                return new BadRequestObjectResult(new { message = "Invalid login Id or Tweet Id" });
            }
            catch (Exception e)
            {
                string eId = await _exceptionService.LogExceptionToDB(e);
                return new BadRequestObjectResult(new { message = "EXCEPTION", id= eId });
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "AuthenticatedUser")]
        [HttpPut]
        [Route("{loginId}/like/{tweetId}")]
        public async Task<IActionResult> LikeTweet(string loginId, string tweetId)
        {
            try
            {
                if (!(string.IsNullOrEmpty(loginId)) || !(string.IsNullOrEmpty(tweetId)))
                {
                    string sessionUser = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
                    if (string.Equals(sessionUser, loginId))
                    {
                        if (await _tweetService.LikeTweet(tweetId,loginId))
                            return new OkObjectResult(new { message = "LIKE_SUCCESS" });

                        return new BadRequestObjectResult(new { message = "NO_UNLIKED_TWEETFOUND"});
                    }
                    return new ConflictObjectResult(new { message = "SESSION_USER_CONFILCT" });
                    
                }
                return new BadRequestObjectResult(new { message = "INVALID_PARAMS" });
            }
            catch (Exception e)
            {
                string eId = await _exceptionService.LogExceptionToDB(e);
                return new BadRequestObjectResult(new { message = "EXCEPTION", id= eId });
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "AuthenticatedUser")]
        [HttpPost]
        [Route("{loginId}/reply/{tweetId}")]
        public async Task<IActionResult> ReplyToTweet([FromBody]Reply reply,string loginId, string tweetId)
        {
            try
            {
                if (!(string.IsNullOrEmpty(loginId)) || !(string.IsNullOrEmpty(tweetId)))
                {
                    reply = await _exCommService.PreventXSS(reply);

                    string sessionUser = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
                    if (string.Equals(sessionUser, loginId) && (string.Equals(loginId, reply.LoginId)))
                    {
                        if (await _tweetService.ReplyToTweet(tweetId, reply, loginId))
                            return new OkObjectResult(new { message = "REPLY_SUCCESS"});

                        return new BadRequestObjectResult(new { message = "NO_TWEET_FOUND" });
                    }
                    return new ConflictObjectResult(new { message = "SESSION_USER_CONFILCT" });                    
                }
                return new BadRequestObjectResult(new { message = "INVALID_PARAMS" });
            }
            catch (Exception e)
            {
                string eId = await _exceptionService.LogExceptionToDB(e);
                return new BadRequestObjectResult(new { message = "EXCEPTION", id= eId });
            }
        }
    }
}
