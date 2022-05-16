using com.tweetapp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace com.tweetapp.Interfaces
{
    public interface IExternalCommunicationService
    {
        Task<T> PreventXSS<T>(T obj) where T : class;
        Task<bool> SendMail(MailModel mailObj);
        Task<List<Tag>> NotifyTaggedUsers(string loginId, List<Tag> tags, string tweetId,string item);
        Task NotifyLikedUser(string likeBy,string likeTo);
        Task NotifyReplyToUser(string replier,string repliedTo);
        string GetVerificationMailBody(OTP otp);
        Task<OTP> GenerateOTP();
        string GetResetPasswordMailBody(string password);
        Task<string> GenerateRandomPassword();
    }
}
