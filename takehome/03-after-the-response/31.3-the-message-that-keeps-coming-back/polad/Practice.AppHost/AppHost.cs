var builder = DistributedApplication.CreateBuilder(args);

var nats = builder.AddNats("nats").WithJetStream();

// orders defines the WORK stream at startup.
var orders = builder.AddProject<Projects.Orders>("orders")
    .WithReference(nats).WaitFor(nats);

// crew is a consumer of that stream, so the stream has to exist first.
builder.AddProject<Projects.Crew>("crew")
    .WithReference(nats).WaitFor(nats)
    .WaitFor(orders);

builder.Build().Run();
