using Serilog;
using SocialAnimal.Core.Portals;
using SocialAnimal.Web.Configuration;
using Microsoft.AspNetCore.Mvc;

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
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SocialAnimal API v1");
    });
}
else
{
    app.UseExceptionHandler("/Admin/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseCors("DefaultPolicy");
app.UseAuthentication();
app.UseAuthorization();

// Configure routing
app.MapControllerRoute(
    name: "admin_area",
    pattern: "admin/{controller=Dashboard}/{action=Index}/{id?}",
    defaults: new { area = "Admin" }
);

app.MapControllerRoute(
    name: "admin_default",
    pattern: "admin",
    defaults: new { area = "Admin", controller = "Dashboard", action = "Index" }
);

app.MapControllers();
app.MapRazorPages();
app.MapHealthChecks("/health");

// Redirect root to admin for now
app.MapGet("/", () => Results.Redirect("/admin"));

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
