using System;

namespace Lib_Mgmt.Data.Entities
{
    /// <summary>
    /// Maps to LibMgmt_Reservations. PK is application-assigned (no identity/sequence).
    /// A row's mere existence means "pending" — there is no status column.
    /// Cancelling (member) or fulfilling (librarian) both delete the row;
    /// fulfilling also creates the matching Borrowing in the same transaction.
    /// </summary>
    public class Reservation
    {
        public int ReservationId { get; set; }
        public int MemberId { get; set; }
        public int BookId { get; set; }
        public DateTime RequestedDate { get; set; }
    }
}
