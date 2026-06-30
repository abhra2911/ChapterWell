using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Lib_Mgmt.Data.Entities;
using Lib_Mgmt.Models;

namespace Lib_Mgmt.Data
{
    /// <summary>
    /// All database access for the library app. EF Core repository over
    /// <see cref="ModelContext"/> (Microsoft.EntityFrameworkCore.SqlServer).
    /// Originally written against Oracle via Devart; the public API is
    /// unchanged by the SQL Server port, so controllers and views need no edits.
    ///
    /// Conventions carried over from the original Oracle schema:
    ///   * Primary keys are application-assigned as MAX(id)+1, computed here
    ///     via Max(...) + 1 inside a transaction (no IDENTITY column).
    ///   * Dates are written date-only (midnight) so day-count maths stays exact.
    ///   * LEAST/GREATEST-style clamping is done in C# (Math.Min/Math.Max).
    ///   * Multi-statement writes run inside a single EF Core transaction.
    /// </summary>
    public class LibraryRepository
    {
        private readonly ModelContext _context;
        private const decimal FinePerDay = 5m;  // ₹5 per overdue day

        public LibraryRepository(ModelContext context)
        {
            _context = context;
        }

        // =====================================================================
        // Authentication / profile
        // =====================================================================

        public sealed class AuthRow
        {
            public int UserId;
            public string Role;          // "LIBRARIAN" | "MEMBER"
            public string Username;
            public string FullName;
            public string Code;
            public string PasswordHash;
        }

        /// <summary>
        /// Looks up an active user by username OR email across both tables. The
        /// identifier is matched case-insensitively against the Email column and
        /// exactly against Username. Returns null if no active row matches; the
        /// caller verifies the bcrypt hash. Librarians take precedence (mirrors
        /// the original UNION ALL order).
        /// </summary>
        public AuthRow FindUserByUsername(string identifier)
        {
            var id = (identifier ?? "").Trim();
            if (id.Length == 0) return null;

            var idLower = id.ToLower();

            //var lib = _db.Librarians.AsNoTracking()
            //    .FirstOrDefault(x => x.IsActive == 1
            //        && (x.Username == id || (x.Email != null && x.Email.ToLower() == idLower)));
            var lib = _context.LibmgmtLibrarians.AsNoTracking()
                .FirstOrDefault(x => x.IsActive == true
                    && (x.Username == id || (x.Email != null && x.Email.ToLower() == idLower)));

            if (lib != null)
                return new AuthRow
                {
                    UserId = Convert.ToInt32(lib.LibrarianId),
                    Role = "LIBRARIAN",
                    Username = lib.Username,
                    FullName = lib.FullName,
                    Code = lib.LibrarianCode,
                    PasswordHash = lib.PasswordHash
                };

            /*var mem = _db.Members.AsNoTracking()
                .FirstOrDefault(x => x.IsActive == 1
                    && (x.Username == id || (x.Email != null && x.Email.ToLower() == idLower))); */
            var mem = _context.LibmgmtMembers.AsNoTracking()
                .FirstOrDefault(x => x.IsActive == true
                     && (x.Username == id || (x.Email != null && x.Email.ToLower() == idLower)));

            if (mem != null)
                return new AuthRow
                {
                    UserId = Convert.ToInt32(mem.MemberId),
                    Role = "MEMBER",
                    Username = mem.Username,
                    FullName = mem.FullName,
                    Code = mem.MemberCode,
                    PasswordHash = mem.PasswordHash
                };

            return null;
        }

        public sealed class Profile
        {
            public string FullName;
            public string Email;
            public string Phone;
            public string Code;
        }

        public Profile GetProfile(string role, int userId)
        {
            if (role == "LIBRARIAN")
            {
                //var l = _db.Librarians.AsNoTracking().FirstOrDefault(x => x.LibrarianId == userId);
                var l = _context.LibmgmtLibrarians.AsNoTracking().FirstOrDefault(x => x.LibrarianId == userId);

                if (l != null)
                    return new Profile { FullName = l.FullName ?? "", Email = l.Email ?? "", Phone = l.Phone ?? "", Code = l.LibrarianCode ?? "" };
            }
            else
            {
                //var m = _db.Members.AsNoTracking().FirstOrDefault(x => x.MemberId == userId);
                var m = _context.LibmgmtMembers.AsNoTracking().FirstOrDefault(x => x.MemberId == userId);

                if (m != null)
                    return new Profile { FullName = m.FullName ?? "", Email = m.Email ?? "", Phone = m.Phone ?? "", Code = m.MemberCode ?? "" };
            }
            return new Profile { FullName = "", Email = "", Phone = "", Code = "" };
        }

        public void UpdateProfile(string role, int userId, string name, string email, string phone)
        {
            if (role == "LIBRARIAN")
            {
                //var l = _db.Librarians.FirstOrDefault(x => x.LibrarianId == userId);
                var l = _context.LibmgmtLibrarians.FirstOrDefault(x => x.LibrarianId == userId);
                if (l == null) return;
                l.FullName = name ?? "";
                l.Email = email ?? "";
                l.Phone = phone;
            }
            else
            {
                //var m = _db.Members.FirstOrDefault(x => x.MemberId == userId);
                var m = _context.LibmgmtMembers.FirstOrDefault(x => x.MemberId == userId);
                if (m == null) return;
                m.FullName = name ?? "";
                m.Email = email ?? "";
                m.Phone = phone;
            }
            //_db.SaveChanges();
            _context.SaveChanges();
        }

        public string GetPasswordHash(string role, int userId)
        {
            if (role == "LIBRARIAN")
                //return _db.Librarians.AsNoTracking()
                //    .Where(x => x.LibrarianId == userId).Select(x => x.PasswordHash).FirstOrDefault() ?? "";
                return _context.LibmgmtLibrarians.AsNoTracking()
                    .Where(x => x.LibrarianId == userId).Select(x => x.PasswordHash).FirstOrDefault() ?? "";

            //return _db.Members.AsNoTracking()
            //    .Where(x => x.MemberId == userId).Select(x => x.PasswordHash).FirstOrDefault() ?? "";
            return _context.LibmgmtMembers.AsNoTracking()
                .Where(x => x.MemberId == userId).Select(x => x.PasswordHash).FirstOrDefault() ?? "";
        }

        public void UpdatePasswordHash(string role, int userId, string newHash)
        {
            if (role == "LIBRARIAN")
            {
                //var l = _db.Librarians.FirstOrDefault(x => x.LibrarianId == userId);
                var l = _context.LibmgmtLibrarians.FirstOrDefault(x => x.LibrarianId == userId);
                if (l == null) return;
                l.PasswordHash = newHash;
            }
            else
            {
                //var m = _db.Members.FirstOrDefault(x => x.MemberId == userId);
                var m = _context.LibmgmtMembers.FirstOrDefault(x => x.MemberId == userId);
                if (m == null) return;
                m.PasswordHash = newHash;
            }
            //_db.SaveChanges();
            _context.SaveChanges();
        }

        // =====================================================================
        // Public site stats (Home/Index "Our Library at a Glance")
        // =====================================================================

        public HomeStatsViewModel GetHomeStats()
        {
            // Total physical inventory — matches the "Books" headline figure
            // better than a distinct-title count.
            //var bookCopies = _db.Books.AsNoTracking()
            //    .Select(b => (int?)b.TotalCopies)
            //    .Sum() ?? 0;
            var bookCopies = _context.LibmgmtBooks.AsNoTracking()
                .Select(b => (int?)b.TotalCopies)
                .Sum() ?? 0;

            //var activeMembers = _db.Members.AsNoTracking()
            //    .Count(m => m.IsActive == 1);
            var activeMembers = _context.LibmgmtMembers.AsNoTracking()
                .Count(m => m.IsActive == true);

            var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            //var loansThisMonth = _db.Borrowings.AsNoTracking()
            //    .Count(b => b.IssueDate >= monthStart);
            var loansThisMonth = _context.LibmgmtBorrowings.AsNoTracking()
                .Count(b => b.IssueDate >= monthStart);

            return new HomeStatsViewModel
            {
                Books = bookCopies,
                Members = activeMembers,
                LoansThisMonth = loansThisMonth,
                EstablishedYear = 1952
            };
        }

        // =====================================================================
        // Catalog (shared by librarian + member Books tabs)
        // =====================================================================

        public List<CatalogBook> GetCatalog()
        {
            //return _db.Books.AsNoTracking()
            return _context.LibmgmtBooks.AsNoTracking()
                .OrderBy(b => b.Title)
                .Select(b => new CatalogBook
                {
                    Id = (int)b.BookId,
                    Title = b.Title,
                    Author = b.Author,
                    Genre = b.Genre,
                    Isbn = b.Isbn,
                    Publisher = b.Publisher,
                    PublishedYear = b.PublishYear,
                    Quantity = b.TotalCopies,
                    AvailableCopies = b.AvailableCopies
                })
                .ToList();
        }

        public List<DamagedBookEntry> GetDamagedBooks()
        {
            // Pull the librarian id -> name map once, then resolve the reporter
            // name in memory (avoids a left-join translation on EF Core 2.1).
            //var libNames = _db.Librarians.AsNoTracking()
            //    .ToDictionary(l => l.LibrarianId, l => l.FullName);
            var libNames = _context.LibmgmtLibrarians.AsNoTracking()
                .ToDictionary(l => Convert.ToInt32(l.LibrarianId), l => l.FullName);

            //var rows = (from d in _db.BookDamages.AsNoTracking()
            //            join b in _db.Books.AsNoTracking() on d.BookId equals b.BookId
            var rows = (from d in _context.LibmgmtBookdamages.AsNoTracking()
                        join b in _context.LibmgmtBooks.AsNoTracking() on d.BookId equals b.BookId
                        orderby d.ReportedDate descending
                        select new
                        {
                            d.DamageId,
                            d.BookId,
                            b.Title,
                            b.Author,
                            b.Isbn,
                            d.DamagedCopies,
                            d.Reason,
                            d.ReportedDate,
                            d.ReportedBy
                        }).ToList();

            return rows.Select(r => new DamagedBookEntry
            {
                Id = (int)r.DamageId,
                BookId = (int)r.BookId,
                Title = r.Title,
                Author = r.Author,
                Isbn = r.Isbn,
                DamagedCopies = r.DamagedCopies,
                Reason = r.Reason,
                ReportedOn = r.ReportedDate,
                ReportedBy = (r.ReportedBy.HasValue && libNames.TryGetValue(Convert.ToInt32(r.ReportedBy.Value), out var n) && !string.IsNullOrEmpty(n)) ? n : "-"
            }).ToList();
        }

        // =====================================================================
        // Librarian dashboard
        // =====================================================================

        public LibrarianDashboardViewModel GetLibrarianDashboard()
        {
            var m = new LibrarianDashboardViewModel();

            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var windowStart = monthStart.AddMonths(-5);   // last 6 month-buckets

            // Headline scalars.
            //m.TotalMembers = _db.Members.Count(x => x.IsActive == 1);
            m.TotalMembers = _context.LibmgmtMembers.Count(x => x.IsActive == true);
            //m.TotalTitles = _db.Books.Count();
            m.TotalTitles = _context.LibmgmtBooks.Count();
            //m.TotalCopiesAvailable = _db.Books.Select(b => (int?)b.AvailableCopies).Sum() ?? 0;
            m.TotalCopiesAvailable = _context.LibmgmtBooks.Select(b => (int?)b.AvailableCopies).Sum() ?? 0;
            //m.BooksBorrowed = _db.Borrowings.Count(x => x.Status == "ACTIVE");
            m.BooksBorrowed = _context.LibmgmtBorrowings.Count(x => x.Status == "ACTIVE");
            //m.OverdueCount = _db.Borrowings.Count(x => x.Status == "ACTIVE" && x.DueDate < today);
            m.OverdueCount = _context.LibmgmtBorrowings.Count(x => x.Status == "ACTIVE" && x.DueDate < today);
            //m.NewMembersThisMonth = _db.Members.Count(x => x.JoinedDate >= monthStart);
            m.NewMembersThisMonth = _context.LibmgmtMembers.Count(x => x.JoinedDate >= monthStart);
            //m.TotalFinesDue = _db.Fines.Where(f => f.PaidDate == null).Select(f => (decimal?)f.Amount).Sum() ?? 0m;
            m.TotalFinesDue = (decimal)_context.LibmgmtFines.Where(f => f.PaidDate == null).Sum(f => f.Amount);
            //m.CopiesDamaged = _db.BookDamages.Select(d => (int?)d.DamagedCopies).Sum() ?? 0;
            m.CopiesDamaged = _context.LibmgmtBookdamages.Select(d => (int?)d.DamagedCopies).Sum() ?? 0;

            m.CopiesAvailable = m.TotalCopiesAvailable;
            m.CopiesBorrowed = m.BooksBorrowed;
            m.CopiesOverdue = m.OverdueCount;

            // Borrowings per month (last 6 months) — bucket in memory.
            //var borrowDates = _db.Borrowings.AsNoTracking()
            var borrowDates = _context.LibmgmtBorrowings.AsNoTracking()
                .Where(x => x.IssueDate >= windowStart)
                .Select(x => x.IssueDate)
                .ToList();
            m.BorrowingsPerMonth = borrowDates
                .GroupBy(d => new DateTime(d.Year, d.Month, 1))
                .OrderBy(g => g.Key)
                .Select(g => new MonthlyCount
                {
                    Month = g.Key.ToString("MMM yyyy", CultureInfo.InvariantCulture),
                    Count = g.Count()
                })
                .ToList();

            // Fines per month (last 6 months) — bucket in memory.
            //var fineRows = _db.Fines.AsNoTracking()
            var fineRows = _context.LibmgmtFines.AsNoTracking()
                .Where(f => f.IssuedDate >= windowStart)
                .Select(f => new { f.IssuedDate, f.Amount })
                .ToList();
            m.FinesPerMonth = fineRows
                .GroupBy(f => new DateTime(f.IssuedDate.Year, f.IssuedDate.Month, 1))
                .OrderBy(g => g.Key)
                .Select(g => new MonthlyAmount
                {
                    Month = g.Key.ToString("MMM yyyy", CultureInfo.InvariantCulture),
                    Amount = (decimal)g.Sum(x => x.Amount)
                })
                .ToList();

            // Top 5 borrowed titles (all-time borrow count, including zero-borrow titles).
            //var countsByBook = _db.Borrowings.AsNoTracking()
            var countsByBook = _context.LibmgmtBorrowings.AsNoTracking()
                .GroupBy(x => x.BookId)
                .Select(g => new { BookId = g.Key, C = g.Count() })
                .ToList()
                .ToDictionary(x => x.BookId, x => x.C);

            //var titles = _db.Books.AsNoTracking()
            var titles = _context.LibmgmtBooks.AsNoTracking()
                .Select(b => new { b.BookId, b.Title })
                .ToList();

            m.TopBorrowedBooks = titles
                .Select(b => new BookBorrowCount
                {
                    Title = b.Title,
                    Count = countsByBook.TryGetValue(b.BookId, out var c) ? c : 0
                })
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Title)
                .Take(5)
                .ToList();

            // Currently overdue loans.
            //m.OverdueBorrowings = (from br in _db.Borrowings.AsNoTracking()
            //                       join mem in _db.Members.AsNoTracking() on br.MemberId equals mem.MemberId
            //                       join bk in _db.Books.AsNoTracking() on br.BookId equals bk.BookId
            m.OverdueBorrowings = (from br in _context.LibmgmtBorrowings.AsNoTracking()
                                   join mem in _context.LibmgmtMembers.AsNoTracking() on br.MemberId equals mem.MemberId
                                   join bk in _context.LibmgmtBooks.AsNoTracking() on br.BookId equals bk.BookId
                                   where br.Status == "ACTIVE" && br.DueDate < today
                                   orderby br.DueDate
                                   select new OverdueBorrowing
                                   {
                                       MemberName = mem.FullName,
                                       MemberId = mem.MemberCode,
                                       BookTitle = bk.Title,
                                       BorrowedOn = br.IssueDate,
                                       DueDate = br.DueDate
                                   }).ToList();

            return m;
        }

        // =====================================================================
        // Librarian — Members + Borrowings tabs
        // =====================================================================

        public List<MemberBorrowingRow> GetMemberBorrowings()
        {
            //return (from br in _db.Borrowings.AsNoTracking()
            //        join mem in _db.Members.AsNoTracking() on br.MemberId equals mem.MemberId
            //        join bk in _db.Books.AsNoTracking() on br.BookId equals bk.BookId
            return (from br in _context.LibmgmtBorrowings.AsNoTracking()
                    join mem in _context.LibmgmtMembers.AsNoTracking() on br.MemberId equals mem.MemberId
                    join bk in _context.LibmgmtBooks.AsNoTracking() on br.BookId equals bk.BookId
                    where br.Status == "ACTIVE"
                    orderby br.DueDate
                    select new MemberBorrowingRow
                    {
                        MemberName = mem.FullName,
                        MemberId = mem.MemberCode,
                        Email = mem.Email,
                        BookTitle = bk.Title,
                        BorrowedOn = br.IssueDate,
                        DueDate = br.DueDate
                    }).ToList();
        }

        /// <summary>
        /// Returns every registered member with loan/fine summaries.
        /// Aggregation is done in-memory (EF Core 2.1 GroupBy limitation).
        /// </summary>
        public List<RegisteredMemberRow> GetRegisteredMembers()
        {
            //var members = _db.Members.AsNoTracking().ToList();
            var members = _context.LibmgmtMembers.AsNoTracking().ToList();

            // Active-loan counts per member
            //var loanCounts = _db.Borrowings.AsNoTracking()
            var loanCounts = _context.LibmgmtBorrowings.AsNoTracking()
                .Where(b => b.Status == "ACTIVE")
                .ToList()
                .GroupBy(b => b.MemberId)
                .ToDictionary(g => g.Key, g => g.Count());

            // Unpaid-fine totals per member
            //var fineTotals = _db.Fines.AsNoTracking()
            var fineTotals = _context.LibmgmtFines.AsNoTracking()
                .Where(f => f.PaidDate == null)
                .ToList()
                .GroupBy(f => f.MemberId)
                .ToDictionary(g => g.Key, g => (decimal)g.Sum(f => f.Amount));

            return members.Select(m => new RegisteredMemberRow
            {
                Id = (int)m.MemberId,
                MemberCode = m.MemberCode ?? "",
                FullName = m.FullName ?? "",
                Email = m.Email ?? "",
                Phone = m.Phone ?? "",
                JoinedDate = m.JoinedDate,
                IsActive = m.IsActive,
                ActiveLoans = loanCounts.ContainsKey(m.MemberId) ? loanCounts[m.MemberId] : 0,
                UnpaidFines = fineTotals.ContainsKey(m.MemberId) ? fineTotals[m.MemberId] : 0m
            })
            .OrderBy(r => r.FullName)
            .ToList();
        }

        public List<ActiveBorrowingRow> GetActiveBorrowings()
        {
            //return (from br in _db.Borrowings.AsNoTracking()
            //        join mem in _db.Members.AsNoTracking() on br.MemberId equals mem.MemberId
            //        join bk in _db.Books.AsNoTracking() on br.BookId equals bk.BookId
            return (from br in _context.LibmgmtBorrowings.AsNoTracking()
                    join mem in _context.LibmgmtMembers.AsNoTracking() on br.MemberId equals mem.MemberId
                    join bk in _context.LibmgmtBooks.AsNoTracking() on br.BookId equals bk.BookId
                    where br.Status == "ACTIVE"
                    orderby br.DueDate
                    select new ActiveBorrowingRow
                    {
                        BorrowingId = (int)br.BorrowingId,
                        MemberName = mem.FullName,
                        MemberId = mem.MemberCode,
                        BookTitle = bk.Title,
                        Isbn = bk.Isbn,
                        BorrowedOn = br.IssueDate,
                        DueDate = br.DueDate
                    }).ToList();
        }

        // =====================================================================
        // Member dashboard + tabs
        // =====================================================================

        public MemberDashboardViewModel GetMemberDashboard(int memberId, string memberName)
        {
            decimal mid = memberId;
            var m = new MemberDashboardViewModel { MemberName = memberName };

            //m.ActiveLoans = (from br in _db.Borrowings.AsNoTracking()
            //                 join bk in _db.Books.AsNoTracking() on br.BookId equals bk.BookId
            //                 where br.MemberId == memberId && br.Status == "ACTIVE"
            m.ActiveLoans = (from br in _context.LibmgmtBorrowings.AsNoTracking()
                             join bk in _context.LibmgmtBooks.AsNoTracking() on br.BookId equals bk.BookId
                             where br.MemberId == mid && br.Status == "ACTIVE"
                             orderby br.DueDate
                             select new MemberLoan
                             {
                                 Title = bk.Title,
                                 BorrowedOn = br.IssueDate,
                                 DueDate = br.DueDate
                             }).ToList();

            m.CurrentlyBorrowed = m.ActiveLoans.Count;
            foreach (var l in m.ActiveLoans)
            {
                if (l.DaysOverdue > 0) m.OverdueCount++;
                m.FineDue += l.Fine;
            }

            // Add unpaid crystallized fines (from past renewals/returns).
            //m.FineDue += _db.Fines
            //    .Where(f => f.MemberId == memberId && f.PaidDate == null)
            //    .Select(f => (decimal?)f.Amount).Sum() ?? 0m;
            m.FineDue += (decimal)_context.LibmgmtFines
                .Where(f => f.MemberId == mid && f.PaidDate == null)
                .Sum(f => f.Amount);

            // Top 10 books by all-time borrow count (including zero-borrow titles).
            //var countsByBook = _db.Borrowings.AsNoTracking()
            var countsByBook = _context.LibmgmtBorrowings.AsNoTracking()
                .GroupBy(x => x.BookId)
                .Select(g => new { BookId = g.Key, C = g.Count() })
                .ToList()
                .ToDictionary(x => x.BookId, x => x.C);

            //var books = _db.Books.AsNoTracking()
            var books = _context.LibmgmtBooks.AsNoTracking()
                .Select(b => new { b.BookId, b.Title, b.Author, b.Isbn, b.AvailableCopies })
                .ToList();

            m.TopBooks = books
                .Select(b => new TopBook
                {
                    Title = b.Title,
                    Author = b.Author,
                    Isbn = b.Isbn,
                    Available = b.AvailableCopies > 0,
                    BorrowCount = countsByBook.TryGetValue(b.BookId, out var c) ? c : 0
                })
                .OrderByDescending(x => x.BorrowCount)
                .ThenBy(x => x.Title)
                .Take(10)
                .ToList();

            return m;
        }

        public List<DetailedLoan> GetMemberActiveLoans(int memberId)
        {
            decimal mid = memberId;
            //return (from br in _db.Borrowings.AsNoTracking()
            //        join bk in _db.Books.AsNoTracking() on br.BookId equals bk.BookId
            //        where br.MemberId == memberId && br.Status == "ACTIVE"
            return (from br in _context.LibmgmtBorrowings.AsNoTracking()
                    join bk in _context.LibmgmtBooks.AsNoTracking() on br.BookId equals bk.BookId
                    where br.MemberId == mid && br.Status == "ACTIVE"
                    orderby br.DueDate
                    select new DetailedLoan
                    {
                        BorrowingId = (int)br.BorrowingId,
                        Title = bk.Title,
                        Author = bk.Author,
                        Isbn = bk.Isbn,
                        BorrowedOn = br.IssueDate,
                        DueDate = br.DueDate
                    }).ToList();
        }

        public List<LoanHistoryRow> GetMemberLoanHistory(int memberId)
        {
            decimal mid = memberId;
            // Returned loans for this member, joined to their book.
            //var loans = (from br in _db.Borrowings.AsNoTracking()
            //             join bk in _db.Books.AsNoTracking() on br.BookId equals bk.BookId
            //             where br.MemberId == memberId && br.Status == "RETURNED"
            var loans = (from br in _context.LibmgmtBorrowings.AsNoTracking()
                         join bk in _context.LibmgmtBooks.AsNoTracking() on br.BookId equals bk.BookId
                         where br.MemberId == mid && br.Status == "RETURNED"
                         orderby br.ReturnDate descending
                         select new
                         {
                             br.BorrowingId,
                             bk.Title,
                             bk.Author,
                             br.IssueDate,
                             br.ReturnDate,
                             br.DueDate
                         }).ToList();

            // Paid-fine totals per borrowing for this member (single grouped query).
            //var paidByBorrowing = _db.Fines.AsNoTracking()
            var paidByBorrowing = _context.LibmgmtFines.AsNoTracking()
                .Where(f => f.MemberId == mid && f.PaidDate != null)
                .GroupBy(f => f.BorrowingId)
                .Select(g => new { BorrowingId = g.Key, Total = g.Sum(x => x.Amount) })
                .ToList()
                .ToDictionary(x => x.BorrowingId, x => x.Total);

            return loans.Select(r => new LoanHistoryRow
            {
                Title = r.Title,
                Author = r.Author,
                BorrowedOn = r.IssueDate,
                ReturnedOn = r.ReturnDate ?? DateTime.MinValue,
                FinePaid = paidByBorrowing.TryGetValue(r.BorrowingId, out var t) ? (decimal)t : 0m,
                WasLate = r.ReturnDate.HasValue && r.ReturnDate.Value > r.DueDate
            }).ToList();
        }

        public List<FineRecord> GetMemberFines(int memberId)
        {
            decimal mid = memberId;
            //return (from f in _db.Fines.AsNoTracking()
            //        join br in _db.Borrowings.AsNoTracking() on f.BorrowingId equals br.BorrowingId
            //        join bk in _db.Books.AsNoTracking() on br.BookId equals bk.BookId
            //        where f.MemberId == memberId
            return (from f in _context.LibmgmtFines.AsNoTracking()
                    join br in _context.LibmgmtBorrowings.AsNoTracking() on f.BorrowingId equals br.BorrowingId
                    join bk in _context.LibmgmtBooks.AsNoTracking() on br.BookId equals bk.BookId
                    where f.MemberId == mid
                    orderby f.IssuedDate descending
                    select new FineRecord
                    {
                        Id = (int)f.FineId,
                        BookTitle = bk.Title,
                        Reason = f.Reason,
                        Amount = (decimal)f.Amount,
                        IssuedOn = f.IssuedDate,
                        PaidOn = f.PaidDate
                    }).ToList();
        }

        public List<WishlistItem> GetMemberWishlist(int memberId)
        {
            decimal mid = memberId;
            //return (from w in _db.Wishlist.AsNoTracking()
            //        join bk in _db.Books.AsNoTracking() on w.BookId equals bk.BookId
            //        where w.MemberId == memberId
            return (from w in _context.LibmgmtWishlist.AsNoTracking()
                    join bk in _context.LibmgmtBooks.AsNoTracking() on w.BookId equals bk.BookId
                    where w.MemberId == mid
                    orderby w.AddedDate descending
                    select new WishlistItem
                    {
                        Id = (int)w.WishlistId,
                        BookId = (int)w.BookId,
                        Title = bk.Title,
                        Author = bk.Author,
                        Isbn = bk.Isbn,
                        Genre = bk.Genre,
                        Available = bk.AvailableCopies > 0,
                        AddedOn = w.AddedDate
                    }).ToList();
        }

        /// <summary>BOOK_IDs already on this member's wishlist (drives the bookmark icon).</summary>
        public HashSet<int> GetWishlistedBookIds(int memberId)
        {
            decimal mid = memberId;
            //return new HashSet<int>(
            //    _db.Wishlist.AsNoTracking()
            //        .Where(w => w.MemberId == memberId)
            //        .Select(w => w.BookId));
            return new HashSet<int>(
                _context.LibmgmtWishlist.AsNoTracking()
                    .Where(w => w.MemberId == mid)
                    .Select(w => (int)w.BookId));
        }

        // =====================================================================
        // Member — reservations (only valid when a book has 0 copies left)
        // =====================================================================

        public List<ReservationItem> GetMemberReservations(int memberId)
        {
            decimal mid = memberId;
            //return (from r in _db.Reservations.AsNoTracking()
            //        join bk in _db.Books.AsNoTracking() on r.BookId equals bk.BookId
            //        where r.MemberId == memberId
            //        orderby r.RequestedDate descending
            //        select new ReservationItem
            //        {
            //            Id = r.ReservationId,
            //            BookId = r.BookId,
            //            Title = bk.Title,
            //            Author = bk.Author,
            //            Isbn = bk.Isbn,
            //            Genre = bk.Genre,
            //            RequestedOn = r.RequestedDate
            //        }).ToList();

            // LibmgmtReservations.BookId/MemberId are nullable decimal, which EF
            // Core 2.1 won't translate cleanly inside a join key — materialize
            // this member's reservations first, then join to books in memory.
            var reservations = _context.LibmgmtReservations.AsNoTracking()
                .Where(r => r.MemberId == mid)
                .ToList();
            var books = _context.LibmgmtBooks.AsNoTracking().ToDictionary(b => b.BookId);

            return reservations
                .Select(r => new { r, bk = books.TryGetValue(r.BookId ?? 0m, out var b) ? b : null })
                .Where(x => x.bk != null)
                .OrderByDescending(x => x.r.RequestedDate)
                .Select(x => new ReservationItem
                {
                    Id = (int)x.r.ReservationId,
                    BookId = (int)(x.r.BookId ?? 0m),
                    Title = x.bk.Title,
                    Author = x.bk.Author,
                    Isbn = x.bk.Isbn,
                    Genre = x.bk.Genre,
                    RequestedOn = x.r.RequestedDate ?? DateTime.MinValue
                }).ToList();
        }

        public enum ReserveResult { Ok, AlreadyReserved, BookNotFound, CopiesAvailable }

        /// <summary>Reserves a book for a member. Only allowed when the book
        /// currently has zero available copies — if any copies are free the
        /// member should simply be issued the book instead.</summary>
        public ReserveResult ReserveBook(int memberId, int bookId, out string title)
        {
            title = null;
            decimal mid = memberId;
            decimal bid = bookId;
            //using (var tx = _db.Database.BeginTransaction())
            using (var tx = _context.Database.BeginTransaction())
            {
                try
                {
                    //var book = _db.Books.FirstOrDefault(b => b.BookId == bookId);
                    var book = _context.LibmgmtBooks.FirstOrDefault(b => b.BookId == bid);
                    if (book == null) { tx.Rollback(); return ReserveResult.BookNotFound; }
                    title = book.Title;

                    if (book.AvailableCopies > 0) { tx.Rollback(); return ReserveResult.CopiesAvailable; }

                    //bool already = _db.Reservations.Any(r => r.MemberId == memberId && r.BookId == bookId);
                    bool already = _context.LibmgmtReservations.Any(r => r.MemberId == mid && r.BookId == bid);
                    if (already) { tx.Rollback(); return ReserveResult.AlreadyReserved; }

                    //var newId = NextId(_db.Reservations, r => r.ReservationId);
                    var newId = NextReservationId();
                    //_db.Reservations.Add(new Reservation
                    _context.LibmgmtReservations.Add(new LibmgmtReservations
                    {
                        ReservationId = newId,
                        MemberId = mid,
                        BookId = bid,
                        RequestedDate = DateTime.Now
                    });

                    //_db.SaveChanges();
                    _context.SaveChanges();
                    tx.Commit();
                    return ReserveResult.Ok;
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        /// <summary>Cancels (hard-deletes) a member's own pending reservation.</summary>
        public void CancelReservation(int reservationId, int memberId)
        {
            decimal mid = memberId;
            long rid = reservationId;
            //var r = _db.Reservations.FirstOrDefault(x => x.ReservationId == reservationId && x.MemberId == memberId);
            var r = _context.LibmgmtReservations.FirstOrDefault(x => x.ReservationId == rid && x.MemberId == mid);
            if (r == null) return;
            //_db.Reservations.Remove(r);
            _context.LibmgmtReservations.Remove(r);
            //_db.SaveChanges();
            _context.SaveChanges();
        }

        // =====================================================================
        // Librarian — reservations (view queue + fulfil)
        // =====================================================================

        public List<LibrarianReservationRow> GetPendingReservations()
        {
            //return (from r in _db.Reservations.AsNoTracking()
            //        join bk in _db.Books.AsNoTracking() on r.BookId equals bk.BookId
            //        join mb in _db.Members.AsNoTracking() on r.MemberId equals mb.MemberId
            //        orderby r.RequestedDate ascending
            //        select new LibrarianReservationRow
            //        {
            //            Id = r.ReservationId,
            //            BookId = r.BookId,
            //            BookTitle = bk.Title,
            //            Isbn = bk.Isbn,
            //            MemberId = mb.MemberId,
            //            MemberName = mb.FullName,
            //            MemberCode = mb.MemberCode,
            //            RequestedOn = r.RequestedDate,
            //            AvailableCopies = bk.AvailableCopies
            //        }).ToList();

            // Same nullable-FK join issue as GetMemberReservations — materialize
            // then join in memory rather than relying on EF Core 2.1 translating
            // the ?? 0m null-coalesce inside a join key.
            var reservations = _context.LibmgmtReservations.AsNoTracking().ToList();
            var books = _context.LibmgmtBooks.AsNoTracking().ToDictionary(b => b.BookId);
            var members = _context.LibmgmtMembers.AsNoTracking().ToDictionary(m => m.MemberId);

            return reservations
                .Select(r => new
                {
                    r,
                    bk = books.TryGetValue(r.BookId ?? 0m, out var b) ? b : null,
                    mb = members.TryGetValue(r.MemberId ?? 0m, out var mm) ? mm : null
                })
                .Where(x => x.bk != null && x.mb != null)
                .OrderBy(x => x.r.RequestedDate)
                .Select(x => new LibrarianReservationRow
                {
                    Id = (int)x.r.ReservationId,
                    BookId = (int)(x.r.BookId ?? 0m),
                    BookTitle = x.bk.Title,
                    Isbn = x.bk.Isbn,
                    MemberId = (int)x.mb.MemberId,
                    MemberName = x.mb.FullName,
                    MemberCode = x.mb.MemberCode,
                    RequestedOn = x.r.RequestedDate ?? DateTime.MinValue,
                    AvailableCopies = x.bk.AvailableCopies
                }).ToList();
        }

        public enum FulfillResult { Ok, NotFound, NoCopies }

        /// <summary>Issues the reserved book to the member who reserved it
        /// and deletes the reservation, in one transaction. Loan period is
        /// the standard 14 days from today, same as a normal issue.</summary>
        public FulfillResult FulfillReservation(int reservationId)
        {
            long rid = reservationId;
            //using (var tx = _db.Database.BeginTransaction())
            using (var tx = _context.Database.BeginTransaction())
            {
                try
                {
                    //var resv = _db.Reservations.FirstOrDefault(r => r.ReservationId == reservationId);
                    var resv = _context.LibmgmtReservations.FirstOrDefault(r => r.ReservationId == rid);
                    if (resv == null) { tx.Rollback(); return FulfillResult.NotFound; }

                    //var book = _db.Books.FirstOrDefault(b => b.BookId == resv.BookId);
                    var book = _context.LibmgmtBooks.FirstOrDefault(b => b.BookId == (resv.BookId ?? 0m));
                    if (book == null || book.AvailableCopies <= 0) { tx.Rollback(); return FulfillResult.NoCopies; }

                    //var newId = NextId(_db.Borrowings, b => b.BorrowingId);
                    var newId = NextBorrowingId();
                    //_db.Borrowings.Add(new Borrowing
                    _context.LibmgmtBorrowings.Add(new LibmgmtBorrowings
                    {
                        BorrowingId = newId,
                        MemberId = resv.MemberId ?? 0m,
                        BookId = resv.BookId ?? 0m,
                        IssueDate = DateTime.Now,
                        DueDate = DateTime.Today.AddDays(14),
                        Status = "ACTIVE"
                    });

                    book.AvailableCopies -= 1;
                    //_db.Reservations.Remove(resv);
                    _context.LibmgmtReservations.Remove(resv);

                    //_db.SaveChanges();
                    _context.SaveChanges();
                    tx.Commit();
                    return FulfillResult.Ok;
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        // =====================================================================
        // Librarian writes — books, damage, issue, renew, return
        // =====================================================================

        private int NextId<T>(IQueryable<T> set, System.Linq.Expressions.Expression<Func<T, int>> idSelector)
        {
            // Mirrors NVL(MAX(id),0)+1 for the app-assigned primary keys.
            var maxId = set.Max(BuildNullableSelector(idSelector));
            return (maxId ?? 0) + 1;
        }

        private static System.Linq.Expressions.Expression<Func<T, int?>> BuildNullableSelector<T>(
            System.Linq.Expressions.Expression<Func<T, int>> idSelector)
        {
            var body = System.Linq.Expressions.Expression.Convert(idSelector.Body, typeof(int?));
            return System.Linq.Expressions.Expression.Lambda<Func<T, int?>>(body, idSelector.Parameters);
        }

        // The Libmgmt* scaffold types its PKs as decimal (Oracle NUMBER with no
        // precision) except LibmgmtReservations, which is long — so the generic
        // NextId<T>(int selector) above can't be reused for them. Same
        // NVL(MAX(id),0)+1 convention, just typed per table.
        private decimal NextBookId() => (_context.LibmgmtBooks.Select(x => (decimal?)x.BookId).Max() ?? 0m) + 1m;
        private decimal NextMemberId() => (_context.LibmgmtMembers.Select(x => (decimal?)x.MemberId).Max() ?? 0m) + 1m;
        private decimal NextBorrowingId() => (_context.LibmgmtBorrowings.Select(x => (decimal?)x.BorrowingId).Max() ?? 0m) + 1m;
        private decimal NextFineId() => (_context.LibmgmtFines.Select(x => (decimal?)x.FineId).Max() ?? 0m) + 1m;
        private decimal NextDamageId() => (_context.LibmgmtBookdamages.Select(x => (decimal?)x.DamageId).Max() ?? 0m) + 1m;
        private decimal NextWishlistId() => (_context.LibmgmtWishlist.Select(x => (decimal?)x.WishlistId).Max() ?? 0m) + 1m;
        private long NextReservationId() => (_context.LibmgmtReservations.Select(x => (long?)x.ReservationId).Max() ?? 0L) + 1L;

        public string AddBook(AddBookViewModel m)
        {
            //using (var tx = _db.Database.BeginTransaction())
            using (var tx = _context.Database.BeginTransaction())
            {
                try
                {
                    //var newId = NextId(_db.Books, b => b.BookId);
                    var newId = NextBookId();
                    //_db.Books.Add(new Book
                    _context.LibmgmtBooks.Add(new LibmgmtBooks
                    {
                        BookId = newId,
                        Title = m.Title ?? "",
                        Author = m.Author ?? "",
                        Isbn = m.Isbn,
                        Genre = m.Genre,
                        Publisher = m.Publisher,
                        PublishYear = m.PublishedYear,
                        TotalCopies = m.Quantity,
                        AvailableCopies = m.Quantity,
                        AddedDate = DateTime.Now
                    });
                    //_db.SaveChanges();
                    _context.SaveChanges();
                    tx.Commit();
                    return m.Title;
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public enum AddMemberResult { Ok, UsernameTaken, EmailTaken }

        /// <summary>
        /// Creates a new member with a bcrypt-hashed password. Username is checked
        /// for uniqueness across both Librarians and Members so logins can't collide.
        /// MemberCode is auto-assigned as MEM-{id:D4} after the new id is known.
        /// </summary>
        public AddMemberResult AddMember(AddMemberViewModel m, out int newMemberId, out string memberCode)
        {
            newMemberId = 0;
            memberCode = null;

            var uname = (m.Username ?? "").Trim();
            var email = (m.Email ?? "").Trim();

            //using (var tx = _db.Database.BeginTransaction())
            using (var tx = _context.Database.BeginTransaction())
            {
                try
                {
                    //bool usernameTaken =
                    //    _db.Librarians.Any(x => x.Username == uname) ||
                    //    _db.Members.Any(x => x.Username == uname);
                    bool usernameTaken =
                        _context.LibmgmtLibrarians.Any(x => x.Username == uname) ||
                        _context.LibmgmtMembers.Any(x => x.Username == uname);
                    if (usernameTaken) { tx.Rollback(); return AddMemberResult.UsernameTaken; }

                    //bool emailTaken = _db.Members.Any(x => x.Email == email);
                    bool emailTaken = _context.LibmgmtMembers.Any(x => x.Email == email);
                    if (emailTaken) { tx.Rollback(); return AddMemberResult.EmailTaken; }

                    //var newId = NextId(_db.Members, x => x.MemberId);
                    var newId = NextMemberId();
                    var code = "MEM-" + ((int)newId).ToString("D4");
                    var hash = BCrypt.Net.BCrypt.HashPassword(m.Password);

                    //_db.Members.Add(new Member
                    _context.LibmgmtMembers.Add(new LibmgmtMembers
                    {
                        MemberId = newId,
                        Username = uname,
                        FullName = (m.FullName ?? "").Trim(),
                        MemberCode = code,
                        PasswordHash = hash,
                        Email = email,
                        Phone = string.IsNullOrWhiteSpace(m.Phone) ? null : m.Phone.Trim(),
                        IsActive = true,
                        JoinedDate = DateTime.Now
                    });

                    //_db.SaveChanges();
                    _context.SaveChanges();
                    tx.Commit();

                    newMemberId = (int)newId;
                    memberCode = code;
                    return AddMemberResult.Ok;
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public enum DeleteMemberResult { Deleted, HasActiveLoans, NotFound }

        /// <summary>
        /// Hard-deletes a member if they have no active (unreturned) loans.
        /// Cascades to fines, borrowing history, wishlist, reservations,
        /// and book-damages reported by librarians (REPORTED_BY is nullable).
        /// </summary>
        public DeleteMemberResult DeleteMember(int memberId)
        {
            decimal mid = memberId;
            //using (var tx = _db.Database.BeginTransaction())
            using (var tx = _context.Database.BeginTransaction())
            {
                try
                {
                    //var member = _db.Members.FirstOrDefault(m => m.MemberId == memberId);
                    var member = _context.LibmgmtMembers.FirstOrDefault(m => m.MemberId == mid);
                    if (member == null) { tx.Rollback(); return DeleteMemberResult.NotFound; }

                    //bool hasActive = _db.Borrowings.Any(b => b.MemberId == memberId && b.Status == "ACTIVE");
                    bool hasActive = _context.LibmgmtBorrowings.Any(b => b.MemberId == mid && b.Status == "ACTIVE");
                    if (hasActive) { tx.Rollback(); return DeleteMemberResult.HasActiveLoans; }

                    // Clear dependent rows first
                    //var fines = _db.Fines.Where(f => f.MemberId == memberId).ToList();
                    var fines = _context.LibmgmtFines.Where(f => f.MemberId == mid).ToList();
                    //if (fines.Count > 0) _db.Fines.RemoveRange(fines);
                    if (fines.Count > 0) _context.LibmgmtFines.RemoveRange(fines);

                    //var borrowings = _db.Borrowings.Where(b => b.MemberId == memberId).ToList();
                    var borrowings = _context.LibmgmtBorrowings.Where(b => b.MemberId == mid).ToList();
                    //if (borrowings.Count > 0) _db.Borrowings.RemoveRange(borrowings);
                    if (borrowings.Count > 0) _context.LibmgmtBorrowings.RemoveRange(borrowings);

                    //var wished = _db.Wishlist.Where(w => w.MemberId == memberId).ToList();
                    var wished = _context.LibmgmtWishlist.Where(w => w.MemberId == mid).ToList();
                    //if (wished.Count > 0) _db.Wishlist.RemoveRange(wished);
                    if (wished.Count > 0) _context.LibmgmtWishlist.RemoveRange(wished);

                    //var reservations = _db.Reservations.Where(r => r.MemberId == memberId).ToList();
                    var reservations = _context.LibmgmtReservations.Where(r => r.MemberId == mid).ToList();
                    //if (reservations.Count > 0) _db.Reservations.RemoveRange(reservations);
                    if (reservations.Count > 0) _context.LibmgmtReservations.RemoveRange(reservations);

                    //_db.Members.Remove(member);
                    _context.LibmgmtMembers.Remove(member);
                    //_db.SaveChanges();
                    _context.SaveChanges();
                    tx.Commit();
                    return DeleteMemberResult.Deleted;
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public void EditBook(EditBookViewModel m)
        {
            decimal bid = m.Id;
            //var book = _db.Books.FirstOrDefault(b => b.BookId == m.Id);
            var book = _context.LibmgmtBooks.FirstOrDefault(b => b.BookId == bid);
            if (book == null) return;

            book.Title = m.Title ?? "";
            book.Author = m.Author ?? "";
            book.Isbn = m.Isbn;
            book.Genre = m.Genre;
            book.TotalCopies = m.Quantity;
            // If the form supplies AvailableCopies, use it; clamp so it
            // never exceeds TotalCopies (chk_books_avail_le_tot constraint).
            book.AvailableCopies = Math.Min(m.AvailableCopies, m.Quantity);

            //_db.SaveChanges();
            _context.SaveChanges();
        }

        public enum DeleteResult { Deleted, HasActiveLoans, HasHistory, NotFound }

        public DeleteResult DeleteBook(int bookId)
        {
            decimal bid = bookId;
            //using (var tx = _db.Database.BeginTransaction())
            using (var tx = _context.Database.BeginTransaction())
            {
                try
                {
                    //var book = _db.Books.FirstOrDefault(b => b.BookId == bookId);
                    var book = _context.LibmgmtBooks.FirstOrDefault(b => b.BookId == bid);
                    if (book == null) { tx.Rollback(); return DeleteResult.NotFound; }

                    //bool hasActive = _db.Borrowings.Any(x => x.BookId == bookId && x.Status == "ACTIVE");
                    bool hasActive = _context.LibmgmtBorrowings.Any(x => x.BookId == bid && x.Status == "ACTIVE");
                    if (hasActive) { tx.Rollback(); return DeleteResult.HasActiveLoans; }

                    //bool hasAnyLoans = _db.Borrowings.Any(x => x.BookId == bookId);
                    bool hasAnyLoans = _context.LibmgmtBorrowings.Any(x => x.BookId == bid);
                    if (hasAnyLoans) { tx.Rollback(); return DeleteResult.HasHistory; }

                    // No loan history — safe to remove. Clear dependent rows first.
                    //var wished = _db.Wishlist.Where(w => w.BookId == bookId).ToList();
                    var wished = _context.LibmgmtWishlist.Where(w => w.BookId == bid).ToList();
                    //if (wished.Count > 0) _db.Wishlist.RemoveRange(wished);
                    if (wished.Count > 0) _context.LibmgmtWishlist.RemoveRange(wished);

                    //var damages = _db.BookDamages.Where(d => d.BookId == bookId).ToList();
                    var damages = _context.LibmgmtBookdamages.Where(d => d.BookId == bid).ToList();
                    //if (damages.Count > 0) _db.BookDamages.RemoveRange(damages);
                    if (damages.Count > 0) _context.LibmgmtBookdamages.RemoveRange(damages);

                    //_db.Books.Remove(book);
                    _context.LibmgmtBooks.Remove(book);
                    //_db.SaveChanges();
                    _context.SaveChanges();

                    tx.Commit();
                    return DeleteResult.Deleted;
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        /// <summary>Logs a damage report and reduces AVAILABLE_COPIES (floored at 0).</summary>
        public string ReportDamage(int bookId, int damagedCopies, string reason, int librarianId)
        {
            decimal bid = bookId;
            //using (var tx = _db.Database.BeginTransaction())
            using (var tx = _context.Database.BeginTransaction())
            {
                try
                {
                    //var book = _db.Books.FirstOrDefault(b => b.BookId == bookId);
                    var book = _context.LibmgmtBooks.FirstOrDefault(b => b.BookId == bid);
                    if (book == null) { tx.Rollback(); return null; }

                    //var newId = NextId(_db.BookDamages, d => d.DamageId);
                    var newId = NextDamageId();
                    //_db.BookDamages.Add(new BookDamage
                    _context.LibmgmtBookdamages.Add(new LibmgmtBookdamages
                    {
                        DamageId = newId,
                        BookId = bid,
                        DamagedCopies = damagedCopies,
                        Reason = reason ?? "",
                        ReportedDate = DateTime.Now,
                        ReportedBy = librarianId
                    });

                    book.AvailableCopies = Math.Max(book.AvailableCopies - damagedCopies, 0);

                    //_db.SaveChanges();
                    _context.SaveChanges();
                    tx.Commit();
                    return book.Title;
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public enum IssueResult { Ok, MemberNotFound, NoCopies, BookNotFound }

        public IssueResult IssueBook(string memberCode, int bookId, DateTime dueDate, out string title)
        {
            title = null;
            decimal bid = bookId;
            //using (var tx = _db.Database.BeginTransaction())
            using (var tx = _context.Database.BeginTransaction())
            {
                try
                {
                    var code = memberCode ?? "";
                    //var memberId = _db.Members
                    //    .Where(x => x.MemberCode == code && x.IsActive == 1)
                    //    .Select(x => (int?)x.MemberId)
                    //    .FirstOrDefault();
                    var memberId = _context.LibmgmtMembers
                        .Where(x => x.MemberCode == code && x.IsActive == true)
                        .Select(x => (decimal?)x.MemberId)
                        .FirstOrDefault();
                    if (!memberId.HasValue) { tx.Rollback(); return IssueResult.MemberNotFound; }

                    //var book = _db.Books.FirstOrDefault(b => b.BookId == bookId);
                    var book = _context.LibmgmtBooks.FirstOrDefault(b => b.BookId == bid);
                    if (book == null) { tx.Rollback(); return IssueResult.BookNotFound; }
                    title = book.Title;
                    if (book.AvailableCopies <= 0) { tx.Rollback(); return IssueResult.NoCopies; }

                    //var newId = NextId(_db.Borrowings, b => b.BorrowingId);
                    var newId = NextBorrowingId();
                    //_db.Borrowings.Add(new Borrowing
                    _context.LibmgmtBorrowings.Add(new LibmgmtBorrowings
                    {
                        BorrowingId = newId,
                        MemberId = memberId.Value,
                        BookId = bid,
                        IssueDate = DateTime.Now,
                        DueDate = dueDate.Date,
                        Status = "ACTIVE"
                    });

                    book.AvailableCopies -= 1;

                    //_db.SaveChanges();
                    _context.SaveChanges();
                    tx.Commit();
                    return IssueResult.Ok;
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// Marks a borrowing returned, restocks the copy, and settles any
        /// outstanding fine attached to that loan.
        /// </summary>
        /// 

        /// <summary>
        /// Snapshots the accrued overdue fine into a real Fine DB row so it
        /// survives renewals/returns.  Called inside an existing SaveChanges
        /// batch — does NOT call SaveChanges itself.
        /// </summary>
        private void CrystallizeFine(LibmgmtBorrowings loan, string reason)
        {
            int daysOverdue = Math.Max(0, (DateTime.Today - loan.DueDate.Date).Days);
            if (daysOverdue <= 0) return;

            decimal amount = daysOverdue * FinePerDay;

            //_db.Fines.Add(new Entities.Fine
            _context.LibmgmtFines.Add(new LibmgmtFines
            {
                FineId = NextFineId(),
                BorrowingId = loan.BorrowingId,
                MemberId = loan.MemberId,
                Reason = reason + " (" + daysOverdue + " day" + (daysOverdue == 1 ? "" : "s") + ")",
                Amount = amount, ////////removed the double casting
                IssuedDate = DateTime.Now,
                PaidDate = null
            });
        }

        /// <summary>Extends a loan by 14 days from today.  If the loan is
        /// overdue, the accrued fine is crystallized first so the debt is
        /// not lost when the due-date resets.</summary>
        public bool RenewLoan(int borrowingId)
        {
            var today = DateTime.Today;
            decimal brid = borrowingId;
            //var loan = _db.Borrowings.FirstOrDefault(x =>
            //    x.BorrowingId == borrowingId &&
            //    x.Status == "ACTIVE");              // removing && x.DueDate >= today allows overdue books to be renewed
            var loan = _context.LibmgmtBorrowings.FirstOrDefault(x =>
                x.BorrowingId == brid &&
                x.Status == "ACTIVE");

            if (loan == null) return false;

            CrystallizeFine(loan, "Overdue at renewal");

            loan.DueDate = today.AddDays(14);
            //_db.SaveChanges();
            _context.SaveChanges();
            return true;
        }
        public bool ReturnBook(int borrowingId)
        {
            decimal brid = borrowingId;
            //using (var tx = _db.Database.BeginTransaction())
            using (var tx = _context.Database.BeginTransaction())
            {
                try
                {
                    //var br = _db.Borrowings.FirstOrDefault(x => x.BorrowingId == borrowingId && x.Status == "ACTIVE");
                    var br = _context.LibmgmtBorrowings.FirstOrDefault(x => x.BorrowingId == brid && x.Status == "ACTIVE");
                    if (br == null) { tx.Rollback(); return false; }

                    br.ReturnDate = DateTime.Now;
                    br.Status = "RETURNED";

                    //var book = _db.Books.FirstOrDefault(b => b.BookId == br.BookId);
                    var book = _context.LibmgmtBooks.FirstOrDefault(b => b.BookId == br.BookId);
                    if (book != null)
                        book.AvailableCopies = Math.Min(book.AvailableCopies + 1, book.TotalCopies);

                    CrystallizeFine(br, "Overdue at return");

                    //_db.SaveChanges();
                    _context.SaveChanges();
                    tx.Commit();
                    return true;
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        // =====================================================================
        // Member writes — wishlist, renew, fines
        // =====================================================================

        public enum WishlistToggle { Added, Removed, BookNotFound }

        public WishlistToggle ToggleWishlist(int memberId, int bookId, out string title)
        {
            title = null;
            decimal mid = memberId;
            decimal bid = bookId;
            //using (var tx = _db.Database.BeginTransaction())
            using (var tx = _context.Database.BeginTransaction())
            {
                try
                {
                    //var book = _db.Books.AsNoTracking().FirstOrDefault(b => b.BookId == bookId);
                    var book = _context.LibmgmtBooks.AsNoTracking().FirstOrDefault(b => b.BookId == bid);
                    if (book == null) { tx.Rollback(); return WishlistToggle.BookNotFound; }
                    title = book.Title;

                    //var existing = _db.Wishlist.FirstOrDefault(w => w.MemberId == memberId && w.BookId == bookId);
                    var existing = _context.LibmgmtWishlist.FirstOrDefault(w => w.MemberId == mid && w.BookId == bid);
                    if (existing != null)
                    {
                        //_db.Wishlist.Remove(existing);
                        _context.LibmgmtWishlist.Remove(existing);
                        //_db.SaveChanges();
                        _context.SaveChanges();
                        tx.Commit();
                        return WishlistToggle.Removed;
                    }

                    //var newId = NextId(_db.Wishlist, w => w.WishlistId);
                    var newId = NextWishlistId();
                    //_db.Wishlist.Add(new WishlistEntry
                    _context.LibmgmtWishlist.Add(new LibmgmtWishlist
                    {
                        WishlistId = newId,
                        MemberId = mid,
                        BookId = bid,
                        AddedDate = DateTime.Now
                    });
                    //_db.SaveChanges();
                    _context.SaveChanges();
                    tx.Commit();
                    return WishlistToggle.Added;
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        /// <summary>Adds to wishlist if not already present. Returns the title (or null).</summary>
        public string AddToWishlist(int memberId, int bookId)
        {
            decimal mid = memberId;
            decimal bid = bookId;
            //using (var tx = _db.Database.BeginTransaction())
            using (var tx = _context.Database.BeginTransaction())
            {
                try
                {
                    //var book = _db.Books.AsNoTracking().FirstOrDefault(b => b.BookId == bookId);
                    var book = _context.LibmgmtBooks.AsNoTracking().FirstOrDefault(b => b.BookId == bid);
                    if (book == null) { tx.Rollback(); return null; }

                    //bool already = _db.Wishlist.Any(w => w.MemberId == memberId && w.BookId == bookId);
                    bool already = _context.LibmgmtWishlist.Any(w => w.MemberId == mid && w.BookId == bid);
                    if (!already)
                    {
                        //var newId = NextId(_db.Wishlist, w => w.WishlistId);
                        var newId = NextWishlistId();
                        //_db.Wishlist.Add(new WishlistEntry
                        _context.LibmgmtWishlist.Add(new LibmgmtWishlist
                        {
                            WishlistId = newId,
                            MemberId = mid,
                            BookId = bid,
                            AddedDate = DateTime.Now
                        });
                        //_db.SaveChanges();
                        _context.SaveChanges();
                    }

                    tx.Commit();
                    return book.Title;
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public void RemoveFromWishlist(int wishlistId, int memberId)
        {
            decimal mid = memberId;
            decimal wid = wishlistId;
            //var entry = _db.Wishlist.FirstOrDefault(w => w.WishlistId == wishlistId && w.MemberId == memberId);
            var entry = _context.LibmgmtWishlist.FirstOrDefault(w => w.WishlistId == wid && w.MemberId == mid);
            if (entry == null) return;
            //_db.Wishlist.Remove(entry);
            _context.LibmgmtWishlist.Remove(entry);
            //_db.SaveChanges();
            _context.SaveChanges();
        }


        public void PayFine(int fineId, int memberId)
        {
            decimal mid = memberId;
            decimal fid = fineId;
            //var fine = _db.Fines.FirstOrDefault(f => f.FineId == fineId && f.MemberId == memberId && f.PaidDate == null);
            var fine = _context.LibmgmtFines.FirstOrDefault(f => f.FineId == fid && f.MemberId == mid && f.PaidDate == null);
            if (fine == null) return;
            fine.PaidDate = DateTime.Now;
            //_db.SaveChanges();
            _context.SaveChanges();
        }

        public void PayAllFines(int memberId)
        {
            decimal mid = memberId;
            //var unpaid = _db.Fines.Where(f => f.MemberId == memberId && f.PaidDate == null).ToList();
            var unpaid = _context.LibmgmtFines.Where(f => f.MemberId == mid && f.PaidDate == null).ToList();
            if (unpaid.Count == 0) return;
            foreach (var f in unpaid) f.PaidDate = DateTime.Now;
            //_db.SaveChanges();
            _context.SaveChanges();
        }
    }
}