using com.tweetapp.Interfaces;
using com.tweetapp.Models;
using Microsoft.AspNet.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace com.tweetapp.Services
{
    class UserService:IUserService
    {
        private readonly ILogger<UserService> _logger;
        private readonly IConfiguration _config;
        private readonly IUserRepo _userRepo;
        private readonly IExternalCommunicationService _exCommService;
        private UserModel _sessionUser;

        public UserService(ILogger<UserService> logger, IConfiguration config, IUserRepo userRepo, IExternalCommunicationService exCommService)
        {
            _logger = logger;
            _config =config;
            _userRepo = userRepo;
            _exCommService = exCommService;
        }

        public async Task<string> Login(string loginId,string password)
        {
            try
            {
                _sessionUser = new UserModel()
                {
                    LoginId = loginId,
                    Password = password
                };
                
                _logger.LogInformation("Login request recieved from : " + _sessionUser.LoginId);
                string sessionToken = "";
                if (await ValidateUser(_sessionUser))
                {
                    if (!IsActive(_sessionUser.LoginId).Result)
                    {
                        sessionToken = "Inactive";
                        return sessionToken;
                    }    
                    sessionToken = await GenerateJsonWebToken(_sessionUser);
                    return sessionToken;
                }
                _logger.LogError("Session Token not created");
                return sessionToken;
            }
            catch(Exception e)
            {
                if(!e.Data.Contains("CurrentUser") && !e.Data.Contains("CurrentApplication"))
                {
                    _logger.LogError("Exception : " + e.Message);
                    e.Data.Add("CurrentMethod",MethodBase.GetCurrentMethod().ReflectedType.Name);
                    e.Data.Add("CurrentApplication", Assembly.GetExecutingAssembly().GetName().Name);
                }                
                throw e;
            }
            
        }

        public async Task<string> Register(UserModel user)
        {
            try
            {
                _sessionUser = user;
                _logger.LogInformation("Registration request recieved from : " + _sessionUser.LoginId);
                bool isRegistered = false;
                if (!(_sessionUser is null) )
                {
                    if (!IfAlreadyExists(user).Result)
                    {
                        _sessionUser.IsMailConfirmed = false;
                        _sessionUser.Otp = null;
                        _sessionUser.Notifications = null;
                        MailModel mail =await GetVerificationMail();

                        if(await _exCommService.SendMail(mail))
                        {
                            _logger.LogInformation("Verification mailer send successfully to :" + _sessionUser.Email);
                            await _userRepo.CreateUserAsync(_sessionUser);
                            isRegistered = true;
                            _logger.LogInformation("User Registration Successfull. LoginId : " + _sessionUser.LoginId);
                            return _sessionUser.Otp.Token;
                        }
                        else
                        {
                            _logger.LogError("Verification mailer sending to : " + _sessionUser.Email + " failed");
                            return "";
                        }
                    }
                    else
                    {
                        _logger.LogInformation("User Registration Failed. User with login id : "+ _sessionUser.LoginId);
                        return "Already";
                    }                    
                }
                else
                {
                    _logger.LogInformation("User Registration Failed. Data null or invalid");
                    return "";
                }
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

        public async Task<bool> UpdateNotifications(string loginId, List<Notification> notifications)
        {
            try
            {
                UserModel userDetails = await _userRepo.GetUserByLoginIdAsync(loginId);
                userDetails.Notifications = notifications;
                await _userRepo.UpdateUserAsync(userDetails);
                UserModel updatedUserDetails = await _userRepo.GetUserByLoginIdAsync(loginId);

                bool isSuccess = false;
                for(int i=0;i< notifications.Count; i++)
                {
                    if (updatedUserDetails.Notifications[i].IsSeen == notifications[i].IsSeen)
                    {
                        isSuccess = true;
                        continue;
                    }
                    isSuccess = false;
                    break;
                }
                return isSuccess;                 
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

        public async Task<bool> ActivateAccount(string loginId, string otp)
        {
            try
            {
                _logger.LogInformation("Activate account request recieved from : " + loginId+" with otp : "+otp);

                if (otp.Length == 6)
                {
                    UserModel currentUser = await _userRepo.GetUserByLoginIdAsync(loginId);
                    if (currentUser is null)
                        return false;
                    
                    if (!(currentUser.Otp is null) && currentUser.Otp.OTPValue == otp && !currentUser.IsMailConfirmed)
                    {
                        currentUser.Otp = null;
                        currentUser.IsMailConfirmed = true;
                        await _userRepo.UpdateUserAsync(currentUser);
                        return true;
                    }                        
                    return false;
                }
                return false;
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

        public async Task<string> SendVerificationMail(string loginId)
        {
            try
            {
                _logger.LogInformation("Send account verification mailer request recieved from : " + loginId);
                _sessionUser = await _userRepo.GetUserByLoginIdAsync(loginId);
                if (!(_sessionUser is null))
                {
                    if (!_sessionUser.IsMailConfirmed)
                    {
                        MailModel mail = await GetVerificationMail();

                        if (await _exCommService.SendMail(mail))
                        {
                            _logger.LogInformation("Verification mailer send successfully to :" + _sessionUser.Email);

                            await _userRepo.UpdateUserAsync(_sessionUser);
                            return _sessionUser.Otp.Token;
                        }
                        else
                        {
                            _logger.LogError("Verification mailer sending to : " + _sessionUser.Email + " failed");
                            return "";
                        }
                    }                       
                    else
                    {
                        _logger.LogInformation("User account is already active : " + _sessionUser.LoginId);
                        return "IsActive";
                    }
                }
                else
                {
                    _logger.LogInformation("No User found with specified login id");
                    return "UserNil";
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

        public async Task<bool> ResetPassword(string loginId)
        {
            try
            {
                _logger.LogInformation("Send account verification mailer request recieved from : " + loginId);
                _sessionUser = await _userRepo.GetUserByLoginIdAsync(loginId);
                if (!(_sessionUser is null))
                {
                    if (_sessionUser.IsMailConfirmed)
                    {
                        MailModel mail = await GetResetPasswordMail();

                        if (await _exCommService.SendMail(mail))
                        {
                            _logger.LogInformation("Reset password mailer send successfully to :" + _sessionUser.Email);

                            await _userRepo.UpdateUserAsync(_sessionUser);
                            return true;
                        }
                        else
                        {
                            _logger.LogError("Reset password mailer sending to : " + _sessionUser.Email + " failed");
                            return false;
                        }
                    }
                    else
                    {
                        _logger.LogInformation("User account with LoginId : " + _sessionUser.LoginId+ " is not active");
                        return false;
                    }
                }
                else
                {
                    _logger.LogInformation("No User found with specified login id");
                    return false;
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

        public async Task<bool> ChangePassword(ForgotPasswordModel password, string loginId)
        {
            try
            {
                _logger.LogInformation("Change Password Request request recieved from : " + loginId);
                bool isUpdated = false;
                if (await CheckOldPassword(loginId, password.OldPassword))
                {
                    if (string.Equals(password.NewPassword, password.ConfirmNewPassword))
                    {
                        UserModel user =await _userRepo.GetUserByLoginIdAsync(loginId);
                        user.Password = password.NewPassword;
                        user.ConfirmPassword = password.ConfirmNewPassword;
                        await _userRepo.UpdateUserAsync(user);
                        isUpdated = true;
                        _logger.LogInformation("Successfully updated password for LoginId : " + loginId);
                        return isUpdated;
                    }
                    _logger.LogInformation("Passwor not updated - new password and confirm new password fields should be same");
                    return isUpdated;

                }
                _logger.LogInformation("Passwor not updated - Old password is incorrect");
                return isUpdated;
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

        public async Task<List<UserModel>> GetAllUsers()
        {
            try
            {
                List<UserModel> users = await _userRepo.GetAllUsersAsync();
                if (users is null || users.Count==0)
                {
                    _logger.LogInformation("No users in DB");
                    return new List<UserModel>();
                }
                _logger.LogInformation("Fetched "+users.Count+" users");
                foreach(UserModel user in users)
                {
                    user.Password = "";
                    user.ConfirmPassword = "";
                }
                return users;
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
        
        public async Task<UserModel> GetCurrentUserDetails(string loginId)
        {
            try
            {
                UserModel user = await _userRepo.GetUserByLoginIdAsync(loginId);
                if (user is null)
                {
                    _logger.LogInformation("No user found with loginId : " + loginId);
                    return user;
                }
                user.Password = "";
                user.ConfirmPassword = "";
                return user;
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
        
        public async Task<List<UserModel>> SearchUsers(string loginIdPattern)
        {
            try
            {
                List<UserModel> users = await _userRepo.GetUsersBysearchPatternAsync(loginIdPattern);
                if (users is null || users.Count == 0)
                    return new List<UserModel>();
                foreach (UserModel user in users)
                {
                    user.Password = "";
                    user.ConfirmPassword = "";
                }
                return users;
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

        private async Task<bool> ValidateUser(UserModel user)
        {
            try
            {
                bool isValid = false;
                if (String.IsNullOrEmpty(user.LoginId))
                {
                    _logger.LogError("Login attempt failed! Reason : Empty username");
                    return isValid;
                }
                UserModel currentUser = await _userRepo.GetUserByLoginIdAsync(user.LoginId);
                if (!(currentUser is null) && user.Password == currentUser.Password)
                {
                    _logger.LogInformation("User validation Successfull");
                    user.FirstName = currentUser.FirstName;
                    user.LastName = currentUser.LastName;
                    user.Email = currentUser.Email;
                    user.ContactNumber = currentUser.ContactNumber;
                    isValid = true;
                    return isValid;
                }
                if (currentUser is null)
                    _logger.LogError("Login attempt failed! Reason : No User Found with username : " + user.LoginId);
                else
                    _logger.LogError("Login attempt failed! Reason : Incorrect password : " + user.Password);
                return isValid;
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

        private Task<string> GenerateJsonWebToken(UserModel user)
        {
            
            return Task.Run(() =>
            {
                try
                {
                    SymmetricSecurityKey symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWT:Key"]));
                    var credentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);
                    var claims = new List<Claim>
                     {
                        new Claim(ClaimTypes.NameIdentifier,user.LoginId),
                        new Claim(ClaimTypes.Name,String.Join(" ",user.FirstName,user.LastName)),
                        new Claim(ClaimTypes.Email,user.Email),
                        new Claim(ClaimTypes.MobilePhone,user.ContactNumber),
                        new Claim(ClaimTypes.Role,"AuthenticatedUser"),
                    };
                    var identity = new ClaimsIdentity(claims, DefaultAuthenticationTypes.ApplicationCookie);
                    var claimsPrincipal = new ClaimsPrincipal(identity);
                    Thread.CurrentPrincipal = claimsPrincipal;
                    var token = new JwtSecurityToken
                        (
                            issuer: _config["JWT:Issuer"].ToString(),
                            audience: _config["JWT:Audience"],
                            claims: claims,
                            expires: DateTime.Now.AddMinutes(15),
                            signingCredentials: credentials
                        );
                    _logger.LogInformation("JWtTOken created for the user : " + claims[1] + " exipres on : " + token.ValidTo);
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

        private async Task<bool> IfAlreadyExists(UserModel user)
        {
            try
            {
                UserModel existingUserwithSameLoginId=await _userRepo.GetUserByLoginIdAsync(user.LoginId);
                UserModel existingUserwithSameEmail=await _userRepo.GetUserByEmailIdAsync(user.Email);
                if (existingUserwithSameLoginId is null && existingUserwithSameEmail is null)
                    return false;               
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

        private async Task<bool> CheckOldPassword(string loginId, string password)
        {
            try
            {
                UserModel existingUser = await _userRepo.GetUserByLoginIdAsync(loginId);
                if (existingUser is null || (!string.Equals(password, existingUser.Password)))
                    return false;
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

        private async Task<MailModel> GetVerificationMail()
        {
            try
            {
                OTP otp = await GenerateOtp();
                _sessionUser.Otp = otp;
                _sessionUser.Otp.OTPValue = otp.OTPValue;
                _sessionUser.Otp.Token = otp.Token;

                var mail = new MailModel(_config);
                mail.Subject = "OTP - TweetApp Registration confirm mailer.";
                mail.Body = _exCommService.GetVerificationMailBody(_sessionUser.Otp);
                mail.ToMailIds.Add(_sessionUser.Email);

                return mail;
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
        
        private async Task<MailModel> GetResetPasswordMail()
        {
            try
            {
                string password = await GenerateNewPassword();
                _sessionUser.Password = password;
                _sessionUser.ConfirmPassword = password;

                var mail = new MailModel(_config);
                mail.Subject = "IMPORTANT! - TweetApp reset password mailer.";
                mail.Body = _exCommService.GetResetPasswordMailBody(_sessionUser.Password);
                mail.ToMailIds.Add(_sessionUser.Email);

                return mail;
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

        private async Task<OTP> GenerateOtp()
        {
            try
            {
                OTP otp = await _exCommService.GenerateOTP();
                return otp;
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

        private async Task<string> GenerateNewPassword()
        {
            try
            {
                string password =await _exCommService.GenerateRandomPassword();
                return password;
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

        private async Task<bool> IsActive(string loginId)
        {
            try
            {
                UserModel loginUser =await _userRepo.GetUserByLoginIdAsync(loginId);
                if (loginUser is null)
                    return false;
                if (loginUser.IsMailConfirmed)
                    return true;
                return false;
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
        
    }
}




