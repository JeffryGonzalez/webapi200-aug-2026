using Marten;

namespace WorkOrders.Api;

/// <summary>
/// Work order numbers are "YYYY-NNNN", assigned in order, because that is what people
/// say on the phone. Nothing here is safe under concurrency; nobody has needed it to be.
/// </summary>
public static class Numbering
{
    public static async Task<string> NextAsync(IDocumentSession session, CancellationToken token)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"{year}-";

        var last = await session.Query<WorkOrder>()
            .Where(w => w.Number.StartsWith(prefix))
            .OrderByDescending(w => w.Number)
            .FirstOrDefaultAsync(token);

        var next = last is null ? 1 : int.Parse(last.Number[5..]) + 1;
        return $"{prefix}{next:0000}";
    }
}
