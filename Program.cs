using Microsoft.EntityFrameworkCore;
using ProductAPI.Data;
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