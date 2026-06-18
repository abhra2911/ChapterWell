using System;

namespace Lib_Mgmt.Data.Entities
{
    /// <summary>Maps to LibMgmt_Books. PK is application-assigned (no identity/sequence).</summary>
    public class Book
    {
        public int BookId { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Genre { get; set; }
        public string Isbn { get; set; }
        public string Publisher { get; set; }
        public int? PublishYear { get; set; }
        public int TotalCopies { get; set; }
        public int AvailableCopies { get; set; }
        public DateTime AddedDate { get; set; }
    }
}
