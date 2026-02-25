using System.Net.Http.Json;
using Microsoft.Extensions.Http.Resilience;

var builder = WebApplication.CreateBuilder(args);

// 1️⃣ Zona de Servicios
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// InventoryClient
builder.Services.AddHttpClient("InventoryClient", client =>
{
    client.BaseAddress = new Uri("http://localhost:5245");
    client.Timeout = TimeSpan.FromSeconds(5);
});

// PriceClient con resiliencia
builder.Services.AddHttpClient("PriceClient", client =>
{
    client.BaseAddress = new Uri("http://localhost:5046");
    client.Timeout = TimeSpan.FromSeconds(5);
})
.AddStandardResilienceHandler();

var app = builder.Build();

// 2️⃣ Zona de Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var productsDB = new List<ProductDto>
{
    new (1, "Super Laptop"),
    new (2, "Gaming Mouse"),
    new (3, "Office Keyboard")
};

// 3️⃣ Zona de Endpoints
app.MapGet("/api/products/{id}/summary", async (int id, IHttpClientFactory factory, HttpContext httpContext) =>
{
    // 🔹 Buscar producto por ID en "base de datos"
    var product = productsDB.FirstOrDefault(p => p.ProductId == id);
    if (product is null)
        return Results.NotFound(new { Error = $"Producto con ID={id} no existe." });

    var inventoryClient = factory.CreateClient("InventoryClient");
    var priceClient = factory.CreateClient("PriceClient");

    var inventoryTask = inventoryClient.GetFromJsonAsync<InventoryResponse>($"/api/inventory/{id}");
    var priceTask = priceClient.GetFromJsonAsync<PriceDto>($"/api/price/{id}");

    try
    {
        await Task.WhenAll(inventoryTask, priceTask);
    }
    catch (OperationCanceledException)
    {
        return Results.Problem(detail: "La solicitud fue cancelada.", statusCode: StatusCodes.Status499ClientClosedRequest);
    }
    catch (Exception)
    {
        // Continuamos
    }

    var inv = inventoryTask.IsCompletedSuccessfully ? inventoryTask.Result : null;
    if (inv is null)
        return Results.NotFound(new { Error = $"Inventario para ProductID={id} no encontrado." });

    if (!priceTask.IsCompletedSuccessfully || priceTask.Result is null)
        return Results.Problem(detail: "No fue posible obtener el precio en este momento. Por favor intenta más tarde.", statusCode: StatusCodes.Status503ServiceUnavailable);

    var price = priceTask.Result;

    return Results.Ok(
        new ProductSummaryDto(
            ProductId: id,
            Name: product.Name,       // <- viene del DTO
            Stock: inv.Stock,
            Price: price.BasePrice,
            Currency: price.Currency
        ));
});

app.Run();

// 4️⃣ Zona de DTOs (al final del archivo fuera de cualquier bloque)
record InventoryResponse(int ProductId, int Stock, string Sku);
record PriceDto(int ProductId, decimal BasePrice, string Currency);
record ProductDto(int ProductId, string Name);
record ProductSummaryDto(int ProductId, string Name, int Stock, decimal Price, string Currency);