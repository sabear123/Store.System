namespace Itm.Order.Api.Dtos;

public record OrderDto(
    Guid Id,
    int ProductId,
    int Quantity,
    decimal UnitPrice,
    decimal TotalToPay
);
