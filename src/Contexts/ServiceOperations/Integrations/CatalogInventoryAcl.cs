namespace CatCar.Contexts.ServiceOperations.Integrations;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RiseOn.RailResult.Upshot;

/// <summary>
/// Translates execution-service catalog responses into ServiceOperations read models.
/// </summary>
public sealed class CatalogInventoryAcl(HttpClient client) : ICatalogInventoryAcl
{
    public async Task<Upshot<CatalogServiceSnapshot>> GetCatalogedServiceSnapshotAsync(Guid catalogedServiceId, CancellationToken cancellationToken = default)
    {
        using var httpResponse = await client.GetAsync(
            $"/internal/v1/catalog/services/{catalogedServiceId}", cancellationToken).ConfigureAwait(false);

        if (httpResponse.StatusCode == HttpStatusCode.NotFound)
            return Upshot<CatalogServiceSnapshot>.Fail("Serviço não encontrado no catálogo.");

        httpResponse.EnsureSuccessStatusCode();
        var response = await httpResponse.Content.ReadFromJsonAsync<CatalogServiceSnapshot>(cancellationToken: cancellationToken).ConfigureAwait(false)
            ?? throw new JsonException("Catalog service response was empty.");

        if (!response.IsActive)
            return Upshot<CatalogServiceSnapshot>.Fail("O serviço solicitado está inativo no catálogo.");

        return Upshot<CatalogServiceSnapshot>.Success(response);
    }

    public async Task<Upshot<InventoryItemSnapshot>> GetInventoryItemSnapshotAsync(Guid inventoryItemId, CancellationToken cancellationToken = default)
    {
        using var httpResponse = await client.GetAsync(
            $"/internal/v1/catalog/parts/{inventoryItemId}", cancellationToken).ConfigureAwait(false);

        if (httpResponse.StatusCode == HttpStatusCode.NotFound)
            return Upshot<InventoryItemSnapshot>.Fail("Peça/insumo não encontrado no estoque.");

        httpResponse.EnsureSuccessStatusCode();
        var response = await httpResponse.Content.ReadFromJsonAsync<InventoryItemSnapshot>(cancellationToken: cancellationToken).ConfigureAwait(false)
            ?? throw new JsonException("Catalog part response was empty.");

        if (!response.IsActive)
            return Upshot<InventoryItemSnapshot>.Fail("A peça/insumo solicitada está inativa no estoque.");

        return Upshot<InventoryItemSnapshot>.Success(response);
    }
}
