var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

var app = builder.Build();
app.MapDefaultEndpoints();

app.MapGet("/", () => "intake is running");

// A work order arrives. We record it, and then we tell the resident we got it.
// Telling the resident is slow: it goes out over somebody else's network.
app.MapPost("/work-orders", async (WorkOrder order, ILogger<Program> logger) =>
{
    logger.LogInformation("Recorded {Number}", order.Number);

    await NotifyResident(order, logger);

    return Results.Created($"/work-orders/{order.Number}", order);
});

static async Task NotifyResident(WorkOrder order, ILogger logger)
{
    await Task.Delay(TimeSpan.FromSeconds(3));
    logger.LogInformation("Notified {Resident} about {Number}", order.Resident, order.Number);
}

app.Run();

public record WorkOrder(string Number, string Resident, string Location);
