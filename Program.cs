using Microsoft.EntityFrameworkCore;
using ProductAPI.Data;
using ProductAPI.DTOs;
using ProductAPI.Models;
using ProductAPI.Repositories;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Get connection string from configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    // Fallback connection string if not found in appsettings
    connectionString = "Server=(localdb)\\MSSQLLocalDB;Database=ProductDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
}

// Add SQL Server DbContext with proper configuration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        sqlServerOptions => sqlServerOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)
    ));


// If using SQL Server instead, use:
// builder.Services.AddDbContext<ApplicationDbContext>(options =>
//     options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IProductRepository, ProductRepository>();
// Configure HTTPS ports explicitly
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5000); // HTTP
    options.ListenLocalhost(5001, listenOptions => // HTTPS
    {
        listenOptions.UseHttps();
    });
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()  // Allow requests from any origin
                  .AllowAnyMethod()   // Allow all HTTP methods
                  .AllowAnyHeader()   // Allow all headers
                  .WithExposedHeaders("*"); // Expose all headers to client
        });
});

var app = builder.Build();
app.MapGet("/", () => Results.Redirect("/swagger"));
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Product API v1");
        c.RoutePrefix = "swagger";
    });
}

// COMMENT OUT HTTPS redirection if not needed for development
// app.UseHttpsRedirection();

app.UseAuthorization();
app.MapControllers();


// Define minimal API endpoints
app.MapGet("/api/products", async (ApplicationDbContext db) =>
{
    var products = await db.Products.ToListAsync();
    return Results.Ok(products);
});

app.MapGet("/api/products/{id}", async (int id, ApplicationDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    return product == null ? Results.NotFound() : Results.Ok(product);
});

app.MapPost("/api/products", async (CreateProductDto dto, ApplicationDbContext db) =>
{
    var product = new Product
    {
        Name = dto.Name,
        Description = dto.Description,
        Price = dto.Price,
        Quantity = dto.Quantity,
        CreatedAt = DateTime.UtcNow
    };
    
    db.Products.Add(product);
    await db.SaveChangesAsync();
    
    return Results.Created($"/api/products/{product.Id}", product);
});

app.MapPut("/api/products/{id}", async (int id, UpdateProductDto dto, ApplicationDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product == null) return Results.NotFound();
    
    product.Name = dto.Name;
    product.Description = dto.Description;
    product.Price = dto.Price;
    product.Quantity = dto.Quantity;
    product.UpdatedAt = DateTime.UtcNow;
    
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/api/products/{id}", async (int id, ApplicationDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product == null) return Results.NotFound();
    
    db.Products.Remove(product);
    await db.SaveChangesAsync();
    return Results.NoContent();
});




using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        
        // Check if database exists and apply migrations
        if (context.Database.CanConnect())
        {
            Console.WriteLine("Database connection successful.");
            
            // Apply any pending migrations
            if (context.Database.GetPendingMigrations().Any())
            {
                Console.WriteLine("Applying pending migrations...");
                context.Database.Migrate();
                Console.WriteLine("Migrations applied successfully.");
            }
        }
        else
        {
            Console.WriteLine("Creating database and applying migrations...");
            context.Database.Migrate();
            Console.WriteLine("Database created successfully.");
        }
        
        // Seed data if table is empty
        if (!context.Products.Any())
        {
            Console.WriteLine("Seeding initial data...");
            context.Products.AddRange(
                new Product
                {
                    Name = "Laptop",
                    Description = "High-performance laptop with 16GB RAM",
                    Price = 1299.99m,
                    Quantity = 15,
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Wireless Mouse",
                    Description = "Ergonomic wireless mouse with RGB lighting",
                    Price = 49.99m,
                    Quantity = 50,
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Mechanical Keyboard",
                    Description = "RGB mechanical keyboard with blue switches",
                    Price = 89.99m,
                    Quantity = 30,
                    CreatedAt = DateTime.UtcNow
                }
            );
            context.SaveChanges();
            Console.WriteLine("Database seeded with initial data.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred: {ex.Message}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
        }
    }
}

app.Run();