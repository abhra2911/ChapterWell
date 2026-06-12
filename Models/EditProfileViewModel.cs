using System.ComponentModel.DataAnnotations;

namespace Lib_Mgmt.Models
{
    // for both librarian and member
    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100)]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(150)]
        public string Email { get; set; }

        [StringLength(20)]
        public string Phone { get; set; }
    }
}
