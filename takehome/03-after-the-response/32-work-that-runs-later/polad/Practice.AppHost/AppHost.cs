var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Intake>("intake");

builder.Build().Run();
