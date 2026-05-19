using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryMS.Models
{
    public enum BookStatus { Available, Borrowed, Reserved, Unavailable }

    public class Book
    {
        public int BookId { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Author is required.")]
        [StringLength(150)]
        public string Author { get; set; } = string.Empty;

        [Required(ErrorMessage = "Genre is required.")]
        [StringLength(80)]
        public string Genre { get; set; } = string.Empty;

        [Required(ErrorMessage = "ISBN is required.")]
        [StringLength(20)]
        [RegularExpression(@"^[0-9\-]{10,17}$", ErrorMessage = "ISBN must be 10-17 digits (hyphens allowed).")]
        public string ISBN { get; set; } = string.Empty;

        [StringLength(1000)]
        [Display(Name = "Summary")]
        public string? Summary { get; set; }

        [Display(Name = "Published Year")]
        [Range(1000, 2100, ErrorMessage = "Please enter a valid year.")]
        public int? PublishedYear { get; set; }

        [StringLength(100)]
        public string? Publisher { get; set; }

        [Required]
        public BookStatus Status { get; set; } = BookStatus.Available;

        [Display(Name = "Total Copies")]
        [Range(1, 999)]
        public int TotalCopies { get; set; } = 1;

        [Display(Name = "Available Copies")]
        public int AvailableCopies { get; set; } = 1;

        [Display(Name = "Cover Image")]
        [StringLength(300)]
        public string? CoverImagePath { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.Now;

        // Navigation
        public ICollection<BorrowTransaction> BorrowTransactions { get; set; } = new List<BorrowTransaction>();
        public ICollection<BookFeedback> Feedbacks { get; set; } = new List<BookFeedback>();
        public ICollection<BookGenre> BookGenres { get; set; } = new List<BookGenre>();

        [NotMapped]
        public double AverageRating =>
            Feedbacks != null && Feedbacks.Any() ? Feedbacks.Average(f => f.Rating) : 0;
    }
}
