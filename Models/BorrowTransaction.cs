using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryMS.Models
{
    public enum TransactionStatus { Borrowed, Reserved, Returned, Overdue, Cancelled }

    public class BorrowTransaction
    {
        [Key]
        public int TransactionId { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [Required]
        public int BookId { get; set; }
        public Book Book { get; set; } = null!;

        [Display(Name = "Borrow Date")]
        public DateTime BorrowDate { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "Due Date")]
        public DateTime DueDate { get; set; }

        [Display(Name = "Return Date")]
        public DateTime? ReturnDate { get; set; }

        [Required]
        public TransactionStatus Status { get; set; } = TransactionStatus.Borrowed;

        [Range(0, 10)]
        [Display(Name = "Renewals Used")]
        public int RenewalsUsed { get; set; } = 0;

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Fine Amount (BDT)")]
        public decimal FineAmount { get; set; } = 0;

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Fine Paid (BDT)")]
        public decimal FinePaid { get; set; } = 0;

        [StringLength(300)]
        [Display(Name = "Admin Notes")]
        public string? Notes { get; set; }

        [NotMapped]
        public bool IsOverdue => Status == TransactionStatus.Borrowed && DueDate < DateTime.Today;

        [NotMapped]
        public decimal OutstandingFine => FineAmount - FinePaid;
    }
}
