using System.Collections.Generic;

namespace Lib_Mgmt.Models
{
    public class MemberDashboardViewModel
    {
        public string MemberName { get; set; } = "";

        public int CurrentlyBorrowed { get; set; }
        public int OverdueCount { get; set; }
        public decimal FineDue { get; set; }

        public List<TopBook> TopBooks { get; set; } = new List<TopBook>();
        public List<MemberLoan> ActiveLoans { get; set; } = new List<MemberLoan>();
    }
}
