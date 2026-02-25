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

var priceDb = new List<PriceDto>
{
    new(1, 1499.99m, "USD"),
    new(2, 799.50m, "USD"),
    new(3, 2299.00m, "USD")
};

app.MapGet("/api/prices/{id}", (int id) =>
{
    var item = priceDb.FirstOrDefault(p => p.ProductId == id);
    return item is not null ? Results.Ok(item) : Results.NotFound(new { Error = $"Precio para el producto {id} no encontrado." });
});

app.Run();
