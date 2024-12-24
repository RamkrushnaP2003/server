using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using UserApi.Models;
using UserApi.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using UserApi.Helper;
using Microsoft.AspNetCore.StaticFiles;
using MySql.Data.MySqlClient;
using System.Data;

namespace UserApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IMongoCollection<User> _users;
        private readonly MySQLDBContext _mysqlDbContext;

        public UserController(IConfiguration configuration, MySQLDBContext mysqlDbContext)
        {
            var dbContext = new MongoDBContext(configuration);
            _users = dbContext.GetCollection<User>("Users");
            _configuration = configuration;
            _mysqlDbContext = mysqlDbContext;
        }

        [HttpPost("new-user")]
        public async Task<IActionResult> CreateUser([FromBody] User newUser)
        {
            if (newUser == null)
                return BadRequest("User data is null");
            var existingUser = await _users.Find(u => u.Phone == newUser.Phone).FirstOrDefaultAsync();
            if (existingUser != null)
                return BadRequest("A user with this phone number already exists");
            newUser.PasswordHash = PasswordHelper.HashPassword(newUser.PasswordHash!);

            newUser.PublishedBook ??= new List<Book>();
            newUser.Wishlist ??= new List<Book>();
            newUser.Cart ??= new List<Book>();

            await _users.InsertOneAsync(newUser);

            return Ok(new { message = "User created successfully", user = newUser, userId = newUser.UserId });
        }

        [HttpPost("upload-profile-image")]
        public async Task<IActionResult> UploadProfileImage(IFormFile profileImage, [FromForm] string userId)
        {
            if (profileImage == null || profileImage.Length == 0)
                return BadRequest("No file uploaded.");

            // Ensure the uploads folder exists
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            Directory.CreateDirectory(uploadsFolder);

            // Generate a unique filename
            var uniqueFileName = $"{userId}_{Path.GetFileName(profileImage.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            Console.WriteLine($"File saved to {filePath}");

            // Save the file to the uploads folder
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await profileImage.CopyToAsync(fileStream);
            }

            // Update the user's profile image path in MongoDB
            var filter = Builders<User>.Filter.Eq(u => u.UserId, userId);
            var update = Builders<User>.Update.Set(u => u.ProfileImagePath, $"/uploads/{uniqueFileName}");
            await _users.UpdateOneAsync(filter, update);

            return Ok(new { message = "Profile image uploaded successfully.", imagePath = $"/uploads/{uniqueFileName}" });
        }

        [HttpGet("Test")]
        public IActionResult Test() {
            return Ok(new {Message = "Okay.."});
        }

        [HttpGet("profile-image/{userId}")]
        public IActionResult GetProfileImage(string userId)
        {
            var user = _users.Find(u => u.UserId == userId).FirstOrDefault();
            if (user == null || string.IsNullOrEmpty(user.ProfileImagePath))
            {
                return NotFound(new { message = "Profile image not found." });
            }

            try
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", user.ProfileImagePath.Split('/')[2]);
                // Console.WriteLine($"FilePath: {filePath}"); 
                if (System.IO.File.Exists(filePath))
                {
                    // Get the file extension
                    var fileExtension = Path.GetExtension(filePath).ToLowerInvariant();
                    
                    // Use FileExtensionContentTypeProvider to get the MIME type based on the extension
                    var provider = new FileExtensionContentTypeProvider();
                    if (!provider.TryGetContentType(filePath, out string contentType))
                    {
                        contentType = "application/octet-stream"; // Default MIME type if unknown
                    }

                    

                    var imageBytes = System.IO.File.ReadAllBytes(filePath);
                    return File(imageBytes, contentType);
                }

                return NotFound(new { message = "Image file not found." });
            }
            catch (Exception ex)
            {
                // Log exception
                return StatusCode(500, new { message = "An error occurred while retrieving the image.", details = ex.Message });
            }
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] Login login)
        {
            try
            {
                var user = await _users.Find(u => u.Email == login.Email).FirstOrDefaultAsync();
                if (user == null)
                    return BadRequest(new { message = "Invalid credentials." });

                bool isPasswordValid = PasswordHelper.VerifyPassword(login.PasswordHash, user.PasswordHash);
                if (!isPasswordValid)
                    return BadRequest(new { message = "Invalid credentials." });

                var token = GenerateJwtToken(user);

                return Ok(new { token, user });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", details = ex.Message });
            }
        }

        private string GenerateJwtToken(User user)
        {
            var secretKey = _configuration["Jwt:SecretKey"];
            if (string.IsNullOrEmpty(secretKey))
            {
                throw new ArgumentNullException("Jwt:SecretKey", "JWT Secret Key is missing or null in configuration.");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Include user details in claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.FirstName), // User's first name
                new Claim(ClaimTypes.Email, user.Email),    // User's email
                new Claim("UserId", user.UserId!),          // Custom claim for user ID
            };

            var token = new JwtSecurityToken(   
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpPost("{userId}/publish-book")]
        public async Task<IActionResult> PublishBook(string userId, [FromBody] Book bookDto)
        {
            try
            {
                // Validate the input data
                if (bookDto == null || string.IsNullOrEmpty(bookDto.Title) || string.IsNullOrEmpty(bookDto.Author))
                {
                    return BadRequest(new { message = "Invalid book data." });
                }

                // Fetch user from MongoDB by userId
                var user = await _users.Find(u => u.UserId == userId).FirstOrDefaultAsync();
                if (user == null)
                {
                    return NotFound(new { message = "User not found." });
                }

                // Check if the ISBN exists in MySQL
                int isbnExistsResult = 0;
                using (var connection = _mysqlDbContext.CreateConnection())
                {
                    try
                    {
                        await connection.OpenAsync();
                        using (var command = new MySqlCommand("IsISBNPresentINTable", connection))
                        {
                            command.CommandType = CommandType.StoredProcedure;
                            command.Parameters.Add(new MySqlParameter("@p_isbn", bookDto.ISBN));

                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                if (reader.HasRows && await reader.ReadAsync())
                                {
                                    isbnExistsResult = reader.GetInt32(0);
                                }
                            }
                        }
                    }
                    catch (Exception dbEx)
                    {
                        return StatusCode(500, new { message = "Database connection error.", details = dbEx.Message });
                    }
                }
                Console.WriteLine(isbnExistsResult);
                // If ISBN already exists, return a conflict response
                if (isbnExistsResult > 0)
                {
                    Console.WriteLine("Conflict: ISBN {ISBN} already exists.", bookDto.ISBN);
                    return Conflict(new { message = "A book with the same ISBN already exists." });
                }

                // Create a new Book object based on the DTO
                var book = new Book
                {
                    Title = bookDto.Title,
                    Author = bookDto.Author,
                    Genre = bookDto.Genre,
                    Description = bookDto.Description,
                    Price = bookDto.Price,
                    StockQuantity = bookDto.StockQuantity,
                    ISBN = bookDto.ISBN,
                    Publisher = bookDto.Publisher ?? user.FirstName + " " + user.LastName, // Default to user’s name if not provided
                    ImageLink = bookDto.ImageLink,
                };

                // Add the new book to the user's PublishedBooks list in MongoDB
                user.PublishedBook ??= new List<Book>();
                user.PublishedBook.Add(book);

                // Update the user document in MongoDB with the new book
                var update = Builders<User>.Update.Set(u => u.PublishedBook, user.PublishedBook);
                await _users.UpdateOneAsync(u => u.UserId == userId, update);

                // Use ADO.NET to store the book in MySQL
                var parameters = new MySqlParameter[]
                {
                    new MySqlParameter("@p_title", book.Title),
                    new MySqlParameter("@p_author", book.Author),
                    new MySqlParameter("@p_genre", book.Genre ?? (object)DBNull.Value),
                    new MySqlParameter("@p_description", book.Description ?? (object)DBNull.Value),
                    new MySqlParameter("@p_price", book.Price),
                    new MySqlParameter("@p_stock_quantity", book.StockQuantity),
                    new MySqlParameter("@p_isbn", book.ISBN ?? (object)DBNull.Value),
                    new MySqlParameter("@p_publisher", book.Publisher),
                    new MySqlParameter("@p_image_link", book.ImageLink ?? (object)DBNull.Value),
                    new MySqlParameter("@p_user_id", userId),
                };

                await _mysqlDbContext.ExecuteStoredProcedureAsync("InsertBook", parameters);

                return Ok(new { message = "Book published successfully!" });
            }
            catch (Exception ex)
            {
                // Log the error and return a 500 error response
                Console.WriteLine(ex+ "An error occurred while publishing the book.");
                return StatusCode(500, new { message = "An error occurred while publishing the book.", details = ex.Message });
            }
        }

        [HttpPost("{userId}/add-to-wishlist/{isbn}")]
        public async Task<IActionResult> AddToWishlist(string userId, string isbn)
        {
            try
            {
                // Fetch the user from MongoDB by userId
                var user = await _users.Find(u => u.UserId == userId).FirstOrDefaultAsync();
                if (user == null)
                {
                    return NotFound(new { message = "User not found." });
                }

                // Check if the book exists in MySQL using a direct SQL query (instead of stored procedure)
                Book book = null;
                using (var connection = _mysqlDbContext.CreateConnection())
                {
                    try
                    {
                        await connection.OpenAsync();

                        // Query to get book details by ISBN
                        string query = "SELECT book_id, title, author, genre, description, price, stock_quantity, ISBN, publisher, image_link FROM Book WHERE ISBN = @isbn";
                        using (var command = new MySqlCommand(query, connection))
                        {
                            command.Parameters.Add(new MySqlParameter("@isbn", isbn));

                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                if (reader.HasRows && await reader.ReadAsync())
                                {
                                    book = new Book
                                    {
                                        BookId = reader.GetInt32("book_id"), // This maps to book_id in MySQL
                                        Title = reader.GetString("title"),
                                        Author = reader.GetString("author"),
                                        Genre = reader.IsDBNull(reader.GetOrdinal("genre")) ? null : reader.GetString("genre"),
                                        Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                                        Price = reader.GetDecimal("price"),
                                        StockQuantity = reader.GetInt32("stock_quantity"),
                                        ISBN = reader.GetString("ISBN"),
                                        Publisher = reader.IsDBNull(reader.GetOrdinal("publisher")) ? null : reader.GetString("publisher"),
                                        ImageLink = reader.IsDBNull(reader.GetOrdinal("image_link")) ? null : reader.GetString("image_link")
                                    };
                                }
                            }
                        }
                    }
                    catch (Exception dbEx)
                    {
                        return StatusCode(500, new { message = "Database connection error.", details = dbEx.Message });
                    }
                }

                Console.WriteLine(book.Title);

                // If the book is not found, return a not found response
                if (book == null)
                {
                    return NotFound(new { message = "Book not found in the database." });
                }

                // Add the book to the user's wishlist if not already present
                if (user.Wishlist == null)
                {
                    user.Wishlist = new List<Book>();
                }

                // Check if the book is already in the wishlist
                var existingBook = user.Wishlist.FirstOrDefault(b => b.ISBN == isbn);
                if (existingBook != null)
                {
                    return Conflict(new { message = "Book is already in the wishlist." });
                }

                // Add the book to the wishlist
                user.Wishlist.Add(book);

                // Update the user document in MongoDB with the new wishlist
                var update = Builders<User>.Update.Set(u => u.Wishlist, user.Wishlist);
                await _users.UpdateOneAsync(u => u.UserId == userId, update);

                return Ok(new { message = "Book added to wishlist successfully!" });
            }
            catch (Exception ex)
            {
                // Log the error and return a 500 error response
                Console.WriteLine(ex + " An error occurred while adding the book to the wishlist.");
                return StatusCode(500, new { message = "An error occurred while adding the book to the wishlist.", details = ex.Message });
            }
        }

        [HttpPost("{userId}/add-to-cart/{isbn}")]
        public async Task<IActionResult> AddToCart(string userId, string isbn)
        {
            try
            {
                // Fetch the user from MongoDB by userId
                var user = await _users.Find(u => u.UserId == userId).FirstOrDefaultAsync();
                if (user == null)
                {
                    return NotFound(new { message = "User not found." });
                }

                // Check if the book exists in MySQL using a direct SQL query (instead of stored procedure)
                Book book = null;
                using (var connection = _mysqlDbContext.CreateConnection())
                {
                    try
                    {
                        await connection.OpenAsync();

                        // Query to get book details by ISBN
                        string query = "SELECT book_id, title, author, genre, description, price, stock_quantity, ISBN, publisher, image_link FROM Book WHERE ISBN = @isbn";
                        using (var command = new MySqlCommand(query, connection))
                        {
                            command.Parameters.Add(new MySqlParameter("@isbn", isbn));

                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                if (reader.HasRows && await reader.ReadAsync())
                                {
                                    book = new Book
                                    {
                                        BookId = reader.GetInt32("book_id"), // This maps to book_id in MySQL
                                        Title = reader.GetString("title"),
                                        Author = reader.GetString("author"),
                                        Genre = reader.IsDBNull(reader.GetOrdinal("genre")) ? null : reader.GetString("genre"),
                                        Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                                        Price = reader.GetDecimal("price"),
                                        StockQuantity = reader.GetInt32("stock_quantity"),
                                        ISBN = reader.GetString("ISBN"),
                                        Publisher = reader.IsDBNull(reader.GetOrdinal("publisher")) ? null : reader.GetString("publisher"),
                                        ImageLink = reader.IsDBNull(reader.GetOrdinal("image_link")) ? null : reader.GetString("image_link")
                                    };
                                }
                            }
                        }
                    }
                    catch (Exception dbEx)
                    {
                        return StatusCode(500, new { message = "Database connection error.", details = dbEx.Message });
                    }
                }

                Console.WriteLine(book.Title);

                // If the book is not found, return a not found response
                if (book == null)
                {
                    return NotFound(new { message = "Book not found in the database." });
                }

                // Add the book to the user's Cart if not already present
                if (user.Cart == null)
                {
                    user.Cart = new List<Book>();
                }

                // Check if the book is already in the Cart
                var existingBook = user.Cart.FirstOrDefault(b => b.ISBN == isbn);
                if (existingBook != null)
                {
                    return Conflict(new { message = "Book is already in the Cart." });
                }

                // Add the book to the Cart
                user.Cart.Add(book);

                // Update the user document in MongoDB with the new Cart
                var update = Builders<User>.Update.Set(u => u.Cart, user.Cart);
                await _users.UpdateOneAsync(u => u.UserId == userId, update);

                return Ok(new { message = "Book added to Cart successfully!" });
            }
            catch (Exception ex)
            {
                // Log the error and return a 500 error response
                Console.WriteLine(ex + " An error occurred while adding the book to the wishlist.");
                return StatusCode(500, new { message = "An error occurred while adding the book to the wishlist.", details = ex.Message });
            }
        }

        [HttpGet("{userId}/cart")]
        public async Task<IActionResult> GetCart(string userId) {
            try {
                var user = await _users.Find(u => u.UserId == userId).FirstOrDefaultAsync();
                if(user == null) {
                    return NotFound(new { message = "User Not Found" });
                }
                return Ok(new { cart = user.Cart});
            } catch (Exception ex) {
                return StatusCode(500, new { message = "An error occure while fetching the cart.", details = ex.Message });
            }
        }

        [HttpGet("{userId}/wishlist")]
        public async Task<IActionResult> GetWishlist(string userId) {
            try {
                var user = await _users.Find(u => u.UserId == userId).FirstOrDefaultAsync();
                if(user == null) {
                    return NotFound(new { message = "User Not Found" });
                }
                return Ok(new { wishlist  = user.Wishlist});
            } catch (Exception ex) {
                return StatusCode(500, new { message = "An error occure while fetching the wishlist.", details = ex.Message });
            }
        }

        [HttpPut("{userId}/update-book/{isbn}")]
        public async Task<IActionResult> UpdateBook(string userId, string isbn, [FromBody] Book book)
        {
            if (book == null || string.IsNullOrEmpty(isbn) || string.IsNullOrEmpty(userId))
            {
                return BadRequest("Invalid data.");
            }

            try
            {
                // Step 1: Update book in MySQL
                var mysqlParams = new MySqlParameter[]
                {
                    new MySqlParameter("p_isbn", isbn),
                    new MySqlParameter("p_title", book.Title),
                    new MySqlParameter("p_author", book.Author),
                    new MySqlParameter("p_genre", book.Genre),
                    new MySqlParameter("p_description", book.Description),
                    new MySqlParameter("p_price", book.Price),
                    new MySqlParameter("p_stock_quantity", book.StockQuantity),
                    new MySqlParameter("p_trending", book.Trending),
                    new MySqlParameter("p_bestseller", book.Bestseller),
                    new MySqlParameter("p_publisher", book.Publisher),
                    new MySqlParameter("p_image_link", book.ImageLink),
                    new MySqlParameter("p_user_id", userId)
                };

                await _mysqlDbContext.ExecuteStoredProcedureAsync("UpdateBookByISBN", mysqlParams);

                // Step 2: Update the user's book in MongoDB
                var filter = Builders<User>.Filter.Eq(u => u.UserId, userId) &
                             Builders<User>.Filter.ElemMatch(u => u.PublishedBook, b => b.ISBN == isbn);

                var update = Builders<User>.Update
                    .Set("PublishedBook.$.Title", book.Title)
                    .Set("PublishedBook.$.Author", book.Author)
                    .Set("PublishedBook.$.Genre", book.Genre)
                    .Set("PublishedBook.$.Description", book.Description)
                    .Set("PublishedBook.$.Price", book.Price)
                    .Set("PublishedBook.$.StockQuantity", book.StockQuantity)
                    .Set("PublishedBook.$.Trending", book.Trending)
                    .Set("PublishedBook.$.Bestseller", book.Bestseller)
                    .Set("PublishedBook.$.Publisher", book.Publisher)
                    .Set("PublishedBook.$.ImageLink", book.ImageLink);

                var result = await _users.UpdateOneAsync(filter, update);

                if (result.ModifiedCount > 0)
                {
                    return Ok(new { message = "Book updated successfully." });
                }
                else
                {
                    return NotFound("Book not found in user's published books.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("{userId}/delete-book/{isbn}")]
        public async Task<IActionResult> DeleteBook(string userId, string isbn)
        {
            if (string.IsNullOrEmpty(isbn) || string.IsNullOrEmpty(userId))
            {
                return BadRequest("Invalid data.");
            }

            try
            {
                // Step 1: Delete book from MySQL
                using (var connection = _mysqlDbContext.CreateConnection())
                {
                    await connection.OpenAsync();

                    var command = new MySqlCommand("DELETE FROM Book WHERE ISBN = @isbn", connection);
                    command.Parameters.AddWithValue("@isbn", isbn);

                    var rowsAffected = await command.ExecuteNonQueryAsync();
                    if (rowsAffected == 0)
                    {
                        return NotFound("Book not found in MySQL database.");
                    }
                }

                // Step 2: Delete book from MongoDB user's PublishedBook list
                var filter = Builders<User>.Filter.And(
                    Builders<User>.Filter.Eq(u => u.UserId, userId),
                    Builders<User>.Filter.ElemMatch(u => u.PublishedBook, b => b.ISBN == isbn)
                );

                var update = Builders<User>.Update.PullFilter(
                    u => u.PublishedBook,
                    b => b.ISBN == isbn
                );

                var result = await _users.UpdateOneAsync(filter, update);

                if (result.ModifiedCount > 0)
                {
                    return Ok(new { message = "Book deleted successfully." });
                }
                else
                {
                    return NotFound("Book not found in user's published books.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // private string GetUserIdFromToken()
        // {
        //     var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        //     var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
        //     var userIdClaim = jwtToken?.Claims.FirstOrDefault(c => c.Type == "userId");
        //     return userIdClaim?.Value;
        // }
    }
}
