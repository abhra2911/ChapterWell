using System;
using System.Collections.Generic;

namespace Lib_Mgmt.Models
{
    public partial class LibmgmtBooks
    {
        public LibmgmtBooks()
        {
            LibmgmtBookdamages = new HashSet<LibmgmtBookdamages>();
            LibmgmtBorrowings = new HashSet<LibmgmtBorrowings>();
            LibmgmtReservations = new HashSet<LibmgmtReservations>();
            LibmgmtWishlist = new HashSet<LibmgmtWishlist>();
        }

        public decimal BookId { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Isbn { get; set; }
        public string Genre { get; set; }
        public string Publisher { get; set; }
        public int? PublishYear { get; set; }
        public int TotalCopies { get; set; }
        public int AvailableCopies { get; set; }
        public DateTime AddedDate { get; set; }

        public ICollection<LibmgmtBookdamages> LibmgmtBookdamages { get; set; }
        public ICollection<LibmgmtBorrowings> LibmgmtBorrowings { get; set; }
        public ICollection<LibmgmtReservations> LibmgmtReservations { get; set; }
        public ICollection<LibmgmtWishlist> LibmgmtWishlist { get; set; }
    }
}
