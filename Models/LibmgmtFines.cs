using System;
using System.Collections.Generic;

namespace Lib_Mgmt.Models
{
    public partial class LibmgmtFines
    {
        public decimal FineId { get; set; }
        public decimal MemberId { get; set; }
        public decimal BorrowingId { get; set; }
        public decimal Amount { get; set; }       ////double to decimal for Amount
        public string Reason { get; set; }
        public DateTime IssuedDate { get; set; }
        public DateTime? PaidDate { get; set; }

        public LibmgmtBorrowings Borrowing { get; set; }
        public LibmgmtMembers Member { get; set; }
    }
}
