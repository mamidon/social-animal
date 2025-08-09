using Serilog;
using SocialAnimal.Core.Portals;
using SocialAnimal.Web.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Add services using module pattern
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddMessageSubscribers();

var app = builder.Build();

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SocialAnimal API v1");
    });
}

app.UseHttpsRedirection();
app.UseCors("DefaultPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// Start message dispatcher
var messageDispatcher = app.Services.GetRequiredService<IMessageDispatcher>();
await messageDispatcher.StartAsync();

// Ensure graceful shutdown
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(async () =>
{
    await messageDispatcher.StopAsync();
});

app.Run();
