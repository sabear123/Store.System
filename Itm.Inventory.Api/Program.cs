using Itm.Inventory.Api.Dtos; // Importamos el DTO para usarlo en el controlador

var builder = WebApplication.CreateBuilder(args);

//  -- 1. Zona de Servicios
// Aquí se registran los servicios que la aplicación va a usar, como controladores,bases de datos, etc.

builder .Services.AddEndpointsApiExplorer(); // Permite explorar los endpoints de la API
builder .Services.AddSwaggerGen(); // Agrega soporte para Swagger, que genera documentación interactiva de la API

var app = builder.Build(); // Construye la aplicación con los servicios registrados

// -- 2. Zona de Middleware (El Portero)
if(app.Environment.IsDevelopment())
{
    app.UseSwagger(); // Habilita Swagger en modo desarrollo
    app.UseSwaggerUI(); // Habilita la interfaz de usuario de Swagger
}

// -- 3. Zona de Datos (Simulación de la Base de Datos)
// Usamos una lista en memoria para simular una base de datos de inventario. En la vida real, iría el código para conectarse a una base de datos.

var inventoryDB = new List<InventoryDto>
{
    new (1, 100, "SKU001"), // Producto con ID 1, stock de 100 y SKU "SKU001"
    new (2, 0, "SKU002"),  // Producto con ID 2, stock de 0  y SKU "SKU002"
    new (3, 200, "SKU003")  // Producto con ID 3, stock de 200 y SKU "SKU003"
};

// -- 4. Zona de Endpoints (Las Rutas)
// MapGet: Define un endpoint GET para obtener el inventario completo. Si el inventario está vacío, devuelve un error 404.
// "api/inventory7{productId}": Define un endpoint GET para obtener el inventario de un producto específico por su ID. Si el producto no se encuentra, devuelve un error 404.
// GET
app.MapGet("/api/inventory/{id}", (int id) =>
{
    // Lógica LINQ para buscar el producto por su ID en la lista de inventario
    var item = inventoryDB.FirstOrDefault(p => p.ProductId == id);
    // Patrón de respuesta HTTP
    // Si existe el producto, se devuelve con un código de estado 200 OK
    // Si no existe, se devuelve un código de estado 404 Not Found
    return item is not null ? Results.Ok(item) : Results.NotFound();
});

// POST
// Nuevo Endpoint para reducir el stock de un producto específico. Recibe un DTO con el ID del producto y
// la cantidad a reducir. Si el producto no se encuentra, devuelve un error 404. Si el producto existe,
// reduce el stock y devuelve una respuesta 200 OK con un mensaje indicando que el stock ha sido reducido.
// Usamos un [FromBody] implícito al recibir el DTO como parámetro, lo que indica que los datos se deben
// enviar en el cuerpo de la solicitud.
app.MapPost("/api/inventory/reduce", (ReduceStockDto request) =>
{
    // Lógica LINQ para buscar el producto por su ID en la lista de inventario
    var item = inventoryDB.FirstOrDefault(p => p.ProductId == request.ProductId);
    // Validación de la existencia del producto - Reglas de Negocio
    // Si no existe el producto, se devuelve un código de estado 404 Not Found
    if (item is null)
    {
        return Results.NotFound(new { Error = $"Producto con ID {request.ProductId} no encontrado." });
    }
    // Si el producto existe, se reduce el stock y se devuelve una respuesta 200 OK con un mensaje
    if (item.Stock < request.Quantity)
    {
        // Si no hay suficiente stock para reducir, se devuelve un código de estado 400 Bad Request con un mensaje de error
        return Results.BadRequest(new { Error = $"No hay suficiente stock para reducir. Stock actual: {item.Stock}" });
    }
    // Reducimos el stock del producto
    // NOTA: Como usamos 'record' para el DTO, no podemos modificar sus propiedades directamente.
    // En una aplicación real, usaríamos una clase mutable o un patrón de actualización.
    // Ejemplo: hariamos item.Stock -= request.Quantity y luego saveChanges() a la base de datos. 
    // En la vida real, hacemo un update del producto en la base de datos, pero aquí solo modificamos el objeto en memoria.
    var index = inventoryDB.IndexOf(item); // Obtenemos el índice del producto en la lista
    inventoryDB[index] = item with { Stock = item.Stock - request.Quantity };
    // Confirmación de la reducción del stock con un mensaje que incluye la cantidad reducida y el stock actual
    return Results.Ok(new { Message = "Stock actualizado ", NewStock = inventoryDB[index].Stock });
});

app.Run(); // Inicia la aplicación y comienza a escuchar las solicitudes entrantes