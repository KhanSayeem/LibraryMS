using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryMS.Data;
using LibraryMS.Models;

namespace LibraryMS.Controllers
{
    public class BooksController : Controller
    {
        private readonly AppDbContext _db;
        public BooksController(AppDbContext db) { _db = db; }

        public async Task<IActionResult> Index(string? search, string? genre, string? status)
        {
            var query = _db.Books.Include(b => b.Feedbacks).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(b => b.Title.Contains(search) || b.Author.Contains(search) || b.ISBN.Contains(search));

            if (!string.IsNullOrWhiteSpace(genre))
                query = query.Where(b => b.Genre == genre);

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<BookStatus>(status, out var s))
                query = query.Where(b => b.Status == s);

            ViewBag.Search = search;
            ViewBag.Genre = genre;
            ViewBag.Status = status;
            ViewBag.Genres = await _db.Books.Select(b => b.Genre).Distinct().ToListAsync();

            return View(await query.OrderBy(b => b.Title).ToListAsync());
        }

        public async Task<IActionResult> Details(int id)
        {
            var book = await _db.Books
                .Include(b => b.Feedbacks).ThenInclude(f => f.User)
                .FirstOrDefaultAsync(b => b.BookId == id);

            if (book == null) return NotFound();

            // check if current member already gave feedback
            var userId = HttpContext.Session.GetInt32("UserId");
            ViewBag.AlreadyReviewed = userId.HasValue &&
                book.Feedbacks.Any(f => f.UserId == userId.Value);
            ViewBag.UserId = userId;

            return View(book);
        }
    }
}
