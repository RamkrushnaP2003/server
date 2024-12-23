using MySql.Data.MySqlClient;
using BookApi.Models;
using BookApi.Repositories.Interfaces;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using BookApi.Data;

namespace BookApi.Repositories.Implementations
{
    public class BookRepository : IBookRepository
    {
        private readonly DBContext _dbContext;
        private readonly string defaultImage = "https://img.freepik.com/free-psd/books-stack-icon-isolated-3d-render-illustration_47987-15482.jpg?semt=ais_hybrid";

        public BookRepository(DBContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Book>> GetAllBooksAsync()
        {
            var books = new List<Book>();
            using (var connection = _dbContext.CreateConnection())
            {
                await connection.OpenAsync();
                using (var command = new MySqlCommand("GetAllBooks", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var book = new Book
                            {
                                BookId = reader.GetInt32("book_id"),
                                Title = reader.GetString("title"),
                                Author = reader.GetString("author"),
                                Genre = reader.IsDBNull(reader.GetOrdinal("genre")) ? null : reader.GetString("genre"),
                                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                                Price = reader.GetDecimal("price"),
                                StockQuantity = reader.GetInt32("stock_quantity"),
                                ISBN = reader.GetString("ISBN"),
                                Trending = reader.GetBoolean("trending"),
                                Bestseller = reader.GetBoolean("bestseller"),
                                Publisher = reader.IsDBNull(reader.GetOrdinal("publisher")) ? null : reader.GetString("publisher"),
                                ImageLink = reader.IsDBNull(reader.GetOrdinal("image_link")) ? defaultImage : reader.GetString("image_link")
                            };
                            books.Add(book);
                        }
                    }
                }
            }
            return books;
        }

        public async Task<Book> GetBookByIsbnAsync(string isbn)
        {
            using (var connection = _dbContext.CreateConnection())
            {
                await connection.OpenAsync();
                using (var command = new MySqlCommand("GetBookByIsbn", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@isbn_param", isbn);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new Book
                            {
                                BookId = reader.GetInt32("book_id"),
                                Title = reader.GetString("title"),
                                Author = reader.GetString("author"),
                                Genre = reader.IsDBNull(reader.GetOrdinal("genre")) ? null : reader.GetString("genre"),
                                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                                Price = reader.GetDecimal("price"),
                                StockQuantity = reader.GetInt32("stock_quantity"),
                                ISBN = reader.GetString("ISBN"),
                                Trending = reader.GetBoolean("trending"),
                                Bestseller = reader.GetBoolean("bestseller"),
                                Publisher = reader.IsDBNull(reader.GetOrdinal("publisher")) ? null : reader.GetString("publisher"),
                                ImageLink = reader.IsDBNull(reader.GetOrdinal("image_link")) ? defaultImage : reader.GetString("image_link")
                            };
                        }
                        return null;
                    }
                }
            }
        }

        public async Task<List<Book>> GetBooksByAuthorAsync(string author, string title)
        {
            var books = new List<Book>();
            using (var connection = _dbContext.CreateConnection())
            {
                await connection.OpenAsync();
                using (var command = new MySqlCommand("GetBooksByAuthor", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@author_param", author);
                    command.Parameters.AddWithValue("@title_param", title);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var book = new Book
                            {
                                BookId = reader.GetInt32("book_id"),
                                Title = reader.GetString("title"),
                                Author = reader.GetString("author"),
                                Genre = reader.IsDBNull(reader.GetOrdinal("genre")) ? null : reader.GetString("genre"),
                                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                                Price = reader.GetDecimal("price"),
                                StockQuantity = reader.GetInt32("stock_quantity"),
                                ISBN = reader.GetString("ISBN"),
                                Trending = reader.GetBoolean("trending"),
                                Bestseller = reader.GetBoolean("bestseller"),
                                Publisher = reader.IsDBNull(reader.GetOrdinal("publisher")) ? null : reader.GetString("publisher"),
                                ImageLink = reader.IsDBNull(reader.GetOrdinal("image_link")) ? defaultImage : reader.GetString("image_link")
                            };
                            books.Add(book);
                        }
                    }
                }
            }
            return books;
        }

        public async Task<List<Book>> GetBooksByGenreAsync(string genre, string excludeTitle)
        {
            var books = new List<Book>();

            using (var connection = _dbContext.CreateConnection())
            {
                await connection.OpenAsync();

                using (var command = new MySqlCommand("GetBooksByGenre", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@genre_param", genre);
                    command.Parameters.AddWithValue("@exclude_title_param", excludeTitle);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            books.Add(new Book
                            {
                                BookId = reader.GetInt32("book_id"),
                                Title = reader.GetString("title"),
                                Author = reader.GetString("author"),
                                Genre = reader.GetString("genre"),
                                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                                Price = reader.GetDecimal("price"),
                                StockQuantity = reader.GetInt32("stock_quantity"),
                                ISBN = reader.GetString("ISBN"),
                                Trending = reader.GetBoolean("trending"),
                                Bestseller = reader.GetBoolean("bestseller"),
                                Publisher = reader.IsDBNull(reader.GetOrdinal("publisher")) ? null : reader.GetString("publisher"),
                                ImageLink = reader.IsDBNull(reader.GetOrdinal("image_link")) ? defaultImage : reader.GetString("image_link")
                            });
                        }
                    }
                }
            }

            return books;
        }

        public async Task<List<Book>> GetBooksByPublisherAsync(string publisher, string excludeTitle)
        {
            var books = new List<Book>();

            using (var connection = _dbContext.CreateConnection())
            {
                await connection.OpenAsync();

                using (var command = new MySqlCommand("GetBooksByPublisher", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@publisher_param", publisher);
                    command.Parameters.AddWithValue("@exclude_title_param", excludeTitle);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            books.Add(new Book
                            {
                                BookId = reader.GetInt32("book_id"),
                                Title = reader.GetString("title"),
                                Author = reader.GetString("author"),
                                Genre = reader.GetString("genre"),
                                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                                Price = reader.GetDecimal("price"),
                                StockQuantity = reader.GetInt32("stock_quantity"),
                                ISBN = reader.GetString("ISBN"),
                                Trending = reader.GetBoolean("trending"),
                                Bestseller = reader.GetBoolean("bestseller"),
                                Publisher = reader.IsDBNull(reader.GetOrdinal("publisher")) ? null : reader.GetString("publisher"),
                                ImageLink = reader.IsDBNull(reader.GetOrdinal("image_link")) ? defaultImage : reader.GetString("image_link")
                            });
                        }
                    }
                }
            }

            return books;
        }

        public async Task<List<Book>> GetBooksByUserIdAsync(string userId)
        {
            var books = new List<Book>();

            // Validate userId is not null or empty
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("Invalid userId provided.");
            }

            using (var connection = _dbContext.CreateConnection())
            {
                await connection.OpenAsync();

                // Ensure correct parameter type (string) when passing to the stored procedure
                using (var command = new MySqlCommand("GetBooksByUserId", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@p_userId", userId); // Use userId as string

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var book = new Book
                            {
                                BookId = reader.GetInt32("book_id"), // Keep BookId as int
                                Title = reader.GetString("title"),
                                Author = reader.GetString("author"),
                                Genre = reader.GetString("genre"),
                                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                                Price = reader.GetDecimal("price"),
                                StockQuantity = reader.GetInt32("stock_quantity"),
                                ISBN = reader.GetString("ISBN"),
                                Trending = reader.GetBoolean("trending"),
                                Bestseller = reader.GetBoolean("bestseller"),
                                Publisher = reader.IsDBNull(reader.GetOrdinal("publisher")) ? null : reader.GetString("publisher"),
                                ImageLink = reader.IsDBNull(reader.GetOrdinal("image_link")) ? defaultImage : reader.GetString("image_link"),
                                UserId = reader.GetString("user_id") // Ensure UserId is treated as a string
                            };

                            books.Add(book);
                        }
                    }
                }
            }

            return books;
        }

    }
}
