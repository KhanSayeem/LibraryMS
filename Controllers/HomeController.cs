using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryMS.Data;
using LibraryMS.ViewModels;

namespace LibraryMS.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;
        public HomeController(AppDbContext db) { _db = db; }

        public async Task<IActionResult> Index()
        {
            var model = new HomeViewModel
            {
                LibraryInfo = await _db.LibraryProfiles.FirstOrDefaultAsync(),
                NewArrivals = await _db.Books
                    .OrderByDescending(b => b.AddedAt)
                    .Take(4).ToListAsync(),
                MostBorrowed = await _db.Books
                    .OrderByDescending(b => b.BorrowTransactions.Count)
                    .Take(4).ToListAsync(),
                AvailableBooks = await _db.Books
                    .Where(b => b.AvailableCopies > 0)
                    .Take(8).ToListAsync()
            };
            return View(model);
        }

        public IActionResult About() => View();
    }
}
