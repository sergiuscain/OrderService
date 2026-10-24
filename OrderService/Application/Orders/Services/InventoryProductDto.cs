namespace OrderService.Application.Orders.Services;

public record InventoryProductDto(
    Guid Id,
    string Name,
    string Sku,
    decimal Price,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int QuantityAvailable,
    int QuantityReserved);