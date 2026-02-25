using Itm.Price.Api.Dtos;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Simulación de base de datos de precios
var pricesDB = new List<PriceDto>
{
    new (1, 1200.50m, "USD"),
    new (2, 950000.00m, "COP"),
    new (3, 450.00m, "USD")
};

// Endpoint GET: /api/price/{id}
app.MapGet("/api/price/{id}", (int id) =>
{
    var price = pricesDB.FirstOrDefault(p => p.ProductId == id);
    return price is not null ? Results.Ok(price) : Results.NotFound();
});

app.Run();

record PriceDto(int ProductId, decimal BasePrice, string Currency);
