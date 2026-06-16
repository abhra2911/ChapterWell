using System;

namespace Lib_Mgmt.Models
{
    /// <summary>
    /// One active (not-yet-returned) borrowing, shown on the librarian
    /// Borrowings workflow page.
    /// TODO(Oracle): project these from a join of BORROWINGS + MEMBERS + BOOKS
    /// where RETURN_DATE IS NULL.
    /// </summary>
    public class ActiveBorrowingRow
    {
        public int BorrowingId { get; set; }
        public string MemberName { get; set; } = "";
        public string MemberId { get; set; } = "";
        public string BookTitle { get; set; } = "";
        public string Isbn { get; set; } = "";
        public DateTime BorrowedOn { get; set; }
        public DateTime DueDate { get; set; }

        // Both dates are date-only/midnight, so the day counts are exact.
        public int DaysOverdue => Math.Max(0, (DateTime.Today - DueDate.Date).Days);
        public bool IsOverdue => DaysOverdue > 0;
        public int DaysLeft => (DueDate.Date - DateTime.Today).Days;
        public decimal Fine => DaysOverdue * 5m;

        // "Overdue" | "Due soon" | "On time" — drives the status pill colour.
        public string Status =>
            IsOverdue ? "Overdue" : (DaysLeft <= 3 ? "Due soon" : "On time");
    }
}
