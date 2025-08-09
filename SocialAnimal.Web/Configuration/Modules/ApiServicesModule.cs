using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NodaTime.Serialization.SystemTextJson;
using Serilog;
using System.Text;
using System.Text.Json;
using SocialAnimal.Infrastructure.Db.Context;

namespace SocialAnimal.Web.Configuration.Modules;

public class ApiServicesModule : IServiceModule
{
    public int Order => 3;
    
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Configure controllers
        ConfigureControllers(services);
        
        // Configure authentication
        ConfigureAuthentication(services, configuration);
        
        // Configure Swagger/OpenAPI
        ConfigureSwagger(services);
        
        // Configure CORS
        ConfigureCors(services, configuration);
        
        // Configure health checks
        ConfigureHealthChecks(services);
    }
    
    private static void ConfigureControllers(IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                // Configure JSON serialization
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.WriteIndented = true;
                
                // Add NodaTime serialization support
                options.JsonSerializerOptions.ConfigureForNodaTime(NodaTime.DateTimeZoneProviders.Tzdb);
            });
        
        services.AddEndpointsApiExplorer();
    }
    
    private static void ConfigureAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"];
        
        // Only configure JWT if settings are provided
        if (!string.IsNullOrEmpty(secretKey))
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ClockSkew = TimeSpan.Zero
                };
            });
        }
        else
        {
            // Basic authentication services without JWT
            services.AddAuthentication();
        }
        
        services.AddAuthorization();
    }
    
    private static void ConfigureSwagger(IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "SocialAnimal API",
                Version = "v1",
                Description = "API for managing social events"
            });
            
            // Add JWT authentication to Swagger
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });
            
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });
    }
    
    private static void ConfigureCors(IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
            ?? new[] { "http://localhost:3000" };
        
        services.AddCors(options =>
        {
            options.AddPolicy("DefaultPolicy", builder =>
            {
                builder.WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });
    }
    
    private static void ConfigureHealthChecks(IServiceCollection services)
    {
        services.AddHealthChecks();
        // Note: Database health checks will be configured in InfrastructureServicesModule
        // based on the actual database configuration being used
    }
}