using System.Collections.Generic;

namespace Lib_Mgmt.Models
{
    public class LibrarianBorrowingsViewModel
    {
        public List<ActiveBorrowingRow> Active { get; set; } = new List<ActiveBorrowingRow>();

        // Books with at least one free copy, used to populate the Issue modal.
        // TODO(Oracle): SELECT ... FROM BOOKS WHERE AVAILABLE_COPIES > 0.
        public List<CatalogBook> IssuableBooks { get; set; } = new List<CatalogBook>();

        public int ActiveCount => Active.Count;
    }
}
