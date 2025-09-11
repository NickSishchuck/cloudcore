using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using CloudCore.Models;
using CloudCore.Services.Interfaces;
using CloudCore.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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
            // Download .env configuration file
            DotNetEnv.Env.Load("../.env");

            // Load db connection string
            var host = Environment.GetEnvironmentVariable("DB_HOST");
            var port = Environment.GetEnvironmentVariable("DB_PORT");
            var database = Environment.GetEnvironmentVariable("DB_NAME");
            var user = Environment.GetEnvironmentVariable("DB_USER");
            var password = Environment.GetEnvironmentVariable("DB_PASSWORD");

            var connectionString = $"Server={host};Port={port};Database={database};Uid={user};Pwd={password};";

            // Create web
            var builder = WebApplication.CreateBuilder(args);

            // Add db context
            builder.Services.AddDbContext<CloudCoreDbContext>(options => options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

            // Add services
            builder.Services.AddScoped<IFileStorageService, FileStorageService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IZipArchiveService, ZipArchiveService>();

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

            app.Run("http://0.0.0.0:5000");
        }
    }
}