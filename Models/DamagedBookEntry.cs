using System;

namespace Lib_Mgmt.Models
{
    /// <summary>
    /// One damaged-copy report shown in the librarian Books page.
    /// Multiple entries can exist for the same title if damage occurred
    /// across separate incidents.
    /// </summary>
    public class DamagedBookEntry
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
        public string Isbn { get; set; } = "";
        public int DamagedCopies { get; set; }
        public string Reason { get; set; } = "";        // e.g. "Water damage", "Torn pages"
        public DateTime ReportedOn { get; set; }
        public string ReportedBy { get; set; } = "";    // librarian or member who reported it
    }
}
