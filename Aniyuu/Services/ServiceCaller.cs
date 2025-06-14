using System.Text;
using Aniyuu.DbContext;
using Aniyuu.Interfaces;
using Aniyuu.Interfaces.AdminServices;
using Aniyuu.Interfaces.AnimeInterfaces;
using Aniyuu.Interfaces.UserInterfaces;
using Aniyuu.Services.AdminServices;
using Aniyuu.Services.AnimeServices;
using Aniyuu.Services.MessageBroker;
using Aniyuu.Services.UserServices;
using Aniyuu.Utils;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RabbitMQ.Client;

namespace Aniyuu.Services;

public sealed class ServiceCaller
{
    public static void RegisterServices(IServiceCollection services)
    {
        SingletonServices(services);
        ScopedServices(services);
        StartSettings(services);
    }

    private static void ScopedServices(IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IActivationService, ActivationService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IMessagePublisherService, MessagePublisherService>();
        services.AddScoped<IAnimeService, AnimeService>();
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<IAdminAnimeService, AdminAnimeService>();
        services.AddScoped<IAdminAdService, AdminAdService>();
        services.AddScoped<IAdService, AdService>();
        services.AddScoped<IGenreService, GenreService>();
        services.AddScoped<IStudioService, StudioService>();
    }

    private static void SingletonServices(IServiceCollection services)
    {
        var factory = new ConnectionFactory
        {
            HostName = AppSettingConfig.Configuration["MessageBroker:ConnectionString"],
            Port = Convert.ToInt32(AppSettingConfig.Configuration["MessageBroker:Port"]!),
            UserName = AppSettingConfig.Configuration["MessageBroker:Username"],
            Password = AppSettingConfig.Configuration["MessageBroker:Password"],
            DispatchConsumersAsync = true
        };
        
        
        services.AddSingleton<IMongoDbContext, MongoDbContext>();
        services.AddSingleton(new MessageConnectionProvider(factory));
        services.AddSingleton(x =>
            new BlobServiceClient(AppSettingConfig.Configuration["AzureBlob:ConnectionString"]));
    }

    private static void StartSettings(IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v2", new OpenApiInfo 
            { 
                Title = "Aniyuu API", 
                Version = "v2" 
            });

            
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT tokenınızı girerken başına 'Bearer ' eklemeyi unutmayın. Örnek: 'Bearer eyJhbGc...'"
            });

           
            options.AddSecurityDefinition("DeviceId", new OpenApiSecurityScheme
            {
                Name = "device-id", // Header key adı
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header
            });

            
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
                    new List<string>()
                },
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "DeviceId"
                        }
                    },
                    new List<string>()
                }
            });
        });

        
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AppSettingConfig.Configuration["JwtSettings:SecretKey"]!)),
                ValidateIssuer = true,
                ValidIssuer = AppSettingConfig.Configuration["JwtSettings:Issuer"],
                ValidateAudience = true,
                ValidAudience = AppSettingConfig.Configuration["JwtSettings:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });
    }
}