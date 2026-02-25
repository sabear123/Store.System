using Microsoft.AspNetCore.Mvc; // Importamos el espacio de nombres para usar atributos como [ApiController] y [Route]
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Http.Resilience;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer(); // Permite explorar los endpoints de la API
builder.Services.AddSwaggerGen(); // Agrega soporte para Swagger, que genera documentación interactiva de la API

// Registro de clientes HTTP para comunicarse con otros microservicios

builder.Services.AddHttpClient("InventoryClient", client =>
{
    client.BaseAddress = new Uri("http://localhost:5245"); // URL del servicio de inventario

    // Timeout: Configura un tiempo de espera para las solicitudes HTTP, lo que ayuda a evitar que la aplicación se quede esperando indefinidamente si el servicio de inventario no responde.
    client.Timeout = TimeSpan.FromSeconds(5); // Establece un tiempo de espera de 5 segundos

})

// Resiliencia: Agrega políticas de resiliencia para manejar fallos en las solicitudes HTTP, como reintentos
// o circuit breakers, lo que mejora la robustez de la aplicación frente a problemas temporales en el servicio
// de inventario.
// .AddStandardResilienceHandlers(): Este método es parte de la biblioteca de Polly, que proporciona políticas de
// resiliencia predefinidas. Al agregarlo al cliente HTTP, se aplican automáticamente políticas como reintentos,
// circuit breakers y manejo de fallos, lo que ayuda a mejorar la estabilidad y confiabilidad de las comunicaciones
// con el servicio de inventario.
// .AddTypedClient(client => new InventoryClient(client)): Este método permite registrar un cliente HTTP tipado,
// en este caso, InventoryClient, que es una clase personalizada que encapsula la lógica para comunicarse con el
// servicio de inventario. Al usar un cliente tipado, se puede inyectar directamente en los controladores o
// servicios de la aplicación, lo que facilita el uso del cliente HTTP y mejora la mantenibilidad del código.
// Reintentos: Configura una política de reintentos para manejar fallos temporales en las solicitudes HTTP.
// Circuit Breaker: Configura una política de circuit breaker para evitar sobrecargar el servicio de inventario

.AddStandardResilienceHandler();

builder.Services.AddHttpClient("PriceClient", client =>
{
    client.BaseAddress = new Uri("http://localhost:5046");
    client.Timeout = TimeSpan.FromSeconds(5);
})

.AddStandardResilienceHandler();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var products = new Dictionary<int, string>
{
    [1] = "Super Laptop",
    [2] = "Mechanical Keyboard",
    [3] = "Ultra Monitor"
};

// Endopoint Orquetador

// app.MapGet("/api/products/{id}/check-stock", async (int id, IHttpClientFactory factory) =>
app.MapGet("/api/products/{id}/summary", async (int id, IHttpClientFactory factory) =>
{
    // 1. Pedimos prestado un cliente HTTP del pool de clientes registrados en el contenedor de dependencias.
    // var client = factory.CreateClient("InventoryClient");
    if (!products.TryGetValue(id, out var productName))
    {
        return Results.NotFound(new { Error = $"Producto con ID {id} no encontrado." });
    }

    var inventoryClient = factory.CreateClient("InventoryClient");
    var priceClient = factory.CreateClient("PriceClient");

    HttpResponseMessage inventoryResponse;
    HttpResponseMessage priceResponse;
    try
    {
        // 2. Hacemos una solicitud HTTP al servicio de inventario para verificar el stock del producto con el ID proporcionado.
        // var stockData = await client.GetFromJsonAsync<InventoryResponse>($"/api/inventory/{id}");
        var inventoryTask = inventoryClient.GetAsync($"/api/inventory/{id}");
        var priceTask = priceClient.GetAsync($"/api/prices/{id}");

        await Task.WhenAll(inventoryTask, priceTask);

        // 3. Si la respuesta es exitosa, leemos el contenido de la respuesta como un entero, que representa el stock disponible.

        // return Results.Ok(new
        // {
        // ProductID = id,
        // MarketingName = "Super Laptop", // En un caso real, este dato vendría de una base de datos o de otro servicio",
        // StockInfo = stockData,
        // Source = "Live drom Microservice"
        // });

        inventoryResponse = await inventoryTask;
        priceResponse = await priceTask;
    }
    // catch (HttpRequestException ex)
    catch (HttpRequestException)
    {
        // 4. Si ocurre un error durante la solicitud, manejamos la excepción y devolvemos una respuesta de error.
        // return Results.Problem($"Error al comunicarse con el servidor: {ex.Message}");
        return Results.Problem(
            title: "Servicio de precios no disponible",
            detail: "No pudimos obtener el precio en este momento. Intenta nuevamente en unos minutos.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    if (inventoryResponse.StatusCode == HttpStatusCode.NotFound)
    {
        return Results.NotFound(new { Error = $"No hay inventario para el producto con ID {id}." });
    }

    if (priceResponse.StatusCode == HttpStatusCode.NotFound)
    {
        return Results.NotFound(new { Error = $"No hay precio para el producto con ID {id}." });
    }

    inventoryResponse.EnsureSuccessStatusCode();
    priceResponse.EnsureSuccessStatusCode();

    // DTO para la respuesta del servicio de inventario
    var stockData = await inventoryResponse.Content.ReadFromJsonAsync<InventoryResponse>();
    var priceData = await priceResponse.Content.ReadFromJsonAsync<PriceResponse>();

    if (stockData is null || priceData is null)
    {
        return Results.Problem("No fue posible obtener la información completa del producto.");
    }

    return Results.Ok(new
    {
        Name = productName,
        Stock = stockData.Stock,
        Price = new
        {
            Amount = priceData.BasePrice,
            Currency = priceData.Currency
        }
    });
})
.WithName("GetProductSummary");

app.Run();

record InventoryResponse(int ProductId, int Stock, string Sku);
record PriceResponse(int ProductID, decimal BasePrice, string Currency);