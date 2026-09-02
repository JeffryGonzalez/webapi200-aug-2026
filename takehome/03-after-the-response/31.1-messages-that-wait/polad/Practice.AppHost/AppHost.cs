var builder = DistributedApplication.CreateBuilder(args);

var nats = builder.AddNats("nats");

var orders = builder.AddProject<Projects.Orders>("orders")
    .WithReference(nats).WaitFor(nats);

// crew becomes a consumer of the WORK stream in this lab, and orders is what
// defines that stream, so crew must not start first.
builder.AddProject<Projects.Crew>("crew")
    .WithReference(nats).WaitFor(nats)
    .WaitFor(orders);

// Written months ago and never switched on. Start it yourself, when you want to.
builder.AddProject<Projects.Notifications>("notifications")
    .WithExplicitStart()
    .WithReference(nats).WaitFor(nats)
    .WaitFor(orders);

builder.Build().Run();
