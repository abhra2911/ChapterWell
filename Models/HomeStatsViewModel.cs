namespace Lib_Mgmt.Models
{
    public class HomeStatsViewModel
    {
        /// <summary>Total physical inventory across the catalog (sum of TotalCopies).</summary>
        public int Books { get; set; }

        /// <summary>Active members.</summary>
        public int Members { get; set; }

        /// <summary>Borrowings issued since the first day of the current month.</summary>
        public int LoansThisMonth { get; set; }

        /// <summary>Year the library was established (constant; not from DB).</summary>
        public int EstablishedYear { get; set; }
    }
}
