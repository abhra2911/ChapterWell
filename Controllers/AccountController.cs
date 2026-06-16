using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Lib_Mgmt.Models;

namespace Lib_Mgmt.Controllers
{
    public class AccountController : Controller
    {
        // GET: /Account/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if ((model.Username == "lib" || model.Username == "liberian") && model.Password == "123")
            {
                return RedirectToAction(nameof(LibrarianDashboard));
            }

            if (model.Username == "mem" && model.Password == "123")
            {
                return RedirectToAction(nameof(MemberDashboard));
            }

            ViewBag.Error = "Invalid username or password.";
            return View();
        }

        // ===============================================================
        // Librarian portal — one action per tab.
        // ===============================================================

        // GET: /Account/LibrarianDashboard
        public IActionResult LibrarianDashboard()
        {
            ViewData["ActiveSection"] = "lib-dashboard";

            var model = new LibrarianDashboardViewModel
            {
                TotalMembers = 50,
                TotalTitles = 134,
                TotalCopiesAvailable = 412,
                BooksBorrowed = 7,
                OverdueCount = 3,
                NewMembersThisMonth = 5,
                TotalFinesDue = 120m,
                CopiesAvailable = 412,
                CopiesBorrowed = 7,
                CopiesOverdue = 3,
                CopiesDamaged = 8,

                BorrowingsPerMonth = new List<MonthlyCount>
                {
                    new MonthlyCount { Month = "Dec 2025", Count = 18 },
                    new MonthlyCount { Month = "Jan 2026", Count = 24 },
                    new MonthlyCount { Month = "Feb 2026", Count = 20 },
                    new MonthlyCount { Month = "Mar 2026", Count = 31 },
                    new MonthlyCount { Month = "Apr 2026", Count = 27 },
                    new MonthlyCount { Month = "May 2026", Count = 14 },
                },

                FinesPerMonth = new List<MonthlyAmount>
                {
                    new MonthlyAmount { Month = "Dec 2025", Amount = 80m  },
                    new MonthlyAmount { Month = "Jan 2026", Amount = 145m },
                    new MonthlyAmount { Month = "Feb 2026", Amount = 95m  },
                    new MonthlyAmount { Month = "Mar 2026", Amount = 200m },
                    new MonthlyAmount { Month = "Apr 2026", Amount = 160m },
                    new MonthlyAmount { Month = "May 2026", Amount = 70m  },
                },

                TopBorrowedBooks = new List<BookBorrowCount>
                {
                    new BookBorrowCount { Title = "Clean Code",               Count = 42 },
                    new BookBorrowCount { Title = "Computer Networks",        Count = 35 },
                    new BookBorrowCount { Title = "OS Concepts",              Count = 30 },
                    new BookBorrowCount { Title = "The Pragmatic Programmer", Count = 28 },
                    new BookBorrowCount { Title = "DBMS by Navathe",          Count = 21 },
                },
                OverdueBorrowings = new List<OverdueBorrowing>
                {
                    new OverdueBorrowing
                    {
                        MemberName = "User",
                        MemberId   = "MEM-00102",
                        BookTitle  = "Clean Code",
                        BorrowedOn = new DateTime(2026, 5, 10),
                        DueDate    = new DateTime(2026, 5, 24)
                    },
                    new OverdueBorrowing
                    {
                        MemberName = "User1",
                        MemberId   = "MEM-00108",
                        BookTitle  = "The Pragmatic Programmer",
                        BorrowedOn = new DateTime(2026, 5, 12),
                        DueDate    = new DateTime(2026, 5, 26)
                    },
                    new OverdueBorrowing
                    {
                        MemberName = "User2",
                        MemberId   = "MEM-00115",
                        BookTitle  = "Computer Networks",
                        BorrowedOn = new DateTime(2026, 5,  8),
                        DueDate    = new DateTime(2026, 5, 22)
                    }
                }
            };

            return View("LibrarianPortal", model);
        }

        // GET: /Account/LibrarianAccount
        public IActionResult LibrarianAccount()
        {
            ViewData["ActiveSection"] = "account";
            return View("LibrarianPortal");
        }

        // GET: /Account/LibrarianBooks
        public IActionResult LibrarianBooks()
        {
            ViewData["ActiveSection"] = "books";

            // TODO(Oracle): replace SampleCatalog() with a query over the BOOKS table,
            //               and SampleDamagedBooks() with a query over the damage-reports table.
            var model = new LibrarianBooksViewModel
            {
                Books = SampleCatalog(),
                Damaged = SampleDamagedBooks()
            };
            return View("LibrarianPortal", model);
        }

        // GET: /Account/LibrarianMembers
        public IActionResult LibrarianMembers()
        {
            ViewData["ActiveSection"] = "members";

            // TODO(Oracle): replace with a join over MEMBERS + BORROWINGS + BOOKS.
            var model = new LibrarianMembersViewModel
            {
                Borrowings = new List<MemberBorrowingRow>
                {
                    new MemberBorrowingRow
                    {
                        MemberName = "User",  MemberId = "MEM-00102", Email = "user@lib.org",
                        BookTitle = "Clean Code",
                        BorrowedOn = new DateTime(2026, 5, 10), DueDate = new DateTime(2026, 5, 24)
                    },
                    new MemberBorrowingRow
                    {
                        MemberName = "User1", MemberId = "MEM-00108", Email = "user1@lib.org",
                        BookTitle = "Operating System Concepts",
                        BorrowedOn = new DateTime(2026, 6, 3), DueDate = new DateTime(2026, 6, 30)
                    },
                    new MemberBorrowingRow
                    {
                        MemberName = "User2", MemberId = "MEM-00115", Email = "user2@lib.org",
                        BookTitle = "Computer Networks",
                        BorrowedOn = new DateTime(2026, 5, 8), DueDate = new DateTime(2026, 5, 22)
                    }
                }
            };
            return View("LibrarianPortal", model);
        }

        // GET: /Account/LibrarianBorrowings
        public IActionResult LibrarianBorrowings()
        {
            ViewData["ActiveSection"] = "borrowings";

            // TODO(Oracle): load active loans (RETURN_DATE IS NULL) joined with
            // MEMBERS + BOOKS, and the list of books that still have free copies.
            var model = new LibrarianBorrowingsViewModel
            {
                Active = SampleActiveBorrowings(),
                IssuableBooks = SampleCatalog().Where(b => b.AvailableCopies > 0).ToList()
            };
            return View("LibrarianPortal", model);
        }

        // ---------------------------------------------------------------
        // Librarian — modal POST handlers
        // ---------------------------------------------------------------

        // POST: /Account/EditLibrarianProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditLibrarianProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["FormError"] = FirstError();
                TempData["ReopenModal"] = "editProfileModal";
                return RedirectToAction(nameof(LibrarianAccount));
            }

            // TODO(Oracle): persist the updated profile.
            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction(nameof(LibrarianAccount));
        }

        // POST: /Account/ChangeLibrarianPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangeLibrarianPassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["FormError"] = FirstError();
                TempData["ReopenModal"] = "changePasswordModal";
                return RedirectToAction(nameof(LibrarianAccount));
            }

            // TODO(Oracle): verify current password, store the new hash.
            TempData["Success"] = "Password changed successfully.";
            return RedirectToAction(nameof(LibrarianAccount));
        }

        // POST: /Account/AddBook
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddBook(AddBookViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["FormError"] = FirstError();
                TempData["ReopenModal"] = "addBookModal";
                return RedirectToAction(nameof(LibrarianBooks));
            }

            // TODO(Oracle): INSERT INTO BOOKS (...).
            TempData["Success"] = $"\"{model.Title}\" added to the catalog.";
            return RedirectToAction(nameof(LibrarianBooks));
        }

        // POST: /Account/EditBook
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditBook(EditBookViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["FormError"] = FirstError();
                TempData["ReopenModal"] = "editBookModal";
                return RedirectToAction(nameof(LibrarianBooks));
            }

            // TODO(Oracle): UPDATE BOOKS SET ... WHERE ID = @model.Id.
            TempData["Success"] = $"\"{model.Title}\" was updated.";
            return RedirectToAction(nameof(LibrarianBooks));
        }

        // POST: /Account/DeleteBook
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteBook(int id, string title)
        {
            if (id <= 0)
            {
                TempData["FormError"] = "Could not identify the book to delete.";
                TempData["ReopenModal"] = "deleteBookModal";
                return RedirectToAction(nameof(LibrarianBooks));
            }

            // TODO(Oracle): DELETE FROM BOOKS WHERE ID = @id (guard against active loans).
            TempData["Success"] = string.IsNullOrEmpty(title)
                ? "Book removed from the catalog."
                : $"\"{title}\" was removed from the catalog.";
            return RedirectToAction(nameof(LibrarianBooks));
        }

        // POST: /Account/ReportDamage
        // Logs a new damage report and (when wired) reduces the book's
        // AVAILABLE_COPIES by the damaged count.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ReportDamage(int bookId, int damagedCopies, string reason)
        {
            if (bookId <= 0 || damagedCopies <= 0)
            {
                TempData["FormError"] = "Please pick a book and enter a valid copy count.";
                TempData["ReopenModal"] = "reportDamageModal";
                return RedirectToAction(nameof(LibrarianBooks));
            }

            // TODO(Oracle): INSERT INTO BOOK_DAMAGES (BOOK_ID, COPIES, REASON, REPORTED_DATE, REPORTED_BY)
            //               and UPDATE BOOKS SET AVAILABLE_COPIES = AVAILABLE_COPIES - @damagedCopies
            //               (floor at 0; do not reduce below 0).
            var title = SampleCatalog().FirstOrDefault(b => b.Id == bookId)?.Title;
            TempData["Success"] = string.IsNullOrEmpty(title)
                ? $"Damage logged: {damagedCopies} copy/copies."
                : $"Damage logged: {damagedCopies} copy/copies of \"{title}\".";
            return RedirectToAction(nameof(LibrarianBooks));
        }

        // POST: /Account/IssueBook
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult IssueBook(IssueBookViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["FormError"] = FirstError();
                TempData["ReopenModal"] = "issueBookModal";
                return RedirectToAction(nameof(LibrarianBorrowings));
            }

            // TODO(Oracle): INSERT INTO BORROWINGS (MEMBER_ID, BOOK_ID, ISSUE_DATE, DUE_DATE)
            //               and decrement BOOKS.AVAILABLE_COPIES for @model.BookId.
            var title = SampleCatalog().FirstOrDefault(b => b.Id == model.BookId)?.Title;
            TempData["Success"] = string.IsNullOrEmpty(title)
                ? $"Book issued to {model.MemberId}."
                : $"\"{title}\" issued to {model.MemberId}.";
            return RedirectToAction(nameof(LibrarianBorrowings));
        }

        // POST: /Account/ReturnBook
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ReturnBook(int id, string title)
        {
            if (id <= 0)
            {
                TempData["FormError"] = "Could not identify the borrowing to return.";
                TempData["ReopenModal"] = "returnBookModal";
                return RedirectToAction(nameof(LibrarianBorrowings));
            }

            // TODO(Oracle): UPDATE BORROWINGS SET RETURN_DATE = SYSDATE, STATUS = 'RETURNED'
            //               WHERE BORROWING_ID = @id; increment BOOKS.AVAILABLE_COPIES;
            //               settle any outstanding fine attached to this loan.
            TempData["Success"] = string.IsNullOrEmpty(title)
                ? "Book marked as returned."
                : $"\"{title}\" marked as returned.";
            return RedirectToAction(nameof(LibrarianBorrowings));
        }

        // ===============================================================
        // Member portal — one action per tab.
        // ===============================================================

        // GET: /Account/MemberDashboard
        public IActionResult MemberDashboard()
        {
            ViewData["ActiveSection"] = "dashboard";

            var loans = new List<MemberLoan>
            {
                new MemberLoan
                {
                    Title      = "Clean Code",
                    BorrowedOn = new DateTime(2026, 5, 10),
                    DueDate    = new DateTime(2026, 5, 24)
                },
                new MemberLoan
                {
                    Title      = "Operating System Concepts",
                    BorrowedOn = new DateTime(2026, 5, 22),
                    DueDate    = new DateTime(2026, 6, 5)
                }
            };

            var model = new MemberDashboardViewModel
            {
                MemberName = "User",
                ActiveLoans = loans,
                CurrentlyBorrowed = loans.Count,
                OverdueCount = loans.Count(l => l.DaysOverdue > 0),
                FineDue = loans.Sum(l => l.Fine),

                // Set CoverImage per row (e.g. "/images/covers/{isbn}.jpg");
                // when left empty the view falls back to placeholder-cover.svg.
                TopBooks = new List<TopBook>
                {
                    new TopBook { Title = "Clean Code",                             Author = "Robert C. Martin",      Isbn = "9780132350884", BorrowCount = 142, Available = true  },
                    new TopBook { Title = "Introduction to Algorithms",             Author = "Cormen et al.",         Isbn = "9780262033848", BorrowCount = 128, Available = true  },
                    new TopBook { Title = "Computer Networks",                      Author = "Andrew S. Tanenbaum",   Isbn = "9780132126953", BorrowCount = 119, Available = false },
                    new TopBook { Title = "Operating System Concepts",             Author = "Silberschatz et al.",   Isbn = "9781118063330", BorrowCount = 110, Available = true  },
                    new TopBook { Title = "The Pragmatic Programmer",              Author = "Hunt & Thomas",         Isbn = "9780201616224", BorrowCount = 103, Available = true  },
                    new TopBook { Title = "Database System Concepts",              Author = "Silberschatz et al.",   Isbn = "9780073523323", BorrowCount = 97,  Available = false },
                    new TopBook { Title = "Design Patterns",                       Author = "Gamma et al.",          Isbn = "9780201633610", BorrowCount = 89,  Available = true  },
                    new TopBook { Title = "The C Programming Language",            Author = "Kernighan & Ritchie",   Isbn = "9780131103627", BorrowCount = 84,  Available = true  },
                    new TopBook { Title = "Artificial Intelligence",               Author = "Russell & Norvig",      Isbn = "9780136042594", BorrowCount = 76,  Available = true  },
                    new TopBook { Title = "Structure and Interpretation of Computer Programs", Author = "Abelson & Sussman", Isbn = "9780262011532", BorrowCount = 71, Available = false }
                }
            };

            return View("MemberPortal", model);
        }

        // GET: /Account/MemberAccount
        public IActionResult MemberAccount()
        {
            ViewData["ActiveSection"] = "account";
            return View("MemberPortal");
        }

        // GET: /Account/MemberBooks
        public IActionResult MemberBooks()
        {
            ViewData["ActiveSection"] = "books";

            // TODO(Oracle): replace SampleCatalog() with a query over the BOOKS table,
            //               and SampleWishlist() with a query over the WISHLIST table for
            //               the currently-signed-in member.
            var wish = SampleWishlist();
            var model = new MemberBooksViewModel
            {
                Books = SampleCatalog(),
                WishlistedBookIds = new HashSet<int>(
                    SampleCatalog()
                        .Where(b => wish.Any(w => w.Isbn == b.Isbn))
                        .Select(b => b.Id))
            };
            return View("MemberPortal", model);
        }

        // GET: /Account/MemberLoans
        public IActionResult MemberLoans()
        {
            ViewData["ActiveSection"] = "loans";

            // TODO(Oracle): active = BORROWINGS WHERE MEMBER_ID = @me AND RETURN_DATE IS NULL;
            //               history = BORROWINGS WHERE MEMBER_ID = @me AND RETURN_DATE IS NOT NULL;
            //               fines = FINES WHERE MEMBER_ID = @me (paid + outstanding).
            var model = new MemberLoansViewModel
            {
                MemberName = "User",
                Active = SampleActiveLoans(),
                History = SampleLoanHistory(),
                Fines = SampleFines()
            };
            return View("MemberPortal", model);
        }

        // GET: /Account/MemberWishlist
        public IActionResult MemberWishlist()
        {
            ViewData["ActiveSection"] = "wishlist";

            // TODO(Oracle): SELECT items from WISHLIST joined with BOOKS for this member.
            var wish = SampleWishlist();
            var onList = new HashSet<string>(wish.Select(w => w.Isbn));

            var model = new MemberWishlistViewModel
            {
                Items = wish,
                AddableBooks = SampleCatalog().Where(b => !onList.Contains(b.Isbn)).ToList()
            };
            return View("MemberPortal", model);
        }

        // NOTE: MemberFines used to be its own tab. It's now a section inside
        // the Loans tab (see MemberLoans above), so the route is no longer
        // exposed in the nav. The PayFine / PayAllFines POST handlers below
        // redirect back to MemberLoans.

        // ---------------------------------------------------------------
        // Member — modal POST handlers
        // ---------------------------------------------------------------

        // POST: /Account/EditMemberProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditMemberProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["FormError"] = FirstError();
                TempData["ReopenModal"] = "editProfileModal";
                return RedirectToAction(nameof(MemberAccount));
            }

            // TODO(Oracle): persist the updated profile.
            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction(nameof(MemberAccount));
        }

        // POST: /Account/ChangeMemberPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangeMemberPassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["FormError"] = FirstError();
                TempData["ReopenModal"] = "changePasswordModal";
                return RedirectToAction(nameof(MemberAccount));
            }

            // TODO(Oracle): verify current password, store the new hash.
            TempData["Success"] = "Password changed successfully.";
            return RedirectToAction(nameof(MemberAccount));
        }

        // POST: /Account/ToggleWishlist
        // Posted by the bookmark button on each book card in the Books tab.
        // Adds the book if not already on the wishlist, removes it otherwise.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleWishlist(int bookId)
        {
            if (bookId <= 0)
            {
                TempData["FormError"] = "Could not identify the book.";
                return RedirectToAction(nameof(MemberBooks));
            }

            // TODO(Oracle):
            //   IF EXISTS(SELECT 1 FROM WISHLIST WHERE MEMBER_ID=@me AND BOOK_ID=@bookId)
            //     THEN DELETE the row
            //     ELSE INSERT a new row
            //   The unique(MEMBER_ID, BOOK_ID) constraint guards against races.
            var book = SampleCatalog().FirstOrDefault(b => b.Id == bookId);
            var alreadyWishlisted = SampleWishlist().Any(w => book != null && w.Isbn == book.Isbn);

            if (book == null)
            {
                TempData["Success"] = "Wishlist updated.";
            }
            else if (alreadyWishlisted)
            {
                TempData["Success"] = $"\"{book.Title}\" removed from your wishlist.";
            }
            else
            {
                TempData["Success"] = $"\"{book.Title}\" added to your wishlist.";
            }
            return RedirectToAction(nameof(MemberBooks));
        }

        // POST: /Account/AddToWishlist
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddToWishlist(int bookId)
        {
            if (bookId <= 0)
            {
                TempData["FormError"] = "Please choose a book to add.";
                TempData["ReopenModal"] = "addWishlistModal";
                return RedirectToAction(nameof(MemberWishlist));
            }

            // TODO(Oracle): INSERT INTO WISHLIST (MEMBER_ID, BOOK_ID, ADDED_DATE)
            //               (let a unique constraint guard against duplicates).
            var title = SampleCatalog().FirstOrDefault(b => b.Id == bookId)?.Title;
            TempData["Success"] = string.IsNullOrEmpty(title)
                ? "Added to your wishlist."
                : $"\"{title}\" added to your wishlist.";
            return RedirectToAction(nameof(MemberWishlist));
        }

        // POST: /Account/RemoveFromWishlist
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveFromWishlist(int id, string title)
        {
            // TODO(Oracle): DELETE FROM WISHLIST WHERE WISHLIST_ID = @id AND MEMBER_ID = @me.
            TempData["Success"] = string.IsNullOrEmpty(title)
                ? "Removed from your wishlist."
                : $"\"{title}\" removed from your wishlist.";
            return RedirectToAction(nameof(MemberWishlist));
        }

        // POST: /Account/RenewLoan
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RenewLoan(int id, string title)
        {
            // TODO(Oracle): UPDATE BORROWINGS SET DUE_DATE = DUE_DATE + 14 WHERE BORROWING_ID = @id
            //               (reject if the loan is overdue or the title is reserved by someone else).
            TempData["Success"] = string.IsNullOrEmpty(title)
                ? "Loan renewed for 14 more days."
                : $"\"{title}\" renewed for 14 more days.";
            return RedirectToAction(nameof(MemberLoans));
        }

        // POST: /Account/PayFine
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PayFine(int id, string book)
        {
            // TODO(Oracle): UPDATE FINES SET PAID_DATE = SYSDATE WHERE FINE_ID = @id AND MEMBER_ID = @me.
            TempData["Success"] = string.IsNullOrEmpty(book)
                ? "Fine paid. Thank you!"
                : $"Fine for \"{book}\" paid. Thank you!";
            return RedirectToAction(nameof(MemberLoans));
        }

        // POST: /Account/PayAllFines
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PayAllFines()
        {
            // TODO(Oracle): UPDATE FINES SET PAID_DATE = SYSDATE
            //               WHERE MEMBER_ID = @me AND PAID_DATE IS NULL.
            TempData["Success"] = "All outstanding fines paid. Thank you!";
            return RedirectToAction(nameof(MemberLoans));
        }

        // GET: /Account/Logout
        public IActionResult Logout()
        {
            return RedirectToAction(nameof(Login));
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------
        private string FirstError()
        {
            return ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault() ?? "Please correct the errors and try again.";
        }

        /// <summary>
        /// Sample catalog used by both Books pages until Oracle is connected.
        /// TODO(Oracle): delete this and load CatalogBook rows from the database.
        /// </summary>
        private static List<CatalogBook> SampleCatalog()
        {
            return new List<CatalogBook>
            {
                new CatalogBook { Id = 1, Title = "Clean Code", Author = "Robert C. Martin", Genre = "Programming", Isbn = "9780132350884", Publisher = "Prentice Hall", PublishedYear = 2008, Quantity = 12, AvailableCopies = 5 },
                new CatalogBook { Id = 2, Title = "The Pragmatic Programmer", Author = "Hunt & Thomas", Genre = "Programming", Isbn = "9780201616224", Publisher = "Addison-Wesley", PublishedYear = 1999, Quantity = 8, AvailableCopies = 0 },
                new CatalogBook { Id = 3, Title = "Computer Networks", Author = "Andrew S. Tanenbaum", Genre = "Networking", Isbn = "9780132126953", Publisher = "Pearson", PublishedYear = 2010, Quantity = 19, AvailableCopies = 7 },
                new CatalogBook { Id = 4, Title = "Operating System Concepts", Author = "Silberschatz et al.", Genre = "Operating Systems", Isbn = "9781118063330", Publisher = "Wiley", PublishedYear = 2012, Quantity = 14, AvailableCopies = 3 },
                new CatalogBook { Id = 5, Title = "Introduction to Algorithms", Author = "Cormen et al.", Genre = "Algorithms", Isbn = "9780262033848", Publisher = "MIT Press", PublishedYear = 2009, Quantity = 10, AvailableCopies = 6 },
                new CatalogBook { Id = 6, Title = "Database System Concepts", Author = "Silberschatz et al.", Genre = "Database", Isbn = "9780073523323", Publisher = "McGraw-Hill", PublishedYear = 2010, Quantity = 9, AvailableCopies = 0 },
                new CatalogBook { Id = 7, Title = "Design Patterns", Author = "Gamma et al.", Genre = "Programming", Isbn = "9780201633610", Publisher = "Addison-Wesley", PublishedYear = 1994, Quantity = 7, AvailableCopies = 4 },
                new CatalogBook { Id = 8, Title = "The C Programming Language", Author = "Kernighan & Ritchie", Genre = "Programming", Isbn = "9780131103627", Publisher = "Prentice Hall", PublishedYear = 1988, Quantity = 11, AvailableCopies = 2 },
                new CatalogBook { Id = 9, Title = "Artificial Intelligence: A Modern Approach", Author = "Russell & Norvig", Genre = "AI", Isbn = "9780136042594", Publisher = "Pearson", PublishedYear = 2009, Quantity = 6, AvailableCopies = 1 },
                new CatalogBook { Id = 10, Title = "Structure and Interpretation of Computer Programs", Author = "Abelson & Sussman", Genre = "Programming", Isbn = "9780262011532", Publisher = "MIT Press", PublishedYear = 1996, Quantity = 5, AvailableCopies = 0 }
            };
        }

        // ---------------------------------------------------------------
        // Sample data for the Borrowings / Loans / Wishlist / Fines tabs.
        // TODO(Oracle): delete all of these once the database is connected.
        // ---------------------------------------------------------------

        private static List<ActiveBorrowingRow> SampleActiveBorrowings()
        {
            return new List<ActiveBorrowingRow>
            {
                new ActiveBorrowingRow { BorrowingId = 5001, MemberName = "User",  MemberId = "MEM-00102", BookTitle = "Clean Code",                 Isbn = "9780132350884", BorrowedOn = new DateTime(2026, 5, 10), DueDate = new DateTime(2026, 5, 24) },
                new ActiveBorrowingRow { BorrowingId = 5002, MemberName = "User1", MemberId = "MEM-00108", BookTitle = "Operating System Concepts",  Isbn = "9781118063330", BorrowedOn = new DateTime(2026, 6,  3), DueDate = new DateTime(2026, 6, 30) },
                new ActiveBorrowingRow { BorrowingId = 5003, MemberName = "User2", MemberId = "MEM-00115", BookTitle = "Computer Networks",          Isbn = "9780132126953", BorrowedOn = new DateTime(2026, 5,  8), DueDate = new DateTime(2026, 5, 22) },
                new ActiveBorrowingRow { BorrowingId = 5004, MemberName = "User3", MemberId = "MEM-00121", BookTitle = "Introduction to Algorithms", Isbn = "9780262033848", BorrowedOn = new DateTime(2026, 6,  9), DueDate = new DateTime(2026, 6, 23) }
            };
        }

        private static List<DetailedLoan> SampleActiveLoans()
        {
            return new List<DetailedLoan>
            {
                new DetailedLoan { BorrowingId = 5001, Title = "Clean Code",                Author = "Robert C. Martin",   Isbn = "9780132350884", BorrowedOn = new DateTime(2026, 5, 10), DueDate = new DateTime(2026, 5, 24) },
                new DetailedLoan { BorrowingId = 5005, Title = "Operating System Concepts", Author = "Silberschatz et al.", Isbn = "9781118063330", BorrowedOn = new DateTime(2026, 5, 22), DueDate = new DateTime(2026, 6, 16) },
                new DetailedLoan { BorrowingId = 5006, Title = "Design Patterns",           Author = "Gamma et al.",       Isbn = "9780201633610", BorrowedOn = new DateTime(2026, 6,  4), DueDate = new DateTime(2026, 6, 18) }
            };
        }

        private static List<LoanHistoryRow> SampleLoanHistory()
        {
            return new List<LoanHistoryRow>
            {
                new LoanHistoryRow { Title = "The Pragmatic Programmer",   Author = "Hunt & Thomas",       BorrowedOn = new DateTime(2026, 3,  2), ReturnedOn = new DateTime(2026, 3, 15), FinePaid = 0m,  WasLate = false },
                new LoanHistoryRow { Title = "The C Programming Language", Author = "Kernighan & Ritchie", BorrowedOn = new DateTime(2026, 2, 10), ReturnedOn = new DateTime(2026, 3,  1), FinePaid = 18m, WasLate = true  },
                new LoanHistoryRow { Title = "Database System Concepts",   Author = "Silberschatz et al.", BorrowedOn = new DateTime(2026, 1, 18), ReturnedOn = new DateTime(2026, 2,  1), FinePaid = 0m,  WasLate = false },
                new LoanHistoryRow { Title = "Artificial Intelligence",    Author = "Russell & Norvig",    BorrowedOn = new DateTime(2025, 12, 5), ReturnedOn = new DateTime(2025, 12, 28), FinePaid = 12m, WasLate = true }
            };
        }

        private static List<WishlistItem> SampleWishlist()
        {
            return new List<WishlistItem>
            {
                new WishlistItem { Id = 9001, Title = "Introduction to Algorithms", Author = "Cormen et al.",       Isbn = "9780262033848", Genre = "Algorithms", Available = true,  AddedOn = new DateTime(2026, 6,  1) },
                new WishlistItem { Id = 9002, Title = "Database System Concepts",   Author = "Silberschatz et al.", Isbn = "9780073523323", Genre = "Database",   Available = false, AddedOn = new DateTime(2026, 5, 28) },
                new WishlistItem { Id = 9003, Title = "Artificial Intelligence",    Author = "Russell & Norvig",    Isbn = "9780136042594", Genre = "AI",         Available = true,  AddedOn = new DateTime(2026, 5, 20) }
            };
        }

        private static List<FineRecord> SampleFines()
        {
            return new List<FineRecord>
            {
                new FineRecord { Id = 7001, BookTitle = "Clean Code",                 Reason = "Overdue (6 days)", Amount = 30m, IssuedOn = new DateTime(2026, 5, 30),  PaidOn = null },
                new FineRecord { Id = 7002, BookTitle = "Computer Networks",          Reason = "Overdue (3 days)", Amount = 15m, IssuedOn = new DateTime(2026, 5, 25),  PaidOn = null },
                new FineRecord { Id = 7003, BookTitle = "The C Programming Language", Reason = "Overdue (9 days)", Amount = 18m, IssuedOn = new DateTime(2026, 3,  1),  PaidOn = new DateTime(2026, 3,  3) },
                new FineRecord { Id = 7004, BookTitle = "Artificial Intelligence",    Reason = "Overdue (6 days)", Amount = 12m, IssuedOn = new DateTime(2025, 12, 28), PaidOn = new DateTime(2025, 12, 29) }
            };
        }

        private static List<DamagedBookEntry> SampleDamagedBooks()
        {
            return new List<DamagedBookEntry>
            {
                new DamagedBookEntry { Id = 8001, BookId = 1, Title = "Clean Code",                Author = "Robert C. Martin",   Isbn = "9780132350884", DamagedCopies = 2, Reason = "Water damage from spilled coffee", ReportedOn = new DateTime(2026, 4, 15), ReportedBy = "Library Admin" },
                new DamagedBookEntry { Id = 8002, BookId = 3, Title = "Computer Networks",         Author = "Andrew S. Tanenbaum",Isbn = "9780132126953", DamagedCopies = 1, Reason = "Torn binding",                     ReportedOn = new DateTime(2026, 5,  2), ReportedBy = "Library Admin" },
                new DamagedBookEntry { Id = 8003, BookId = 5, Title = "Introduction to Algorithms",Author = "Cormen et al.",      Isbn = "9780262033848", DamagedCopies = 3, Reason = "Pages missing (chapter 22)",       ReportedOn = new DateTime(2026, 5, 20), ReportedBy = "Library Admin" },
                new DamagedBookEntry { Id = 8004, BookId = 8, Title = "The C Programming Language",Author = "Kernighan & Ritchie",Isbn = "9780131103627", DamagedCopies = 2, Reason = "Cover detached",                   ReportedOn = new DateTime(2026, 6,  8), ReportedBy = "Library Admin" }
            };
        }
    }
}
