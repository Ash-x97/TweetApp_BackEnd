using com.tweetapp.Interfaces;
using com.tweetapp.Models;
using Microsoft.AspNet.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace com.tweetapp.Services
{
    public class ExternalCommunicationService: IExternalCommunicationService
    {
        private readonly ILogger<ExternalCommunicationService> _logger;
        private readonly IConfiguration _config;
        private readonly IUserRepo _userRepo;
        private readonly ITweetRepo _tweetRepo;

        public ExternalCommunicationService(ILogger<ExternalCommunicationService> logger, IConfiguration config, IExceptionService exceptionService, IUserRepo userRepo, ITweetRepo tweetRepo)
        {
            _logger = logger;
            _config = config;
            _userRepo = userRepo;
            _tweetRepo = tweetRepo;
        }

        public Task<T> PreventXSS<T>(T obj) where T : class
        {
            return Task.Run(() =>
            {
                try
                {
                    var type = typeof(T);
                    if (type == typeof(UserModel))
                    {
                        UserModel user = obj as UserModel;
                        user.FirstName = HttpUtility.HtmlEncode(user.FirstName);
                        user.LastName = HttpUtility.HtmlEncode(user.LastName);
                        user.LoginId = HttpUtility.HtmlEncode(user.LoginId);
                        user.Email = HttpUtility.HtmlEncode(user.Email);
                        user.ContactNumber = HttpUtility.HtmlEncode(user.ContactNumber);
                        user.Password = HttpUtility.HtmlEncode(user.Password);
                        user.ConfirmPassword = HttpUtility.HtmlEncode(user.ConfirmPassword);

                        return user as T;
                    }
                    else if (type == typeof(ForgotPasswordModel))
                    {
                        ForgotPasswordModel password = obj as ForgotPasswordModel;
                        password.OldPassword = HttpUtility.HtmlEncode(password.OldPassword);
                        password.NewPassword = HttpUtility.HtmlEncode(password.NewPassword);
                        password.ConfirmNewPassword = HttpUtility.HtmlEncode(password.ConfirmNewPassword);

                        return password as T;
                    }
                    else if (type == typeof(LoginModel))
                    {
                        LoginModel login = obj as LoginModel;
                        login.LoginId = HttpUtility.HtmlEncode(login.LoginId);
                        login.Password = HttpUtility.HtmlEncode(login.Password);

                        return login as T;
                    }
                    else if (type == typeof(TweetModel))
                    {
                        TweetModel tweet = obj as TweetModel;
                        tweet.LoginId = HttpUtility.HtmlEncode(tweet.LoginId);
                        tweet.Text = HttpUtility.HtmlEncode(tweet.Text);
                        tweet.CreatedTime = Convert.ToDateTime(HttpUtility.HtmlEncode(tweet.CreatedTime));                        

                        if (!(tweet.Replies is null))
                        {
                            foreach (var reply in tweet.Replies)
                            {
                                var tempTags = new List<Tag>();
                                foreach (var tag in reply.Tags)
                                {
                                    Tag tempTag = new Tag()
                                    {
                                        TaggedUser = HttpUtility.HtmlEncode(tag.TaggedUser),
                                        IsNotified = Convert.ToBoolean(HttpUtility.HtmlEncode(tag.IsNotified))
                                    };
                                    tempTags.Add(tempTag);
                                }
                                reply.Tags = tempTags;
                            }
                        }                                                   
                        return tweet as T;
                    }
                    else if (type == typeof(Reply))
                    {
                        Reply reply = obj as Reply;
                        reply.LoginId = HttpUtility.HtmlEncode(reply.LoginId);
                        reply.ReplyText = HttpUtility.HtmlEncode(reply.ReplyText);
                        reply.CreatedTime = Convert.ToDateTime(HttpUtility.HtmlEncode(reply.CreatedTime));

                        if (!(reply.Tags is null))
                        {
                            var tempTags = new List<Tag>();
                            foreach (var tag in reply.Tags)
                            {
                                Tag tempTag = new Tag()
                                {
                                    TaggedUser = HttpUtility.HtmlEncode(tag.TaggedUser),
                                    IsNotified = Convert.ToBoolean(HttpUtility.HtmlEncode(tag.IsNotified))
                                };
                                tempTags.Add(tempTag);
                            }
                            reply.Tags = tempTags;
                        }

                        return reply as T;
                    }
                    return null;
                }
                catch (Exception e)
                {
                    if (!e.Data.Contains("CurrentUser") && !e.Data.Contains("CurrentApplication"))
                    {
                        _logger.LogError("Exception : " + e.Message);
                        e.Data.Add("CurrentMethod", MethodBase.GetCurrentMethod().ReflectedType.Name + "." + MethodBase.GetCurrentMethod().Name);
                        e.Data.Add("CurrentApplication", Assembly.GetExecutingAssembly().GetName().Name);
                    }
                    throw e;
                }
            });
            
        }

        public async Task<bool> SendMail(MailModel mailObj)
        {
            try
            {                
                using (MailMessage mail= new MailMessage())
                {
                    mail.From = new MailAddress(mailObj.FromMailId);
                    mailObj.ToMailIds.ForEach(x =>
                    {
                        mail.To.Add(x);
                    });
                    mail.Subject = mailObj.Subject;
                    mail.Body = mailObj.Body;
                    mail.IsBodyHtml = mailObj.IsBodyHtml;
                    mailObj.Attachments.ForEach(x =>
                    {
                        mail.Attachments.Add(new Attachment(x));
                    });
                    using (SmtpClient smtp =new SmtpClient("smtp.gmail.com", 587))
                    {
                        smtp.Credentials = new System.Net.NetworkCredential(mailObj.FromMailId, mailObj.FromMailIdPassword);
                        smtp.EnableSsl = true;
                        await smtp.SendMailAsync(mail);
                    }
                }
                return true;
            }
            catch(Exception e)
            {
                if (!e.Data.Contains("CurrentUser") && !e.Data.Contains("CurrentApplication"))
                {
                    _logger.LogError("Exception : " + e.Message);
                    e.Data.Add("CurrentMethod", MethodBase.GetCurrentMethod().ReflectedType.Name + "." + MethodBase.GetCurrentMethod().Name);
                    e.Data.Add("CurrentApplication", Assembly.GetExecutingAssembly().GetName().Name);
                }
                throw e;
            }
        }

        public async Task<List<Tag>> NotifyTaggedUsers(string loginId, List<Tag> tags, string tweetId,string item)
        {
            try
            {
                TweetModel tweet = await _tweetRepo.GetTweetById(tweetId);
                foreach (Tag tag in tags)
                {
                    if (tag.IsNotified)
                        continue;
                    UserModel toNotifyUser = await _userRepo.GetUserByLoginIdAsync(tag.TaggedUser);
                    if (toNotifyUser is null)
                        continue;
                    if (toNotifyUser.Notifications is null)
                        toNotifyUser.Notifications = new List<Notification>();

                    Notification notification = new Notification()
                    {
                        Message = loginId + " tagged you in a tweet thread by :" + tweet.LoginId,
                        IsSeen = false
                    };
                    toNotifyUser.Notifications.Add(notification);
                    await _userRepo.UpdateUserAsync(toNotifyUser);

                    MailModel mail = new MailModel(_config);
                    mail.Subject = "Tweet app Tagg Notifier Mail";
                    mail.Body = GetNotificationMailBody(notification.Message);
                    mail.ToMailIds.Add(toNotifyUser.Email);

                    if (await SendMail(mail))
                        _logger.LogInformation("Notification mailer send to user with loginId : " + toNotifyUser.LoginId);
                    else
                        _logger.LogInformation("Notification mailer sending to user with loginId : " + toNotifyUser.LoginId + " failed");

                    tag.IsNotified = true;
                }
                return tags;
            }
            catch (Exception e)
            {
                if (!e.Data.Contains("CurrentUser") && !e.Data.Contains("CurrentApplication"))
                {
                    _logger.LogError("Exception : " + e.Message);
                    e.Data.Add("CurrentMethod", MethodBase.GetCurrentMethod().ReflectedType.Name + "." + MethodBase.GetCurrentMethod().Name);
                    e.Data.Add("CurrentApplication", Assembly.GetExecutingAssembly().GetName().Name);
                }
                throw e;
            }
        }

        public async Task NotifyLikedUser(string likeBy, string likeTo)
        {
            try
            {
                UserModel toNotifyUser = await _userRepo.GetUserByLoginIdAsync(likeTo);
                if(!(toNotifyUser is null))
                {
                    if (toNotifyUser.Notifications is null)
                        toNotifyUser.Notifications = new List<Notification>();

                    Notification notification = new Notification()
                    {
                        Message = likeBy + " liked your Tweet",
                        IsSeen = false
                    };
                    toNotifyUser.Notifications.Add(notification);
                    await _userRepo.UpdateUserAsync(toNotifyUser);

                    MailModel mail = new MailModel(_config);
                    mail.Subject = "Tweet app Like Notifier Mail";
                    mail.Body = GetNotificationMailBody(notification.Message);
                    mail.ToMailIds.Add(toNotifyUser.Email);

                    if (await SendMail(mail))
                        _logger.LogInformation("Notification mailer send to user with loginId : " + toNotifyUser.LoginId);
                    else
                        _logger.LogInformation("Notification mailer sending to user with loginId : " + toNotifyUser.LoginId + " failed");
                }               
            }
            catch (Exception e)
            {
                if (!e.Data.Contains("CurrentUser") && !e.Data.Contains("CurrentApplication"))
                {
                    _logger.LogError("Exception : " + e.Message);
                    e.Data.Add("CurrentMethod", MethodBase.GetCurrentMethod().ReflectedType.Name + "." + MethodBase.GetCurrentMethod().Name);
                    e.Data.Add("CurrentApplication", Assembly.GetExecutingAssembly().GetName().Name);
                }
                throw e;
            }
        }

        public async Task NotifyReplyToUser(string replier, string repliedTo)
        {
            try
            {
                UserModel toNotifyUser = await _userRepo.GetUserByLoginIdAsync(repliedTo);
                if (!(toNotifyUser is null))
                {
                    if (toNotifyUser.Notifications is null)
                        toNotifyUser.Notifications = new List<Notification>();

                    Notification notification = new Notification()
                    {
                        Message = replier + " replied to your Tweet",
                        IsSeen = false
                    };
                    toNotifyUser.Notifications.Add(notification);
                    await _userRepo.UpdateUserAsync(toNotifyUser);

                    MailModel mail = new MailModel(_config)
                    {
                        Subject = "Tweet app Reply Notifier Mail",
                        Body = GetNotificationMailBody(notification.Message)
                    };
                    mail.ToMailIds.Add(toNotifyUser.Email);

                    if (await SendMail(mail))
                        _logger.LogInformation("Notification mailer send to user with loginId : " + toNotifyUser.LoginId);
                    else
                        _logger.LogInformation("Notification mailer sending to user with loginId : " + toNotifyUser.LoginId + " failed");
                }
            }
            catch (Exception e)
            {
                if (!e.Data.Contains("CurrentUser") && !e.Data.Contains("CurrentApplication"))
                {
                    _logger.LogError("Exception : " + e.Message);
                    e.Data.Add("CurrentMethod", MethodBase.GetCurrentMethod().ReflectedType.Name + "." + MethodBase.GetCurrentMethod().Name);
                    e.Data.Add("CurrentApplication", Assembly.GetExecutingAssembly().GetName().Name);
                }
                throw e;
            }
        }

        public string GetVerificationMailBody(OTP otp)
        {
            return string.Format(@"<div style='text-align:center;'>
                                    <h1>Welcome to TweetApp</h1>
                                    <h3>Please find OTP generated for you below.</h3><br/>
                                    <h2>{0}</h2><br/>
                                    <p><strong>OTP Validity: 5 minutes</strond></p>
                                   </div>",otp.OTPValue);
        }
        
        public string GetResetPasswordMailBody(string password)
        {
            return string.Format(@"<div style='text-align:center;'>
                                    <h1>Hi again, Greetings from TweetApp</h1>
                                    <h3>Password has been resetted and new password generated by system successfully. Please find the new password below :</h3><br/>
                                    <h2>{0}</h2><br/>
                                    <p><strong>Please change your password to a new and strong password; Kepp your passwords safe, Thank you.</strond></p>
                                   </div>",password);
        }                

        public Task<OTP> GenerateOTP()
        {
            return Task.Run(() =>
            {
                string sOTP = String.Empty;
                string sTempChars = String.Empty;
                int iOTPLength = 6;
                string[] allowedCharachters = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };

                Random rand = new Random();

                for (int i = 0; i < iOTPLength; i++)
                {
                    //int p = rand.Next(0, allowedCharachters.Length);
                    sTempChars = allowedCharachters[rand.Next(0, allowedCharachters.Length)];
                    sOTP += sTempChars;
                    _logger.LogInformation("OTP Generated");
                }

                OTP otp = new OTP();
                otp.OTPValue = sOTP;
                otp.Token = GenerateJWTForOTP(otp.OTPValue).Result;

                return otp;
            });
           
        }

        private Task<string> GenerateJWTForOTP(string otp)
        {
            return Task.Run(() =>
            {
                try
                {
                    SymmetricSecurityKey symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWT:Key"]));
                    var credentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);
                    var claims = new List<Claim>
                     {
                        new Claim(ClaimTypes.Sid,otp),
                        new Claim(ClaimTypes.Expiration,DateTime.Now.AddMinutes(5).ToString()),
                        new Claim(ClaimTypes.Role,"InactiveUser"),
                    };
                    var identity = new ClaimsIdentity(claims, DefaultAuthenticationTypes.ApplicationCookie);
                    var claimsPrincipal = new ClaimsPrincipal(identity);
                    Thread.CurrentPrincipal = claimsPrincipal;
                    var token = new JwtSecurityToken
                        (
                            issuer: _config["JWT:Issuer"].ToString(),
                            audience: _config["JWT:Audience"],
                            claims: claims,
                            expires: DateTime.Now.AddMinutes(5),
                            signingCredentials: credentials
                        );
                    _logger.LogInformation("Temporary JWtToken created for generated OTP : " + claims[1].Value + " exipres on : " + token.ValidTo);
                    return new JwtSecurityTokenHandler().WriteToken(token);
                }
                catch (Exception e)
                {
                    if (!e.Data.Contains("CurrentUser") && !e.Data.Contains("CurrentApplication"))
                    {
                        _logger.LogError("Exception : " + e.Message);
                        e.Data.Add("CurrentMethod", MethodBase.GetCurrentMethod().ReflectedType.Name + "." + MethodBase.GetCurrentMethod().Name);
                        e.Data.Add("CurrentApplication", Assembly.GetExecutingAssembly().GetName().Name);
                    }
                    throw e;
                }
            });
        }

        public Task<string> GenerateRandomPassword()
        {
            return Task.Run(() =>
            {
                try
                {
                    StringBuilder builder = new StringBuilder();
                    builder.Append(RandomString(4, true).Result);
                    builder.Append(GenerateOTP().Result.OTPValue);
                    builder.Append(RandomString(2, false).Result);

                    var result = builder.ToString();
                    return result;
                }
                catch (Exception e)
                {
                    if (!e.Data.Contains("CurrentUser") && !e.Data.Contains("CurrentApplication"))
                    {
                        _logger.LogError("Exception : " + e.Message);
                        e.Data.Add("CurrentMethod", MethodBase.GetCurrentMethod().ReflectedType.Name + "." + MethodBase.GetCurrentMethod().Name);
                        e.Data.Add("CurrentApplication", Assembly.GetExecutingAssembly().GetName().Name);
                    }
                    throw e;
                }
            });           
        }

        private Task<string> RandomString(int size, bool lowerCase)
        {
            return Task.Run(() =>
            {
                StringBuilder builder = new StringBuilder();
                Random random = new Random();
                char ch;
                for (int i = 0; i < size; i++)
                {
                    ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                    builder.Append(ch);
                }
                if (lowerCase)
                    return builder.ToString().ToLower();
                return builder.ToString();
            });            
        }

        public string GetNotificationMailBody(string message)
        {
            return string.Format(@"<div style='text-align:center;'>
                                    <h1>Hi, Greetings from TweetApp</h1>
                                    <h3>{0}</h3><br/>
                                    <p><strong>Please go to your profile and see Notifications area to get all updates, Thank you for using the app.</strond></p>
                                   </div>", message);
        }      
    }
}
