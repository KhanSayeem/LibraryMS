using System;
using System.ComponentModel.DataAnnotations;

namespace LibraryMS.Models
{
    public class BookFeedback
    {
        [Key]
        public int FeedbackId { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [Required]
        public int BookId { get; set; }
        public Book Book { get; set; } = null!;

        [Required(ErrorMessage = "Rating is required.")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        [StringLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters.")]
        [Display(Name = "Your Review")]
        public string? Comment { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.Now;

        public bool IsApproved { get; set; } = true;
    }
}
