namespace Itm.Inventory.Api.Dtos;

    // Por qué: Solo necesitamos saber QUÉ producto y CUÁNTO reducir, no necesitamos el SKU, el nombre de la operación.
    // Solo necesitamos el ID del producto y la cantidad a reducir.

    public record ReduceStockDto(int ProductId, int Quantity);


