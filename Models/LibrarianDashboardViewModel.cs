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
}
