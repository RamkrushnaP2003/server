using System.Collections.Generic;
using System.Threading.Tasks;
using BookApi.Models;

namespace BookApi.Services.Interfaces
{
    public interface IBookService
    {
        Task<List<Book>> GetAllBooksAsync();
        Task<Book> GetBookByIsbnAsync(string isbn);
        Task<List<Book>> GetBooksByAuthorAsync(string author, string title);
        Task<List<Book>> GetBooksByGenreAsync(string genre, string excludeTitle);
        Task<List<Book>> GetBooksByPublisherAsync(string publisher, string excludeTitle);
        Task<List<Book>> GetBooksByUserIdAsync(string userId);
    }
}
