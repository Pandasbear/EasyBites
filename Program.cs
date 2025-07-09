using Supabase;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EasyBites.Services;
using System;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Configure Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "easybites_session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = (context) => 
        {
            context.Response.StatusCode = 401; // Unauthorized
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = (context) =>
        {
            context.Response.StatusCode = 403; // Forbidden
            return Task.CompletedTask;
        };
    });

// This is for Supabase JWTs, keep if you plan to use them for other API calls
var supaSection = builder.Configuration.GetSection("Supabase");

// Register Supabase client as singleton (DI)
builder.Services.AddSingleton(provider =>
{
    var url = supaSection["Url"] ?? string.Empty;
    var anonKey = supaSection["AnonKey"] ?? string.Empty;
    var serviceRoleKey = supaSection["JwtSecret"] ?? anonKey;
    
    // Ensure URL is in the correct format
    if (!url.StartsWith("https://") && !url.StartsWith("http://"))
    {
        url = $"https://{url}";
    }
    
    Console.WriteLine($"Initializing Supabase client with URL: {url}");
    var options = new SupabaseOptions { AutoConnectRealtime = true };
    
    // Use service role key for better permissions when available
    return new Client(url, serviceRoleKey, options);
});

// Optional: JWT bearer authentication for Supabase Auth tokens if JwtSecret provided
// var jwtSecret = supaSection["JwtSecret"];
// if (!string.IsNullOrWhiteSpace(jwtSecret))
// {
//     var url = supaSection["Url"] ?? string.Empty;
//     if (!url.StartsWith("https://") && !url.StartsWith("http://"))
//     {
//         url = $"https://{url}";
//     }
    
//     var issuer = $"{url}/auth/v1";
//     var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

//     builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//         .AddJwtBearer(options =>
//         {
//             options.TokenValidationParameters = new TokenValidationParameters
//             {
//                 ValidateIssuerSigningKey = true,
//                 IssuerSigningKey = key,
//                 ValidateIssuer = true,
//                 ValidIssuer = issuer,
//                 ValidateAudience = true,
//                 ValidAudiences = new[] { "authenticated" },
//                 ValidateLifetime = true
//             };
//         });

//     builder.Services.AddAuthorization();
// }

// Register Gemini and image generation services
builder.Services.AddScoped<GeminiService>();
// Register Lazy factory so services can request Lazy<GeminiService> without issues
builder.Services.AddScoped(provider => new Lazy<GeminiService>(() => provider.GetRequiredService<GeminiService>()));
builder.Services.AddScoped<SupabaseStorageService>();
builder.Services.AddScoped<RecipeImageService>();
builder.Services.AddScoped<ActivityLogService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = false;
        
        // Add converter to handle any remaining serialization issues
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

var app = builder.Build();

// Add Google Cloud credentials debugging
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var credentialsPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
if (!string.IsNullOrEmpty(credentialsPath))
{
    logger.LogInformation("GOOGLE_APPLICATION_CREDENTIALS is set to: {CredentialsPath}", credentialsPath);
    if (File.Exists(credentialsPath))
    {
        logger.LogInformation("Google credentials file exists and is accessible");
    }
    else
    {
        logger.LogWarning("Google credentials file does not exist at: {CredentialsPath}", credentialsPath);
    }
}
else
{
    logger.LogWarning("GOOGLE_APPLICATION_CREDENTIALS environment variable is not set");
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Ensure authentication middleware is always added to the pipeline BEFORE authorization.
app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapStaticAssets();

// Redirect root to static site
app.MapGet("/", () => Results.Redirect("/easybites-platform/index.html"));

app.Run();
