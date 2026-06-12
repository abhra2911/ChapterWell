using System.ComponentModel.DataAnnotations;

namespace Lib_Mgmt.Models
{
    // Bound by EditLibrarianProfile / EditMemberProfile POST actions.
    // Maps 1:1 to the profile columns we'll persist in Oracle
    // (LIBRARIANS / MEMBERS tables).
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
