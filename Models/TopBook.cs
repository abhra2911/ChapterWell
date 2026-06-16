namespace Lib_Mgmt.Models
{
    public class TopBook
    {
        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
        public string Isbn { get; set; } = "";
        public int BorrowCount { get; set; }
        public bool Available { get; set; }

        // Path under wwwroot to the cover image, e.g. "/images/covers/clean-code.jpg".
        // Leave empty to fall back to the placeholder cover in the view.
        public string CoverImage { get; set; } = "";
    }
}
