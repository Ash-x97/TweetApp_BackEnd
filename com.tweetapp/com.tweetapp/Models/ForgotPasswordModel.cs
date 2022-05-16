using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace com.tweetapp.Models
{
    public class ForgotPasswordModel
    {
        [Required(ErrorMessage = " Old password id required")]
        [StringLength(18, ErrorMessage = "Password must be between 6 to 18 characters", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string OldPassword { get; set; }

        [Required(ErrorMessage = "New password id required")]
        [StringLength(18, ErrorMessage = "Password must be between 6 to 18 characters", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Confirm new password id required")]
        [StringLength(18, ErrorMessage = "Password must be between 6 to 18 characters", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Compare("NewPassword")]
        public string ConfirmNewPassword { get; set; }
    }
}
