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
//warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
                optionsBuilder.UseOracle(" License Key=vEyr8GdnIarOKlEHQKxi+4E0HlXN85PVGHI096M18fE9npitBshNzxHQrLkykgaCoG7fjhKMHmjSsRlGn3Y0zsGnqDrlT9eeGNFRi0n5gWVUwWR7N+m+qdM30pW6lBqxNU59leVb8A8SXrkYPS3kP1BTVBaR6SVdPKxAGqx4xHtTaIJzbXTymo3JLBjB6PvfzGNXObzIAtG2UwVrPhg9/spSFeJRX3a5ir6Y16Y4KYc4ZTcy9mdRHMkrDhXoUB4HaGAs4L/og1GmqrsibbkYA2MR6ZT7Z6z4HH0DeKQLWe4=; User Id=IUSF;Password=iusf_dev;SERVICE NAME=oracle;Direct=true;Data Source= iffcoexadr-92rdq-scan.drhyddbcltsn01.drhydebsprodvcn.oraclevcn.com:1521/ifppdbdev.drhyddbcltsn01.drhydebsprodvcn.oraclevcn.com;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LibmgmtBookdamages>(entity =>
            {
                entity.HasKey(e => e.DamageId);

                entity.ToTable("LIBMGMT_BOOKDAMAGES", "IUSF");

                entity.HasIndex(e => e.DamageId)
                    .HasName("LIBMGMT_BOOKDAMAGES_PK")
                    .IsUnique();

                entity.Property(e => e.DamageId).HasColumnName("DAMAGE_ID");

                entity.Property(e => e.BookId).HasColumnName("BOOK_ID");

                entity.Property(e => e.DamagedCopies).HasColumnName("DAMAGED_COPIES");

                entity.Property(e => e.Reason)
                    .IsRequired()
                    .HasColumnName("REASON")
                    .HasColumnType("varchar2")
                    .HasMaxLength(200);

                entity.Property(e => e.ReportedBy).HasColumnName("REPORTED_BY");

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

                entity.ToTable("LIBMGMT_BOOKS", "IUSF");

                entity.HasIndex(e => e.BookId)
                    .HasName("LIBMGMT_BOOKS_PK")
                    .IsUnique();

                entity.HasIndex(e => e.Genre)
                    .HasName("IDX_BOOKS_GENRE");

                entity.HasIndex(e => e.Isbn)
                    .HasName("SYS_C0093245")
                    .IsUnique();

                entity.Property(e => e.BookId).HasColumnName("BOOK_ID");

                entity.Property(e => e.AddedDate)
                    .HasColumnName("ADDED_DATE")
                    .HasColumnType("date");

                entity.Property(e => e.Author)
                    .IsRequired()
                    .HasColumnName("AUTHOR")
                    .HasColumnType("varchar2")
                    .HasMaxLength(150);

                entity.Property(e => e.AvailableCopies).HasColumnName("AVAILABLE_COPIES");

                entity.Property(e => e.Genre)
                    .HasColumnName("GENRE")
                    .HasColumnType("varchar2")
                    .HasMaxLength(50);

                entity.Property(e => e.Isbn)
                    .HasColumnName("ISBN")
                    .HasColumnType("varchar2")
                    .HasMaxLength(20);

                entity.Property(e => e.PublishYear).HasColumnName("PUBLISH_YEAR");

                entity.Property(e => e.Publisher)
                    .HasColumnName("PUBLISHER")
                    .HasColumnType("varchar2")
                    .HasMaxLength(150);

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasColumnName("TITLE")
                    .HasColumnType("varchar2")
                    .HasMaxLength(200);

                entity.Property(e => e.TotalCopies).HasColumnName("TOTAL_COPIES");
            });

            modelBuilder.Entity<LibmgmtBorrowings>(entity =>
            {
                entity.HasKey(e => e.BorrowingId);

                entity.ToTable("LIBMGMT_BORROWINGS", "IUSF");

                entity.HasIndex(e => e.BookId)
                    .HasName("IDX_BORROW_BOOK");

                entity.HasIndex(e => e.BorrowingId)
                    .HasName("LIBMGMT_BORROWINGS_PK")
                    .IsUnique();

                entity.HasIndex(e => e.MemberId)
                    .HasName("IDX_BORROW_MEMBER");

                entity.HasIndex(e => e.Status)
                    .HasName("IDX_BORROW_STATUS");

                entity.Property(e => e.BorrowingId).HasColumnName("BORROWING_ID");

                entity.Property(e => e.BookId).HasColumnName("BOOK_ID");

                entity.Property(e => e.DueDate)
                    .HasColumnName("DUE_DATE")
                    .HasColumnType("date");

                entity.Property(e => e.IssueDate)
                    .HasColumnName("ISSUE_DATE")
                    .HasColumnType("date");

                entity.Property(e => e.MemberId).HasColumnName("MEMBER_ID");

                entity.Property(e => e.ReturnDate)
                    .HasColumnName("RETURN_DATE")
                    .HasColumnType("date");

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasColumnName("STATUS")
                    .HasColumnType("varchar2")
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

                entity.ToTable("LIBMGMT_FINES", "IUSF");

                entity.HasIndex(e => e.FineId)
                    .HasName("LIBMGMT_FINES_PK")
                    .IsUnique();

                entity.HasIndex(e => e.MemberId)
                    .HasName("IDX_FINE_MEMBER");

                entity.HasIndex(e => new { e.MemberId, e.PaidDate })
                    .HasName("IDX_FINE_UNPAID");

                entity.Property(e => e.FineId).HasColumnName("FINE_ID");

                entity.Property(e => e.Amount)
                    .HasColumnName("AMOUNT")
                    .HasColumnType("double");

                entity.Property(e => e.BorrowingId).HasColumnName("BORROWING_ID");

                entity.Property(e => e.IssuedDate)
                    .HasColumnName("ISSUED_DATE")
                    .HasColumnType("date");

                entity.Property(e => e.MemberId).HasColumnName("MEMBER_ID");

                entity.Property(e => e.PaidDate)
                    .HasColumnName("PAID_DATE")
                    .HasColumnType("date");

                entity.Property(e => e.Reason)
                    .IsRequired()
                    .HasColumnName("REASON")
                    .HasColumnType("varchar2")
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

                entity.ToTable("LIBMGMT_LIBRARIANS", "IUSF");

                entity.HasIndex(e => e.Email)
                    .HasName("SYS_C0093225")
                    .IsUnique();

                entity.HasIndex(e => e.LibrarianCode)
                    .HasName("SYS_C0093223")
                    .IsUnique();

                entity.HasIndex(e => e.LibrarianId)
                    .HasName("LIBMGMT_LIBRARIANS_PK")
                    .IsUnique();

                entity.HasIndex(e => e.Username)
                    .HasName("SYS_C0093224")
                    .IsUnique();

                entity.Property(e => e.LibrarianId).HasColumnName("LIBRARIAN_ID");

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasColumnName("EMAIL")
                    .HasColumnType("varchar2")
                    .HasMaxLength(150);

                entity.Property(e => e.FullName)
                    .IsRequired()
                    .HasColumnName("FULL_NAME")
                    .HasColumnType("varchar2")
                    .HasMaxLength(100);

                entity.Property(e => e.IsActive).HasColumnName("IS_ACTIVE");

                entity.Property(e => e.JoinedDate)
                    .HasColumnName("JOINED_DATE")
                    .HasColumnType("date");

                entity.Property(e => e.LibrarianCode)
                    .IsRequired()
                    .HasColumnName("LIBRARIAN_CODE")
                    .HasColumnType("varchar2")
                    .HasMaxLength(20);

                entity.Property(e => e.PasswordHash)
                    .IsRequired()
                    .HasColumnName("PASSWORD_HASH")
                    .HasColumnType("varchar2")
                    .HasMaxLength(255);

                entity.Property(e => e.Phone)
                    .HasColumnName("PHONE")
                    .HasColumnType("varchar2")
                    .HasMaxLength(20);

                entity.Property(e => e.Username)
                    .IsRequired()
                    .HasColumnName("USERNAME")
                    .HasColumnType("varchar2")
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<LibmgmtMembers>(entity =>
            {
                entity.HasKey(e => e.MemberId);

                entity.ToTable("LIBMGMT_MEMBERS", "IUSF");

                entity.HasIndex(e => e.Email)
                    .HasName("SYS_C0093234")
                    .IsUnique();

                entity.HasIndex(e => e.MemberCode)
                    .HasName("SYS_C0093232")
                    .IsUnique();

                entity.HasIndex(e => e.MemberId)
                    .HasName("LIBMGMT_MEMBERS_PK")
                    .IsUnique();

                entity.HasIndex(e => e.Username)
                    .HasName("SYS_C0093233")
                    .IsUnique();

                entity.Property(e => e.MemberId).HasColumnName("MEMBER_ID");

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasColumnName("EMAIL")
                    .HasColumnType("varchar2")
                    .HasMaxLength(150);

                entity.Property(e => e.FullName)
                    .IsRequired()
                    .HasColumnName("FULL_NAME")
                    .HasColumnType("varchar2")
                    .HasMaxLength(100);

                entity.Property(e => e.IsActive).HasColumnName("IS_ACTIVE");

                entity.Property(e => e.JoinedDate)
                    .HasColumnName("JOINED_DATE")
                    .HasColumnType("date");

                entity.Property(e => e.MemberCode)
                    .IsRequired()
                    .HasColumnName("MEMBER_CODE")
                    .HasColumnType("varchar2")
                    .HasMaxLength(20);

                entity.Property(e => e.PasswordHash)
                    .IsRequired()
                    .HasColumnName("PASSWORD_HASH")
                    .HasColumnType("varchar2")
                    .HasMaxLength(255);

                entity.Property(e => e.Phone)
                    .HasColumnName("PHONE")
                    .HasColumnType("varchar2")
                    .HasMaxLength(20);

                entity.Property(e => e.Username)
                    .IsRequired()
                    .HasColumnName("USERNAME")
                    .HasColumnType("varchar2")
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<LibmgmtReservations>(entity =>
            {
                entity.HasKey(e => e.ReservationId);

                entity.ToTable("LIBMGMT_RESERVATIONS", "IUSF");

                entity.HasIndex(e => e.ReservationId)
                    .HasName("PK_LIBMGMT_RESERVATIONS")
                    .IsUnique();

                entity.HasIndex(e => new { e.MemberId, e.BookId })
                    .HasName("UQ_RESV_MEMBER_BOOK")
                    .IsUnique();

                entity.Property(e => e.ReservationId).HasColumnName("RESERVATION_ID");

                entity.Property(e => e.BookId).HasColumnName("BOOK_ID");

                entity.Property(e => e.MemberId).HasColumnName("MEMBER_ID");

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

                entity.ToTable("LIBMGMT_WISHLIST", "IUSF");

                entity.HasIndex(e => e.MemberId)
                    .HasName("IDX_WISH_MEMBER");

                entity.HasIndex(e => e.WishlistId)
                    .HasName("LIBMGMT_WISHLIST_PK")
                    .IsUnique();

                entity.HasIndex(e => new { e.MemberId, e.BookId })
                    .HasName("UQ_WISH_MEMBER_BOOK")
                    .IsUnique();

                entity.Property(e => e.WishlistId).HasColumnName("WISHLIST_ID");

                entity.Property(e => e.AddedDate)
                    .HasColumnName("ADDED_DATE")
                    .HasColumnType("date");

                entity.Property(e => e.BookId).HasColumnName("BOOK_ID");

                entity.Property(e => e.MemberId).HasColumnName("MEMBER_ID");

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
