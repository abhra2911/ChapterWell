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
    /// All database access for the library app. This is now an EF Core 2.1
    /// repository over <see cref="LibraryDbContext"/> (Oracle.EntityFrameworkCore),
    /// replacing the previous raw-ADO.NET implementation. The public API is
    /// unchanged, so controllers and views need no edits.
    ///
    /// Conventions carried over from the schema:
    ///   * Primary keys are application-assigned as NVL(MAX(id),0)+1, computed
    ///     here via Max(...) + 1 inside a transaction (no identity/sequence).
    ///   * Oracle's TRUNC(SYSDATE) is mirrored with DateTime.Today (midnight);
    ///     stored dates are written date-only so day-count maths stays exact.
    ///   * Oracle LEAST/GREATEST clamping is done in C# (Math.Min/Math.Max).
    ///   * Multi-statement writes run inside a single EF Core transaction.
    /// </summary>
    public class LibraryRepository
    {
        private readonly LibraryDbContext _db;
        private const decimal FinePerDay = 5m;  // ₹5 per overdue day

        public LibraryRepository(LibraryDbContext db)
        {
            _db = db;
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

            var lib = _db.Librarians.AsNoTracking()
                .FirstOrDefault(x => x.IsActive == 1
                    && (x.Username == id || (x.Email != null && x.Email.ToLower() == idLower)));

            if (lib != null)
                return new AuthRow
                {
                    UserId = lib.LibrarianId,
                    Role = "LIBRARIAN",
                    Username = lib.Username,
                    FullName = lib.FullName,
                    Code = lib.LibrarianCode,
                    PasswordHash = lib.PasswordHash
                };

            var mem = _db.Members.AsNoTracking()
                .FirstOrDefault(x => x.IsActive == 1
                    && (x.Username == id || (x.Email != null && x.Email.ToLower() == idLower)));

            if (mem != null)
                return new AuthRow
                {
                    UserId = mem.MemberId,
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
                var l = _db.Librarians.AsNoTracking().FirstOrDefault(x => x.LibrarianId == userId);
                if (l != null)
                    return new Profile { FullName = l.FullName ?? "", Email = l.Email ?? "", Phone = l.Phone ?? "", Code = l.LibrarianCode ?? "" };
            }
            else
            {
                var m = _db.Members.AsNoTracking().FirstOrDefault(x => x.MemberId == userId);
                if (m != null)
                    return new Profile { FullName = m.FullName ?? "", Email = m.Email ?? "", Phone = m.Phone ?? "", Code = m.MemberCode ?? "" };
            }
            return new Profile { FullName = "", Email = "", Phone = "", Code = "" };
        }

        public void UpdateProfile(string role, int userId, string name, string email, string phone)
        {
            if (role == "LIBRARIAN")
            {
                var l = _db.Librarians.FirstOrDefault(x => x.LibrarianId == userId);
                if (l == null) return;
                l.FullName = name ?? "";
                l.Email = email ?? "";
                l.Phone = phone;
            }
            else
            {
                var m = _db.Members.FirstOrDefault(x => x.MemberId == userId);
                if (m == null) return;
                m.FullName = name ?? "";
                m.Email = email ?? "";
                m.Phone = phone;
            }
            _db.SaveChanges();
        }

        public string GetPasswordHash(string role, int userId)
        {
            if (role == "LIBRARIAN")
                return _db.Librarians.AsNoTracking()
                    .Where(x => x.LibrarianId == userId).Select(x => x.PasswordHash).FirstOrDefault() ?? "";

            return _db.Members.AsNoTracking()
                .Where(x => x.MemberId == userId).Select(x => x.PasswordHash).FirstOrDefault() ?? "";
        }

        public void UpdatePasswordHash(string role, int userId, string newHash)
        {
            if (role == "LIBRARIAN")
            {
                var l = _db.Librarians.FirstOrDefault(x => x.LibrarianId == userId);
                if (l == null) return;
                l.PasswordHash = newHash;
            }
            else
            {
                var m = _db.Members.FirstOrDefault(x => x.MemberId == userId);
                if (m == null) return;
                m.PasswordHash = newHash;
            }
            _db.SaveChanges();
        }

        // =====================================================================
        // Public site stats (Home/Index "Our Library at a Glance")
        // =====================================================================

        public HomeStatsViewModel GetHomeStats()
        {
            // Total physical inventory — matches the "Books" headline figure
            // better than a distinct-title count.
            var bookCopies = _db.Books.AsNoTracking()
                .Select(b => (int?)b.TotalCopies)
                .Sum() ?? 0;

            var activeMembers = _db.Members.AsNoTracking()
                .Count(m => m.IsActive == 1);

            var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var loansThisMonth = _db.Borrowings.AsNoTracking()
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
            return _db.Books.AsNoTracking()
                .OrderBy(b => b.Title)
                .Select(b => new CatalogBook
                {
                    Id = b.BookId,
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
            var libNames = _db.Librarians.AsNoTracking()
                .ToDictionary(l => l.LibrarianId, l => l.FullName);

            var rows = (from d in _db.BookDamages.AsNoTracking()
                        join b in _db.Books.AsNoTracking() on d.BookId equals b.BookId
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
                Id = r.DamageId,
                BookId = r.BookId,
                Title = r.Title,
                Author = r.Author,
                Isbn = r.Isbn,
                DamagedCopies = r.DamagedCopies,
                Reason = r.Reason,
                ReportedOn = r.ReportedDate,
                ReportedBy = (r.ReportedBy.HasValue && libNames.TryGetValue(r.ReportedBy.Value, out var n) && !string.IsNullOrEmpty(n)) ? n : "-"
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
            m.TotalMembers = _db.Members.Count(x => x.IsActive == 1);
            m.TotalTitles = _db.Books.Count();
            m.TotalCopiesAvailable = _db.Books.Select(b => (int?)b.AvailableCopies).Sum() ?? 0;
            m.BooksBorrowed = _db.Borrowings.Count(x => x.Status == "ACTIVE");
            m.OverdueCount = _db.Borrowings.Count(x => x.Status == "ACTIVE" && x.DueDate < today);
            m.NewMembersThisMonth = _db.Members.Count(x => x.JoinedDate >= monthStart);
            m.TotalFinesDue = _db.Fines.Where(f => f.PaidDate == null).Select(f => (decimal?)f.Amount).Sum() ?? 0m;
            m.CopiesDamaged = _db.BookDamages.Select(d => (int?)d.DamagedCopies).Sum() ?? 0;

            m.CopiesAvailable = m.TotalCopiesAvailable;
            m.CopiesBorrowed = m.BooksBorrowed;
            m.CopiesOverdue = m.OverdueCount;

            // Borrowings per month (last 6 months) — bucket in memory.
            var borrowDates = _db.Borrowings.AsNoTracking()
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
            var fineRows = _db.Fines.AsNoTracking()
                .Where(f => f.IssuedDate >= windowStart)
                .Select(f => new { f.IssuedDate, f.Amount })
                .ToList();
            m.FinesPerMonth = fineRows
                .GroupBy(f => new DateTime(f.IssuedDate.Year, f.IssuedDate.Month, 1))
                .OrderBy(g => g.Key)
                .Select(g => new MonthlyAmount
                {
                    Month = g.Key.ToString("MMM yyyy", CultureInfo.InvariantCulture),
                    Amount = g.Sum(x => x.Amount)
                })
                .ToList();

            // Top 5 borrowed titles (all-time borrow count, including zero-borrow titles).
            var countsByBook = _db.Borrowings.AsNoTracking()
                .GroupBy(x => x.BookId)
                .Select(g => new { BookId = g.Key, C = g.Count() })
                .ToList()
                .ToDictionary(x => x.BookId, x => x.C);

            var titles = _db.Books.AsNoTracking()
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
            m.OverdueBorrowings = (from br in _db.Borrowings.AsNoTracking()
                                   join mem in _db.Members.AsNoTracking() on br.MemberId equals mem.MemberId
                                   join bk in _db.Books.AsNoTracking() on br.BookId equals bk.BookId
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
            return (from br in _db.Borrowings.AsNoTracking()
                    join mem in _db.Members.AsNoTracking() on br.MemberId equals mem.MemberId
                    join bk in _db.Books.AsNoTracking() on br.BookId equals bk.BookId
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

        public List<ActiveBorrowingRow> GetActiveBorrowings()
        {
            return (from br in _db.Borrowings.AsNoTracking()
                    join mem in _db.Members.AsNoTracking() on br.MemberId equals mem.MemberId
                    join bk in _db.Books.AsNoTracking() on br.BookId equals bk.BookId
                    where br.Status == "ACTIVE"
                    orderby br.DueDate
                    select new ActiveBorrowingRow
                    {
                        BorrowingId = br.BorrowingId,
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
            var m = new MemberDashboardViewModel { MemberName = memberName };

            m.ActiveLoans = (from br in _db.Borrowings.AsNoTracking()
                             join bk in _db.Books.AsNoTracking() on br.BookId equals bk.BookId
                             where br.MemberId == memberId && br.Status == "ACTIVE"
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
            m.FineDue += _db.Fines
                .Where(f => f.MemberId == memberId && f.PaidDate == null)
                .Select(f => (decimal?)f.Amount).Sum() ?? 0m;

            // Top 10 books by all-time borrow count (including zero-borrow titles).
            var countsByBook = _db.Borrowings.AsNoTracking()
                .GroupBy(x => x.BookId)
                .Select(g => new { BookId = g.Key, C = g.Count() })
                .ToList()
                .ToDictionary(x => x.BookId, x => x.C);

            var books = _db.Books.AsNoTracking()
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
            return (from br in _db.Borrowings.AsNoTracking()
                    join bk in _db.Books.AsNoTracking() on br.BookId equals bk.BookId
                    where br.MemberId == memberId && br.Status == "ACTIVE"
                    orderby br.DueDate
                    select new DetailedLoan
                    {
                        BorrowingId = br.BorrowingId,
                        Title = bk.Title,
                        Author = bk.Author,
                        Isbn = bk.Isbn,
                        BorrowedOn = br.IssueDate,
                        DueDate = br.DueDate
                    }).ToList();
        }

        public List<LoanHistoryRow> GetMemberLoanHistory(int memberId)
        {
            // Returned loans for this member, joined to their book.
            var loans = (from br in _db.Borrowings.AsNoTracking()
                         join bk in _db.Books.AsNoTracking() on br.BookId equals bk.BookId
                         where br.MemberId == memberId && br.Status == "RETURNED"
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
            var paidByBorrowing = _db.Fines.AsNoTracking()
                .Where(f => f.MemberId == memberId && f.PaidDate != null)
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
                FinePaid = paidByBorrowing.TryGetValue(r.BorrowingId, out var t) ? t : 0m,
                WasLate = r.ReturnDate.HasValue && r.ReturnDate.Value > r.DueDate
            }).ToList();
        }

        public List<FineRecord> GetMemberFines(int memberId)
        {
            return (from f in _db.Fines.AsNoTracking()
                    join br in _db.Borrowings.AsNoTracking() on f.BorrowingId equals br.BorrowingId
                    join bk in _db.Books.AsNoTracking() on br.BookId equals bk.BookId
                    where f.MemberId == memberId
                    orderby f.IssuedDate descending
                    select new FineRecord
                    {
                        Id = f.FineId,
                        BookTitle = bk.Title,
                        Reason = f.Reason,
                        Amount = f.Amount,
                        IssuedOn = f.IssuedDate,
                        PaidOn = f.PaidDate
                    }).ToList();
        }

        public List<WishlistItem> GetMemberWishlist(int memberId)
        {
            return (from w in _db.Wishlist.AsNoTracking()
                    join bk in _db.Books.AsNoTracking() on w.BookId equals bk.BookId
                    where w.MemberId == memberId
                    orderby w.AddedDate descending
                    select new WishlistItem
                    {
                        Id = w.WishlistId,
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
            return new HashSet<int>(
                _db.Wishlist.AsNoTracking()
                    .Where(w => w.MemberId == memberId)
                    .Select(w => w.BookId));
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

        public string AddBook(AddBookViewModel m)
        {
            using (var tx = _db.Database.BeginTransaction())
            {
                try
                {
                    var newId = NextId(_db.Books, b => b.BookId);
                    _db.Books.Add(new Book
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
                    _db.SaveChanges();
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

            using (var tx = _db.Database.BeginTransaction())
            {
                try
                {
                    bool usernameTaken =
                        _db.Librarians.Any(x => x.Username == uname) ||
                        _db.Members.Any(x => x.Username == uname);
                    if (usernameTaken) { tx.Rollback(); return AddMemberResult.UsernameTaken; }

                    bool emailTaken = _db.Members.Any(x => x.Email == email);
                    if (emailTaken) { tx.Rollback(); return AddMemberResult.EmailTaken; }

                    var newId = NextId(_db.Members, x => x.MemberId);
                    var code = "MEM-" + newId.ToString("D4");
                    var hash = BCrypt.Net.BCrypt.HashPassword(m.Password);

                    _db.Members.Add(new Member
                    {
                        MemberId = newId,
                        Username = uname,
                        FullName = (m.FullName ?? "").Trim(),
                        MemberCode = code,
                        PasswordHash = hash,
                        Email = email,
                        Phone = string.IsNullOrWhiteSpace(m.Phone) ? null : m.Phone.Trim(),
                        IsActive = 1,
                        JoinedDate = DateTime.Now
                    });

                    _db.SaveChanges();
                    tx.Commit();

                    newMemberId = newId;
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

        public void EditBook(EditBookViewModel m)
        {
            var book = _db.Books.FirstOrDefault(b => b.BookId == m.Id);
            if (book == null) return;

            book.Title = m.Title ?? "";
            book.Author = m.Author ?? "";
            book.Isbn = m.Isbn;
            book.Genre = m.Genre;
            book.TotalCopies = m.Quantity;
            // Clamp so AVAILABLE_COPIES never exceeds the new total
            // (chk_books_avail_le_tot would otherwise reject it).
            book.AvailableCopies = Math.Min(book.AvailableCopies, m.Quantity);

            _db.SaveChanges();
        }

        public enum DeleteResult { Deleted, HasActiveLoans, HasHistory, NotFound }

        public DeleteResult DeleteBook(int bookId)
        {
            using (var tx = _db.Database.BeginTransaction())
            {
                try
                {
                    var book = _db.Books.FirstOrDefault(b => b.BookId == bookId);
                    if (book == null) { tx.Rollback(); return DeleteResult.NotFound; }

                    bool hasActive = _db.Borrowings.Any(x => x.BookId == bookId && x.Status == "ACTIVE");
                    if (hasActive) { tx.Rollback(); return DeleteResult.HasActiveLoans; }

                    bool hasAnyLoans = _db.Borrowings.Any(x => x.BookId == bookId);
                    if (hasAnyLoans) { tx.Rollback(); return DeleteResult.HasHistory; }

                    // No loan history — safe to remove. Clear dependent rows first.
                    var wished = _db.Wishlist.Where(w => w.BookId == bookId).ToList();
                    if (wished.Count > 0) _db.Wishlist.RemoveRange(wished);

                    var damages = _db.BookDamages.Where(d => d.BookId == bookId).ToList();
                    if (damages.Count > 0) _db.BookDamages.RemoveRange(damages);

                    _db.Books.Remove(book);
                    _db.SaveChanges();

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
            using (var tx = _db.Database.BeginTransaction())
            {
                try
                {
                    var book = _db.Books.FirstOrDefault(b => b.BookId == bookId);
                    if (book == null) { tx.Rollback(); return null; }

                    var newId = NextId(_db.BookDamages, d => d.DamageId);
                    _db.BookDamages.Add(new BookDamage
                    {
                        DamageId = newId,
                        BookId = bookId,
                        DamagedCopies = damagedCopies,
                        Reason = reason ?? "",
                        ReportedDate = DateTime.Now,
                        ReportedBy = librarianId
                    });

                    book.AvailableCopies = Math.Max(book.AvailableCopies - damagedCopies, 0);

                    _db.SaveChanges();
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
            using (var tx = _db.Database.BeginTransaction())
            {
                try
                {
                    var code = memberCode ?? "";
                    var memberId = _db.Members
                        .Where(x => x.MemberCode == code && x.IsActive == 1)
                        .Select(x => (int?)x.MemberId)
                        .FirstOrDefault();
                    if (!memberId.HasValue) { tx.Rollback(); return IssueResult.MemberNotFound; }

                    var book = _db.Books.FirstOrDefault(b => b.BookId == bookId);
                    if (book == null) { tx.Rollback(); return IssueResult.BookNotFound; }
                    title = book.Title;
                    if (book.AvailableCopies <= 0) { tx.Rollback(); return IssueResult.NoCopies; }

                    var newId = NextId(_db.Borrowings, b => b.BorrowingId);
                    _db.Borrowings.Add(new Borrowing
                    {
                        BorrowingId = newId,
                        MemberId = memberId.Value,
                        BookId = bookId,
                        IssueDate = DateTime.Now,
                        DueDate = dueDate.Date,
                        Status = "ACTIVE"
                    });

                    book.AvailableCopies -= 1;

                    _db.SaveChanges();
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
        private void CrystallizeFine(Borrowing loan, string reason)
        {
            int daysOverdue = Math.Max(0, (DateTime.Today - loan.DueDate.Date).Days);
            if (daysOverdue <= 0) return;

            decimal amount = daysOverdue * FinePerDay;

            _db.Fines.Add(new Entities.Fine
            {
                FineId = NextId(_db.Fines, f => f.FineId),
                BorrowingId = loan.BorrowingId,
                MemberId = loan.MemberId,
                Reason = reason + " (" + daysOverdue + " day" + (daysOverdue == 1 ? "" : "s") + ")",
                Amount = amount,
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
            var loan = _db.Borrowings.FirstOrDefault(x =>
                x.BorrowingId == borrowingId &&
                x.Status == "ACTIVE");              // removing && x.DueDate >= today allows overdue books to be renewed

            if (loan == null) return false;

            CrystallizeFine(loan, "Overdue at renewal");

            loan.DueDate = today.AddDays(14);
            _db.SaveChanges();
            return true;
        }
        public bool ReturnBook(int borrowingId)
        {
            using (var tx = _db.Database.BeginTransaction())
            {
                try
                {
                    var br = _db.Borrowings.FirstOrDefault(x => x.BorrowingId == borrowingId && x.Status == "ACTIVE");
                    if (br == null) { tx.Rollback(); return false; }

                    br.ReturnDate = DateTime.Now;
                    br.Status = "RETURNED";

                    var book = _db.Books.FirstOrDefault(b => b.BookId == br.BookId);
                    if (book != null)
                        book.AvailableCopies = Math.Min(book.AvailableCopies + 1, book.TotalCopies);

                    CrystallizeFine(br, "Overdue at return");

                    _db.SaveChanges();
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
            using (var tx = _db.Database.BeginTransaction())
            {
                try
                {
                    var book = _db.Books.AsNoTracking().FirstOrDefault(b => b.BookId == bookId);
                    if (book == null) { tx.Rollback(); return WishlistToggle.BookNotFound; }
                    title = book.Title;

                    var existing = _db.Wishlist.FirstOrDefault(w => w.MemberId == memberId && w.BookId == bookId);
                    if (existing != null)
                    {
                        _db.Wishlist.Remove(existing);
                        _db.SaveChanges();
                        tx.Commit();
                        return WishlistToggle.Removed;
                    }

                    var newId = NextId(_db.Wishlist, w => w.WishlistId);
                    _db.Wishlist.Add(new WishlistEntry
                    {
                        WishlistId = newId,
                        MemberId = memberId,
                        BookId = bookId,
                        AddedDate = DateTime.Now
                    });
                    _db.SaveChanges();
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
            using (var tx = _db.Database.BeginTransaction())
            {
                try
                {
                    var book = _db.Books.AsNoTracking().FirstOrDefault(b => b.BookId == bookId);
                    if (book == null) { tx.Rollback(); return null; }

                    bool already = _db.Wishlist.Any(w => w.MemberId == memberId && w.BookId == bookId);
                    if (!already)
                    {
                        var newId = NextId(_db.Wishlist, w => w.WishlistId);
                        _db.Wishlist.Add(new WishlistEntry
                        {
                            WishlistId = newId,
                            MemberId = memberId,
                            BookId = bookId,
                            AddedDate = DateTime.Now
                        });
                        _db.SaveChanges();
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
            var entry = _db.Wishlist.FirstOrDefault(w => w.WishlistId == wishlistId && w.MemberId == memberId);
            if (entry == null) return;
            _db.Wishlist.Remove(entry);
            _db.SaveChanges();
        }


        public void PayFine(int fineId, int memberId)
        {
            var fine = _db.Fines.FirstOrDefault(f => f.FineId == fineId && f.MemberId == memberId && f.PaidDate == null);
            if (fine == null) return;
            fine.PaidDate = DateTime.Now;
            _db.SaveChanges();
        }

        public void PayAllFines(int memberId)
        {
            var unpaid = _db.Fines.Where(f => f.MemberId == memberId && f.PaidDate == null).ToList();
            if (unpaid.Count == 0) return;
            foreach (var f in unpaid) f.PaidDate = DateTime.Now;
            _db.SaveChanges();
        }
    }
}