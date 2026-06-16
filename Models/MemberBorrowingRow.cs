using System;

namespace Lib_Mgmt.Models
{
    /// <summary>One current borrowing, shown on the librarian Members page.</summary>
    public class MemberBorrowingRow
    {
        public string MemberName { get; set; } = "";
        public string MemberId { get; set; } = "";
        public string Email { get; set; } = "";
        public string BookTitle { get; set; } = "";
        public DateTime BorrowedOn { get; set; }
        public DateTime DueDate { get; set; }

        public int DaysOverdue => Math.Max(0, (DateTime.Today - DueDate.Date).Days);
        public bool IsOverdue => DaysOverdue > 0;
        public decimal Fine => DaysOverdue * 5m;
    }
}
