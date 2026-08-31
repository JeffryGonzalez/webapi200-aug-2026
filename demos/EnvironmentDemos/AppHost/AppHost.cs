var builder = DistributedApplication.CreateBuilder(args);

var dbConnectionString = builder.AddConnectionString("db");

var apiOne = builder.AddProject<Projects.ApiOne>("apiOne").WithReference(dbConnectionString);

builder.Build().Run();
