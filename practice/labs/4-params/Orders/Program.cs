using Orders;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.Configure<RouteHandlerOptions>(options =>
{
    options.ThrowOnBadRequest = true;
});
builder.Services.AddProblemDetails();
builder.Services.AddControllers();

// three ways to register services - and this is almost certainly the wrong way.
//builder.Services.AddTransient<WorkOrder>(); // A whole new instance of this will be created every time it is provided.
//builder.Services.AddScoped<WorkOrder>(); // A new instance of this will be created for each request, and shared within that request.

// If you have mutable data in this class, you have to lock access to it.
// If it doesn't have mutable data, no problem. 
builder.Services.AddSingleton<WorkOrders>(); // A single instance of this will be created and shared for the lifetime of the application.

var app = builder.Build();
app.UseExceptionHandler();
app.MapDefaultEndpoints();
app.UseStatusCodePages();

app.MapGet("/work-orders", (int page, string? department, WorkOrders orders) =>
{
    var results = orders.Page(page, department);
    return Results.Ok(results);
});
app.MapGet("/", () => "orders is running");
app.MapControllers();
app.Run();
