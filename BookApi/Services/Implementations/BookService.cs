using System.Collections.Generic;
using System.Threading.Tasks;
using BookApi.Models;
using BookApi.Repositories.Interfaces;
using BookApi.Services.Interfaces;

namespace BookApi.Services.Implementations
{
    public class BookService : IBookService
    {
        private readonly IBookRepository _bookRepository;

        public BookService(IBookRepository bookRepository)
        {
            _bookRepository = bookRepository;
        }

        public async Task<List<Book>> GetAllBooksAsync()
        {
            return await _bookRepository.GetAllBooksAsync();
        }

        public async Task<Book> GetBookByIsbnAsync(string isbn)
        {
            return await _bookRepository.GetBookByIsbnAsync(isbn);
        }

        public async Task<List<Book>> GetBooksByAuthorAsync(string author, string title)
        {
            return await _bookRepository.GetBooksByAuthorAsync(author, title);
        }

        public async Task<List<Book>> GetBooksByGenreAsync(string genre, string excludeTitle)
        {
            return await _bookRepository.GetBooksByGenreAsync(genre, excludeTitle);
        }

        public async Task<List<Book>> GetBooksByPublisherAsync(string publisher, string excludeTitle)
        {
            return await _bookRepository.GetBooksByPublisherAsync(publisher, excludeTitle);
        }

        public async Task<List<Book>> GetBooksByUserIdAsync(string userId)
        {
            return await _bookRepository.GetBooksByUserIdAsync(userId);
        }
    }
}
