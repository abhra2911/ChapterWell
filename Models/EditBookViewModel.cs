using System.ComponentModel.DataAnnotations;

namespace Lib_Mgmt.Models
{
    public class EditBookViewModel
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(200)]
        public string Title { get; set; }

        [Required(ErrorMessage = "Author is required.")]
        [StringLength(150)]
        public string Author { get; set; }

        [StringLength(20)]
        [Display(Name = "ISBN")]
        public string Isbn { get; set; }

        [Required(ErrorMessage = "Genre is required.")]
        [StringLength(50)]
        public string Genre { get; set; }

        [Required(ErrorMessage = "Quantity is required.")]
        [Range(0, 9999, ErrorMessage = "Quantity must be between 0 and 9999.")]
        public int Quantity { get; set; }
    }
}