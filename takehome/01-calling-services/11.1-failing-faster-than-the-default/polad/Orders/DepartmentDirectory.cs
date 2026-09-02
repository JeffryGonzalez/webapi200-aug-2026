namespace Orders;

public record Department(string Code, string Name, string? Contact);

public class DepartmentDirectory(HttpClient client)
{
    public async Task<IReadOnlyList<Department>> GetDepartmentsAsync(CancellationToken token = default)
    {
        var result = await client.GetFromJsonAsync<List<Department>>("/departments", token);
        return result ?? [];
    }
}
