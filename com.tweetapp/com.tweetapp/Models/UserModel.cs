using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace com.tweetapp.Models
{
    public class UserModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Required(ErrorMessage ="First name required")] 
        public string FirstName { get; set; }

        [Required(ErrorMessage ="Last name required")]
        public string LastName { get; set; }

        [Required(ErrorMessage ="Email address required")]
        [EmailAddress(ErrorMessage ="invalid email address")]
        public string Email { get; set; }

        [Required(ErrorMessage ="Login id required")]
        [StringLength(10,ErrorMessage ="Login id must be between 5 to 10 characters",MinimumLength =5)]
        public string LoginId { get; set; }

        [Required]
        [Phone]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "PhoneNumber Length must be 10")]
        public string ContactNumber { get; set; }

        [Required(ErrorMessage = "Password id required")]
        [StringLength(18, ErrorMessage = "password must be between 6 to 18 characters", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm password id required")]
        [DataType(DataType.Password)]
        [Compare("Password")]
        public string ConfirmPassword { get; set; }     
        
        public bool IsMailConfirmed { get; set; }

        //[DataType(DataType.Upload)]
        public byte[] Avatar { get; set; }

        public OTP Otp { get; set; }
        
        public List<Notification> Notifications { get; set; }
    }

    public partial class OTP
    {
        public string OTPValue { get; set; }
        public string Token { get; set; }

    }

    public partial class Notification
    {
        public string Message { get; set; }
        public bool IsSeen { get; set; }
    }

    public class OTPV
    {
        [Required(ErrorMessage ="OTP required")]
        public string otpValue { get; set; }
    }
}
