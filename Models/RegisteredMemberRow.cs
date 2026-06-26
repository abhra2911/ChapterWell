using System;

namespace Lib_Mgmt.Models
{
    /// <summary>One row on the librarian Members page — every registered member.</summary>
    public class RegisteredMemberRow
    {
        public int Id { get; set; }              // numeric PK (for delete form)
        public string MemberCode { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public DateTime JoinedDate { get; set; }
        public bool IsActive { get; set; }
        public int ActiveLoans { get; set; }     // current unreturned borrowings
        public decimal UnpaidFines { get; set; }  // sum of outstanding fines
    }
}
