using System;

namespace Lib_Mgmt.Data.Entities
{
    /// <summary>Maps to LibMgmt_Wishlist. PK is application-assigned (no identity/sequence).</summary>
    public class WishlistEntry
    {
        public int WishlistId { get; set; }
        public int MemberId { get; set; }
        public int BookId { get; set; }
        public DateTime AddedDate { get; set; }
    }
}
