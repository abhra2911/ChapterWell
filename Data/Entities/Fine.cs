using System;

namespace Lib_Mgmt.Data.Entities
{
    /// <summary>Maps to LibMgmt_Fines. PK is application-assigned (no identity/sequence).</summary>
    public class Fine
    {
        public int FineId { get; set; }
        public int BorrowingId { get; set; }
        public int MemberId { get; set; }
        public string Reason { get; set; }
        public decimal Amount { get; set; }
        public DateTime IssuedDate { get; set; }
        public DateTime? PaidDate { get; set; }   // null => still outstanding
    }
}
