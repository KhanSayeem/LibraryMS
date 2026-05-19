using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LibraryMS.Models
{
    public class Genre
    {
        public int GenreId { get; set; }

        [Required(ErrorMessage = "Genre name is required.")]
        [StringLength(80)]
        public string Name { get; set; } = string.Empty;

        public ICollection<BookGenre> BookGenres { get; set; } = new List<BookGenre>();
    }

    // Many-to-Many join table: Book <-> Genre
    public class BookGenre
    {
        public int BookId { get; set; }
        public Book Book { get; set; } = null!;

        public int GenreId { get; set; }
        public Genre Genre { get; set; } = null!;
    }
}
