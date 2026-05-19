using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryMS.Models
{
    public class LibraryProfile
    {
        [Key]
        public int LibraryProfileId { get; set; }

        [Required(ErrorMessage = "Library name is required.")]
        [StringLength(150, ErrorMessage = "Name cannot exceed 150 characters.")]
        [Display(Name = "Library Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Location is required.")]
        [StringLength(250)]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Operating hours are required.")]
        [StringLength(100)]
        [Display(Name = "Operating Hours")]
        public string OperatingHours { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact email is required.")]
        [EmailAddress(ErrorMessage = "Invalid contact email.")]
        [Display(Name = "Contact Email")]
        public string ContactEmail { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "Contact Phone")]
        public string? ContactPhone { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        // One-to-One with User (Admin)
        [ForeignKey("Admin")]
        public int AdminUserId { get; set; }
        public User Admin { get; set; } = null!;

        // Borrowing configuration
        [Range(1, 365, ErrorMessage = "Loan duration must be between 1 and 365 days.")]
        [Display(Name = "Loan Duration (Days)")]
        public int LoanDurationDays { get; set; } = 14;

        [Range(0, 10, ErrorMessage = "Renewal limit must be between 0 and 10.")]
        [Display(Name = "Max Renewals Allowed")]
        public int RenewalLimit { get; set; } = 2;

        [Range(0, 1000)]
        [Display(Name = "Overdue Penalty Per Day (BDT)")]
        [Column(TypeName = "decimal(10,2)")]
        public decimal OverduePenaltyPerDay { get; set; } = 5.00m;

        [Range(1, 20, ErrorMessage = "Max borrowable items must be between 1 and 20.")]
        [Display(Name = "Max Borrowable Items")]
        public int MaxBorrowableItems { get; set; } = 5;
    }
}
