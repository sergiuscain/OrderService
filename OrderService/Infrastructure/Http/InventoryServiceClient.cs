using OrderService.Application.Orders.Services;

namespace OrderService.Infrastructure.Http;

public class InventoryServiceClient : IInventoryService
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public InventoryServiceClient(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _baseUrl = configuration["InventoryService:BaseUrl"]
                   ?? throw new InvalidOperationException("InventoryService:BaseUrl is not configured.");
    }

    public async Task<IReadOnlyList<InventoryProductDto>> GetProductsAsync(
        IEnumerable<Guid> productIds, CancellationToken ct = default)
    {
        var ids = string.Join(',', productIds.Distinct());
        var url = $"{_baseUrl}/api/products/by-ids?ids={ids}";
        var result = await _http.GetFromJsonAsync<List<InventoryProductDto>>(url, ct);
        return result ?? new List<InventoryProductDto>();
    }

    public async Task<bool> TryReserveAsync(
        IReadOnlyDictionary<Guid, int> productQuantities, CancellationToken ct = default)
    {
        var url = $"{_baseUrl}/api/products/reserve";
        var payload = productQuantities
            .Select(kv => new { productId = kv.Key, quantity = kv.Value })
            .ToList();

        using var response = await _http.PostAsJsonAsync(url, new { items = payload }, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task ReleaseAsync(
        IReadOnlyDictionary<Guid, int> productQuantities, CancellationToken ct = default)
    {
        var url = $"{_baseUrl}/api/products/release";
        var payload = productQuantities
            .Select(kv => new { productId = kv.Key, quantity = kv.Value })
            .ToList();

        using var response = await _http.PostAsJsonAsync(url, new { items = payload }, ct);
        response.EnsureSuccessStatusCode();
    }
}
