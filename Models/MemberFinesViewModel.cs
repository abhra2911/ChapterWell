using System.Collections.Generic;
using System.Linq;

namespace Lib_Mgmt.Models
{
    public class MemberFinesViewModel
    {
        public List<FineRecord> Fines { get; set; } = new List<FineRecord>();

        public decimal TotalOutstanding => Fines.Where(f => !f.IsPaid).Sum(f => f.Amount);
        public decimal TotalPaid => Fines.Where(f => f.IsPaid).Sum(f => f.Amount);
        public int OutstandingCount => Fines.Count(f => !f.IsPaid);
        public bool HasOutstanding => OutstandingCount > 0;
    }
}
