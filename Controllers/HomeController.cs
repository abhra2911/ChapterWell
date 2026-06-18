using Lib_Mgmt.Data;
using Lib_Mgmt.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Lib_Mgmt.Controllers
{
    public class HomeController : Controller
    {
        private readonly LibraryRepository _repo;

        public HomeController(LibraryRepository repo)
        {
            _repo = repo;
        }

        public IActionResult Index()
        {
            var stats = _repo.GetHomeStats();
            return View(stats);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
