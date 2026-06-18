using System;

namespace Lib_Mgmt.Data.Entities
{
    /// <summary>Maps to LibMgmt_BookDamages. PK is application-assigned (no identity/sequence).</summary>
    public class BookDamage
    {
        public int DamageId { get; set; }
        public int BookId { get; set; }
        public int DamagedCopies { get; set; }
        public string Reason { get; set; }
        public DateTime ReportedDate { get; set; }
        public int? ReportedBy { get; set; }   // LibMgmt_Librarians.LIBRARIAN_ID, nullable
    }
}
