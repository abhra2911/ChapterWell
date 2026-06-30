using System;
using System.Collections.Generic;

namespace Lib_Mgmt.Models
{
    public partial class LibmgmtReservations
    {
        public decimal ReservationId { get; set; }
        public decimal? MemberId { get; set; }
        public decimal? BookId { get; set; }
        public DateTime? RequestedDate { get; set; }

        public LibmgmtBooks Book { get; set; }
        public LibmgmtMembers Member { get; set; }
    }
}
