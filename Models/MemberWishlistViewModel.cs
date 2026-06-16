using System.Collections.Generic;
using System.Linq;

namespace Lib_Mgmt.Models
{
    public class MemberWishlistViewModel
    {
        public List<WishlistItem> Items { get; set; } = new List<WishlistItem>();

        // Catalog books not already on the wishlist, for the "Add" modal.
        // TODO(Oracle): SELECT books from BOOKS not present in WISHLIST for this member.
        public List<CatalogBook> AddableBooks { get; set; } = new List<CatalogBook>();

        public int Count => Items.Count;
        public int AvailableCount => Items.Count(i => i.Available);
    }
}
