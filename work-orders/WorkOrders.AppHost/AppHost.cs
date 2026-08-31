var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("workorders");

// NATS is here and nothing publishes to it yet. That is deliberate — the wiring is
// present so the first message is a five-minute change rather than an afternoon.
var nats = builder.AddNats("nats");

builder.AddProject<Projects.WorkOrders_Api>("api")
    .WithReference(postgres).WaitFor(postgres)
    .WithReference(nats).WaitFor(nats);

builder.AddProject<Projects.WorkOrders_Routing>("routing")
    .WithReference(nats).WaitFor(nats);

builder.AddProject<Projects.WorkOrders_Notifications>("notifications")
    .WithReference(nats).WaitFor(nats);

builder.Build().Run();
