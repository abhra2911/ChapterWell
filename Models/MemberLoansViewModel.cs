using System.Collections.Generic;
using System.Linq;

namespace Lib_Mgmt.Models
{
    public class MemberLoansViewModel
    {
        public string MemberName { get; set; } = "";
        public List<DetailedLoan> Active { get; set; } = new List<DetailedLoan>();
        public List<LoanHistoryRow> History { get; set; } = new List<LoanHistoryRow>();

        // Fines used to be its own tab; it's now folded in as a section here.
        public List<FineRecord> Fines { get; set; } = new List<FineRecord>();

        public int ActiveCount => Active.Count;
        public int OverdueCount => Active.Count(l => l.IsOverdue);
        public int HistoryCount => History.Count;

        public decimal TotalOutstanding => Fines.Where(f => !f.IsPaid).Sum(f => f.Amount);
        public decimal TotalPaid => Fines.Where(f => f.IsPaid).Sum(f => f.Amount);
        public int OutstandingCount => Fines.Count(f => !f.IsPaid);
        public bool HasOutstanding => OutstandingCount > 0;
    }
}
