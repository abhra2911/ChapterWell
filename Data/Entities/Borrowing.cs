using System;

namespace Lib_Mgmt.Data.Entities
{
    /// <summary>Maps to LibMgmt_Borrowings. PK is application-assigned (no identity/sequence).</summary>
    public class Borrowing
    {
        public int BorrowingId { get; set; }
        public int MemberId { get; set; }
        public int BookId { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public string Status { get; set; }   // "ACTIVE" | "RETURNED"
    }
}
