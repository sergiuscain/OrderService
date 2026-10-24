namespace OrderService.Application.Orders.Services;

public interface IInventoryService
{
    Task<IReadOnlyList<InventoryProductDto>> GetProductsAsync(
        IEnumerable<Guid> productIds, CancellationToken ct = default);
    Task<bool> TryReserveAsync(
        IReadOnlyDictionary<Guid, int> productQuantities, CancellationToken ct = default);
    Task ReleaseAsync(
        IReadOnlyDictionary<Guid, int> productQuantities, CancellationToken ct = default);
}


