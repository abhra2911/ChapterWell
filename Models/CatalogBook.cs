using System;

namespace Lib_Mgmt.Models
{
    /// <summary>
    /// Read model for a row in the Book catalog. This is what the Books pages
    /// (librarian + member) bind to. When Oracle is wired up, populate a
    /// List&lt;CatalogBook&gt; from the BOOKS table instead of the hard-coded
    /// sample data in AccountController.
    /// </summary>
    public class CatalogBook
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
        public string Genre { get; set; } = "";
        public string Isbn { get; set; } = "";
        public string Publisher { get; set; } = "";
        public int? PublishedYear { get; set; }

        public int Quantity { get; set; }          // total copies owned
        public int AvailableCopies { get; set; }    // copies on the shelf right now
        public int ShelfNumber { get; set; }        // physical shelf, assigned randomly on AddBook

        // Path under wwwroot, e.g. "/images/covers/9780132350884.jpg".
        // Leave empty to fall back to placeholder-cover.svg in the view.
        public string CoverImage { get; set; } = "";

        public bool Available => AvailableCopies > 0;
    }
}
