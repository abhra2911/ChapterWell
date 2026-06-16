using System;

namespace Lib_Mgmt.Models
{
    /// <summary>One fine charged to the member (paid or outstanding).</summary>
    public class FineRecord
    {
        public int Id { get; set; }
        public string BookTitle { get; set; } = "";
        public string Reason { get; set; } = "";       // e.g. "Overdue (5 days)" / "Damaged copy"
        public decimal Amount { get; set; }
        public DateTime IssuedOn { get; set; }
        public DateTime? PaidOn { get; set; }           // null => still outstanding

        public bool IsPaid => PaidOn.HasValue;
    }
}
