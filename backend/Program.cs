using Microsoft.EntityFrameworkCore;
using CloudCore.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using CloudCore.Middleware;
using CloudCore.Services.Implementations;
using CloudCore.Data.Context;
using Serilog;
using Serilog.Events;

namespace CloudCore
{
    public class Program
    {
        /// <summary>
        /// Starts api and web backend.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static async Task Main(string[] args)
        {

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .WriteTo.Console()
                .WriteTo.File(
                    "logs/cloudCore.txt",
                    rollingInterval: RollingInterval.Day,
                    fileSizeLimitBytes: 10 * 1024 * 1024,
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: 31
                    )
                .CreateLogger();

            try
            {

                Log.Information("Starting CloudCore application");

                // Download .env configuration file
                DotNetEnv.Env.Load("../.env");

                // Load db connection string
                var host = Environment.GetEnvironmentVariable("DB_HOST");
                var port = Environment.GetEnvironmentVariable("DB_PORT");
                var database = Environment.GetEnvironmentVariable("DB_NAME");
                var user = Environment.GetEnvironmentVariable("DB_USER");
                var password = Environment.GetEnvironmentVariable("DB_PASSWORD");

                if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(database))
                {
                    Log.Error("Required environment variables are missing. DB_HOST: {Host}, DB_NAME: {Database}", host, database);
                    throw new InvalidOperationException("Database connection parameters are not configured");
                }

                Log.Information("Database connection configured for {Host}:{Port}/{Database}", host, port, database);

                var connectionString = $"Server={host};Port={port};Database={database};Uid={user};Pwd={password};";



                // Create web
                var builder = WebApplication.CreateBuilder(args);

                builder.Host.UseSerilog();

                // Add db context (in case of multiple use of context, context factory provided)
                builder.Services.AddDbContextFactory<CloudCoreDbContext>(options => options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

                // Add services
                builder.Services.AddScoped<IItemStorageService, ItemStorageService>();
                builder.Services.AddScoped<IAuthService, AuthService>();
                builder.Services.AddScoped<IZipArchiveService, ZipArchiveService>();
                builder.Services.AddScoped<IValidationService, ValidationService>();
                builder.Services.AddScoped<IItemApplication, ItemApplication>();
                builder.Services.AddScoped<IItemRepository, DbRepository>();
                builder.Services.AddScoped<ISubscriptionService, DbRepository>();
                builder.Services.AddScoped<ITrashCleanupService, TrashCleanupService>();
                builder.Services.AddScoped<IItemManagerService, ItemManagerService>();
                builder.Services.AddScoped<IStorageCalculationService, StorageCalculationService>();


                // Add JWT Authentication
                var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? "your-super-secret-key-that-is-at-least-32-characters-long";
                builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = "CloudCore",
                            ValidAudience = "CloudCore",
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                        };
                    });

                // Add authorization
                builder.Services.AddAuthorization();

                // Add controllers and endpoints
                builder.Services.AddControllers();
                builder.Services.AddEndpointsApiExplorer();

                builder.Services.AddSwaggerGen();

                // Add all policy !!!
                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("AllowAll", policy =>
                    {
                        policy.AllowAnyOrigin()
                            .AllowAnyMethod()
                            .AllowAnyHeader();
                    });
                });

                var app = builder.Build();

                app.UseMiddleware<GlobalErrorHandler>();

                // Configure the HTTP request pipeline.
                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }


                // activate
                app.UseCors("AllowAll");
                app.UseRouting();

                // Add authentication & authorization middleware
                app.UseAuthentication();
                app.UseAuthorization();

                app.MapControllers();

                Log.Information("Starting server on http://0.0.0.0:5000");

                app.Run("http://0.0.0.0:5000");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application failed to start");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
