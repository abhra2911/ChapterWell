using System;
using System.Collections.Generic;

namespace Lib_Mgmt.Models
{
    public partial class LibmgmtBorrowings
    {
        public LibmgmtBorrowings()
        {
            LibmgmtFines = new HashSet<LibmgmtFines>();
        }

        public decimal BorrowingId { get; set; }
        public decimal MemberId { get; set; }
        public decimal BookId { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public string Status { get; set; }

        public LibmgmtBooks Book { get; set; }
        public LibmgmtMembers Member { get; set; }
        public ICollection<LibmgmtFines> LibmgmtFines { get; set; }
    }
}
