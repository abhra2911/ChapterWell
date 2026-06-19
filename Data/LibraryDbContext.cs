using Microsoft.EntityFrameworkCore;
using Lib_Mgmt.Data.Entities;

namespace Lib_Mgmt.Data
{
    /// <summary>
    /// EF Core 2.1 context over the existing Oracle 19c schema
    /// (Oracle.EntityFrameworkCore provider). The schema was created by hand,
    /// so every mapping is explicit:
    ///   * Table and column names are pinned to the exact identifiers the
    ///     schema uses (unquoted Oracle identifiers fold to upper-case).
    ///   * Primary keys are application-assigned via NVL(MAX(id),0)+1, so each
    ///     key is marked ValueGeneratedNever() — EF must not assume an
    ///     identity column or sequence.
    /// LibraryRepository wraps it and keeps the same public API.
    /// </summary>
    public class LibraryDbContext : DbContext
    {
        public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options) { }

        public DbSet<Librarian> Librarians { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<Borrowing> Borrowings { get; set; }
        public DbSet<Fine> Fines { get; set; }
        public DbSet<BookDamage> BookDamages { get; set; }
        public DbSet<WishlistEntry> Wishlist { get; set; }

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<Librarian>(e =>
            {
                e.ToTable("LIBMGMT_LIBRARIANS");
                e.HasKey(x => x.LibrarianId);
                e.Property(x => x.LibrarianId).HasColumnName("LIBRARIAN_ID").ValueGeneratedNever();
                e.Property(x => x.Username).HasColumnName("USERNAME");
                e.Property(x => x.FullName).HasColumnName("FULL_NAME");
                e.Property(x => x.LibrarianCode).HasColumnName("LIBRARIAN_CODE");
                e.Property(x => x.PasswordHash).HasColumnName("PASSWORD_HASH");
                e.Property(x => x.Email).HasColumnName("EMAIL");
                e.Property(x => x.Phone).HasColumnName("PHONE");
                e.Property(x => x.IsActive).HasColumnName("IS_ACTIVE");
            });

            b.Entity<Member>(e =>
            {
                e.ToTable("LIBMGMT_MEMBERS");
                e.HasKey(x => x.MemberId);
                e.Property(x => x.MemberId).HasColumnName("MEMBER_ID").ValueGeneratedNever();
                e.Property(x => x.Username).HasColumnName("USERNAME");
                e.Property(x => x.FullName).HasColumnName("FULL_NAME");
                e.Property(x => x.MemberCode).HasColumnName("MEMBER_CODE");
                e.Property(x => x.PasswordHash).HasColumnName("PASSWORD_HASH");
                e.Property(x => x.Email).HasColumnName("EMAIL");
                e.Property(x => x.Phone).HasColumnName("PHONE");
                e.Property(x => x.IsActive).HasColumnName("IS_ACTIVE");
                e.Property(x => x.JoinedDate).HasColumnName("JOINED_DATE");
            });

            b.Entity<Book>(e =>
            {
                e.ToTable("LIBMGMT_BOOKS");
                e.HasKey(x => x.BookId);
                e.Property(x => x.BookId).HasColumnName("BOOK_ID").ValueGeneratedNever();
                e.Property(x => x.Title).HasColumnName("TITLE");
                e.Property(x => x.Author).HasColumnName("AUTHOR");
                e.Property(x => x.Genre).HasColumnName("GENRE");
                e.Property(x => x.Isbn).HasColumnName("ISBN");
                e.Property(x => x.Publisher).HasColumnName("PUBLISHER");
                e.Property(x => x.PublishYear).HasColumnName("PUBLISH_YEAR");
                e.Property(x => x.TotalCopies).HasColumnName("TOTAL_COPIES");
                e.Property(x => x.AvailableCopies).HasColumnName("AVAILABLE_COPIES");
                e.Property(x => x.AddedDate).HasColumnName("ADDED_DATE");
            });

            b.Entity<Borrowing>(e =>
            {
                e.ToTable("LIBMGMT_BORROWINGS");
                e.HasKey(x => x.BorrowingId);
                e.Property(x => x.BorrowingId).HasColumnName("BORROWING_ID").ValueGeneratedNever();
                e.Property(x => x.MemberId).HasColumnName("MEMBER_ID");
                e.Property(x => x.BookId).HasColumnName("BOOK_ID");
                e.Property(x => x.IssueDate).HasColumnName("ISSUE_DATE");
                e.Property(x => x.DueDate).HasColumnName("DUE_DATE");
                e.Property(x => x.ReturnDate).HasColumnName("RETURN_DATE");
                e.Property(x => x.Status).HasColumnName("STATUS");
            });

            b.Entity<Fine>(e =>
            {
                e.ToTable("LIBMGMT_FINES");
                e.HasKey(x => x.FineId);
                e.Property(x => x.FineId).HasColumnName("FINE_ID").ValueGeneratedNever();
                e.Property(x => x.BorrowingId).HasColumnName("BORROWING_ID");
                e.Property(x => x.MemberId).HasColumnName("MEMBER_ID");
                e.Property(x => x.Reason).HasColumnName("REASON");
                e.Property(x => x.Amount).HasColumnName("AMOUNT");
                e.Property(x => x.IssuedDate).HasColumnName("ISSUED_DATE");
                e.Property(x => x.PaidDate).HasColumnName("PAID_DATE");
            });

            b.Entity<BookDamage>(e =>
            {
                e.ToTable("LIBMGMT_BOOKDAMAGES");
                e.HasKey(x => x.DamageId);
                e.Property(x => x.DamageId).HasColumnName("DAMAGE_ID").ValueGeneratedNever();
                e.Property(x => x.BookId).HasColumnName("BOOK_ID");
                e.Property(x => x.DamagedCopies).HasColumnName("DAMAGED_COPIES");
                e.Property(x => x.Reason).HasColumnName("REASON");
                e.Property(x => x.ReportedDate).HasColumnName("REPORTED_DATE");
                e.Property(x => x.ReportedBy).HasColumnName("REPORTED_BY");
            });

            b.Entity<WishlistEntry>(e =>
            {
                e.ToTable("LIBMGMT_WISHLIST");
                e.HasKey(x => x.WishlistId);
                e.Property(x => x.WishlistId).HasColumnName("WISHLIST_ID").ValueGeneratedNever();
                e.Property(x => x.MemberId).HasColumnName("MEMBER_ID");
                e.Property(x => x.BookId).HasColumnName("BOOK_ID");
                e.Property(x => x.AddedDate).HasColumnName("ADDED_DATE");
            });
        }
    }
}
