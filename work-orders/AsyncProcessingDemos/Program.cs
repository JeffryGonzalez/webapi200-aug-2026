using JasperFx;
using JasperFx.CodeGeneration;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddValidation();
builder.Host.UseWolverine(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.UseRuntimeCompilation();
    }
    else
    {
        options.CodeGeneration.TypeLoadMode = TypeLoadMode.Static;
    }
}); // come back to this.
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/start-work/{seconds:int}", async (int seconds, IMessageBus bus) =>
{
    //await Task.Delay(seconds * 1000); // have this work happen somewhere else, later
    await bus.PublishAsync(new WaitCommand(seconds));
    return Results.Ok($"Work completed after {seconds} seconds.");
});

app.MapPost("/do-some-work", async (DoSomeWork work) =>
{
   
    return Results.Ok($"Work for {work.Name} completed.");
});


return await app.RunJasperFxCommands(args);

public record WaitCommand(int Seconds);

[JsonSerializable(typeof(DoSomeWork))]
public record DoSomeWork
{
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required, Range(0, 120)]
    public int Age { get; set; }
}