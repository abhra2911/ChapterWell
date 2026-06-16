using System;

namespace Lib_Mgmt.Models
{
    /// <summary>One past (already-returned) borrowing in the member's history table.</summary>
    public class LoanHistoryRow
    {
        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
        public DateTime BorrowedOn { get; set; }
        public DateTime ReturnedOn { get; set; }
        public decimal FinePaid { get; set; }
        public bool WasLate { get; set; }
    }
}
