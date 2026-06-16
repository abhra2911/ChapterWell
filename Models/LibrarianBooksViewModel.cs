using System.Collections.Generic;

namespace Lib_Mgmt.Models
{
    public class LibrarianBooksViewModel
    {
        public List<CatalogBook> Books { get; set; } = new List<CatalogBook>();

        // Damaged-copy reports rendered in the "Damaged Books" section.
        // TODO(Oracle): replace with a query that aggregates the damaged
        // copy reports table (or filter on BORROWINGS.STATUS = 'DAMAGED').
        public List<DamagedBookEntry> Damaged { get; set; } = new List<DamagedBookEntry>();

        public int DamagedTotal
        {
            get
            {
                int t = 0;
                foreach (var d in Damaged) t += d.DamagedCopies;
                return t;
            }
        }
    }
}
