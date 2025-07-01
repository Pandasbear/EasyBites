using Supabase;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var supaSection = builder.Configuration.GetSection("Supabase");

// Register Supabase client as singleton (DI)
builder.Services.AddSingleton(provider =>
{
    var url = supaSection["Url"] ?? string.Empty;
    var anonKey = supaSection["AnonKey"] ?? string.Empty;
    var options = new SupabaseOptions { AutoConnectRealtime = true };
    return new Client(url, anonKey, options);
});

// Optional: JWT bearer authentication for Supabase Auth tokens if JwtSecret provided
var jwtSecret = supaSection["JwtSecret"];
if (!string.IsNullOrWhiteSpace(jwtSecret))
{
    var issuer = $"{supaSection["Url"]}/auth/v1";
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudiences = new[] { "authenticated" },
                ValidateLifetime = true
            };
        });

    builder.Services.AddAuthorization();
}

builder.Services.AddControllers();

var app = builder.Build();

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

// If authentication was configured, add the middleware before authorization
if (!string.IsNullOrWhiteSpace(jwtSecret))
{
    app.UseAuthentication();
}

app.UseAuthorization();

app.MapStaticAssets();

// Redirect root to static site
app.MapGet("/", () => Results.Redirect("/easybites-platform/index.html"));

app.MapControllers();

app.Run();
