using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using LibraryMS.Models;

namespace LibraryMS.ViewModels
{
    // Auth
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }
    }

    // Home
    public class HomeViewModel
    {
        public List<Book> NewArrivals { get; set; } = new();
        public List<Book> MostBorrowed { get; set; } = new();
        public List<Book> AvailableBooks { get; set; } = new();
        public LibraryProfile? LibraryInfo { get; set; }
    }

    // Books
    public class BookFormViewModel
    {
        public int BookId { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Author is required.")]
        [StringLength(150)]
        public string Author { get; set; } = string.Empty;

        [Required(ErrorMessage = "Genre is required.")]
        [StringLength(80)]
        public string Genre { get; set; } = string.Empty;

        [Required(ErrorMessage = "ISBN is required.")]
        [StringLength(20)]
        [RegularExpression(@"^[0-9\-]{10,17}$", ErrorMessage = "ISBN must be 10-17 digits.")]
        public string ISBN { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Summary { get; set; }

        [Range(1000, 2100)]
        [Display(Name = "Published Year")]
        public int? PublishedYear { get; set; }

        [StringLength(100)]
        public string? Publisher { get; set; }

        [Range(1, 999)]
        [Display(Name = "Total Copies")]
        public int TotalCopies { get; set; } = 1;

        public BookStatus Status { get; set; } = BookStatus.Available;

        [Display(Name = "Cover Image")]
        public IFormFile? CoverImage { get; set; }

        public string? ExistingCoverImagePath { get; set; }
    }

    // Member Dashboard
    public class MemberDashboardViewModel
    {
        public User Member { get; set; } = null!;
        public List<BorrowTransaction> ActiveBorrows { get; set; } = new();
        public List<BorrowTransaction> BorrowHistory { get; set; } = new();
        public decimal TotalOutstandingFines { get; set; }
        public int ActiveBorrowCount { get; set; }
    }

    // Admin Dashboard
    public class AdminDashboardViewModel
    {
        public int TotalBooks { get; set; }
        public int TotalMembers { get; set; }
        public int ActiveBorrows { get; set; }
        public int OverdueCount { get; set; }
        public decimal TotalFinesOutstanding { get; set; }
        public List<BorrowTransaction> RecentTransactions { get; set; } = new();
        public List<Book> RecentlyAdded { get; set; } = new();
    }

    // Reports
    public class ReportsViewModel
    {
        public List<BookBorrowStat> MostPopularBooks { get; set; } = new();
        public List<BorrowTransaction> OverdueTransactions { get; set; } = new();
        public List<MemberActivityStat> MostActiveMembers { get; set; } = new();
        public List<MonthlyBorrowStat> MonthlyTrends { get; set; } = new();
        public int TotalBorrows { get; set; }
        public int TotalReturned { get; set; }
        public decimal TotalFinesCollected { get; set; }
    }

    public class BookBorrowStat
    {
        public Book Book { get; set; } = null!;
        public int BorrowCount { get; set; }
    }

    public class MemberActivityStat
    {
        public User Member { get; set; } = null!;
        public int BorrowCount { get; set; }
    }

    public class MonthlyBorrowStat
    {
        public string Month { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
