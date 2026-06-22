using System.Collections.Generic;

namespace Lib_Mgmt.Models
{
    public class LibrarianBorrowingsViewModel
    {
        public List<ActiveBorrowingRow> Active { get; set; } = new List<ActiveBorrowingRow>();

        // Books with at least one free copy
        public List<CatalogBook> IssuableBooks { get; set; } = new List<CatalogBook>();

        public List<LibrarianReservationRow> PendingReservations { get; set; } = new List<LibrarianReservationRow>();

        public int ActiveCount => Active.Count;
        public int PendingReservationCount => PendingReservations.Count;
    }
}
