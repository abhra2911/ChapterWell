using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Lib_Mgmt.Models
{
    public partial class ModelContext : DbContext
    {
        public ModelContext()
        {
        }

        public ModelContext(DbContextOptions<ModelContext> options)
            : base(options)
        {
        }

        public virtual DbSet<LibmgmtBookdamages> LibmgmtBookdamages { get; set; }
        public virtual DbSet<LibmgmtBooks> LibmgmtBooks { get; set; }
        public virtual DbSet<LibmgmtBorrowings> LibmgmtBorrowings { get; set; }
        public virtual DbSet<LibmgmtFines> LibmgmtFines { get; set; }
        public virtual DbSet<LibmgmtLibrarians> LibmgmtLibrarians { get; set; }
        public virtual DbSet<LibmgmtMembers> LibmgmtMembers { get; set; }
        public virtual DbSet<LibmgmtReservations> LibmgmtReservations { get; set; }
        public virtual DbSet<LibmgmtWishlist> LibmgmtWishlist { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Fallback used by `dotnet ef` design-time tooling and anywhere
                // ModelContext is constructed without DI (e.g. `new ModelContext()`).
                // Normal requests get their connection string from appsettings.json
                // via AddDbContext in Program.cs instead. No secrets here on purpose
                // — Windows Authentication needs none.
                optionsBuilder.UseSqlServer(
                    "Server=localhost;Database=Chapterly;Trusted_Connection=True;TrustServerCertificate=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LibmgmtBookdamages>(entity =>
            {
                entity.HasKey(e => e.DamageId);

                entity.ToTable("LIBMGMT_BOOKDAMAGES");

                entity.HasIndex(e => e.DamageId)
                    .HasDatabaseName("LIBMGMT_BOOKDAMAGES_PK")
                    .IsUnique();

                entity.Property(e => e.DamageId)
                    .HasColumnName("DAMAGE_ID")
                    .HasColumnType("decimal(18,0)");

                entity.Property(e => e.BookId)
                    .HasColumnName("BOOK_ID")
                    .HasColumnType("decimal(18,0)");

                entity.Property(e => e.DamagedCopies).HasColumnName("DAMAGED_COPIES");

                entity.Property(e => e.Reason)
                    .IsRequired()
                    .HasColumnName("REASON")
                    .HasColumnType("varchar")
                    .HasMaxLength(200);

                entity.Property(e => e.ReportedBy)
                    .HasColumnName("REPORTED_BY")
                    .HasColumnType("decimal(18,0)");

                entity.Property(e => e.ReportedDate)
                    .HasColumnName("REPORTED_DATE")
                    .HasColumnType("date");

                entity.HasOne(d => d.Book)
                    .WithMany(p => p.LibmgmtBookdamages)
                    .HasForeignKey(d => d.BookId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_DMG_BOOK");

                entity.HasOne(d => d.ReportedByNavigation)
                    .WithMany(p => p.LibmgmtBookdamages)
                    .HasForeignKey(d => d.ReportedBy)
                    .HasConstraintName("FK_DMG_LIBRARIAN");

            });

            modelBuilder.Entity<LibmgmtBooks>(entity =>
            {
                entity.HasKey(e => e.BookId);

                entity.ToTable("LIBMGMT_BOOKS");

                entity.HasIndex(e => e.BookId)
                    .HasDatabaseName("LIBMGMT_BOOKS_PK")
                    .IsUnique();

                entity.HasIndex(e => e.Genre)
                    .HasDatabaseName("IDX_BOOKS_GENRE");

                entity.HasIndex(e => e.Isbn)
                    .HasDatabaseName("UQ_BOOKS_ISBN")
                    .IsUnique();

                entity.Property(e => e.BookId)
                    .HasColumnName("BOOK_ID")
                    .HasColumnType("decimal(18,0)");

                entity.Property(e => e.AddedDate)
                    .HasColumnName("ADDED_DATE")
                    .HasColumnType("date");

                entity.Property(e => e.Author)
                    .IsRequired()
                    .HasColumnName("AUTHOR")
                    .HasColumnType("varchar")
                    .HasMaxLength(150);

                entity.Property(e => e.AvailableCopies).HasColumnName("AVAILABLE_COPIES");

                entity.Property(e => e.Genre)
                    .HasColumnName("GENRE")
                    .HasColumnType("varchar")
                    .HasMaxLength(50);

                entity.Property(e => e.Isbn)
                    .HasColumnName("ISBN")
                    .HasColumnType("varchar")
                    .HasMaxLength(20);

                entity.Property(e => e.PublishYear).HasColumnName("PUBLISH_YEAR");

                entity.Property(e => e.Publisher)
                    .HasColumnName("PUBLISHER")
                    .HasColumnType("varchar")
                    .HasMaxLength(150);

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasColumnName("TITLE")
                    .HasColumnType("varchar")
                    .HasMaxLength(200);

                entity.Property(e => e.TotalCopies).HasColumnName("TOTAL_COPIES");

                entity.Property(e => e.ShelfNumber).HasColumnName("SHELF_NUMBER");
            });

            modelBuilder.Entity<LibmgmtBorrowings>(entity =>
            {
                entity.HasKey(e => e.BorrowingId);

                entity.ToTable("LIBMGMT_BORROWINGS");

                entity.HasIndex(e => e.BookId)
                    .HasDatabaseName("IDX_BORROW_BOOK");

                entity.HasIndex(e => e.BorrowingId)
                    .HasDatabaseName("LIBMGMT_BORROWINGS_PK")
                    .IsUnique();

                entity.HasIndex(e => e.MemberId)
                    .HasDatabaseName("IDX_BORROW_MEMBER");

                entity.HasIndex(e => e.Status)
                    .HasDatabaseName("IDX_BORROW_STATUS");

                entity.Property(e => e.BorrowingId)
                    .HasColumnName("BORROWING_ID")
                    .HasColumnType("decimal(18,0)");

                entity.Property(e => e.BookId)
                    .HasColumnName("BOOK_ID")
                    .HasColumnType("decimal(18,0)");

                entity.Property(e => e.DueDate)
                    .HasColumnName("DUE_DATE")
                    .HasColumnType("date");

                entity.Property(e => e.IssueDate)
                    .HasColumnName("ISSUE_DATE")
                    .HasColumnType("date");

                entity.Property(e => e.MemberId)
                    .HasColumnName("MEMBER_ID")
                    .HasColumnType("decimal(18,0)");

                entity.Property(e => e.ReturnDate)
                    .HasColumnName("RETURN_DATE")
                    .HasColumnType("date");

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasColumnName("STATUS")
                    .HasColumnType("varchar")
                    .HasMaxLength(10);

                entity.HasOne(d => d.Book)
                    .WithMany(p => p.LibmgmtBorrowings)
                    .HasForeignKey(d => d.BookId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_BORROW_BOOK");

                entity.HasOne(d => d.Member)
                    .WithMany(p => p.LibmgmtBorrowings)
                    .HasForeignKey(d => d.MemberId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_BORROW_MEMBER");
            });

            modelBuilder.Entity<LibmgmtFines>(entity =>
            {
                entity.HasKey(e => e.FineId);

                entity.ToTable("LIBMGMT_FINES");

                entity.HasIndex(e => e.FineId)
                    .HasDatabaseName("LIBMGMT_FINES_PK")
                    .IsUnique();

                entity.HasIndex(e => e.MemberId)
                    .HasDatabaseName("IDX_FINE_MEMBER");

                entity.HasIndex(e => new { e.MemberId, e.PaidDate })
                    .HasDatabaseName("IDX_FINE_UNPAID");

                entity.Property(e => e.FineId)
                    .HasColumnName("FINE_ID")
                    .HasColumnType("decimal(18,0)");

                entity.Property(e => e.Amount)
                    .HasColumnName("AMOUNT")
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.BorrowingId)
                    .HasColumnName("BORROWING_ID")
                    .HasColumnType("decimal(18,0)");

                entity.Property(e => e.IssuedDate)
                    .HasColumnName("ISSUED_DATE")
                    .HasColumnType("date");

                entity.Property(e => e.MemberId)
                    .HasColumnName("MEMBER_ID")
                    .HasColumnType("decimal(18,0)");

                entity.Property(e => e.PaidDate)
                    .HasColumnName("PAID_DATE")
                    .HasColumnType("date");

                entity.Property(e => e.Reason)
                    .IsRequired()
                    .HasColumnName("REASON")
                    .HasColumnType("varchar")
                    .HasMaxLength(200);

                entity.HasOne(d => d.Borrowing)
                    .WithMany(p => p.LibmgmtFines)
                    .HasForeignKey(d => d.BorrowingId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_FINE_BORROWING");

                entity.HasOne(d => d.Member)
                    .WithMany(p => p.LibmgmtFines)
                    .HasForeignKey(d => d.MemberId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_FINE_MEMBER");
            });

            modelBuilder.Entity<LibmgmtLibrarians>(entity =>
            {
                entity.HasKey(e => e.LibrarianId);

                entity.ToTable("LIBMGMT_LIBRARIANS");

                entity.HasIndex(e => e.Email)
                    .HasDatabaseName("UQ_LIBRARIANS_EMAIL")
                    .IsUnique();

                entity.HasIndex(e => e.LibrarianCode)
                    .HasDatabaseName("UQ_LIBRARIANS_CODE")
                    .IsUnique();

                entity.HasIndex(e => e.LibrarianId)
                    .HasDatabaseName("LIBMGMT_LIBRARIANS_PK")
                    .IsUnique();

                entity.HasIndex(e => e.Username)
                    .HasDatabaseName("UQ_LIBRARIANS_USERNAME")
                    .IsUnique();

                entity.Property(e => e.LibrarianId)
                    .HasColumnName("LIBRARIAN_ID")
                    .HasColumnType("decimal(18,0)");

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasColumnName("EMAIL")
                    .HasColumnType("varchar")
                    .HasMaxLength(150);

                entity.Property(e => e.FullName)
                    .IsRequired()
                    .HasColumnName("FULL_NAME")
                    .HasColumnType("varchar")
                    .HasMaxLength(100);

                entity.Property(e => e.IsActive).HasColumnName("IS_ACTIVE");

                entity.Property(e => e.JoinedDate)
                    .HasColumnName("JOINED_DATE")
                    .HasColumnType("date");

                entity.Property(e => e.LibrarianCode)
                    .IsRequired()
                    .HasColumnName("LIBRARIAN_CODE")
                    .HasColumnType("varchar")
                    .HasMaxLength(20);

                entity.Property(e => e.PasswordHash)
                    .IsRequired()
                    .HasColumnName("PASSWORD_HASH")
                    .HasColumnType("varchar")
                    .HasMaxLength(255);

                entity.Property(e => e.Phone)
                    .HasColumnName("PHONE")
                    .HasColumnType("varchar")
                    .HasMaxLength(20);

                entity.Property(e => e.Username)
                    .IsRequired()
                    .HasColumnName("USERNAME")
                    .HasColumnType("varchar")
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<LibmgmtMembers>(entity =>
            {
                entity.HasKey(e => e.MemberId);

                entity.ToTable("LIBMGMT_MEMBERS");

                entity.HasIndex(e => e.Email)
                    .HasDatabaseName("UQ_MEMBERS_EMAIL")
                    .IsUnique();

                entity.HasIndex(e => e.MemberCode)
                    .HasDatabaseName("UQ_MEMBERS_CODE")
                    .IsUnique();

                entity.HasIndex(e => e.MemberId)
                    .HasDatabaseName("LIBMGMT_MEMBERS_PK")
                    .IsUnique();

                entity.HasIndex(e => e.Username)
                    .HasDatabaseName("UQ_MEMBERS_USERNAME")
                    .IsUnique();

                entity.Property(e => e.MemberId)
                    .HasColumnName("MEMBER_ID")
                    .HasColumnType("decimal(18,0)");

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasColumnName("EMAIL")
                    .HasColumnType("varchar")
                    .HasMaxLength(150);

                entity.Property(e => e.FullName)
                    .IsRequired()
                    .HasColumnName("FULL_NAME")
                    .HasColumnType("varchar")
                    .HasMaxLength(100);

                entity.Property(e => e.IsActive).HasColumnName("IS_ACTIVE");

                entity.Property(e => e.JoinedDate)
                    .HasColumnName("JOINED_DATE")
                    .HasColumnType("date");

                entity.Property(e => e.MemberCode)
                    .IsRequired()
                    .HasColumnName("MEMBER_CODE")
                    .HasColumnType("varchar")
                    .HasMaxLength(20);

                entity.Property(e => e.PasswordHash)
                    .IsRequired()
                    .HasColumnName("PASSWORD_HASH")
                    .HasColumnType("varchar")
                    .HasMaxLength(255);

                entity.Property(e => e.Phone)
                    .HasColumnName("PHONE")
                    .HasColumnType("varchar")
                    .HasMaxLength(20);

                entity.Property(e => e.Username)
                    .IsRequired()
                    .HasColumnName("USERNAME")
                    .HasColumnType("varchar")
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<LibmgmtReservations>(entity =>
            {
                entity.HasKey(e => e.ReservationId);

                entity.ToTable("LIBMGMT_RESERVATIONS");

                entity.HasIndex(e => e.ReservationId)
                    .HasDatabaseName("PK_LIBMGMT_RESERVATIONS")
                    .IsUnique();

                entity.HasIndex(e => new { e.MemberId, e.BookId })
                    .HasDatabaseName("UQ_RESV_MEMBER_BOOK")
                    .IsUnique();

                entity.Property(e => e.ReservationId)
                    .HasColumnName("RESERVATION_ID")
                    .HasColumnType("decimal(18,0)");

                entity.Property(e => e.BookId)
                    .HasColumnName("BOOK_ID")
                    .HasColumnType("decimal(18,0)");

                entity.Property(e => e.MemberId)
                    .HasColumnName("MEMBER_ID")
                    .HasColumnType("decimal(18,0)");

                entity.Property(e => e.RequestedDate)
                    .HasColumnName("REQUESTED_DATE")
                    .HasColumnType("date");

                entity.HasOne(d => d.Book)
                    .WithMany(p => p.LibmgmtReservations)
                    .HasForeignKey(d => d.BookId)
                    .HasConstraintName("FK_RESV_BOOK");

                entity.HasOne(d => d.Member)
                    .WithMany(p => p.LibmgmtReservations)
                    .HasForeignKey(d => d.MemberId)
                    .HasConstraintName("FK_RESV_MEMBER");
            });

            modelBuilder.Entity<LibmgmtWishlist>(entity =>
            {
                entity.HasKey(e => e.WishlistId);

                entity.ToTable("LIBMGMT_WISHLIST");

                entity.HasIndex(e => e.MemberId)
                    .HasDatabaseName("IDX_WISH_MEMBER");

                entity.HasIndex(e => e.WishlistId)
                    .HasDatabaseName("LIBMGMT_WISHLIST_PK")
                    .IsUnique();

                entity.HasIndex(e => new { e.MemberId, e.BookId })
                    .HasDatabaseName("UQ_WISH_MEMBER_BOOK")
                    .IsUnique();

                entity.Property(e => e.WishlistId)
                    .HasColumnName("WISHLIST_ID")
                    .HasColumnType("decimal(18,0)");

                entity.Property(e => e.AddedDate)
                    .HasColumnName("ADDED_DATE")
                    .HasColumnType("date");

                entity.Property(e => e.BookId)
                    .HasColumnName("BOOK_ID")
                    .HasColumnType("decimal(18,0)");

                entity.Property(e => e.MemberId)
                    .HasColumnName("MEMBER_ID")
                    .HasColumnType("decimal(18,0)");

                entity.HasOne(d => d.Book)
                    .WithMany(p => p.LibmgmtWishlist)
                    .HasForeignKey(d => d.BookId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_WISH_BOOK");

                entity.HasOne(d => d.Member)
                    .WithMany(p => p.LibmgmtWishlist)
                    .HasForeignKey(d => d.MemberId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_WISH_MEMBER");
            });
        }
    }
}
