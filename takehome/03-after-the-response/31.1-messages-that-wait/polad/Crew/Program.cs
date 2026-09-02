using Wolverine;
using Wolverine.Nats;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.UseWolverine(opts =>
{
    opts.UseNats(builder.Configuration.GetConnectionString("nats")!);
    opts.ListenToNatsSubject("work-assigned");
});

var app = builder.Build();
app.MapDefaultEndpoints();

app.MapGet("/", () => "crew is running");

app.Run();
