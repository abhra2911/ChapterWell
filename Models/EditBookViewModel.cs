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

        [Required(ErrorMessage = "Total copies is required.")]
        [Range(0, 9999, ErrorMessage = "Total copies must be between 0 and 9999.")]
        [Display(Name = "Total Copies")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Available copies is required.")]
        [Range(0, 9999, ErrorMessage = "Available copies must be between 0 and 9999.")]
        [Display(Name = "Available Copies")]
        public int AvailableCopies { get; set; }

        [Required(ErrorMessage = "Shelf number is required.")]
        [Range(1, 100, ErrorMessage = "Shelf must be between 1 and 100.")]
        [Display(Name = "Shelf")]
        public int ShelfNumber { get; set; }
    }
}
