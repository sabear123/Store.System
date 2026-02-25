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

var pricesDb = new List<PriceDto>
{
    new(1, 1299.99m, "USD"),
    new(2, 199.90m, "USD"),
    new(3, 59.50m, "USD")
};

app.MapGet("/api/prices/{id}", (int id) =>
{
    var price = pricesDb.FirstOrDefault(p => p.ProductId == id);

    return price is not null
        ? Results.Ok(price)
        : Results.NotFound(new { Error = $"No existe precio para el producto {id}." });
});

app.Run();