using System;
using Microsoft.EntityFrameworkCore;
using LibraryMS.Models;

namespace LibraryMS.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<LibraryProfile> LibraryProfiles { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<BookGenre> BookGenres { get; set; }
        public DbSet<BorrowTransaction> BorrowTransactions { get; set; }
        public DbSet<BookFeedback> BookFeedbacks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // BookGenre composite key (many-to-many)
            modelBuilder.Entity<BookGenre>()
                .HasKey(bg => new { bg.BookId, bg.GenreId });

            modelBuilder.Entity<BookGenre>()
                .HasOne(bg => bg.Book)
                .WithMany(b => b.BookGenres)
                .HasForeignKey(bg => bg.BookId);

            modelBuilder.Entity<BookGenre>()
                .HasOne(bg => bg.Genre)
                .WithMany(g => g.BookGenres)
                .HasForeignKey(bg => bg.GenreId);

            // LibraryProfile one-to-one with User
            modelBuilder.Entity<LibraryProfile>()
                .HasOne(lp => lp.Admin)
                .WithOne(u => u.LibraryProfile)
                .HasForeignKey<LibraryProfile>(lp => lp.AdminUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // BorrowTransaction -> User (no cascade to avoid cycles)
            modelBuilder.Entity<BorrowTransaction>()
                .HasOne(t => t.User)
                .WithMany(u => u.BorrowTransactions)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // BorrowTransaction -> Book
            modelBuilder.Entity<BorrowTransaction>()
                .HasOne(t => t.Book)
                .WithMany(b => b.BorrowTransactions)
                .HasForeignKey(t => t.BookId)
                .OnDelete(DeleteBehavior.Restrict);

            // BookFeedback -> User
            modelBuilder.Entity<BookFeedback>()
                .HasOne(f => f.User)
                .WithMany(u => u.Feedbacks)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // BookFeedback -> Book
            modelBuilder.Entity<BookFeedback>()
                .HasOne(f => f.Book)
                .WithMany(b => b.Feedbacks)
                .HasForeignKey(f => f.BookId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique email
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // --- SEED DATA ---

            modelBuilder.Entity<User>().HasData(
                new User { UserId = 1, FullName = "Admin User", Email = "admin@library.com", PasswordHash = "240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9", Role = UserRole.Admin, CreatedAt = new DateTime(2024, 1, 1) },
                new User { UserId = 2, FullName = "Rahim Hossain", Email = "rahim@email.com", PasswordHash = "5600376e863d2f57a053518f324ad3840b0bc2348b573af281a7b7cbe7a228c6", Role = UserRole.Member, CreatedAt = new DateTime(2024, 1, 5) },
                new User { UserId = 3, FullName = "Fatema Begum", Email = "fatema@email.com", PasswordHash = "5600376e863d2f57a053518f324ad3840b0bc2348b573af281a7b7cbe7a228c6", Role = UserRole.Member, CreatedAt = new DateTime(2024, 2, 1) }
            );

            modelBuilder.Entity<LibraryProfile>().HasData(
                new LibraryProfile
                {
                    LibraryProfileId = 1,
                    Name = "Dhaka Central Library",
                    Location = "Shahbag, Dhaka 1000",
                    OperatingHours = "Sat-Thu 8:00 AM - 8:00 PM",
                    ContactEmail = "contact@dhakalibrary.com",
                    ContactPhone = "01700000000",
                    Description = "Main branch of the Dhaka Central Library network.",
                    AdminUserId = 1,
                    LoanDurationDays = 14,
                    RenewalLimit = 2,
                    OverduePenaltyPerDay = 5.00m,
                    MaxBorrowableItems = 5
                }
            );

            modelBuilder.Entity<Book>().HasData(
                new Book { BookId = 1, Title = "Clean Code", Author = "Robert C. Martin", Genre = "Technology", ISBN = "978-0132350884", Summary = "A handbook of agile software craftsmanship.", PublishedYear = 2008, Publisher = "Prentice Hall", Status = BookStatus.Available, TotalCopies = 3, AvailableCopies = 3, AddedAt = new DateTime(2024, 1, 10) },
                new Book { BookId = 2, Title = "The Great Gatsby", Author = "F. Scott Fitzgerald", Genre = "Fiction", ISBN = "978-0743273565", Summary = "A story of the fabulously wealthy Jay Gatsby.", PublishedYear = 1925, Publisher = "Scribner", Status = BookStatus.Available, TotalCopies = 2, AvailableCopies = 2, AddedAt = new DateTime(2024, 1, 10) },
                new Book { BookId = 3, Title = "Sapiens", Author = "Yuval Noah Harari", Genre = "History", ISBN = "978-0062316097", Summary = "A brief history of humankind.", PublishedYear = 2011, Publisher = "Harper", Status = BookStatus.Available, TotalCopies = 4, AvailableCopies = 3, AddedAt = new DateTime(2024, 1, 15) },
                new Book { BookId = 4, Title = "1984", Author = "George Orwell", Genre = "Fiction", ISBN = "978-0451524935", Summary = "A dystopian social science fiction novel.", PublishedYear = 1949, Publisher = "Secker & Warburg", Status = BookStatus.Available, TotalCopies = 3, AvailableCopies = 3, AddedAt = new DateTime(2024, 2, 1) },
                new Book { BookId = 5, Title = "Introduction to Algorithms", Author = "Cormen et al.", Genre = "Technology", ISBN = "978-0262033848", Summary = "Comprehensive guide to algorithms.", PublishedYear = 2009, Publisher = "MIT Press", Status = BookStatus.Available, TotalCopies = 2, AvailableCopies = 2, AddedAt = new DateTime(2024, 2, 5) }
            );

            modelBuilder.Entity<BorrowTransaction>().HasData(
                new BorrowTransaction
                {
                    TransactionId = 1, UserId = 2, BookId = 3,
                    BorrowDate = new DateTime(2024, 3, 1),
                    DueDate = new DateTime(2024, 3, 15),
                    ReturnDate = new DateTime(2024, 3, 14),
                    Status = TransactionStatus.Returned,
                    FineAmount = 0, FinePaid = 0
                },
                new BorrowTransaction
                {
                    TransactionId = 2, UserId = 3, BookId = 1,
                    BorrowDate = DateTime.Today.AddDays(-20),
                    DueDate = DateTime.Today.AddDays(-6),
                    Status = TransactionStatus.Overdue,
                    FineAmount = 30, FinePaid = 0
                }
            );

            modelBuilder.Entity<BookFeedback>().HasData(
                new BookFeedback { FeedbackId = 1, UserId = 2, BookId = 3, Rating = 5, Comment = "Excellent book, changed my perspective!", SubmittedAt = new DateTime(2024, 3, 15), IsApproved = true },
                new BookFeedback { FeedbackId = 2, UserId = 3, BookId = 2, Rating = 4, Comment = "A classic worth reading.", SubmittedAt = new DateTime(2024, 2, 20), IsApproved = true }
            );
        }
    }
}
