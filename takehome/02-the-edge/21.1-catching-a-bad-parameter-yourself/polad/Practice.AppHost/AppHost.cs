var builder = DistributedApplication.CreateBuilder(args);

// One service this time. The work orders live in memory inside it.
builder.AddProject<Projects.Orders>("orders");

builder.Build().Run();
