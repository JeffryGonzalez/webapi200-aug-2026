using System.Text.Json;
using System.Text.Json.Serialization;
using Marten;
using WorkOrders.Api;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// Enums go over the wire as lowercase kebab strings — "website-form", "dispatched" —
// matching the purchasing catalog, which callers of this service also call.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter(JsonNamingPolicy.KebabCaseLower));
});

builder.Services.AddMarten((StoreOptions options) =>
    {
        options.Connection(builder.Configuration.GetConnectionString("workorders")!);
    })
    .UseLightweightSessions()
    .InitializeWith(new WorkOrderSeed());

builder.Services.AddHostedService<MailboxAdapter>();

var app = builder.Build();
app.MapDefaultEndpoints();

app.MapGet("/", () => "work orders");
app.MapWorkOrders();

// Stands in for a message arriving in the shared mailbox, so the adapter has
// something to poll without an IMAP server in the room.
app.MapPost("/intake/shared-mailbox/deliver", (MailboxMessage message) =>
{
    MailboxAdapter.Deliver(message);
    return Results.Accepted();
});

app.Run();
