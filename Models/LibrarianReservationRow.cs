using System;

namespace Lib_Mgmt.Models
{
    /// <summary>One pending reservation as shown to a librarian — same
    /// underlying row as ReservationItem, but with the requesting member's
    /// details and the book's current stock so the librarian can see at a
    /// glance whether it's fulfillable right now.</summary>
    public class LibrarianReservationRow
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public string BookTitle { get; set; } = "";
        public string Isbn { get; set; } = "";

        public int MemberId { get; set; }
        public string MemberName { get; set; } = "";
        public string MemberCode { get; set; } = "";

        public DateTime RequestedOn { get; set; }
        public int AvailableCopies { get; set; }

        public bool CanFulfill => AvailableCopies > 0;
    }
}
