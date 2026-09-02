var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
var app = builder.Build();
app.MapDefaultEndpoints();

// The department list, as this service happens to return it.
//
// Two things here are deliberate and neither is announced:
//   - Sanitation has no `contact`, sent explicitly as null.
//   - Water & Sewer has no `name` property AT ALL. Not null - absent.
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

// What a gateway returns when it cannot reach the thing behind it. Note the status:
// as far as HTTP is concerned this request succeeded.
const string ProxyErrorPage = """
<!doctype html>
<html><head><title>502 Bad Gateway</title></head>
<body><h1>502 Bad Gateway</h1><p>The upstream server did not respond.</p></body></html>
""";

// This service is a stand-in for one you do not control, so it can be told to
// misbehave. Nothing outside a practice repository should have an endpoint like this.
var mode = "ok";

app.MapPost("/mode/{value}", (string value) =>
{
    mode = value;
    return Results.Ok(new { mode });
});

app.MapGet("/mode", () => Results.Ok(new { mode }));

app.MapGet("/departments", () => mode switch
{
    "empty" => Results.NoContent(),
    "html" => Results.Content(ProxyErrorPage, "text/html"),
    "object" => Results.Content("""{"departments":[]}""", "application/json"),
    "null" => Results.Content("null", "application/json"),
    "emptylist" => Results.Content("[]", "application/json"),
    _ => Results.Content(Departments, "application/json")
});

app.Run();
