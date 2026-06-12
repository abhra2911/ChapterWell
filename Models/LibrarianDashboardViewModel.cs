using System;
using System.Collections.Generic;

namespace Lib_Mgmt.Models
{
    public class LibrarianDashboardViewModel
    {
        public int TotalMembers { get; set; }
        public int TotalTitles { get; set; }
        public int TotalCopiesAvailable { get; set; }
        public int BooksBorrowed { get; set; }
        public int OverdueCount { get; set; }
        public int NewMembersThisMonth { get; set; }
        public decimal TotalFinesDue { get; set; }

        public List<OverdueBorrowing> OverdueBorrowings { get; set; } = new List<OverdueBorrowing>();

        public int CopiesAvailable { get; set; }
        public int CopiesBorrowed { get; set; }
        public int CopiesOverdue { get; set; }
        public int CopiesDamaged { get; set; }

        public List<MonthlyCount> BorrowingsPerMonth { get; set; } = new List<MonthlyCount>();
        public List<MonthlyAmount> FinesPerMonth { get; set; } = new List<MonthlyAmount>();
        public List<BookBorrowCount> TopBorrowedBooks { get; set; } = new List<BookBorrowCount>();
    }

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

    public class MonthlyCount
    {
        public string Month { get; set; } = "";
        public int Count { get; set; }
    }

    public class MonthlyAmount
    {
        public string Month { get; set; } = "";
        public decimal Amount { get; set; }
    }

    public class BookBorrowCount
    {
        public string Title { get; set; } = "";
        public int Count { get; set; }
    }
}
