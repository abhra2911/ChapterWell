using System;
using System.Collections.Generic;

namespace Lib_Mgmt.Models
{
    public partial class LibmgmtMembers
    {
        public LibmgmtMembers()
        {
            LibmgmtBorrowings = new HashSet<LibmgmtBorrowings>();
            LibmgmtFines = new HashSet<LibmgmtFines>();
            LibmgmtReservations = new HashSet<LibmgmtReservations>();
            LibmgmtWishlist = new HashSet<LibmgmtWishlist>();
        }

        public decimal MemberId { get; set; }
        public string MemberCode { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string PasswordHash { get; set; }
        public DateTime JoinedDate { get; set; }
        public bool IsActive { get; set; }
        public string Address { get; set; }

        public ICollection<LibmgmtBorrowings> LibmgmtBorrowings { get; set; }
        public ICollection<LibmgmtFines> LibmgmtFines { get; set; }
        public ICollection<LibmgmtReservations> LibmgmtReservations { get; set; }
        public ICollection<LibmgmtWishlist> LibmgmtWishlist { get; set; }
    }
}
