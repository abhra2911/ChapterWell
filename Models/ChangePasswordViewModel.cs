using System.ComponentModel.DataAnnotations;

namespace Lib_Mgmt.Models
{
    // Bound by ChangeLibrarianPassword / ChangeMemberPassword POST actions.
    // When wired to Oracle, CurrentPassword is verified against the stored
    // hash; NewPassword is re-hashed before UPDATE.
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Current password is required.")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "New password is required.")]
        [StringLength(100, MinimumLength = 6,
            ErrorMessage = "Password must be at least 6 characters.")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Please confirm the new password.")]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword),
            ErrorMessage = "The new password and confirmation do not match.")]
        public string ConfirmPassword { get; set; }
    }
}
