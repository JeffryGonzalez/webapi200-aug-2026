var builder = DistributedApplication.CreateBuilder(args);

var parameter1 = builder.AddParameter("parameter1", false); 
var parameter2 = builder.AddParameter("parameter2", true);

var catalog = builder.AddExternalService("catalog",
    "https://theoria.hypertheory-labs.com/clerk-records/purchasing/");

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume() // store any data outside the container in a volume so it surives reboots
    .AddDatabase("workorders"); // create me a database on that server called "workorders" 

// NATS is here and nothing publishes to it yet. That is deliberate — the wiring is
// present so the first message is a five-minute change rather than an afternoon.
var nats = builder.AddNats("nats");

builder.AddProject<Projects.WorkOrders_Api>("api")
    .WithEnvironment("SOME_ENV_VAR", parameter1)
    .WithEnvironment("SOME_OTHER_ENV_VAR", parameter2)
    .WithReference(postgres).WaitFor(postgres)
    .WithReference(catalog)
    .WithReference(nats).WaitFor(nats);

builder.AddProject<Projects.WorkOrders_Routing>("routing")
    .WithReference(nats).WaitFor(nats);

builder.AddProject<Projects.WorkOrders_Notifications>("notifications")
    .WithReference(nats).WaitFor(nats);

builder.Build().Run();
