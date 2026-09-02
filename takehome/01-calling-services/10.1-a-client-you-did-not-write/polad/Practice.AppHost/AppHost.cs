var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Dispatch>("dispatch");

builder.Build().Run();
