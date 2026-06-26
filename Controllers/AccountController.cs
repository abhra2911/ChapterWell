using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Lib_Mgmt.Data;
using Lib_Mgmt.Models;

namespace Lib_Mgmt.Controllers
{
    public class AccountController : Controller
    {
        private readonly ModelContext _context;
        private readonly LibraryRepository _repo;

        public AccountController(LibraryRepository repo, ModelContext context)
        {
            _context = context;
            _repo = repo;
        }

        // ---------------------------------------------------------------
        // Auth guards. Each returns the current user id, or null + a redirect
        // target when the session is missing / the wrong role.
        // ---------------------------------------------------------------
        private bool TryLibrarian(out int id)
        {
            id = CurrentUser.Id(HttpContext.Session);
            return CurrentUser.IsLibrarian(HttpContext.Session) && id > 0;
        }

        private bool TryMember(out int id)
        {
            id = CurrentUser.Id(HttpContext.Session);
            return CurrentUser.IsMember(HttpContext.Session) && id > 0;
        }

        // GET: /Account/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Username) || string.IsNullOrEmpty(model.Password))
            {
                ViewBag.Error = "Please enter your username (or email) and password.";
                return View();
            }

            var user = _repo.FindUserByUsername(model.Username.Trim());

            // Verify against the stored bcrypt hash. BCrypt.Verify is constant-time
            // and handles the $2a/$2b prefixes in the seed data.
            bool ok = user != null
                      && !string.IsNullOrEmpty(user.PasswordHash)
                      && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash);

            if (!ok)
            {
                ViewBag.Error = "Invalid username or password.";
                return View();
            }

            CurrentUser.SignIn(HttpContext.Session, user.UserId, user.Role,
                               user.FullName, user.Username, user.Code);

            return user.Role == "LIBRARIAN"
                ? RedirectToAction(nameof(LibrarianDashboard))
                : RedirectToAction(nameof(MemberDashboard));
        }


        // GET: /Account/Logout
        public IActionResult Logout()
        {
            CurrentUser.SignOut(HttpContext.Session);
            return RedirectToAction(nameof(Login));
        }


        // ===============================================================
        // Librarian portal — one action per tab.
        // ===============================================================

        // GET: /Account/LibrarianDashboard
        public IActionResult LibrarianDashboard()
        {
            if (!TryLibrarian(out _)) return RedirectToAction(nameof(Login));

            ViewData["ActiveSection"] = "lib-dashboard";
            var model = _repo.GetLibrarianDashboard();
            return View("LibrarianPortal", model);
        }

        // GET: /Account/LibrarianAccount
        public IActionResult LibrarianAccount()
        {
            if (!TryLibrarian(out var id)) return RedirectToAction(nameof(Login));

            ViewData["ActiveSection"] = "account";
            LoadProfileIntoViewData("LIBRARIAN", id);
            return View("LibrarianPortal");
        }

        // GET: /Account/LibrarianBooks
        public IActionResult LibrarianBooks()
        {
            if (!TryLibrarian(out _)) return RedirectToAction(nameof(Login));

            ViewData["ActiveSection"] = "books";
            var model = new LibrarianBooksViewModel
            {
                Books = _repo.GetCatalog(),
                Damaged = _repo.GetDamagedBooks()
            };
            return View("LibrarianPortal", model);
        }

        // GET: /Account/LibrarianMembers
        public IActionResult LibrarianMembers()
        {
            if (!TryLibrarian(out _)) return RedirectToAction(nameof(Login));

            ViewData["ActiveSection"] = "members";
            var model = new LibrarianMembersViewModel
            {
                Members = _repo.GetRegisteredMembers()
            };
            return View("LibrarianPortal", model);
        }

        // GET: /Account/LibrarianBorrowings
        public IActionResult LibrarianBorrowings()
        {
            if (!TryLibrarian(out _)) return RedirectToAction(nameof(Login));

            ViewData["ActiveSection"] = "borrowings";
            var model = new LibrarianBorrowingsViewModel
            {
                Active = _repo.GetActiveBorrowings(),
                IssuableBooks = _repo.GetCatalog().Where(b => b.AvailableCopies > 0).ToList(),
                PendingReservations = _repo.GetPendingReservations()
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
            if (!TryLibrarian(out var id)) return RedirectToAction(nameof(Login));

            if (!ModelState.IsValid)
            {
                TempData["FormError"] = FirstError();
                TempData["ReopenModal"] = "editProfileModal";
                return RedirectToAction(nameof(LibrarianAccount));
            }

            _repo.UpdateProfile("LIBRARIAN", id, model.Name, model.Email, model.Phone);
            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction(nameof(LibrarianAccount));
        }

        // POST: /Account/ChangeLibrarianPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangeLibrarianPassword(ChangePasswordViewModel model)
        {
            if (!TryLibrarian(out var id)) return RedirectToAction(nameof(Login));

            if (!ModelState.IsValid)
            {
                TempData["FormError"] = FirstError();
                TempData["ReopenModal"] = "changePasswordModal";
                return RedirectToAction(nameof(LibrarianAccount));
            }

            if (!ChangePassword("LIBRARIAN", id, model))
            {
                TempData["FormError"] = "Your current password is incorrect.";
                TempData["ReopenModal"] = "changePasswordModal";
                return RedirectToAction(nameof(LibrarianAccount));
            }

            TempData["Success"] = "Password changed successfully.";
            return RedirectToAction(nameof(LibrarianAccount));
        }

        // POST: /Account/AddBook
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddBook(AddBookViewModel model)
        {
            if (!TryLibrarian(out _)) return RedirectToAction(nameof(Login));

            if (!ModelState.IsValid)
            {
                TempData["FormError"] = FirstError();
                TempData["ReopenModal"] = "addBookModal";
                return RedirectToAction(nameof(LibrarianBooks));
            }

            var title = _repo.AddBook(model);
            TempData["Success"] = $"\"{title}\" added to the catalog.";
            return RedirectToAction(nameof(LibrarianBooks));
        }

        // POST: /Account/AddMember
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddMember(AddMemberViewModel model)
        {
            if (!TryLibrarian(out _)) return RedirectToAction(nameof(Login));

            if (!ModelState.IsValid)
            {
                TempData["FormError"] = FirstError();
                TempData["ReopenModal"] = "addMemberModal";
                return RedirectToAction(nameof(LibrarianMembers));
            }

            int newId;
            string code;
            var result = _repo.AddMember(model, out newId, out code);

            if (result == LibraryRepository.AddMemberResult.UsernameTaken)
            {
                TempData["FormError"] = "That username is already taken.";
                TempData["ReopenModal"] = "addMemberModal";
                return RedirectToAction(nameof(LibrarianMembers));
            }
            if (result == LibraryRepository.AddMemberResult.EmailTaken)
            {
                TempData["FormError"] = "A member with that email already exists.";
                TempData["ReopenModal"] = "addMemberModal";
                return RedirectToAction(nameof(LibrarianMembers));
            }

            TempData["Success"] = $"Member \"{model.FullName}\" added (code {code}).";
            return RedirectToAction(nameof(LibrarianMembers));
        }

        // POST: /Account/DeleteMember
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteMember(int id, string name)
        {
            if (!TryLibrarian(out _)) return RedirectToAction(nameof(Login));

            if (id <= 0)
            {
                TempData["FormError"] = "Could not identify the member to delete.";
                return RedirectToAction(nameof(LibrarianMembers));
            }

            var label = string.IsNullOrEmpty(name) ? "The member" : $"\"{name}\"";
            switch (_repo.DeleteMember(id))
            {
                case LibraryRepository.DeleteMemberResult.Deleted:
                    TempData["Success"] = $"{label} has been removed.";
                    break;
                case LibraryRepository.DeleteMemberResult.HasActiveLoans:
                    TempData["FormError"] = $"{label} still has active loans and can't be removed.";
                    break;
                default:
                    TempData["FormError"] = "That member no longer exists.";
                    break;
            }
            return RedirectToAction(nameof(LibrarianMembers));
        }

        // POST: /Account/EditBook
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditBook(EditBookViewModel model)
        {
            if (!TryLibrarian(out _)) return RedirectToAction(nameof(Login));

            if (!ModelState.IsValid)
            {
                TempData["FormError"] = FirstError();
                TempData["ReopenModal"] = "editBookModal";
                return RedirectToAction(nameof(LibrarianBooks));
            }

            _repo.EditBook(model);
            TempData["Success"] = $"\"{model.Title}\" was updated.";
            return RedirectToAction(nameof(LibrarianBooks));
        }

        // POST: /Account/DeleteBook
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteBook(int id, string title)
        {
            if (!TryLibrarian(out _)) return RedirectToAction(nameof(Login));

            if (id <= 0)
            {
                TempData["FormError"] = "Could not identify the book to delete.";
                TempData["ReopenModal"] = "deleteBookModal";
                return RedirectToAction(nameof(LibrarianBooks));
            }

            var label = string.IsNullOrEmpty(title) ? "The book" : $"\"{title}\"";
            switch (_repo.DeleteBook(id))
            {
                case LibraryRepository.DeleteResult.Deleted:
                    TempData["Success"] = $"{label} was removed from the catalog.";
                    break;
                case LibraryRepository.DeleteResult.HasActiveLoans:
                    TempData["FormError"] = $"{label} still has active loans and can't be removed.";
                    break;
                case LibraryRepository.DeleteResult.HasHistory:
                    TempData["FormError"] = $"{label} has loan history and can't be deleted (consider setting copies to 0 instead).";
                    break;
                default:
                    TempData["FormError"] = "That book no longer exists.";
                    break;
            }
            return RedirectToAction(nameof(LibrarianBooks));
        }

        // POST: /Account/ReportDamage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ReportDamage(int bookId, int damagedCopies, string reason)
        {
            if (!TryLibrarian(out var libId)) return RedirectToAction(nameof(Login));

            if (bookId <= 0 || damagedCopies <= 0)
            {
                TempData["FormError"] = "Please pick a book and enter a valid copy count.";
                TempData["ReopenModal"] = "reportDamageModal";
                return RedirectToAction(nameof(LibrarianBooks));
            }

            var title = _repo.ReportDamage(bookId, damagedCopies, reason, libId);
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
            if (!TryLibrarian(out _)) return RedirectToAction(nameof(Login));

            if (!ModelState.IsValid)
            {
                TempData["FormError"] = FirstError();
                TempData["ReopenModal"] = "issueBookModal";
                return RedirectToAction(nameof(LibrarianBorrowings));
            }

            var result = _repo.IssueBook(model.MemberId, model.BookId, model.DueDate, out var title);
            switch (result)
            {
                case LibraryRepository.IssueResult.Ok:
                    TempData["Success"] = string.IsNullOrEmpty(title)
                        ? $"Book issued to {model.MemberId}."
                        : $"\"{title}\" issued to {model.MemberId}.";
                    break;
                case LibraryRepository.IssueResult.MemberNotFound:
                    TempData["FormError"] = $"No active member with ID {model.MemberId}.";
                    TempData["ReopenModal"] = "issueBookModal";
                    break;
                case LibraryRepository.IssueResult.NoCopies:
                    TempData["FormError"] = "That title has no available copies right now.";
                    TempData["ReopenModal"] = "issueBookModal";
                    break;
                default:
                    TempData["FormError"] = "That book could not be found.";
                    TempData["ReopenModal"] = "issueBookModal";
                    break;
            }
            return RedirectToAction(nameof(LibrarianBorrowings));
        }

        // POST: /Account/FulfillReservation
        // Issues the reserved book straight to the member who requested it
        // and removes the reservation from the queue.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult FulfillReservation(int id, string title)
        {
            if (!TryLibrarian(out _)) return RedirectToAction(nameof(Login));

            var result = _repo.FulfillReservation(id);
            switch (result)
            {
                case LibraryRepository.FulfillResult.Ok:
                    TempData["Success"] = string.IsNullOrEmpty(title)
                        ? "Reservation fulfilled."
                        : $"\"{title}\" issued — reservation fulfilled.";
                    break;
                case LibraryRepository.FulfillResult.NoCopies:
                    TempData["FormError"] = "No copies are available for that book yet.";
                    break;
                default:
                    TempData["FormError"] = "Could not find that reservation.";
                    break;
            }
            return RedirectToAction(nameof(LibrarianBorrowings));
        }

        // POST: /Account/ReturnBook
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ReturnBook(int id, string title)
        {
            if (!TryLibrarian(out _)) return RedirectToAction(nameof(Login));

            if (id <= 0)
            {
                TempData["FormError"] = "Could not identify the borrowing to return.";
                TempData["ReopenModal"] = "returnBookModal";
                return RedirectToAction(nameof(LibrarianBorrowings));
            }

            var ok = _repo.ReturnBook(id);
            if (ok)
            {
                TempData["Success"] = string.IsNullOrEmpty(title)
                    ? "Book marked as returned."
                    : $"\"{title}\" marked as returned.";
            }
            else
            {
                TempData["FormError"] = "That loan was already returned or no longer exists.";
            }
            return RedirectToAction(nameof(LibrarianBorrowings));
        }

        // ===============================================================
        // Member portal — one action per tab.
        // ===============================================================

        // GET: /Account/MemberDashboard
        public IActionResult MemberDashboard()
        {
            if (!TryMember(out var id)) return RedirectToAction(nameof(Login));

            ViewData["ActiveSection"] = "dashboard";
            var model = _repo.GetMemberDashboard(id, CurrentUser.FullName(HttpContext.Session));
            return View("MemberPortal", model);
        }

        // GET: /Account/MemberAccount
        public IActionResult MemberAccount()
        {
            if (!TryMember(out var id)) return RedirectToAction(nameof(Login));

            ViewData["ActiveSection"] = "account";
            LoadProfileIntoViewData("MEMBER", id);
            return View("MemberPortal");
        }

        // GET: /Account/MemberBooks
        public IActionResult MemberBooks()
        {
            if (!TryMember(out var id)) return RedirectToAction(nameof(Login));

            ViewData["ActiveSection"] = "books";
            var model = new MemberBooksViewModel
            {
                Books = _repo.GetCatalog(),
                WishlistedBookIds = _repo.GetWishlistedBookIds(id)
            };
            return View("MemberPortal", model);
        }

        // GET: /Account/MemberLoans
        public IActionResult MemberLoans()
        {
            if (!TryMember(out var id)) return RedirectToAction(nameof(Login));

            ViewData["ActiveSection"] = "loans";
            var model = new MemberLoansViewModel
            {
                MemberName = CurrentUser.FullName(HttpContext.Session),
                Active = _repo.GetMemberActiveLoans(id),
                History = _repo.GetMemberLoanHistory(id),
                Fines = _repo.GetMemberFines(id)
            };
            return View("MemberPortal", model);
        }

        // GET: /Account/MemberWishlist
        public IActionResult MemberWishlist()
        {
            if (!TryMember(out var id)) return RedirectToAction(nameof(Login));

            ViewData["ActiveSection"] = "wishlist";
            var items = _repo.GetMemberWishlist(id);
            var onList = new HashSet<string>(items.Select(w => w.Isbn));

            var model = new MemberWishlistViewModel
            {
                Items = items,
                AddableBooks = _repo.GetCatalog().Where(b => !onList.Contains(b.Isbn)).ToList()
            };
            return View("MemberPortal", model);
        }

        // GET: /Account/MemberReservations
        public IActionResult MemberReservations()
        {
            if (!TryMember(out var id)) return RedirectToAction(nameof(Login));

            ViewData["ActiveSection"] = "reservations";
            var model = new MemberReservationsViewModel
            {
                Items = _repo.GetMemberReservations(id),
                ReservableBooks = _repo.GetCatalog().Where(b => b.AvailableCopies == 0).ToList()
            };
            return View("MemberPortal", model);
        }

        // ---------------------------------------------------------------
        // Member — modal POST handlers
        // ---------------------------------------------------------------

        // POST: /Account/EditMemberProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditMemberProfile(EditProfileViewModel model)
        {
            if (!TryMember(out var id)) return RedirectToAction(nameof(Login));

            if (!ModelState.IsValid)
            {
                TempData["FormError"] = FirstError();
                TempData["ReopenModal"] = "editProfileModal";
                return RedirectToAction(nameof(MemberAccount));
            }

            _repo.UpdateProfile("MEMBER", id, model.Name, model.Email, model.Phone);
            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction(nameof(MemberAccount));
        }

        // POST: /Account/ChangeMemberPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangeMemberPassword(ChangePasswordViewModel model)
        {
            if (!TryMember(out var id)) return RedirectToAction(nameof(Login));

            if (!ModelState.IsValid)
            {
                TempData["FormError"] = FirstError();
                TempData["ReopenModal"] = "changePasswordModal";
                return RedirectToAction(nameof(MemberAccount));
            }

            if (!ChangePassword("MEMBER", id, model))
            {
                TempData["FormError"] = "Your current password is incorrect.";
                TempData["ReopenModal"] = "changePasswordModal";
                return RedirectToAction(nameof(MemberAccount));
            }

            TempData["Success"] = "Password changed successfully.";
            return RedirectToAction(nameof(MemberAccount));
        }

        // POST: /Account/ToggleWishlist
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleWishlist(int bookId)
        {
            if (!TryMember(out var id)) return RedirectToAction(nameof(Login));

            if (bookId <= 0)
            {
                TempData["FormError"] = "Could not identify the book.";
                return RedirectToAction(nameof(MemberBooks));
            }

            var result = _repo.ToggleWishlist(id, bookId, out var title);
            if (result == LibraryRepository.WishlistToggle.Removed)
                TempData["Success"] = $"\"{title}\" removed from your wishlist.";
            else if (result == LibraryRepository.WishlistToggle.Added)
                TempData["Success"] = $"\"{title}\" added to your wishlist.";
            else
                TempData["Success"] = "Wishlist updated.";

            return RedirectToAction(nameof(MemberBooks));
        }

        // POST: /Account/AddToWishlist
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddToWishlist(int bookId)
        {
            if (!TryMember(out var id)) return RedirectToAction(nameof(Login));

            if (bookId <= 0)
            {
                TempData["FormError"] = "Please choose a book to add.";
                TempData["ReopenModal"] = "addWishlistModal";
                return RedirectToAction(nameof(MemberWishlist));
            }

            var title = _repo.AddToWishlist(id, bookId);
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
            if (!TryMember(out var memberId)) return RedirectToAction(nameof(Login));

            _repo.RemoveFromWishlist(id, memberId);
            TempData["Success"] = string.IsNullOrEmpty(title)
                ? "Removed from your wishlist."
                : $"\"{title}\" removed from your wishlist.";
            return RedirectToAction(nameof(MemberWishlist));
        }

        // POST: /Account/ReserveBook
        // Called both from the Wishlist page (book already known, no copies
        // left) and from the "Reserve a Book" modal on the Reservations tab.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ReserveBook(int bookId, string returnTo)
        {
            if (!TryMember(out var id)) return RedirectToAction(nameof(Login));

            var redirectTarget = returnTo == "wishlist" ? nameof(MemberWishlist) : nameof(MemberReservations);

            if (bookId <= 0)
            {
                TempData["FormError"] = "Please choose a book to reserve.";
                TempData["ReopenModal"] = "reserveBookModal";
                return RedirectToAction(redirectTarget);
            }

            var result = _repo.ReserveBook(id, bookId, out var title);
            switch (result)
            {
                case LibraryRepository.ReserveResult.Ok:
                    TempData["Success"] = $"\"{title}\" reserved. We'll let you know when a copy is free.";
                    break;
                case LibraryRepository.ReserveResult.AlreadyReserved:
                    TempData["FormError"] = $"You've already reserved \"{title}\".";
                    break;
                case LibraryRepository.ReserveResult.CopiesAvailable:
                    TempData["FormError"] = $"\"{title}\" is available right now — ask a librarian to issue it to you.";
                    break;
                case LibraryRepository.ReserveResult.BookNotFound:
                    TempData["FormError"] = "Could not find that book.";
                    break;
            }
            return RedirectToAction(redirectTarget);
        }

        // POST: /Account/CancelReservation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelReservation(int id, string title)
        {
            if (!TryMember(out var memberId)) return RedirectToAction(nameof(Login));

            _repo.CancelReservation(id, memberId);
            TempData["Success"] = string.IsNullOrEmpty(title)
                ? "Reservation cancelled."
                : $"Reservation for \"{title}\" cancelled.";
            return RedirectToAction(nameof(MemberReservations));
        }

        // POST: /Account/RenewLoan  //////////////////////////////////////////////////////////////////////////////////////////////////////////
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RenewLoan(int id, string title)
        {
            if (!TryLibrarian(out _)) return RedirectToAction(nameof(Login));  

            var renewed = _repo.RenewLoan(id);
            if (renewed)
            {
                TempData["Success"] = string.IsNullOrEmpty(title)
                    ? "Loan renewed for 14 more days."
                    : $"\"{title}\" renewed for 14 more days.";
            }
            else
            {
                TempData["FormError"] = "This loan can't be renewed (it may be already returned).";
            }
            return RedirectToAction(nameof(LibrarianBorrowings));
        }

        // POST: /Account/PayFine
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PayFine(int id, string book)
        {
            if (!TryMember(out var memberId)) return RedirectToAction(nameof(Login));

            _repo.PayFine(id, memberId);
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
            if (!TryMember(out var memberId)) return RedirectToAction(nameof(Login));

            _repo.PayAllFines(memberId);
            TempData["Success"] = "All outstanding fines paid. Thank you!";
            return RedirectToAction(nameof(MemberLoans));
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

        private void LoadProfileIntoViewData(string role, int userId)
        {
            var p = _repo.GetProfile(role, userId);
            ViewData["ProfileName"] = p.FullName;
            ViewData["ProfileEmail"] = p.Email;
            ViewData["ProfilePhone"] = p.Phone;
            ViewData["ProfileCode"] = p.Code;
        }

        /// <summary>Verifies the current password, then stores a fresh bcrypt hash.</summary>
        private bool ChangePassword(string role, int userId, ChangePasswordViewModel model)
        {
            var hash = _repo.GetPasswordHash(role, userId);
            if (string.IsNullOrEmpty(hash) || !BCrypt.Net.BCrypt.Verify(model.CurrentPassword, hash))
                return false;

            var newHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            _repo.UpdatePasswordHash(role, userId, newHash);
            return true;
        }
    }
}