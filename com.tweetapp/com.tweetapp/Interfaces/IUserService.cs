using com.tweetapp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace com.tweetapp.Interfaces
{
    public interface IUserService
    {
        Task<string> Login(string loginId, string password);
        Task<string> Register(UserModel user);
        Task<bool> UpdateNotifications(string loginId, List<Notification> notifications);
        Task<bool> ActivateAccount(string loginId, string otp);
        Task<string> SendVerificationMail(string loginId);
        Task<bool> ResetPassword(string loginId);
        Task<bool> ChangePassword(ForgotPasswordModel password, string loginId);
        Task<List<UserModel>> GetAllUsers();
        Task<List<UserModel>> SearchUsers(string loginIdPattern);
        Task<UserModel> GetCurrentUserDetails(string loginId);
    }
}
