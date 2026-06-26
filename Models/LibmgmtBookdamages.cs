using System;
using System.Collections.Generic;

namespace Lib_Mgmt.Models
{
    public partial class LibmgmtBookdamages
    {
        public decimal DamageId { get; set; }
        public decimal BookId { get; set; }
        public int DamagedCopies { get; set; }
        public string Reason { get; set; }
        public DateTime ReportedDate { get; set; }
        public decimal? ReportedBy { get; set; }

        public LibmgmtBooks Book { get; set; }
        public LibmgmtLibrarians ReportedByNavigation { get; set; }
    }
}
