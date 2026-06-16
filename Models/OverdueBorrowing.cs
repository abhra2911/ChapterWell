using System;

namespace Lib_Mgmt.Models
{
    public class OverdueBorrowing
    {
        public string MemberName { get; set; } = "";
        public string MemberId { get; set; } = "";
        public string BookTitle { get; set; } = "";
        public DateTime BorrowedOn { get; set; }
        public DateTime DueDate { get; set; }

        // Whole days between today and the due date, floored at 0.
        public int DaysOverdue => Math.Max(0, (DateTime.Today - DueDate.Date).Days);
        public decimal Fine => DaysOverdue * 5m;
    }
}
