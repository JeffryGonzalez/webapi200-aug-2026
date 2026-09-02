var builder = DistributedApplication.CreateBuilder(args);

var nats = builder.AddNats("nats");

builder.AddProject<Projects.Orders>("orders")
    .WithReference(nats).WaitFor(nats);

builder.AddProject<Projects.Crew>("crew")
    .WithReference(nats).WaitFor(nats);

builder.Build().Run();
