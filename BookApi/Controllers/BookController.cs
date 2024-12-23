using Microsoft.AspNetCore.Mvc;
using BookApi.Models;
using BookApi.Services.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookController : ControllerBase
    {
        private readonly IBookService _bookService;

        public BookController(IBookService bookService)
        {
            _bookService = bookService;
        }

        [HttpGet("all-books")]
        public async Task<IActionResult> GetAllBooks()
        {
            var books = await _bookService.GetAllBooksAsync();

            if (books == null || books.Count == 0)
            {
                return NotFound("No books found.");
            }

            return Ok(books);
        }

        [HttpGet("book-info/{isbn}")]
        public async Task<IActionResult> GetBookByIsbn(string isbn)
        {
            var book = await _bookService.GetBookByIsbnAsync(isbn);
            if (book == null)
            {
                return NotFound(new { Message = "Book not found" });
            }
            return Ok(book);
        }

        [HttpGet("books-by-author")]
        public async Task<IActionResult> GetBooksByAuthor([FromQuery] string author, [FromQuery] string title)
        {
            if (string.IsNullOrWhiteSpace(author) || string.IsNullOrWhiteSpace(title))
            {
                return BadRequest(new { Message = "Both 'author' and 'title' parameters are required." });
            }

            var books = await _bookService.GetBooksByAuthorAsync(author, title);

            if (books == null || books.Count == 0)
            {
                return NotFound(new { Message = "No books found matching the criteria." });
            }

            return Ok(new { books = books });
        }

        [HttpGet("books-by-genre")]
        public async Task<IActionResult> GetBooksByGenre([FromQuery] string genre, [FromQuery] string excludeTitle)
        {
            if (string.IsNullOrWhiteSpace(genre) || string.IsNullOrWhiteSpace(excludeTitle))
            {
                return BadRequest(new { Message = "Both 'genre' and 'excludeTitle' parameters are required." });
            }

            var books = await _bookService.GetBooksByGenreAsync(genre, excludeTitle);

            if (books == null || books.Count == 0)
            {
                return NotFound(new { Message = "No books found matching the criteria." });
            }

            return Ok(new { Books = books });
        }

        [HttpGet("books-by-publisher")]
        public async Task<IActionResult> GetBooksByPublisher([FromQuery] string publisher, [FromQuery] string excludeTitle)
        {
            if (string.IsNullOrWhiteSpace(publisher) || string.IsNullOrWhiteSpace(excludeTitle))
            {
                return BadRequest(new { Message = "Both 'publisher' and 'excludeTitle' parameters are required." });
            }

            var books = await _bookService.GetBooksByPublisherAsync(publisher, excludeTitle);

            if (books == null || books.Count == 0)
            {
                return NotFound(new { Message = "No books found matching the criteria." });
            }

            return Ok(new { Books = books });
        }

        [HttpGet("{userId}/book-store")]
        public async Task<IActionResult> GetBooksByLoggedInUserAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest(new { Message = "User ID is required." });
            }

            var books = await _bookService.GetBooksByUserIdAsync(userId);

            if (books == null || books.Count == 0)
            {
                return NotFound(new { Message = "No books found for the current user." });
            }

            return Ok(new { Books = books });
        }
    }
}
