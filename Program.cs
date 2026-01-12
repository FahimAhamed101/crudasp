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

// Add DbContext - Using InMemory for simplicity
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("ProductDB"));

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




// Seed initial data
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    // Ensure database is created
    dbContext.Database.EnsureCreated();
    
    // Seed data if empty
    if (!dbContext.Products.Any())
    {
        dbContext.Products.AddRange(
            new Product
            {
                Id = 1,
                Name = "Laptop",
                Description = "High-performance laptop",
                Price = 999.99m,
                Quantity = 10,
                CreatedAt = DateTime.UtcNow
            },
            new Product
            {
                Id = 2,
                Name = "Mouse",
                Description = "Wireless mouse",
                Price = 29.99m,
                Quantity = 50,
                CreatedAt = DateTime.UtcNow
            }
        );
        dbContext.SaveChanges();
    }
}

app.Run();