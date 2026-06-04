using System;
using System.Collections.Generic;

namespace Lib_Mgmt.Models
{
    public class MemberDashboardViewModel
    {
        public string MemberName { get; set; } = "";

        public int CurrentlyBorrowed { get; set; }
        public int OverdueCount { get; set; }
        public decimal FineDue { get; set; }

        public List<TopBook> TopBooks { get; set; } = new List<TopBook>();
        public List<MemberLoan> ActiveLoans { get; set; } = new List<MemberLoan>();
    }

    public class TopBook
    {
        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
        public string Isbn { get; set; } = "";
        public int BorrowCount { get; set; }
        public bool Available { get; set; }
    }

    public class MemberLoan
    {
        public string Title { get; set; } = "";
        public DateTime BorrowedOn { get; set; }
        public DateTime DueDate { get; set; }

        // Both dates are date-only/midnight, so the day count is exact.
        // DateOnly is unavailable before .NET 6, so DateTime is used here.
        public int DaysOverdue => Math.Max(0, (DateTime.Today - DueDate.Date).Days);
        public decimal Fine => DaysOverdue * 2m;
        public string Status => DaysOverdue > 0 ? "Overdue" : "On time";
    }
}
