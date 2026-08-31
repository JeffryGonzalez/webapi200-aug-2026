using Orders.ApiClients;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// if your API is always going to be calling exactly one outside service, and you know it will stay that way:
//builder.Services.AddHttpClient((client) =>
//{
//    client.BaseAddress = new Uri("////");
//});

//builder.Services.AddHttpClient("github-api", (client) =>
//{
//    // configure
//});

//builder.Services.AddHttpClient("artifactory-api", (client) =>
//{
//    // configure here
//});

// anywhere in my API that needs to call THAT api with the directory, use this client.

builder.Services.AddHttpClient<DepartmentDirectory>(client =>
{
    // you are saying I would like the https endpoint for this, and if that isn't available, I'll take the http one.
    //var directoryUrl = new Uri(builder.Configuration.GetValue<string>("services__directory__https__0"));
    //if(directoryUrl is null)
    //{
    //    directoryUrl = new Uri(builder.Configuration.GetValue<string>("services__directory__http__0"));
    //}
    client.BaseAddress = new Uri("https+http://directory");
    //if(directoryUrl is null)
    //{
    //    throw new Exception("No url for the directory - don't start me!");
    //}
    //client.BaseAddress = new Uri(directoryUrl);
    // configuration might also include, authn/authz, proxy configuration, all that nasty stuff goes here.
});

//builder.Services.AddHttpClient<GithubApi>(client =>
//{
//    client.BaseAddress = new Uri("some-provided-url");
//});
var app = builder.Build();
app.MapDefaultEndpoints();

app.MapGet("/departments-we-know-about", async (DepartmentDirectory directory, CancellationToken ct) =>
{
    var departments = await directory.GetDepartmentsAsync(ct);
    return Results.Ok(departments);
});

//app.MapGet("/something-from-github", (IHttpClientFactory factory) =>
//{
//    var client = factory.CreateClient("github-api");
   
//});

app.Run();
