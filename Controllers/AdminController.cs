using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryMS.Data;
using LibraryMS.Models;
using LibraryMS.ViewModels;

namespace LibraryMS.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public AdminController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        private bool IsAdmin() => HttpContext.Session.GetString("UserRole") == "Admin";

        private IActionResult RequireAdmin()
        {
            TempData["Error"] = "Access denied. Admins only.";
            return RedirectToAction("Login", "Auth");
        }

        // ── Dashboard ────────────────────────────────────────────────────────────
        public async Task<IActionResult> Dashboard()
        {
            if (!IsAdmin()) return RequireAdmin();

            // Fetch counts
            var totalBooks = await _db.Books.CountAsync();
            var totalMembers = await _db.Users.CountAsync(u => u.Role == UserRole.Member && u.IsActive);
            var activeBorrows = await _db.BorrowTransactions.CountAsync(t => t.Status == TransactionStatus.Borrowed || t.Status == TransactionStatus.Reserved);
            var overdueCount = await _db.BorrowTransactions.CountAsync(t => t.Status == TransactionStatus.Borrowed && t.DueDate < DateTime.Today);

            // Safer Sum calculation for SQLite
            var totalFinesOutstanding = await _db.BorrowTransactions
                .Where(t => t.FineAmount > t.FinePaid)
                .Select(t => t.FineAmount - t.FinePaid)
                .ToListAsync();
            
            var finesSum = totalFinesOutstanding.Sum();

            var recentTransactions = await _db.BorrowTransactions
                .Include(t => t.User)
                .Include(t => t.Book)
                .OrderByDescending(t => t.BorrowDate)
                .Take(10)
                .ToListAsync();

            var recentlyAdded = await _db.Books
                .OrderByDescending(b => b.AddedAt)
                .Take(5)
                .ToListAsync();

            var model = new AdminDashboardViewModel
            {
                TotalBooks = totalBooks,
                TotalMembers = totalMembers,
                ActiveBorrows = activeBorrows,
                OverdueCount = overdueCount,
                TotalFinesOutstanding = finesSum,
                RecentTransactions = recentTransactions,
                RecentlyAdded = recentlyAdded
            };

            return View(model);
        }

        // ── Library Profile ──────────────────────────────────────────────────────
        public async Task<IActionResult> LibraryProfile()
        {
            if (!IsAdmin()) return RequireAdmin();
            var profile = await _db.LibraryProfiles.FirstOrDefaultAsync();
            return View(profile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LibraryProfile(LibraryProfile model)
        {
            if (!IsAdmin()) return RequireAdmin();
            if (!ModelState.IsValid) return View(model);

            var adminId = HttpContext.Session.GetInt32("UserId")!.Value;
            var existing = await _db.LibraryProfiles.FirstOrDefaultAsync();

            if (existing == null)
            {
                model.AdminUserId = adminId;
                _db.LibraryProfiles.Add(model);
            }
            else
            {
                existing.Name = model.Name;
                existing.Location = model.Location;
                existing.OperatingHours = model.OperatingHours;
                existing.ContactEmail = model.ContactEmail;
                existing.ContactPhone = model.ContactPhone;
                existing.Description = model.Description;
                existing.LoanDurationDays = model.LoanDurationDays;
                existing.RenewalLimit = model.RenewalLimit;
                existing.OverduePenaltyPerDay = model.OverduePenaltyPerDay;
                existing.MaxBorrowableItems = model.MaxBorrowableItems;
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Library profile saved.";
            return RedirectToAction("LibraryProfile");
        }

        // ── Books ────────────────────────────────────────────────────────────────
        public async Task<IActionResult> Books(string? search)
        {
            if (!IsAdmin()) return RequireAdmin();
            var query = _db.Books.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(b => b.Title.Contains(search) || b.Author.Contains(search));
            ViewBag.Search = search;
            return View(await query.OrderByDescending(b => b.AddedAt).ToListAsync());
        }

        [HttpGet]
        public IActionResult AddBook()
        {
            if (!IsAdmin()) return RequireAdmin();
            return View(new BookFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBook(BookFormViewModel model)
        {
            if (!IsAdmin()) return RequireAdmin();
            if (!ModelState.IsValid) return View(model);

            var book = new Book
            {
                Title = model.Title, Author = model.Author, Genre = model.Genre,
                ISBN = model.ISBN, Summary = model.Summary, PublishedYear = model.PublishedYear,
                Publisher = model.Publisher, TotalCopies = model.TotalCopies,
                AvailableCopies = model.TotalCopies, Status = BookStatus.Available,
                AddedAt = DateTime.Now
            };

            if (model.CoverImage != null && model.CoverImage.Length > 0)
                book.CoverImagePath = await SaveCoverImage(model.CoverImage);

            _db.Books.Add(book);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Book added successfully.";
            return RedirectToAction("Books");
        }

        [HttpGet]
        public async Task<IActionResult> EditBook(int id)
        {
            if (!IsAdmin()) return RequireAdmin();
            var book = await _db.Books.FindAsync(id);
            if (book == null) return NotFound();

            return View(new BookFormViewModel
            {
                BookId = book.BookId, Title = book.Title, Author = book.Author,
                Genre = book.Genre, ISBN = book.ISBN, Summary = book.Summary,
                PublishedYear = book.PublishedYear, Publisher = book.Publisher,
                TotalCopies = book.TotalCopies, Status = book.Status,
                ExistingCoverImagePath = book.CoverImagePath
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBook(int id, BookFormViewModel model)
        {
            if (!IsAdmin()) return RequireAdmin();
            if (!ModelState.IsValid) return View(model);

            var book = await _db.Books.FindAsync(id);
            if (book == null) return NotFound();

            book.Title = model.Title; book.Author = model.Author; book.Genre = model.Genre;
            book.ISBN = model.ISBN; book.Summary = model.Summary; book.PublishedYear = model.PublishedYear;
            book.Publisher = model.Publisher; book.TotalCopies = model.TotalCopies; book.Status = model.Status;

            var diff = model.TotalCopies - book.TotalCopies;
            book.AvailableCopies = Math.Max(0, book.AvailableCopies + diff);

            if (model.CoverImage != null && model.CoverImage.Length > 0)
                book.CoverImagePath = await SaveCoverImage(model.CoverImage);

            await _db.SaveChangesAsync();
            TempData["Success"] = "Book updated.";
            return RedirectToAction("Books");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBook(int id)
        {
            if (!IsAdmin()) return RequireAdmin();
            var book = await _db.Books.FindAsync(id);
            if (book != null) { _db.Books.Remove(book); await _db.SaveChangesAsync(); }
            TempData["Success"] = "Book deleted.";
            return RedirectToAction("Books");
        }

        // ── Transactions ─────────────────────────────────────────────────────────
        public async Task<IActionResult> Transactions(string? filter)
        {
            if (!IsAdmin()) return RequireAdmin();
            var query = _db.BorrowTransactions
                .Include(t => t.User).Include(t => t.Book).AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter) && Enum.TryParse<TransactionStatus>(filter, out var s))
                query = query.Where(t => t.Status == s);

            ViewBag.Filter = filter;
            return View(await query.OrderByDescending(t => t.BorrowDate).ToListAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkReturned(int id)
        {
            if (!IsAdmin()) return RequireAdmin();
            var tx = await _db.BorrowTransactions.Include(t => t.Book).FirstOrDefaultAsync(t => t.TransactionId == id);
            if (tx == null) return NotFound();

            tx.ReturnDate = DateTime.Now;
            tx.Status = TransactionStatus.Returned;
            tx.Book.AvailableCopies = Math.Min(tx.Book.TotalCopies, tx.Book.AvailableCopies + 1);
            if (tx.Book.AvailableCopies > 0) tx.Book.Status = BookStatus.Available;

            // calculate fine
            if (tx.DueDate < tx.ReturnDate)
            {
                var profile = await _db.LibraryProfiles.FirstOrDefaultAsync();
                var days = (tx.ReturnDate.Value - tx.DueDate).Days;
                tx.FineAmount = days * (profile?.OverduePenaltyPerDay ?? 5);
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Book marked as returned.";
            return RedirectToAction("Transactions");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordFinePaid(int id)
        {
            if (!IsAdmin()) return RequireAdmin();
            var tx = await _db.BorrowTransactions.FindAsync(id);
            if (tx != null) { tx.FinePaid = tx.FineAmount; await _db.SaveChangesAsync(); }
            TempData["Success"] = "Fine marked as paid.";
            return RedirectToAction("Transactions");
        }

        // ── Members ──────────────────────────────────────────────────────────────
        public async Task<IActionResult> Members()
        {
            if (!IsAdmin()) return RequireAdmin();
            var members = await _db.Users
                .Where(u => u.Role == UserRole.Member)
                .Include(u => u.BorrowTransactions)
                .ToListAsync();
            return View(members);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleMember(int id)
        {
            if (!IsAdmin()) return RequireAdmin();
            var user = await _db.Users.FindAsync(id);
            if (user != null) { user.IsActive = !user.IsActive; await _db.SaveChangesAsync(); }
            TempData["Success"] = "Member status updated.";
            return RedirectToAction("Members");
        }

        // ── Feedback ─────────────────────────────────────────────────────────────
        public async Task<IActionResult> Feedback()
        {
            if (!IsAdmin()) return RequireAdmin();
            var feedbacks = await _db.BookFeedbacks
                .Include(f => f.User).Include(f => f.Book)
                .OrderByDescending(f => f.SubmittedAt).ToListAsync();
            return View(feedbacks);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFeedback(int id)
        {
            if (!IsAdmin()) return RequireAdmin();
            var fb = await _db.BookFeedbacks.FindAsync(id);
            if (fb != null) { _db.BookFeedbacks.Remove(fb); await _db.SaveChangesAsync(); }
            TempData["Success"] = "Feedback removed.";
            return RedirectToAction("Feedback");
        }

        // ── Reports ──────────────────────────────────────────────────────────────
        public async Task<IActionResult> Reports()
        {
            if (!IsAdmin()) return RequireAdmin();

            var transactions = await _db.BorrowTransactions
                .Include(t => t.User)
                .Include(t => t.Book)
                .ToListAsync();

            var books = await _db.Books
                .Include(b => b.BorrowTransactions)
                .ToListAsync();

            var users = await _db.Users
                .Where(u => u.Role == UserRole.Member)
                .Include(u => u.BorrowTransactions)
                .ToListAsync();

            var model = new ReportsViewModel
            {
                TotalBorrows = transactions.Count,
                TotalReturned = transactions.Count(t => t.Status == TransactionStatus.Returned),
                TotalFinesCollected = transactions.Sum(t => t.FinePaid),

                OverdueTransactions = transactions
                    .Where(t => t.Status == TransactionStatus.Borrowed && t.DueDate < DateTime.Today)
                    .ToList(),

                MostPopularBooks = books
                    .OrderByDescending(b => b.BorrowTransactions.Count)
                    .Take(10)
                    .Select(b => new BookBorrowStat { Book = b, BorrowCount = b.BorrowTransactions.Count })
                    .ToList(),

                MostActiveMembers = users
                    .OrderByDescending(u => u.BorrowTransactions.Count)
                    .Take(10)
                    .Select(u => new MemberActivityStat { Member = u, BorrowCount = u.BorrowTransactions.Count })
                    .ToList(),

                MonthlyTrends = transactions
                    .GroupBy(t => new { t.BorrowDate.Year, t.BorrowDate.Month })
                    .Select(g => new MonthlyBorrowStat
                    {
                        Month = g.Key.Year + "-" + g.Key.Month.ToString("D2"),
                        Count = g.Count()
                    })
                    .OrderBy(m => m.Month)
                    .ToList()
            };

            return View(model);
        }

        // ── Helper ───────────────────────────────────────────────────────────────
        private async Task<string> SaveCoverImage(IFormFile file)
        {
            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "covers");
            Directory.CreateDirectory(uploadsDir);
            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadsDir, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);
            return "/uploads/covers/" + fileName;
        }
    }
}
