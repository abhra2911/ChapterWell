using System.Collections.Generic;

namespace Lib_Mgmt.Models
{
    public class LibrarianMembersViewModel
    {
        public List<MemberBorrowingRow> Borrowings { get; set; } = new List<MemberBorrowingRow>();
    }
}
