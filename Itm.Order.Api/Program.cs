using Itm.Order.Api.Dtos;
using System.Collections;
using System.Net.NetworkInformation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// REGISTREN LOS CLIENTES HTTP AQUÍ// Recuerden poner los puertos correctos de Inventory y Price

builder.Services.AddHttpClient("InventoryClient", c => c.BaseAddress = new Uri("http://localhost:5245"));

builder.Services.AddHttpClient("PriceClient", c => c.BaseAddress = new Uri("http://localhost:5046"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var ordersDB = new List<OrderResponse>();

app.MapPost("/api/orders", async (CreateOrderDto order, IHttpClientFactory factory) =>
{
    var invClient = factory.CreateClient("InventoryClient");
    var priceClient = factory.CreateClient("PriceClient");

    try
    {
        // 1. Primero obtenemos inventory
        var stock = await invClient.GetFromJsonAsync<InventoryResponse>($"/api/inventory/{order.ProductId}");
        if (stock is null)
        {
            return Results.NotFound("Producto no encontrado en inventario");
        }

        // 2. Luego obtenemos price con manejo de error explícito
        HttpResponseMessage priceResponse = await priceClient.GetAsync($"/api/price/{order.ProductId}");

        if (!priceResponse.IsSuccessStatusCode)
        {
            var error = await priceResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"Error en Price API: {priceResponse.StatusCode} - {error}");
            return Results.Problem($"Error al obtener precio. Status: {priceResponse.StatusCode}");
        }

        var price = await priceResponse.Content.ReadFromJsonAsync<PriceResponse>();

        if (price is null)
        {
            return Results.Problem("No se pudo deserializar la respuesta de precios");
        }

        // 3. Validar Stock
        if (stock.Stock < order.Quantity)
        {
            return Results.BadRequest("No hay suficiente mercancía.");
        }

        // 4. Calcular Total
        decimal total = price.BasePrice * order.Quantity;

        var newOrder = new OrderResponse(
            OrderId: Guid.NewGuid(),
            Product: stock.Sku,
            Quantity: order.Quantity,
            UnitPrice: price.BasePrice,
            TotalToPay: total
            );

        ordersDB.Add(newOrder);

        // 5. Retornar Factura
        return Results.Ok(newOrder);
    }
    catch (HttpRequestException ex)
    {
        return Results.Problem($"Error al consultar servicios externos: {ex.Message}", statusCode: 503);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error inesperado: {ex.Message}", statusCode: 500);
    }
});

app.MapGet("/api/orders", () =>
{
    return Results.Ok(ordersDB);
});

app.Run();

// DTOs Auxiliares (Cópienlos de los otros proyectos)

record InventoryResponse(int ProductId, int Stock, string Sku);
record PriceResponse(int ProductId, decimal BasePrice, string Currency);
record OrderResponse(Guid OrderId, string Product, int Quantity, decimal UnitPrice, decimal TotalToPay);