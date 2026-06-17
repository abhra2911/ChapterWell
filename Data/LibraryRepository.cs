using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using Lib_Mgmt.Models;

namespace Lib_Mgmt.Data
{
    /// <summary>
    /// All database access for the library app. Uses the Oracle managed driver
    /// (Oracle.ManagedDataAccess.Core) with plain ADO.NET — no EF Core, which
    /// keeps it compatible with netcoreapp2.1.
    ///
    /// Conventions:
    ///   * Every command sets BindByName = true so :named parameters work.
    ///   * New primary keys are computed as NVL(MAX(id),0)+1 inside a
    ///     transaction (the schema has no sequences/identity columns).
    ///   * Multi-statement writes run inside a single OracleTransaction.
    /// </summary>
    public class LibraryRepository
    {
        private readonly string _connString;

        public LibraryRepository(IConfiguration config)
        {
            _connString = config.GetConnectionString("LibraryDb");
        }

        private OracleConnection Open()
        {
            var c = new OracleConnection(_connString);
            c.Open();
            return c;
        }

        private static OracleCommand Cmd(OracleConnection c, string sql)
        {
            var cmd = c.CreateCommand();
            cmd.CommandText = sql;
            cmd.BindByName = true;
            return cmd;
        }

        // Small reader helpers (Oracle returns DBNull for NULLs).
        private static string S(IDataRecord r, int i) => r.IsDBNull(i) ? "" : r.GetString(i);
        private static int I(IDataRecord r, int i) => r.IsDBNull(i) ? 0 : Convert.ToInt32(r.GetValue(i));
        private static int? NI(IDataRecord r, int i) => r.IsDBNull(i) ? (int?)null : Convert.ToInt32(r.GetValue(i));
        private static decimal D(IDataRecord r, int i) => r.IsDBNull(i) ? 0m : Convert.ToDecimal(r.GetValue(i));
        private static DateTime DT(IDataRecord r, int i) => r.IsDBNull(i) ? DateTime.MinValue : r.GetDateTime(i);
        private static DateTime? NDT(IDataRecord r, int i) => r.IsDBNull(i) ? (DateTime?)null : r.GetDateTime(i);

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
        /// Looks up an active user by username across both tables. Returns null
        /// if no active row matches. The caller verifies the bcrypt hash.
        /// </summary>
        public AuthRow FindUserByUsername(string username)
        {
            const string sql = @"
                SELECT LIBRARIAN_ID AS USER_ID, 'LIBRARIAN' AS ROLE, USERNAME,
                       FULL_NAME, LIBRARIAN_CODE AS CODE, PASSWORD_HASH
                  FROM LibMgmt_Librarians
                 WHERE USERNAME = :u AND IS_ACTIVE = 1
                UNION ALL
                SELECT MEMBER_ID, 'MEMBER', USERNAME,
                       FULL_NAME, MEMBER_CODE, PASSWORD_HASH
                  FROM LibMgmt_Members
                 WHERE USERNAME = :u AND IS_ACTIVE = 1";

            using (var c = Open())
            using (var cmd = Cmd(c, sql))
            {
                cmd.Parameters.Add(new OracleParameter("u", username ?? ""));
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        return new AuthRow
                        {
                            UserId = I(r, 0),
                            Role = S(r, 1),
                            Username = S(r, 2),
                            FullName = S(r, 3),
                            Code = S(r, 4),
                            PasswordHash = S(r, 5)
                        };
                    }
                }
            }
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
            string sql = role == "LIBRARIAN"
                ? "SELECT FULL_NAME, EMAIL, PHONE, LIBRARIAN_CODE FROM LibMgmt_Librarians WHERE LIBRARIAN_ID = :id"
                : "SELECT FULL_NAME, EMAIL, PHONE, MEMBER_CODE    FROM LibMgmt_Members    WHERE MEMBER_ID    = :id";

            using (var c = Open())
            using (var cmd = Cmd(c, sql))
            {
                cmd.Parameters.Add(new OracleParameter("id", userId));
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                        return new Profile { FullName = S(r, 0), Email = S(r, 1), Phone = S(r, 2), Code = S(r, 3) };
                }
            }
            return new Profile { FullName = "", Email = "", Phone = "", Code = "" };
        }

        public void UpdateProfile(string role, int userId, string name, string email, string phone)
        {
            string sql = role == "LIBRARIAN"
                ? "UPDATE LibMgmt_Librarians SET FULL_NAME = :n, EMAIL = :e, PHONE = :p WHERE LIBRARIAN_ID = :id"
                : "UPDATE LibMgmt_Members    SET FULL_NAME = :n, EMAIL = :e, PHONE = :p WHERE MEMBER_ID    = :id";

            using (var c = Open())
            using (var cmd = Cmd(c, sql))
            {
                cmd.Parameters.Add(new OracleParameter("n", name ?? ""));
                cmd.Parameters.Add(new OracleParameter("e", email ?? ""));
                cmd.Parameters.Add(new OracleParameter("p", (object)phone ?? DBNull.Value));
                cmd.Parameters.Add(new OracleParameter("id", userId));
                cmd.ExecuteNonQuery();
            }
        }

        public string GetPasswordHash(string role, int userId)
        {
            string sql = role == "LIBRARIAN"
                ? "SELECT PASSWORD_HASH FROM LibMgmt_Librarians WHERE LIBRARIAN_ID = :id"
                : "SELECT PASSWORD_HASH FROM LibMgmt_Members    WHERE MEMBER_ID    = :id";

            using (var c = Open())
            using (var cmd = Cmd(c, sql))
            {
                cmd.Parameters.Add(new OracleParameter("id", userId));
                var o = cmd.ExecuteScalar();
                return o == null || o == DBNull.Value ? "" : Convert.ToString(o);
            }
        }

        public void UpdatePasswordHash(string role, int userId, string newHash)
        {
            string sql = role == "LIBRARIAN"
                ? "UPDATE LibMgmt_Librarians SET PASSWORD_HASH = :h WHERE LIBRARIAN_ID = :id"
                : "UPDATE LibMgmt_Members    SET PASSWORD_HASH = :h WHERE MEMBER_ID    = :id";

            using (var c = Open())
            using (var cmd = Cmd(c, sql))
            {
                cmd.Parameters.Add(new OracleParameter("h", newHash));
                cmd.Parameters.Add(new OracleParameter("id", userId));
                cmd.ExecuteNonQuery();
            }
        }

        // =====================================================================
        // Catalog (shared by librarian + member Books tabs)
        // =====================================================================

        public List<CatalogBook> GetCatalog()
        {
            const string sql = @"
                SELECT BOOK_ID, TITLE, AUTHOR, GENRE, ISBN, PUBLISHER,
                       PUBLISH_YEAR, TOTAL_COPIES, AVAILABLE_COPIES
                  FROM LibMgmt_Books
                 ORDER BY TITLE";

            var list = new List<CatalogBook>();
            using (var c = Open())
            using (var cmd = Cmd(c, sql))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    list.Add(new CatalogBook
                    {
                        Id = I(r, 0),
                        Title = S(r, 1),
                        Author = S(r, 2),
                        Genre = S(r, 3),
                        Isbn = S(r, 4),
                        Publisher = S(r, 5),
                        PublishedYear = NI(r, 6),
                        Quantity = I(r, 7),
                        AvailableCopies = I(r, 8)
                    });
                }
            }
            return list;
        }

        public List<DamagedBookEntry> GetDamagedBooks()
        {
            const string sql = @"
                SELECT d.DAMAGE_ID, d.BOOK_ID, b.TITLE, b.AUTHOR, b.ISBN,
                       d.DAMAGED_COPIES, d.REASON, d.REPORTED_DATE,
                       NVL(l.FULL_NAME, '-') AS REPORTED_BY
                  FROM LibMgmt_BookDamages d
                  JOIN LibMgmt_Books b      ON b.BOOK_ID = d.BOOK_ID
                  LEFT JOIN LibMgmt_Librarians l ON l.LIBRARIAN_ID = d.REPORTED_BY
                 ORDER BY d.REPORTED_DATE DESC";

            var list = new List<DamagedBookEntry>();
            using (var c = Open())
            using (var cmd = Cmd(c, sql))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    list.Add(new DamagedBookEntry
                    {
                        Id = I(r, 0),
                        BookId = I(r, 1),
                        Title = S(r, 2),
                        Author = S(r, 3),
                        Isbn = S(r, 4),
                        DamagedCopies = I(r, 5),
                        Reason = S(r, 6),
                        ReportedOn = DT(r, 7),
                        ReportedBy = S(r, 8)
                    });
                }
            }
            return list;
        }

        // =====================================================================
        // Librarian dashboard
        // =====================================================================

        public LibrarianDashboardViewModel GetLibrarianDashboard()
        {
            var m = new LibrarianDashboardViewModel();

            using (var c = Open())
            {
                // Scalar headline numbers in one round trip.
                const string stats = @"
                    SELECT
                        (SELECT COUNT(*) FROM LibMgmt_Members WHERE IS_ACTIVE = 1)                          AS TOTAL_MEMBERS,
                        (SELECT COUNT(*) FROM LibMgmt_Books)                                                AS TOTAL_TITLES,
                        (SELECT NVL(SUM(AVAILABLE_COPIES),0) FROM LibMgmt_Books)                            AS COPIES_AVAIL,
                        (SELECT COUNT(*) FROM LibMgmt_Borrowings WHERE STATUS = 'ACTIVE')                   AS BORROWED,
                        (SELECT COUNT(*) FROM LibMgmt_Borrowings WHERE STATUS = 'ACTIVE' AND DUE_DATE < TRUNC(SYSDATE)) AS OVERDUE,
                        (SELECT COUNT(*) FROM LibMgmt_Members WHERE JOINED_DATE >= TRUNC(SYSDATE,'MM'))     AS NEW_MEMBERS,
                        (SELECT NVL(SUM(AMOUNT),0) FROM LibMgmt_Fines WHERE PAID_DATE IS NULL)              AS FINES_DUE,
                        (SELECT NVL(SUM(DAMAGED_COPIES),0) FROM LibMgmt_BookDamages)                        AS DAMAGED
                    FROM dual";

                using (var cmd = Cmd(c, stats))
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        m.TotalMembers = I(r, 0);
                        m.TotalTitles = I(r, 1);
                        m.TotalCopiesAvailable = I(r, 2);
                        m.BooksBorrowed = I(r, 3);
                        m.OverdueCount = I(r, 4);
                        m.NewMembersThisMonth = I(r, 5);
                        m.TotalFinesDue = D(r, 6);
                        m.CopiesDamaged = I(r, 7);

                        m.CopiesAvailable = m.TotalCopiesAvailable;
                        m.CopiesBorrowed = m.BooksBorrowed;
                        m.CopiesOverdue = m.OverdueCount;
                    }
                }

                // Borrowings per month (last 6 months).
                const string perMonth = @"
                    SELECT TO_CHAR(TRUNC(ISSUE_DATE,'MM'),'Mon YYYY') AS M,
                           TRUNC(ISSUE_DATE,'MM') AS MS,
                           COUNT(*) AS C
                      FROM LibMgmt_Borrowings
                     WHERE ISSUE_DATE >= ADD_MONTHS(TRUNC(SYSDATE,'MM'), -5)
                     GROUP BY TRUNC(ISSUE_DATE,'MM')
                     ORDER BY MS";
                using (var cmd = Cmd(c, perMonth))
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        m.BorrowingsPerMonth.Add(new MonthlyCount { Month = S(r, 0), Count = I(r, 2) });

                // Fines per month (last 6 months).
                const string finesMonth = @"
                    SELECT TO_CHAR(TRUNC(ISSUED_DATE,'MM'),'Mon YYYY') AS M,
                           TRUNC(ISSUED_DATE,'MM') AS MS,
                           NVL(SUM(AMOUNT),0) AS A
                      FROM LibMgmt_Fines
                     WHERE ISSUED_DATE >= ADD_MONTHS(TRUNC(SYSDATE,'MM'), -5)
                     GROUP BY TRUNC(ISSUED_DATE,'MM')
                     ORDER BY MS";
                using (var cmd = Cmd(c, finesMonth))
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        m.FinesPerMonth.Add(new MonthlyAmount { Month = S(r, 0), Amount = D(r, 2) });

                // Top 5 borrowed titles (all-time borrow count).
                const string top = @"
                    SELECT b.TITLE, COUNT(br.BORROWING_ID) AS C
                      FROM LibMgmt_Books b
                      LEFT JOIN LibMgmt_Borrowings br ON br.BOOK_ID = b.BOOK_ID
                     GROUP BY b.TITLE
                     ORDER BY C DESC, b.TITLE
                     FETCH FIRST 5 ROWS ONLY";
                using (var cmd = Cmd(c, top))
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        m.TopBorrowedBooks.Add(new BookBorrowCount { Title = S(r, 0), Count = I(r, 1) });

                // Currently overdue loans.
                const string overdue = @"
                    SELECT m.FULL_NAME, m.MEMBER_CODE, b.TITLE, br.ISSUE_DATE, br.DUE_DATE
                      FROM LibMgmt_Borrowings br
                      JOIN LibMgmt_Members m ON m.MEMBER_ID = br.MEMBER_ID
                      JOIN LibMgmt_Books   b ON b.BOOK_ID   = br.BOOK_ID
                     WHERE br.STATUS = 'ACTIVE' AND br.DUE_DATE < TRUNC(SYSDATE)
                     ORDER BY br.DUE_DATE";
                using (var cmd = Cmd(c, overdue))
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        m.OverdueBorrowings.Add(new OverdueBorrowing
                        {
                            MemberName = S(r, 0),
                            MemberId = S(r, 1),
                            BookTitle = S(r, 2),
                            BorrowedOn = DT(r, 3),
                            DueDate = DT(r, 4)
                        });
            }
            return m;
        }

        // =====================================================================
        // Librarian — Members + Borrowings tabs
        // =====================================================================

        public List<MemberBorrowingRow> GetMemberBorrowings()
        {
            const string sql = @"
                SELECT m.FULL_NAME, m.MEMBER_CODE, m.EMAIL, b.TITLE,
                       br.ISSUE_DATE, br.DUE_DATE
                  FROM LibMgmt_Borrowings br
                  JOIN LibMgmt_Members m ON m.MEMBER_ID = br.MEMBER_ID
                  JOIN LibMgmt_Books   b ON b.BOOK_ID   = br.BOOK_ID
                 WHERE br.STATUS = 'ACTIVE'
                 ORDER BY br.DUE_DATE";

            var list = new List<MemberBorrowingRow>();
            using (var c = Open())
            using (var cmd = Cmd(c, sql))
            using (var r = cmd.ExecuteReader())
                while (r.Read())
                    list.Add(new MemberBorrowingRow
                    {
                        MemberName = S(r, 0),
                        MemberId = S(r, 1),
                        Email = S(r, 2),
                        BookTitle = S(r, 3),
                        BorrowedOn = DT(r, 4),
                        DueDate = DT(r, 5)
                    });
            return list;
        }

        public List<ActiveBorrowingRow> GetActiveBorrowings()
        {
            const string sql = @"
                SELECT br.BORROWING_ID, m.FULL_NAME, m.MEMBER_CODE,
                       b.TITLE, b.ISBN, br.ISSUE_DATE, br.DUE_DATE
                  FROM LibMgmt_Borrowings br
                  JOIN LibMgmt_Members m ON m.MEMBER_ID = br.MEMBER_ID
                  JOIN LibMgmt_Books   b ON b.BOOK_ID   = br.BOOK_ID
                 WHERE br.STATUS = 'ACTIVE'
                 ORDER BY br.DUE_DATE";

            var list = new List<ActiveBorrowingRow>();
            using (var c = Open())
            using (var cmd = Cmd(c, sql))
            using (var r = cmd.ExecuteReader())
                while (r.Read())
                    list.Add(new ActiveBorrowingRow
                    {
                        BorrowingId = I(r, 0),
                        MemberName = S(r, 1),
                        MemberId = S(r, 2),
                        BookTitle = S(r, 3),
                        Isbn = S(r, 4),
                        BorrowedOn = DT(r, 5),
                        DueDate = DT(r, 6)
                    });
            return list;
        }

        // =====================================================================
        // Member dashboard + tabs
        // =====================================================================

        public MemberDashboardViewModel GetMemberDashboard(int memberId, string memberName)
        {
            var m = new MemberDashboardViewModel { MemberName = memberName };

            using (var c = Open())
            {
                const string loans = @"
                    SELECT b.TITLE, br.ISSUE_DATE, br.DUE_DATE
                      FROM LibMgmt_Borrowings br
                      JOIN LibMgmt_Books b ON b.BOOK_ID = br.BOOK_ID
                     WHERE br.MEMBER_ID = :mid AND br.STATUS = 'ACTIVE'
                     ORDER BY br.DUE_DATE";
                using (var cmd = Cmd(c, loans))
                {
                    cmd.Parameters.Add(new OracleParameter("mid", memberId));
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            m.ActiveLoans.Add(new MemberLoan
                            {
                                Title = S(r, 0),
                                BorrowedOn = DT(r, 1),
                                DueDate = DT(r, 2)
                            });
                }

                m.CurrentlyBorrowed = m.ActiveLoans.Count;
                foreach (var l in m.ActiveLoans)
                {
                    if (l.DaysOverdue > 0) m.OverdueCount++;
                    m.FineDue += l.Fine;
                }

                const string top = @"
                    SELECT b.TITLE, b.AUTHOR, b.ISBN, b.AVAILABLE_COPIES,
                           (SELECT COUNT(*) FROM LibMgmt_Borrowings x WHERE x.BOOK_ID = b.BOOK_ID) AS CNT
                      FROM LibMgmt_Books b
                     ORDER BY CNT DESC, b.TITLE
                     FETCH FIRST 10 ROWS ONLY";
                using (var cmd = Cmd(c, top))
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        m.TopBooks.Add(new TopBook
                        {
                            Title = S(r, 0),
                            Author = S(r, 1),
                            Isbn = S(r, 2),
                            Available = I(r, 3) > 0,
                            BorrowCount = I(r, 4)
                        });
            }
            return m;
        }

        public List<DetailedLoan> GetMemberActiveLoans(int memberId)
        {
            const string sql = @"
                SELECT br.BORROWING_ID, b.TITLE, b.AUTHOR, b.ISBN,
                       br.ISSUE_DATE, br.DUE_DATE
                  FROM LibMgmt_Borrowings br
                  JOIN LibMgmt_Books b ON b.BOOK_ID = br.BOOK_ID
                 WHERE br.MEMBER_ID = :mid AND br.STATUS = 'ACTIVE'
                 ORDER BY br.DUE_DATE";

            var list = new List<DetailedLoan>();
            using (var c = Open())
            using (var cmd = Cmd(c, sql))
            {
                cmd.Parameters.Add(new OracleParameter("mid", memberId));
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new DetailedLoan
                        {
                            BorrowingId = I(r, 0),
                            Title = S(r, 1),
                            Author = S(r, 2),
                            Isbn = S(r, 3),
                            BorrowedOn = DT(r, 4),
                            DueDate = DT(r, 5)
                        });
            }
            return list;
        }

        public List<LoanHistoryRow> GetMemberLoanHistory(int memberId)
        {
            const string sql = @"
                SELECT b.TITLE, b.AUTHOR, br.ISSUE_DATE, br.RETURN_DATE,
                       NVL((SELECT SUM(f.AMOUNT) FROM LibMgmt_Fines f
                              WHERE f.BORROWING_ID = br.BORROWING_ID
                                AND f.PAID_DATE IS NOT NULL), 0) AS FINE_PAID,
                       CASE WHEN br.RETURN_DATE > br.DUE_DATE THEN 1 ELSE 0 END AS WAS_LATE
                  FROM LibMgmt_Borrowings br
                  JOIN LibMgmt_Books b ON b.BOOK_ID = br.BOOK_ID
                 WHERE br.MEMBER_ID = :mid AND br.STATUS = 'RETURNED'
                 ORDER BY br.RETURN_DATE DESC";

            var list = new List<LoanHistoryRow>();
            using (var c = Open())
            using (var cmd = Cmd(c, sql))
            {
                cmd.Parameters.Add(new OracleParameter("mid", memberId));
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new LoanHistoryRow
                        {
                            Title = S(r, 0),
                            Author = S(r, 1),
                            BorrowedOn = DT(r, 2),
                            ReturnedOn = DT(r, 3),
                            FinePaid = D(r, 4),
                            WasLate = I(r, 5) == 1
                        });
            }
            return list;
        }

        public List<FineRecord> GetMemberFines(int memberId)
        {
            const string sql = @"
                SELECT f.FINE_ID, b.TITLE, f.REASON, f.AMOUNT, f.ISSUED_DATE, f.PAID_DATE
                  FROM LibMgmt_Fines f
                  JOIN LibMgmt_Borrowings br ON br.BORROWING_ID = f.BORROWING_ID
                  JOIN LibMgmt_Books b       ON b.BOOK_ID       = br.BOOK_ID
                 WHERE f.MEMBER_ID = :mid
                 ORDER BY f.ISSUED_DATE DESC";

            var list = new List<FineRecord>();
            using (var c = Open())
            using (var cmd = Cmd(c, sql))
            {
                cmd.Parameters.Add(new OracleParameter("mid", memberId));
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new FineRecord
                        {
                            Id = I(r, 0),
                            BookTitle = S(r, 1),
                            Reason = S(r, 2),
                            Amount = D(r, 3),
                            IssuedOn = DT(r, 4),
                            PaidOn = NDT(r, 5)
                        });
            }
            return list;
        }

        public List<WishlistItem> GetMemberWishlist(int memberId)
        {
            const string sql = @"
                SELECT w.WISHLIST_ID, b.TITLE, b.AUTHOR, b.ISBN, b.GENRE,
                       b.AVAILABLE_COPIES, w.ADDED_DATE
                  FROM LibMgmt_Wishlist w
                  JOIN LibMgmt_Books b ON b.BOOK_ID = w.BOOK_ID
                 WHERE w.MEMBER_ID = :mid
                 ORDER BY w.ADDED_DATE DESC";

            var list = new List<WishlistItem>();
            using (var c = Open())
            using (var cmd = Cmd(c, sql))
            {
                cmd.Parameters.Add(new OracleParameter("mid", memberId));
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new WishlistItem
                        {
                            Id = I(r, 0),
                            Title = S(r, 1),
                            Author = S(r, 2),
                            Isbn = S(r, 3),
                            Genre = S(r, 4),
                            Available = I(r, 5) > 0,
                            AddedOn = DT(r, 6)
                        });
            }
            return list;
        }

        /// <summary>BOOK_IDs already on this member's wishlist (drives the bookmark icon).</summary>
        public HashSet<int> GetWishlistedBookIds(int memberId)
        {
            var set = new HashSet<int>();
            using (var c = Open())
            using (var cmd = Cmd(c, "SELECT BOOK_ID FROM LibMgmt_Wishlist WHERE MEMBER_ID = :mid"))
            {
                cmd.Parameters.Add(new OracleParameter("mid", memberId));
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        set.Add(I(r, 0));
            }
            return set;
        }

        // =====================================================================
        // Librarian writes — books, damage, issue, return
        // =====================================================================

        public string AddBook(AddBookViewModel m)
        {
            const string sql = @"
                INSERT INTO LibMgmt_Books
                    (BOOK_ID, TITLE, AUTHOR, ISBN, GENRE, PUBLISHER,
                     PUBLISH_YEAR, TOTAL_COPIES, AVAILABLE_COPIES, ADDED_DATE)
                VALUES
                    ((SELECT NVL(MAX(BOOK_ID),0)+1 FROM LibMgmt_Books),
                     :title, :author, :isbn, :genre, :publisher,
                     :year, :qty, :qty, SYSDATE)";

            using (var c = Open())
            using (var cmd = Cmd(c, sql))
            {
                cmd.Parameters.Add(new OracleParameter("title", m.Title ?? ""));
                cmd.Parameters.Add(new OracleParameter("author", m.Author ?? ""));
                cmd.Parameters.Add(new OracleParameter("isbn", (object)m.Isbn ?? DBNull.Value));
                cmd.Parameters.Add(new OracleParameter("genre", (object)m.Genre ?? DBNull.Value));
                cmd.Parameters.Add(new OracleParameter("publisher", (object)m.Publisher ?? DBNull.Value));
                cmd.Parameters.Add(new OracleParameter("year", (object)m.PublishedYear ?? DBNull.Value));
                cmd.Parameters.Add(new OracleParameter("qty", m.Quantity));
                cmd.ExecuteNonQuery();
            }
            return m.Title;
        }

        public void EditBook(EditBookViewModel m)
        {
            // Clamp AVAILABLE_COPIES so it never exceeds the new total
            // (the chk_books_avail_le_tot constraint would otherwise reject it).
            const string sql = @"
                UPDATE LibMgmt_Books
                   SET TITLE = :title, AUTHOR = :author, ISBN = :isbn,
                       GENRE = :genre, TOTAL_COPIES = :qty,
                       AVAILABLE_COPIES = LEAST(AVAILABLE_COPIES, :qty)
                 WHERE BOOK_ID = :id";

            using (var c = Open())
            using (var cmd = Cmd(c, sql))
            {
                cmd.Parameters.Add(new OracleParameter("title", m.Title ?? ""));
                cmd.Parameters.Add(new OracleParameter("author", m.Author ?? ""));
                cmd.Parameters.Add(new OracleParameter("isbn", (object)m.Isbn ?? DBNull.Value));
                cmd.Parameters.Add(new OracleParameter("genre", (object)m.Genre ?? DBNull.Value));
                cmd.Parameters.Add(new OracleParameter("qty", m.Quantity));
                cmd.Parameters.Add(new OracleParameter("id", m.Id));
                cmd.ExecuteNonQuery();
            }
        }

        public enum DeleteResult { Deleted, HasActiveLoans, HasHistory, NotFound }

        public DeleteResult DeleteBook(int bookId)
        {
            using (var c = Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    int total;
                    using (var cmd = Cmd(c, "SELECT COUNT(*) FROM LibMgmt_Books WHERE BOOK_ID = :id"))
                    {
                        cmd.Transaction = tx;
                        cmd.Parameters.Add(new OracleParameter("id", bookId));
                        total = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    if (total == 0) { tx.Rollback(); return DeleteResult.NotFound; }

                    int active;
                    using (var cmd = Cmd(c, "SELECT COUNT(*) FROM LibMgmt_Borrowings WHERE BOOK_ID = :id AND STATUS = 'ACTIVE'"))
                    {
                        cmd.Transaction = tx;
                        cmd.Parameters.Add(new OracleParameter("id", bookId));
                        active = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    if (active > 0) { tx.Rollback(); return DeleteResult.HasActiveLoans; }

                    int anyLoans;
                    using (var cmd = Cmd(c, "SELECT COUNT(*) FROM LibMgmt_Borrowings WHERE BOOK_ID = :id"))
                    {
                        cmd.Transaction = tx;
                        cmd.Parameters.Add(new OracleParameter("id", bookId));
                        anyLoans = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    if (anyLoans > 0) { tx.Rollback(); return DeleteResult.HasHistory; }

                    // No loan history — safe to remove. Clear dependent rows first.
                    foreach (var del in new[]
                    {
                        "DELETE FROM LibMgmt_Wishlist    WHERE BOOK_ID = :id",
                        "DELETE FROM LibMgmt_BookDamages WHERE BOOK_ID = :id",
                        "DELETE FROM LibMgmt_Books       WHERE BOOK_ID = :id"
                    })
                    {
                        using (var cmd = Cmd(c, del))
                        {
                            cmd.Transaction = tx;
                            cmd.Parameters.Add(new OracleParameter("id", bookId));
                            cmd.ExecuteNonQuery();
                        }
                    }

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
            using (var c = Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    string title = null;
                    using (var cmd = Cmd(c, "SELECT TITLE FROM LibMgmt_Books WHERE BOOK_ID = :id"))
                    {
                        cmd.Transaction = tx;
                        cmd.Parameters.Add(new OracleParameter("id", bookId));
                        var o = cmd.ExecuteScalar();
                        if (o == null || o == DBNull.Value) { tx.Rollback(); return null; }
                        title = Convert.ToString(o);
                    }

                    const string ins = @"
                        INSERT INTO LibMgmt_BookDamages
                            (DAMAGE_ID, BOOK_ID, DAMAGED_COPIES, REASON, REPORTED_DATE, REPORTED_BY)
                        VALUES
                            ((SELECT NVL(MAX(DAMAGE_ID),0)+1 FROM LibMgmt_BookDamages),
                             :bid, :qty, :reason, SYSDATE, :lib)";
                    using (var cmd = Cmd(c, ins))
                    {
                        cmd.Transaction = tx;
                        cmd.Parameters.Add(new OracleParameter("bid", bookId));
                        cmd.Parameters.Add(new OracleParameter("qty", damagedCopies));
                        cmd.Parameters.Add(new OracleParameter("reason", reason ?? ""));
                        cmd.Parameters.Add(new OracleParameter("lib", librarianId));
                        cmd.ExecuteNonQuery();
                    }

                    using (var cmd = Cmd(c,
                        "UPDATE LibMgmt_Books SET AVAILABLE_COPIES = GREATEST(AVAILABLE_COPIES - :qty, 0) WHERE BOOK_ID = :bid"))
                    {
                        cmd.Transaction = tx;
                        cmd.Parameters.Add(new OracleParameter("qty", damagedCopies));
                        cmd.Parameters.Add(new OracleParameter("bid", bookId));
                        cmd.ExecuteNonQuery();
                    }

                    tx.Commit();
                    return title;
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
            using (var c = Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    int memberId;
                    using (var cmd = Cmd(c, "SELECT MEMBER_ID FROM LibMgmt_Members WHERE MEMBER_CODE = :code AND IS_ACTIVE = 1"))
                    {
                        cmd.Transaction = tx;
                        cmd.Parameters.Add(new OracleParameter("code", memberCode ?? ""));
                        var o = cmd.ExecuteScalar();
                        if (o == null || o == DBNull.Value) { tx.Rollback(); return IssueResult.MemberNotFound; }
                        memberId = Convert.ToInt32(o);
                    }

                    int avail;
                    using (var cmd = Cmd(c, "SELECT TITLE, AVAILABLE_COPIES FROM LibMgmt_Books WHERE BOOK_ID = :id"))
                    {
                        cmd.Transaction = tx;
                        cmd.Parameters.Add(new OracleParameter("id", bookId));
                        using (var r = cmd.ExecuteReader())
                        {
                            if (!r.Read()) { tx.Rollback(); return IssueResult.BookNotFound; }
                            title = S(r, 0);
                            avail = I(r, 1);
                        }
                    }
                    if (avail <= 0) { tx.Rollback(); return IssueResult.NoCopies; }

                    const string ins = @"
                        INSERT INTO LibMgmt_Borrowings
                            (BORROWING_ID, MEMBER_ID, BOOK_ID, ISSUE_DATE, DUE_DATE, STATUS)
                        VALUES
                            ((SELECT NVL(MAX(BORROWING_ID),0)+1 FROM LibMgmt_Borrowings),
                             :mid, :bid, SYSDATE, :due, 'ACTIVE')";
                    using (var cmd = Cmd(c, ins))
                    {
                        cmd.Transaction = tx;
                        cmd.Parameters.Add(new OracleParameter("mid", memberId));
                        cmd.Parameters.Add(new OracleParameter("bid", bookId));
                        cmd.Parameters.Add(new OracleParameter("due", dueDate.Date));
                        cmd.ExecuteNonQuery();
                    }

                    using (var cmd = Cmd(c,
                        "UPDATE LibMgmt_Books SET AVAILABLE_COPIES = AVAILABLE_COPIES - 1 WHERE BOOK_ID = :bid"))
                    {
                        cmd.Transaction = tx;
                        cmd.Parameters.Add(new OracleParameter("bid", bookId));
                        cmd.ExecuteNonQuery();
                    }

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
        public bool ReturnBook(int borrowingId)
        {
            using (var c = Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    int bookId;
                    using (var cmd = Cmd(c, "SELECT BOOK_ID FROM LibMgmt_Borrowings WHERE BORROWING_ID = :id AND STATUS = 'ACTIVE'"))
                    {
                        cmd.Transaction = tx;
                        cmd.Parameters.Add(new OracleParameter("id", borrowingId));
                        var o = cmd.ExecuteScalar();
                        if (o == null || o == DBNull.Value) { tx.Rollback(); return false; }
                        bookId = Convert.ToInt32(o);
                    }

                    using (var cmd = Cmd(c,
                        "UPDATE LibMgmt_Borrowings SET RETURN_DATE = SYSDATE, STATUS = 'RETURNED' WHERE BORROWING_ID = :id"))
                    {
                        cmd.Transaction = tx;
                        cmd.Parameters.Add(new OracleParameter("id", borrowingId));
                        cmd.ExecuteNonQuery();
                    }

                    using (var cmd = Cmd(c,
                        "UPDATE LibMgmt_Books SET AVAILABLE_COPIES = LEAST(AVAILABLE_COPIES + 1, TOTAL_COPIES) WHERE BOOK_ID = :bid"))
                    {
                        cmd.Transaction = tx;
                        cmd.Parameters.Add(new OracleParameter("bid", bookId));
                        cmd.ExecuteNonQuery();
                    }

                    using (var cmd = Cmd(c,
                        "UPDATE LibMgmt_Fines SET PAID_DATE = SYSDATE WHERE BORROWING_ID = :id AND PAID_DATE IS NULL"))
                    {
                        cmd.Transaction = tx;
                        cmd.Parameters.Add(new OracleParameter("id", borrowingId));
                        cmd.ExecuteNonQuery();
                    }

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
            using (var c = Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    using (var cmd = Cmd(c, "SELECT TITLE FROM LibMgmt_Books WHERE BOOK_ID = :bid"))
                    {
                        cmd.Transaction = tx;
                        cmd.Parameters.Add(new OracleParameter("bid", bookId));
                        var o = cmd.ExecuteScalar();
                        if (o == null || o == DBNull.Value) { tx.Rollback(); return WishlistToggle.BookNotFound; }
                        title = Convert.ToString(o);
                    }

                    int exists;
                    using (var cmd = Cmd(c, "SELECT COUNT(*) FROM LibMgmt_Wishlist WHERE MEMBER_ID = :mid AND BOOK_ID = :bid"))
                    {
                        cmd.Transaction = tx;
                        cmd.Parameters.Add(new OracleParameter("mid", memberId));
                        cmd.Parameters.Add(new OracleParameter("bid", bookId));
                        exists = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    if (exists > 0)
                    {
                        using (var cmd = Cmd(c, "DELETE FROM LibMgmt_Wishlist WHERE MEMBER_ID = :mid AND BOOK_ID = :bid"))
                        {
                            cmd.Transaction = tx;
                            cmd.Parameters.Add(new OracleParameter("mid", memberId));
                            cmd.Parameters.Add(new OracleParameter("bid", bookId));
                            cmd.ExecuteNonQuery();
                        }
                        tx.Commit();
                        return WishlistToggle.Removed;
                    }

                    using (var cmd = Cmd(c, @"
                        INSERT INTO LibMgmt_Wishlist (WISHLIST_ID, MEMBER_ID, BOOK_ID, ADDED_DATE)
                        VALUES ((SELECT NVL(MAX(WISHLIST_ID),0)+1 FROM LibMgmt_Wishlist), :mid, :bid, SYSDATE)"))
                    {
                        cmd.Transaction = tx;
                        cmd.Parameters.Add(new OracleParameter("mid", memberId));
                        cmd.Parameters.Add(new OracleParameter("bid", bookId));
                        cmd.ExecuteNonQuery();
                    }
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
            using (var c = Open())
            {
                string title;
                using (var cmd = Cmd(c, "SELECT TITLE FROM LibMgmt_Books WHERE BOOK_ID = :bid"))
                {
                    cmd.Parameters.Add(new OracleParameter("bid", bookId));
                    var o = cmd.ExecuteScalar();
                    if (o == null || o == DBNull.Value) return null;
                    title = Convert.ToString(o);
                }

                using (var cmd = Cmd(c, @"
                    INSERT INTO LibMgmt_Wishlist (WISHLIST_ID, MEMBER_ID, BOOK_ID, ADDED_DATE)
                    SELECT (SELECT NVL(MAX(WISHLIST_ID),0)+1 FROM LibMgmt_Wishlist), :mid, :bid, SYSDATE
                      FROM dual
                     WHERE NOT EXISTS (SELECT 1 FROM LibMgmt_Wishlist WHERE MEMBER_ID = :mid AND BOOK_ID = :bid)"))
                {
                    cmd.Parameters.Add(new OracleParameter("mid", memberId));
                    cmd.Parameters.Add(new OracleParameter("bid", bookId));
                    cmd.ExecuteNonQuery();
                }
                return title;
            }
        }

        public void RemoveFromWishlist(int wishlistId, int memberId)
        {
            using (var c = Open())
            using (var cmd = Cmd(c, "DELETE FROM LibMgmt_Wishlist WHERE WISHLIST_ID = :id AND MEMBER_ID = :mid"))
            {
                cmd.Parameters.Add(new OracleParameter("id", wishlistId));
                cmd.Parameters.Add(new OracleParameter("mid", memberId));
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>Extends a loan by 14 days. Rejected if the loan is already overdue.</summary>
        public bool RenewLoan(int borrowingId, int memberId)
        {
            using (var c = Open())
            using (var cmd = Cmd(c, @"
                UPDATE LibMgmt_Borrowings
                   SET DUE_DATE = DUE_DATE + 14
                 WHERE BORROWING_ID = :id AND MEMBER_ID = :mid
                   AND STATUS = 'ACTIVE' AND DUE_DATE >= TRUNC(SYSDATE)"))
            {
                cmd.Parameters.Add(new OracleParameter("id", borrowingId));
                cmd.Parameters.Add(new OracleParameter("mid", memberId));
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public void PayFine(int fineId, int memberId)
        {
            using (var c = Open())
            using (var cmd = Cmd(c,
                "UPDATE LibMgmt_Fines SET PAID_DATE = SYSDATE WHERE FINE_ID = :id AND MEMBER_ID = :mid AND PAID_DATE IS NULL"))
            {
                cmd.Parameters.Add(new OracleParameter("id", fineId));
                cmd.Parameters.Add(new OracleParameter("mid", memberId));
                cmd.ExecuteNonQuery();
            }
        }

        public void PayAllFines(int memberId)
        {
            using (var c = Open())
            using (var cmd = Cmd(c,
                "UPDATE LibMgmt_Fines SET PAID_DATE = SYSDATE WHERE MEMBER_ID = :mid AND PAID_DATE IS NULL"))
            {
                cmd.Parameters.Add(new OracleParameter("mid", memberId));
                cmd.ExecuteNonQuery();
            }
        }
    }
}
