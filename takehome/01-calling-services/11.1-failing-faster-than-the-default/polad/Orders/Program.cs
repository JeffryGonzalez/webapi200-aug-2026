using Orders;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddHttpClient<DepartmentDirectory>(client =>
{
    client.BaseAddress = new Uri("https+http://directory");
});

var app = builder.Build();
app.MapDefaultEndpoints();

app.MapGet("/", () => "orders is running");

app.MapGet("/departments-we-know-about", async (
    DepartmentDirectory directory, CancellationToken token) =>
{
    var departments = await directory.GetDepartmentsAsync(token);
    return Results.Ok(departments);
});

app.Run();
