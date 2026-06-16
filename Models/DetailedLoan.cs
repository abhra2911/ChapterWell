using System;

namespace Lib_Mgmt.Models
{
    /// <summary>One currently-borrowed book shown on the member's "My Loans" tab.</summary>
    public class DetailedLoan
    {
        public int BorrowingId { get; set; }
        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
        public string Isbn { get; set; } = "";
        public DateTime BorrowedOn { get; set; }
        public DateTime DueDate { get; set; }

        public int DaysOverdue => Math.Max(0, (DateTime.Today - DueDate.Date).Days);
        public bool IsOverdue => DaysOverdue > 0;
        public int DaysLeft => (DueDate.Date - DateTime.Today).Days;
        public decimal Fine => DaysOverdue * 2m;

        // "Overdue" | "Due soon" | "On time" — drives the status pill colour.
        public string Status =>
            IsOverdue ? "Overdue" : (DaysLeft <= 3 ? "Due soon" : "On time");
    }
}
