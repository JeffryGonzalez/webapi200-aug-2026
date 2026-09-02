var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
var app = builder.Build();
app.MapDefaultEndpoints();

// The department list, as this service happens to return it.
//
// Two things here are deliberate and neither is announced:
//   - Sanitation has no `contact`, sent explicitly as null.
//   - Water & Sewer has no `name` property AT ALL. Not null — absent.
// Written as a raw JSON string so the payload is exactly this and nothing
// serialises the absence away.
const string Departments = """
[
  { "code": "STR", "name": "Streets & Public Works", "contact": "T. Vosmik, x231" },
  { "code": "CLK", "name": "Clerk & Records", "contact": "D. Kuchenbrod, x104" },
  { "code": "SAN", "name": "Sanitation & Solid Waste", "contact": null },
  { "code": "WTR", "contact": "R. Amankwah, x220" },
  { "code": "PRK", "name": "Parks & Recreation", "contact": "J. Prill, x318" }
]
""";

// This service stands in for one you do not control, so it can be told to be slow.
// Nothing outside a practice repository should have an endpoint like this.
var delay = TimeSpan.Zero;

app.MapPost("/delay/{seconds:int}", (int seconds) =>
{
    delay = TimeSpan.FromSeconds(seconds);
    return Results.Ok(new { delaySeconds = delay.TotalSeconds });
});

app.MapGet("/delay", () => Results.Ok(new { delaySeconds = delay.TotalSeconds }));

app.MapGet("/departments", async (CancellationToken token) =>
{
    if (delay > TimeSpan.Zero)
    {
        await Task.Delay(delay, token);
    }

    return Results.Content(Departments, "application/json");
});

app.Run();
