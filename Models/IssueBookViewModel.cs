using System;
using System.ComponentModel.DataAnnotations;

namespace Lib_Mgmt.Models
{
    /// <summary>Posted by the "Issue Book" modal on the librarian Borrowings tab.</summary>
    public class IssueBookViewModel
    {
        [Required(ErrorMessage = "Member ID is required.")]
        [StringLength(20)]
        [Display(Name = "Member ID")]
        public string MemberId { get; set; }

        [Required(ErrorMessage = "Please choose a book.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please choose a book.")]
        [Display(Name = "Book")]
        public int BookId { get; set; }

        [Required(ErrorMessage = "Due date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Due Date")]
        public DateTime DueDate { get; set; }
    }
}
