using System.Collections.Generic;
using System.Linq;

namespace Lib_Mgmt.Models
{
    public class MemberLoansViewModel
    {
        public string MemberName { get; set; } = "";
        public List<DetailedLoan> Active { get; set; } = new List<DetailedLoan>();
        public List<LoanHistoryRow> History { get; set; } = new List<LoanHistoryRow>();

        public int ActiveCount => Active.Count;
        public int OverdueCount => Active.Count(l => l.IsOverdue);
        public int HistoryCount => History.Count;
    }
}
