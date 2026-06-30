using System;
using System.Collections.Generic;

namespace Lib_Mgmt.Models
{
    public partial class LibmgmtWishlist
    {
        public decimal WishlistId { get; set; }
        public decimal MemberId { get; set; }
        public decimal BookId { get; set; }
        public DateTime AddedDate { get; set; }

        public LibmgmtBooks Book { get; set; }
        public LibmgmtMembers Member { get; set; }
    }
}
