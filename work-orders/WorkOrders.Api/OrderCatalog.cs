namespace WorkOrders.Api;


public class PurchasingCatalog(HttpClient client)
{
    public async Task<VendorStanding?> GetStandingAsync(Guid vendorId, CancellationToken token = default)
    {
        var response = await client.GetAsync($"vendors/{vendorId}/standing", token);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<VendorStanding>(token);
    }

    public async Task<Vendor?> FindVendorAsync(string name, CancellationToken token = default)
    {
        var page = await client.GetFromJsonAsync<VendorPage>(
            $"vendors?q={Uri.EscapeDataString(name)}", token);

        return page?.Items.FirstOrDefault();
    }
}

public record Vendor(Guid Id, string Name);
public record VendorPage(List<Vendor> Items, int Page, int PageSize, long Total);

public record VendorStanding(
    Guid VendorId,
    string Status,
    DateOnly EffectiveDate,
    DateOnly? ExpiresOn,
    string Authority,
    string Reason,
    DateOnly ReviewedOn);