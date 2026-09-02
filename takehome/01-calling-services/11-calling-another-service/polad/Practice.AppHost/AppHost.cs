var builder = DistributedApplication.CreateBuilder(args);

// directory belongs to somebody else. Students read it; they do not change it.
var directory = builder.AddProject<Projects.Directory>("directory");

// orders is the service students work in.
builder.AddProject<Projects.Orders>("orders")
    .WithReference(directory)
    .WaitFor(directory);

builder.Build().Run();
