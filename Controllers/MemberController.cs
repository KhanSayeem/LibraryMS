using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryMS.Data;
using LibraryMS.Models;
using LibraryMS.ViewModels;

namespace LibraryMS.Controllers
{
    public class MemberController : Controller
    {
        private readonly AppDbContext _db;
        public MemberController(AppDbContext db) { _db = db; }

        private int? GetUserId() => HttpContext.Session.GetInt32("UserId");
        private bool IsMember() => HttpContext.Session.GetString("UserRole") == "Member";

        private IActionResult RequireMember()
        {
            TempData["Error"] = "Please log in as a member.";
            return RedirectToAction("Login", "Auth");
        }

        // ── Dashboard ────────────────────────────────────────────────────────────
        public async Task<IActionResult> Dashboard()
        {
            if (!IsMember()) return RequireMember();
            var uid = GetUserId()!.Value;

            var member = await _db.Users.FindAsync(uid);
            var txs = await _db.BorrowTransactions
                .Include(t => t.Book)
                .Where(t => t.UserId == uid)
                .OrderByDescending(t => t.BorrowDate)
                .ToListAsync();

            var model = new MemberDashboardViewModel
            {
                Member = member!,
                ActiveBorrows = txs.Where(t => t.Status == TransactionStatus.Borrowed || t.Status == TransactionStatus.Reserved).ToList(),
                BorrowHistory = txs.Where(t => t.Status == TransactionStatus.Returned || t.Status == TransactionStatus.Cancelled).ToList(),
                TotalOutstandingFines = txs.Sum(t => t.FineAmount - t.FinePaid),
                ActiveBorrowCount = txs.Count(t => t.Status == TransactionStatus.Borrowed || t.Status == TransactionStatus.Reserved)
            };

            return View(model);
        }

        // ── Borrow Book ──────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Borrow(int bookId)
        {
            if (!IsMember()) return RequireMember();
            var uid = GetUserId()!.Value;

            var profile = await _db.LibraryProfiles.FirstOrDefaultAsync();
            var maxItems = profile?.MaxBorrowableItems ?? 5;
            var loanDays = profile?.LoanDurationDays ?? 14;

            var activeCount = await _db.BorrowTransactions
                .CountAsync(t => t.UserId == uid && (t.Status == TransactionStatus.Borrowed || t.Status == TransactionStatus.Reserved));

            if (activeCount >= maxItems)
            {
                TempData["Error"] = $"You have reached the maximum borrowing limit of {maxItems} items.";
                return RedirectToAction("Details", "Books", new { id = bookId });
            }

            var alreadyBorrowed = await _db.BorrowTransactions.AnyAsync(t =>
                t.UserId == uid && t.BookId == bookId &&
                (t.Status == TransactionStatus.Borrowed || t.Status == TransactionStatus.Reserved));

            if (alreadyBorrowed)
            {
                TempData["Error"] = "You already have this book borrowed or reserved.";
                return RedirectToAction("Details", "Books", new { id = bookId });
            }

            var book = await _db.Books.FindAsync(bookId);
            if (book == null) return NotFound();

            TransactionStatus txStatus;
            if (book.AvailableCopies > 0)
            {
                book.AvailableCopies--;
                if (book.AvailableCopies == 0) book.Status = BookStatus.Borrowed;
                txStatus = TransactionStatus.Borrowed;
            }
            else
            {
                book.Status = BookStatus.Reserved;
                txStatus = TransactionStatus.Reserved;
            }

            _db.BorrowTransactions.Add(new BorrowTransaction
            {
                UserId = uid,
                BookId = bookId,
                BorrowDate = DateTime.Now,
                DueDate = DateTime.Now.AddDays(loanDays),
                Status = txStatus
            });

            await _db.SaveChangesAsync();
            TempData["Success"] = txStatus == TransactionStatus.Borrowed
                ? "Book borrowed successfully! Return by " + DateTime.Now.AddDays(loanDays).ToString("dd MMM yyyy")
                : "No copies available. You've been added to the reservation queue.";

            return RedirectToAction("Dashboard");
        }

        // ── Renew ────────────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Renew(int transactionId)
        {
            if (!IsMember()) return RequireMember();
            var uid = GetUserId()!.Value;

            var tx = await _db.BorrowTransactions.FirstOrDefaultAsync(t => t.TransactionId == transactionId && t.UserId == uid);
            if (tx == null) return NotFound();

            var profile = await _db.LibraryProfiles.FirstOrDefaultAsync();
            var renewalLimit = profile?.RenewalLimit ?? 2;
            var loanDays = profile?.LoanDurationDays ?? 14;

            if (tx.RenewalsUsed >= renewalLimit)
            {
                TempData["Error"] = $"You have used all {renewalLimit} renewal(s) for this book.";
                return RedirectToAction("Dashboard");
            }

            if (tx.Status != TransactionStatus.Borrowed)
            {
                TempData["Error"] = "Only actively borrowed books can be renewed.";
                return RedirectToAction("Dashboard");
            }

            tx.DueDate = tx.DueDate.AddDays(loanDays);
            tx.RenewalsUsed++;
            await _db.SaveChangesAsync();

            TempData["Success"] = "Renewed! New due date: " + tx.DueDate.ToString("dd MMM yyyy");
            return RedirectToAction("Dashboard");
        }

        // ── Submit Feedback ──────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitFeedback(int bookId, int rating, string? comment)
        {
            if (!IsMember()) return RequireMember();
            var uid = GetUserId()!.Value;

            // member must have returned this book
            var hasBorrowed = await _db.BorrowTransactions.AnyAsync(t =>
                t.UserId == uid && t.BookId == bookId && t.Status == TransactionStatus.Returned);

            if (!hasBorrowed)
            {
                TempData["Error"] = "You can only review books you have returned.";
                return RedirectToAction("Details", "Books", new { id = bookId });
            }

            var alreadyReviewed = await _db.BookFeedbacks.AnyAsync(f => f.UserId == uid && f.BookId == bookId);
            if (alreadyReviewed)
            {
                TempData["Error"] = "You have already submitted a review for this book.";
                return RedirectToAction("Details", "Books", new { id = bookId });
            }

            if (rating < 1 || rating > 5)
            {
                TempData["Error"] = "Rating must be between 1 and 5.";
                return RedirectToAction("Details", "Books", new { id = bookId });
            }

            _db.BookFeedbacks.Add(new BookFeedback
            {
                UserId = uid, BookId = bookId, Rating = rating,
                Comment = comment, SubmittedAt = DateTime.Now, IsApproved = true
            });

            await _db.SaveChangesAsync();
            TempData["Success"] = "Review submitted. Thank you!";
            return RedirectToAction("Details", "Books", new { id = bookId });
        }
    }
}
