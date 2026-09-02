using Practice.Contracts;
using Wolverine;
using Wolverine.Nats;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.UseWolverine(opts =>
{
    opts.UseNats(builder.Configuration.GetConnectionString("nats")!);
    opts.PublishAllMessages().ToNatsSubject("work-assigned");
});

var app = builder.Build();
app.MapDefaultEndpoints();

app.MapGet("/", () => "orders is running");

app.MapPost("/assign", async (WorkAssigned assignment, IMessageBus bus) =>
{
    await bus.PublishAsync(assignment);
    return Results.Accepted();
});

app.Run();
