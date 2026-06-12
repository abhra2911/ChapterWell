using System;
using System.Collections.Generic;

namespace Lib_Mgmt.Models
{
    /// <summary>
    /// Read model for a row in the Book catalog. This is what the Books pages
    /// (librarian + member) bind to. When Oracle is wired up, populate a
    /// List&lt;CatalogBook&gt; from the BOOKS table instead of the hard-coded
    /// sample data in AccountController.
    /// </summary>
    public class CatalogBook
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
        public string Genre { get; set; } = "";
        public string Isbn { get; set; } = "";
        public string Publisher { get; set; } = "";
        public int? PublishedYear { get; set; }

        public int Quantity { get; set; }          // total copies owned
        public int AvailableCopies { get; set; }    // copies on the shelf right now

        // Path under wwwroot, e.g. "/images/covers/9780132350884.jpg".
        // Leave empty to fall back to placeholder-cover.svg in the view.
        public string CoverImage { get; set; } = "";

        public bool Available => AvailableCopies > 0;
    }

    public class LibrarianBooksViewModel
    {
        public List<CatalogBook> Books { get; set; } = new List<CatalogBook>();
    }

    public class MemberBooksViewModel
    {
        public List<CatalogBook> Books { get; set; } = new List<CatalogBook>();
    }

    /// <summary>One current borrowing, shown on the librarian Members page.</summary>
    public class MemberBorrowingRow
    {
        public string MemberName { get; set; } = "";
        public string MemberId { get; set; } = "";
        public string Email { get; set; } = "";
        public string BookTitle { get; set; } = "";
        public DateTime BorrowedOn { get; set; }
        public DateTime DueDate { get; set; }

        public int DaysOverdue => Math.Max(0, (DateTime.Today - DueDate.Date).Days);
        public bool IsOverdue => DaysOverdue > 0;
        public decimal Fine => DaysOverdue * 5m;
    }

    public class LibrarianMembersViewModel
    {
        public List<MemberBorrowingRow> Borrowings { get; set; } = new List<MemberBorrowingRow>();
    }

    /// <summary>
    /// Maps a genre name to one of a fixed set of badge colour classes so the
    /// same genre always gets the same colour across the app.
    /// </summary>
    public static class GenreBadge
    {
        private static readonly string[] Palette =
            { "g-indigo", "g-teal", "g-violet", "g-amber", "g-rose", "g-slate" };

        public static string CssClass(string genre)
        {
            if (string.IsNullOrWhiteSpace(genre)) return "g-slate";
            int hash = 0;
            foreach (char c in genre.Trim().ToLowerInvariant())
            {
                hash = (hash * 31 + c) & 0x7fffffff;
            }
            return Palette[hash % Palette.Length];
        }
    }
}
