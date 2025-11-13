using Agent2AgentProtocol.Discovery.Service;
using Microsoft.OpenApi.Models;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Configure Kestrel and URL
builder.WebHost.UseUrls("http://localhost:5000");

// Register services
builder.Services.AddSingleton<ICapabilityRegistry, InMemoryCapabilityRegistry>();

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Agent Discovery Service API",
        Version = "v1",
        Description = "API for registering and resolving agent capabilities in the A2A protocol."
    });
});

WebApplication app = builder.Build();

// Enable Swagger middleware
if(app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Agent Discovery Service API v1");
        options.RoutePrefix = string.Empty; // open Swagger UI at root (http://localhost:5000)
    });
}

// Your existing endpoints
app.MapPost("/register", (CapabilityRegistration registration, ICapabilityRegistry registry) =>
{
    registry.RegisterCapability(registration.Capability);
    Console.WriteLine($"/register endpoint requested, {registration.Capability} / {registration.Endpoint} registered.");
    return Results.Ok();
})
.WithName("RegisterCapability"); // helpful for Swagger

app.MapGet("/resolve/{capability}", (string capability, ICapabilityRegistry registry) =>
{
    AgentCapability? endpoint = registry.ResolveCapability(capability);
    return endpoint is not null ? Results.Ok(endpoint) : Results.NotFound();
})
.WithName("ResolveCapability"); // helpful for Swagger

app.MapGet("/list", (ICapabilityRegistry registry) =>
{
    if(registry is InMemoryCapabilityRegistry memoryRegistry)
    {
        IReadOnlyDictionary<string, AgentCapability> all = memoryRegistry.GetAllCapabilities();
        Console.WriteLine($"/list endpoint requested — returning {all.Count} registered capabilities.");
        return Results.Ok(all);
    }

    return Results.BadRequest("Registry does not support listing capabilities.");
});

await app.RunAsync();