using System;
using System.Collections.Generic;

namespace Lib_Mgmt.Models
{
    public partial class LibmgmtLibrarians
    {
        public LibmgmtLibrarians()
        {
            LibmgmtBookdamages = new HashSet<LibmgmtBookdamages>();
        }

        public decimal LibrarianId { get; set; }
        public string LibrarianCode { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string PasswordHash { get; set; }
        public DateTime JoinedDate { get; set; }
        public bool IsActive { get; set; }

        public ICollection<LibmgmtBookdamages> LibmgmtBookdamages { get; set; }
    }
}
