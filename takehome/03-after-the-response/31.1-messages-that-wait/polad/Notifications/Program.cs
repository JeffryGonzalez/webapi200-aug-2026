using Wolverine;
using Wolverine.Nats;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.UseWolverine(opts =>
{
    opts.UseNats(builder.Configuration.GetConnectionString("nats")!);

    // Tells residents their work order was assigned.
    opts.ListenToNatsSubject("work-assigned");

    // Crew board chatter. Shown on the wallboard in the break room.
    opts.ListenToNatsSubject("shift-notes");
});

var app = builder.Build();
app.MapDefaultEndpoints();

app.MapGet("/", () => "notifications is running");

app.Run();
