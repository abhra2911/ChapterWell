using System;

namespace Lib_Mgmt.Models
{
    /// <summary>One book a member is waiting on (no copies were available
    /// when they reserved it). A row existing IS the pending state — there
    /// is no separate status to track.</summary>
    public class ReservationItem
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
        public string Isbn { get; set; } = "";
        public string Genre { get; set; } = "";
        public DateTime RequestedOn { get; set; }

        // Path under wwwroot; falls back to placeholder-cover.svg in the view.
        public string CoverImage { get; set; } = "";
    }
}
