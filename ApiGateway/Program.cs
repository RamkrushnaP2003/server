using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.Text.Json;

Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

var builder = WebApplication.CreateBuilder(args);

// Debugging information
Console.WriteLine($"Environment: {builder.Environment.EnvironmentName.ToLower()}");
Console.WriteLine($"Content Root Path: {builder.Environment.ContentRootPath}");

// Paths for JSON configurations
string developmentFolder = Path.Combine(builder.Environment.ContentRootPath, "development");
string ocelotJsonPath = Path.Combine(developmentFolder, "ocelot.json"); // Store it in the development folder
string bookConfigPath = Path.Combine(developmentFolder, "ocelot.book.json");
string userConfigPath = Path.Combine(developmentFolder, "ocelot.user.json");

// Ensure ocelot.json exists or create it dynamically
if (!File.Exists(ocelotJsonPath))
{
    Console.WriteLine($"File not found: {ocelotJsonPath}. Generating it dynamically.");

    if (!Directory.Exists(developmentFolder))
    {
        throw new DirectoryNotFoundException($"Directory not found: {developmentFolder}");
    }

    if (!File.Exists(bookConfigPath) || !File.Exists(userConfigPath))
    {
        throw new FileNotFoundException($"One or both required configuration files are missing: {bookConfigPath}, {userConfigPath}");
    }

    // Merge configurations
    var mergedConfig = MergeOcelotConfigurations(bookConfigPath, userConfigPath);
    File.WriteAllText(ocelotJsonPath, mergedConfig);

    Console.WriteLine("Generated ocelot.json");
}

builder.Services.AddCors(options =>
{
    // Allow all origins, methods, and headers
    options.AddPolicy("AllowAllOrigins", policy =>
    {
        policy.AllowAnyOrigin()  // Allow any origin
              .AllowAnyMethod()  // Allow any HTTP method
              .AllowAnyHeader(); // Allow any headers
    });
});

// Configure Ocelot
builder.Configuration.SetBasePath(builder.Environment.ContentRootPath)
                     .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                     .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                     .AddOcelot(developmentFolder, builder.Environment) // Pass the folder containing ocelot.json
                     .AddEnvironmentVariables();

builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();
app.UseCors("AllowAllOrigins"); // Enable CORS for all origins

// Use Ocelot middleware
await app.UseOcelot();

app.Run();

// Method to merge configurations
static string MergeOcelotConfigurations(string bookConfigPath, string userConfigPath)
{
    // Read the JSON files
    var bookConfigJson = File.ReadAllText(bookConfigPath);
    var userConfigJson = File.ReadAllText(userConfigPath);

    // Parse JSON to objects
    var bookConfig = JsonDocument.Parse(bookConfigJson).RootElement;
    var userConfig = JsonDocument.Parse(userConfigJson).RootElement;

    // Combine routes from both configurations
    var mergedRoutes = new List<JsonElement>();

    if (bookConfig.TryGetProperty("Routes", out var bookRoutes) && bookRoutes.ValueKind == JsonValueKind.Array)
    {
        mergedRoutes.AddRange(bookRoutes.EnumerateArray());
    }

    if (userConfig.TryGetProperty("Routes", out var userRoutes) && userRoutes.ValueKind == JsonValueKind.Array)
    {
        mergedRoutes.AddRange(userRoutes.EnumerateArray());
    }

    // Create the merged configuration
    var mergedConfig = new
    {
        Routes = mergedRoutes
    };

    // Serialize merged configuration to JSON
    return JsonSerializer.Serialize(mergedConfig, new JsonSerializerOptions { WriteIndented = true });
}
