using Marten;
using WorkOrders.Contracts;

namespace WorkOrders.Api;

public static class Endpoints
{
    public static void MapWorkOrders(this IEndpointRouteBuilder app)
    {
        app.MapGet("/work-orders", async (IQuerySession session, CancellationToken token) =>
            Results.Ok(await session.Query<WorkOrder>()
                .OrderBy(w => w.Number)
                .ToListAsync(token)));

        app.MapGet("/work-orders/{number}", async (
            string number, IQuerySession session, CancellationToken token) =>
        {
            var order = await session.Query<WorkOrder>()
                .FirstOrDefaultAsync(w => w.Number == number, token);

            return order is null
                ? Results.Problem(statusCode: 404, title: "No such work order")
                : Results.Ok(order);
        });

        // The website form. The only channel that works end to end.
        app.MapPost("/intake/website-form", async (
            WebsiteFormSubmission submission, IDocumentSession session, CancellationToken token) =>
        {
            var order = new WorkOrder
            {
                Id = Guid.CreateVersion7(),
                Number = await Numbering.NextAsync(session, token),
                Channel = Channel.WebsiteForm,
                Status = WorkOrderStatus.Open,
                ReportedBy = submission.ReportedBy,
                Location = submission.Location,
                Description = submission.Description,
                ReportedOn = DateOnly.FromDateTime(DateTime.UtcNow)
            };

            session.Store(order);
            await session.SaveChangesAsync(token);

            return Results.Created($"/work-orders/{order.Number}", order);
        });
        // The phone at Village Hall.
        app.MapPost("/intake/phone", async (
            PhoneCallReport report, IDocumentSession session, CancellationToken token) =>
        {
            var order = new WorkOrder
            {
                Id = Guid.CreateVersion7(), // Team Venue Decision here - same with date representations.
                Number = await Numbering.NextAsync(session, token),
                Channel = Channel.Phone,
                Status = WorkOrderStatus.Open,
                ReportedBy = string.IsNullOrWhiteSpace(report.ReportedBy)
                    ? "caller did not give name"
                    : report.ReportedBy,
                Location = report.Location,
                Description = report.Description,
                ReportedOn = DateOnly.FromDateTime(DateTime.UtcNow) // my venue decision on dates and times.
            };

            session.Store(order);
            await session.SaveChangesAsync(token);

            return Results.Created($"/work-orders/{order.Number}", order);
        });

        // Dispatch. Nothing here checks whether the vendor may be used.
        app.MapPost("/work-orders/{number}/dispatch", async (
            string number, DispatchRequest request,
            PurchasingCatalog catalog,
            IDocumentSession session, CancellationToken token) =>
        {

            var vendor = await catalog.FindVendorAsync(request.Vendor, token);
            if (vendor is null)
            {
                return Results.Problem(statusCode: 422,
                    title: $"'{request.Vendor}' is not a registered vendor");
            }

            var standing = await catalog.GetStandingAsync(vendor.Id, token);
            if (standing is null)
            {
                return Results.Problem(statusCode: 422,
                    title: $"No standing on record for {vendor.Name}");
            }

            if (standing.Status is not "approved")
            {
                return Results.Problem(statusCode: 422, 
                    title: $"{vendor.Name} is {standing.Status}",
                    detail: $"Effective {standing.EffectiveDate}. {standing.Reason}");
            }

            var order = await session.Query<WorkOrder>()
                .FirstOrDefaultAsync(w => w.Number == number, token);
             
            if (order is null)
            {
                return Results.Problem(statusCode: 404, title: "No such work order");
            }

            order.DispatchedTo = request.Vendor;
            order.DispatchedOn = DateOnly.FromDateTime(DateTime.UtcNow);
            order.Status = WorkOrderStatus.Dispatched;

            session.Store(order);
            await session.SaveChangesAsync(token);

            return Results.Ok(order);
        });
    }
}

public record WebsiteFormSubmission(string ReportedBy, string Location, string Description);
public record DispatchRequest(string Vendor);
public record PhoneCallReport(string? ReportedBy, string Location, string Description);