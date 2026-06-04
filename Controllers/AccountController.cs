using System;
using System.Collections.Generic;
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
                return RedirectToAction(nameof(MemberAccount));
            }

            ViewBag.Error = "Invalid username or password.";
            return View();
        }

        // ---------------------------------------------------------------
        // Librarian portal — one action per tab.
        // Each action renders the shared LibrarianPortal shell, which
        // shows the nav + the single active section partial.
        // ---------------------------------------------------------------

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
        // Member portal — one action per tab.
        // ---------------------------------------------------------------

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

        // GET: /Account/MemberBorrowing
        public IActionResult MemberBorrowing()
        {
            ViewData["ActiveSection"] = "borrowing";
            return View("MemberPortal");
        }

        // GET: /Account/Logout
        public IActionResult Logout()
        {
            return RedirectToAction(nameof(Login));
        }
    }
}
