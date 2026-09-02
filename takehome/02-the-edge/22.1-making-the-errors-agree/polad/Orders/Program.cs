using Orders;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddSingleton<WorkOrders>();

builder.Services.AddValidation();

var app = builder.Build();
app.MapDefaultEndpoints();

app.MapGet("/", () => "orders is running");

app.MapGet("/work-orders", (int page, string? department, WorkOrders orders) =>
{
    var results = orders.Page(page, department);
    return Results.Ok(results);
});

app.MapPost("/work-orders", (NewWorkOrder order, WorkOrders orders) =>
{
    var created = orders.Add(order.Department, order.Description);
    return Results.Created($"/work-orders/{created.Id}", created);
});

app.Run();
