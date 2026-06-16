using System.Collections.Generic;

namespace Lib_Mgmt.Models
{
    public class MemberBooksViewModel
    {
        public List<CatalogBook> Books { get; set; } = new List<CatalogBook>();

        // IDs of books already on the current member's wishlist.
        // The Books partial uses this to render the bookmark icon filled vs empty.
        // TODO(Oracle): SELECT BOOK_ID FROM WISHLIST WHERE MEMBER_ID = @me.
        public HashSet<int> WishlistedBookIds { get; set; } = new HashSet<int>();
    }
}
