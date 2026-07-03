using System.ComponentModel.DataAnnotations;

namespace Lib_Mgmt.Models
{

    public class AddBookViewModel
    {
        [Required(ErrorMessage = "Title is required.")]
        [StringLength(200)]
        public string Title { get; set; }

        [Required(ErrorMessage = "Author is required.")]
        [StringLength(150)]
        public string Author { get; set; }

        // ISBN-10 = 10 chars, ISBN-13 = 13 chars (with hyphens, up to 17).
        [StringLength(20)]
        [Display(Name = "ISBN")]
        public string Isbn { get; set; }

        [Required(ErrorMessage = "Genre is required.")]
        [StringLength(50)]
        public string Genre { get; set; }

        [Required(ErrorMessage = "Quantity is required.")]
        [Range(0, 9999, ErrorMessage = "Quantity must be between 0 and 9999.")]
        public int Quantity { get; set; }

        [Range(1000, 9999, ErrorMessage = "Enter a valid year.")]
        [Display(Name = "Published Year")]
        public int? PublishedYear { get; set; }

        [StringLength(150)]
        public string Publisher { get; set; }

        [Required(ErrorMessage = "Please Specify Shelf Number")]
        //[Range(1, 100, ErrorMessage = "Pick a shelf between 1-100 only")]   what if number of shelves in library expand later?
        public int ShelfNumber { get; set; }
    }
}
