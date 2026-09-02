using Orders;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddSingleton<WorkOrders>();

var app = builder.Build();
app.MapDefaultEndpoints();

app.MapGet("/", () => "orders is running");

app.Run();
