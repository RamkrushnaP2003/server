using BookApi.Repositories.Interfaces;
using BookApi.Repositories.Implementations;
using BookApi.Services.Interfaces;
using BookApi.Services.Implementations;
using Microsoft.Extensions.Configuration;
using BookApi.Data;

var builder = WebApplication.CreateBuilder(args);

// Add controllers
builder.Services.AddControllers();

// Register DBContext and Services
builder.Services.AddSingleton<DBContext>();
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<IBookService, BookService>();

// Configure CORS to allow access from the React app
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", builder =>
    {
        builder.WithOrigins("http://localhost:3000") // React app URL
               .AllowAnyMethod()    // Allow all HTTP methods (GET, POST, etc.)
               .AllowAnyHeader();   // Allow any headers
    });
});

// Build the application
var app = builder.Build();

// Use Static Files (if needed)
app.UseStaticFiles();

// Authentication and Authorization middleware (if applicable)
app.UseAuthentication();
app.UseAuthorization();

// Apply CORS policy - Only "AllowReactApp" in this case
app.UseCors("AllowReactApp");

// Map Controllers (for API endpoints)
app.MapControllers();

// Run the application
app.Run();
