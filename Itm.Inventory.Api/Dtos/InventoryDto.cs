namespace Itm.Inventory.Api.Dtos;

//El contrato

// 1. public record: Usamos en vez clase para crear un tipo inmutable y con soporte para igualdad estructural.
// Parametros: (int ProductID,  int Stock, string SKU) define las propiedades del record.

public record InventoryDto(int ProductID,  int Stock, string SKU);