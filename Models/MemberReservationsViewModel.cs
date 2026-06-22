using System.Collections.Generic;

namespace Lib_Mgmt.Models
{
    public class MemberReservationsViewModel
    {
        public List<ReservationItem> Items { get; set; } = new List<ReservationItem>();

        // Books currently out of stock — the only ones eligible to reserve.
        // The "Reserve a Book" modal's picker is restricted to this list.
        public List<CatalogBook> ReservableBooks { get; set; } = new List<CatalogBook>();

        public int Count => Items.Count;
    }
}
