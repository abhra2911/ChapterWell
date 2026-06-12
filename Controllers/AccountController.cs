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

        // ---------------------------------------------------------------
        // Librarian portal — one action per tab.

        // GET: /Account/LibrarianDashboard
        public IActionResult LibrarianDashboard()
        {
            ViewData["ActiveSection"] = "lib-dashboard";

            var model = new LibrarianDashboardViewModel
            {
                TotalMembers         = 50,
                TotalTitles          = 134,
                TotalCopiesAvailable = 412,
                BooksBorrowed        = 7,
                OverdueCount         = 3,
                NewMembersThisMonth  = 5,
                TotalFinesDue        = 120m,
                CopiesAvailable = 412,
                CopiesBorrowed  = 7,
                CopiesOverdue   = 3,
                CopiesDamaged   = 8,

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
            return View("LibrarianPortal");
        }

        // GET: /Account/LibrarianMembers
        public IActionResult LibrarianMembers()
        {
            ViewData["ActiveSection"] = "members";
            return View("LibrarianPortal");
        }

        // ---------------------------------------------------------------
        // Librarian — modal POST handlers

        // POST: /Account/EditLibrarianProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditLibrarianProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["FormError"]  = FirstError();
                TempData["ReopenModal"] = "editProfileModal";
                return RedirectToAction(nameof(LibrarianAccount));
            }

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
                TempData["FormError"]  = FirstError();
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
            if (!ModelState.IsValid)
            {
                TempData["FormError"]  = FirstError();
                TempData["ReopenModal"] = "addBookModal";
                return RedirectToAction(nameof(LibrarianBooks));
            }


            TempData["Success"] = $"\"{model.Title}\" added to the catalog.";
            return RedirectToAction(nameof(LibrarianBooks));
        }

        // ---------------------------------------------------------------
        // Member portal — one action per tab.
        // ---------------------------------------------------------------

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
                MemberName        = "User",
                ActiveLoans       = loans,
                CurrentlyBorrowed = loans.Count,
                OverdueCount      = loans.Count(l => l.DaysOverdue > 0),
                FineDue           = loans.Sum(l => l.Fine),


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
            return View("MemberPortal");
        }

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
                TempData["FormError"]  = FirstError();
                TempData["ReopenModal"] = "editProfileModal";
                return RedirectToAction(nameof(MemberAccount));
            }

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
                TempData["FormError"]  = FirstError();
                TempData["ReopenModal"] = "changePasswordModal";
                return RedirectToAction(nameof(MemberAccount));
            }

            TempData["Success"] = "Password changed successfully.";
            return RedirectToAction(nameof(MemberAccount));
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
    }
}
