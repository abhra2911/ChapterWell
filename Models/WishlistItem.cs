using System;

namespace Lib_Mgmt.Models
{
    /// <summary>One book the member has saved to read later.</summary>
    public class WishlistItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
        public string Isbn { get; set; } = "";
        public string Genre { get; set; } = "";
        public bool Available { get; set; }
        public DateTime AddedOn { get; set; }

        // Path under wwwroot; falls back to placeholder-cover.svg in the view.
        public string CoverImage { get; set; } = "";
    }
}
