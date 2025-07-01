using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace EasyBites.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly Supabase.Client _supabase;
    private static readonly Dictionary<string, string> Sessions = new(); // sessionId -> email

    private static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }

    private static string GenerateSessionId() => Guid.NewGuid().ToString();

    public AuthController(Supabase.Client supabase)
    {
        _supabase = supabase;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (request.Password != request.ConfirmPassword)
            return BadRequest("Passwords do not match");
        if (request.Password.Length < 8)
            return BadRequest("Password must be at least 8 characters.");
        // Check for existing user in Supabase
        var existing = await _supabase.From<User>().Filter("email", Supabase.Postgrest.Constants.Operator.Equals, request.Email).Get();
        if (existing.Models.Any())
            return Conflict("Email already registered");
        var existingUsername = await _supabase.From<User>().Filter("username", Supabase.Postgrest.Constants.Operator.Equals, request.Username).Get();
        if (existingUsername.Models.Any())
            return Conflict("Username already taken");

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Username = request.Username,
            PasswordHash = HashPassword(request.Password),
            CookingLevel = request.CookingLevel,
            IsAdmin = false
        };
        await _supabase.From<User>().Insert(user);
        return Created("/api/auth/me", new { user.Id, user.Email, user.Username });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        // Query by email or username
        var filters = new List<Supabase.Postgrest.Interfaces.IPostgrestQueryFilter>
        {
            new Supabase.Postgrest.QueryFilter("email", Supabase.Postgrest.Constants.Operator.Equals, request.LoginEmail),
            new Supabase.Postgrest.QueryFilter("username", Supabase.Postgrest.Constants.Operator.Equals, request.LoginEmail)
        };
        var users = await _supabase.From<User>().Or(filters).Get();
        var user = users.Models.FirstOrDefault();
        if (user == null || user.PasswordHash != HashPassword(request.LoginPassword))
            return Unauthorized("Invalid credentials");
        var sessionId = GenerateSessionId();
        Sessions[sessionId] = user.Email;
        Response.Cookies.Append("session_id", sessionId, new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Strict });
        return Ok(new { user.Id, user.Email, user.Username });
    }

    [HttpPost("admin-login")]
    public async Task<IActionResult> AdminLogin(AdminLoginRequest request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var users = await _supabase.From<User>()
            .Filter("username", Supabase.Postgrest.Constants.Operator.Equals, request.AdminUsername)
            .Filter("is_admin", Supabase.Postgrest.Constants.Operator.Equals, true)
            .Get();
        var user = users.Models.FirstOrDefault();
        if (user == null || user.PasswordHash != HashPassword(request.AdminPassword) || request.AdminCode != "123456")
            return Unauthorized("Invalid admin credentials");
        var sessionId = GenerateSessionId();
        Sessions[sessionId] = user.Email;
        Response.Cookies.Append("session_id", sessionId, new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Strict });
        return Ok(new { user.Id, user.Email, user.Username, Role = "admin" });
    }

    // DTOs -----------------------------------------------------------------

    public record RegisterRequest(
        [Required] string FirstName,
        [Required] string LastName,
        [Required, EmailAddress] string Email,
        [Required, MinLength(3)] string Username,
        [Required, MinLength(8)] string Password,
        [Required] string ConfirmPassword,
        string? CookingLevel,
        [Required] bool Terms);

    public record LoginRequest(
        [Required] string LoginEmail,
        [Required] string LoginPassword);

    public record AdminLoginRequest(
        [Required] string AdminUsername,
        [Required] string AdminPassword,
        [Required, StringLength(6, MinimumLength = 6)] string AdminCode);

    // User model for Supabase
    [Table("users")]
    public class User : BaseModel
    {
        [PrimaryKey("id")]
        public Guid Id { get; set; }
        [Column("first_name")] public string FirstName { get; set; }
        [Column("last_name")] public string LastName { get; set; }
        [Column("email")] public string Email { get; set; }
        [Column("username")] public string Username { get; set; }
        [Column("password_hash")] public string PasswordHash { get; set; }
        [Column("cooking_level")] public string CookingLevel { get; set; }
        [Column("is_admin")] public bool IsAdmin { get; set; }
    }
} 