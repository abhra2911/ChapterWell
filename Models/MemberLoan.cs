using System;

namespace Lib_Mgmt.Models
{
    public class MemberLoan
    {
        public string Title { get; set; } = "";
        public DateTime BorrowedOn { get; set; }
        public DateTime DueDate { get; set; }

        // Both dates are date-only/midnight, so the day count is exact.
        // DateOnly is unavailable before .NET 6, so DateTime is used here.
        public int DaysOverdue => Math.Max(0, (DateTime.Today - DueDate.Date).Days);
        public decimal Fine => DaysOverdue * 5m;
        public string Status => DaysOverdue > 0 ? "Overdue" : "On time";
    }
}