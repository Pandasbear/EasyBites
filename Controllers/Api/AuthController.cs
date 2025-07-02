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
        var hash = BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        Console.WriteLine($"[HashPassword] Input: {password}, Output hash: {hash}");
        return hash;
    }

    private static string GenerateSessionId() => Guid.NewGuid().ToString();

    public AuthController(Supabase.Client supabase)
    {
        _supabase = supabase;
        Console.WriteLine("[AuthController] Initialized with Supabase client");
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        Console.WriteLine($"[Register] Attempt: {request.Email}, {request.Username}");
        if (!ModelState.IsValid) { Console.WriteLine("[Register] Invalid model"); return ValidationProblem(ModelState); }
        if (request.Password != request.ConfirmPassword) { Console.WriteLine("[Register] Passwords do not match"); return BadRequest("Passwords do not match"); }
        if (request.Password.Length < 8) { Console.WriteLine("[Register] Password too short"); return BadRequest("Password must be at least 8 characters."); }
        
        try {
            Console.WriteLine($"[Register] Using Supabase client to register user");
            // Check for existing user in Supabase
            var existing = await _supabase.From<User>().Filter("email", Supabase.Postgrest.Constants.Operator.Equals, request.Email).Get();
            Console.WriteLine($"[Register] Existing email count: {existing.Models.Count}");
            if (existing.Models.Any()) { Console.WriteLine("[Register] Email already registered"); return Conflict("Email already registered"); }
            
            var existingUsername = await _supabase.From<User>().Filter("username", Supabase.Postgrest.Constants.Operator.Equals, request.Username).Get();
            Console.WriteLine($"[Register] Existing username count: {existingUsername.Models.Count}");
            if (existingUsername.Models.Any()) { Console.WriteLine("[Register] Username already taken"); return Conflict("Username already taken"); }
            
            var user = new User
            {
                Id = Guid.NewGuid(),
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Username = request.Username,
                PasswordHash = HashPassword(request.Password),
                CookingLevel = request.CookingLevel ?? "Beginner",
                IsAdmin = false,
                CreatedAt = DateTime.UtcNow,
                Bio = request.Bio,
                FavoriteCuisine = request.FavoriteCuisine,
                Location = request.Location,
                ProfileImageUrl = null // Will be updated later if user uploads an image
            };
            
            Console.WriteLine($"[Register] Inserting user: {user.Email}, {user.Username}, Hash: {user.PasswordHash}");
            Console.WriteLine($"[Register] User ID: {user.Id}");
            var response = await _supabase.From<User>().Insert(user);
            Console.WriteLine($"[Register] Insert response: {response.Models.Count} records");
            
            if (response.Models.Count == 0) {
                Console.WriteLine("[Register] Failed to insert user into Supabase");
                return StatusCode(500, "Failed to create user account");
            }
            
            Console.WriteLine("[Register] Success");
            return Created("/api/auth/me", new { user.Id, user.Email, user.Username });
        }
        catch (Exception ex) {
            Console.WriteLine($"[Register] Error: {ex.Message}");
            Console.WriteLine($"[Register] Error details: {ex.ToString()}");
            return StatusCode(500, "An error occurred during registration");
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        Console.WriteLine($"[Login] Attempt: {request.LoginEmail}");
        if (!ModelState.IsValid) { Console.WriteLine("[Login] Invalid model"); return ValidationProblem(ModelState); }
        
        try {
            Console.WriteLine("[Login] Creating filters for email/username search");
            // Create OR query to match either email or username
            var filters = new List<Supabase.Postgrest.Interfaces.IPostgrestQueryFilter>
            {
                new Supabase.Postgrest.QueryFilter("email", Supabase.Postgrest.Constants.Operator.Equals, request.LoginEmail),
                new Supabase.Postgrest.QueryFilter("username", Supabase.Postgrest.Constants.Operator.Equals, request.LoginEmail)
            };
            
            Console.WriteLine($"[Login] Querying Supabase for user: {request.LoginEmail}");
            Console.WriteLine("[Login] Using Supabase client to query database");
            Console.WriteLine($"[Login] Table being queried: users");
            
            var users = await _supabase.From<User>().Or(filters).Get();
            Console.WriteLine($"[Login] Users found: {users.Models.Count}");
            
            var user = users.Models.FirstOrDefault();
            if (user == null)
            {
                Console.WriteLine("[Login] User not found");
                return Unauthorized(new { success = false, message = "Invalid credentials" });
            }
            
            var inputHash = HashPassword(request.LoginPassword);
            Console.WriteLine($"[Login] Input hash: {inputHash}");
            Console.WriteLine($"[Login] DB hash: {user.PasswordHash}");
            Console.WriteLine($"[Login] Hash comparison: {inputHash == user.PasswordHash}");
            Console.WriteLine($"[Login] Input hash length: {inputHash.Length}, DB hash length: {user.PasswordHash.Length}");
            
            if (inputHash != user.PasswordHash)
            {
                Console.WriteLine("[Login] Password hash mismatch");
                return Unauthorized(new { success = false, message = "Invalid credentials" });
            }
            
            var sessionId = GenerateSessionId();
            Sessions[sessionId] = user.Email;
            Response.Cookies.Append("session_id", sessionId, new CookieOptions { 
                HttpOnly = true, 
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.Now.AddDays(7) // Set cookie to expire in 7 days
            });
            Console.WriteLine($"[Login] Success: {user.Email}");
            return Ok(new { success = true, user = new { user.Id, user.Email, user.Username, user.FirstName, user.LastName } });
        }
        catch (Exception ex) {
            Console.WriteLine($"[Login] Error: {ex.Message}");
            Console.WriteLine($"[Login] Error details: {ex.ToString()}");
            return StatusCode(500, new { success = false, message = "An error occurred during login" });
        }
    }

    [HttpPost("admin-login")]
    public async Task<IActionResult> AdminLogin(AdminLoginRequest request)
    {
        Console.WriteLine($"[AdminLogin] Attempt: {request.AdminUsername}");
        if (!ModelState.IsValid) { Console.WriteLine("[AdminLogin] Invalid model"); return ValidationProblem(ModelState); }
        
        try {
            var users = await _supabase.From<User>()
                .Filter("username", Supabase.Postgrest.Constants.Operator.Equals, request.AdminUsername)
                .Filter("is_admin", Supabase.Postgrest.Constants.Operator.Equals, true)
                .Get();
                
            Console.WriteLine($"[AdminLogin] Users found: {users.Models.Count}");
            var user = users.Models.FirstOrDefault();
            if (user == null)
            {
                Console.WriteLine("[AdminLogin] User not found");
                return Unauthorized("Invalid admin credentials");
            }
            
            var inputHash = HashPassword(request.AdminPassword);
            Console.WriteLine($"[AdminLogin] Input hash: {inputHash}, DB hash: {user.PasswordHash}");
            if (user.PasswordHash != inputHash || request.AdminCode != "123456")
            {
                Console.WriteLine("[AdminLogin] Password hash mismatch or code invalid");
                return Unauthorized("Invalid admin credentials");
            }
            
            var sessionId = GenerateSessionId();
            Sessions[sessionId] = user.Email;
            Response.Cookies.Append("session_id", sessionId, new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Strict });
            Console.WriteLine($"[AdminLogin] Success: {user.Email}");
            return Ok(new { user.Id, user.Email, user.Username, Role = "admin" });
        }
        catch (Exception ex) {
            Console.WriteLine($"[AdminLogin] Error: {ex.Message}");
            Console.WriteLine($"[AdminLogin] Error details: {ex.ToString()}");
            return StatusCode(500, "An error occurred during admin login");
        }
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            if (!Request.Cookies.TryGetValue("session_id", out var sessionId))
            {
                Console.WriteLine("[GetCurrentUser] No session cookie found");
                return Unauthorized(new { success = false, message = "Not authenticated" });
            }

            if (!Sessions.TryGetValue(sessionId, out var email))
            {
                Console.WriteLine("[GetCurrentUser] Session ID not found in session store");
                return Unauthorized(new { success = false, message = "Session expired" });
            }

            Console.WriteLine($"[GetCurrentUser] Looking up user with email: {email}");
            var users = await _supabase.From<User>()
                .Filter("email", Supabase.Postgrest.Constants.Operator.Equals, email)
                .Get();

            var user = users.Models.FirstOrDefault();
            if (user == null)
            {
                Console.WriteLine("[GetCurrentUser] User not found in database");
                Sessions.Remove(sessionId);
                Response.Cookies.Delete("session_id");
                return Unauthorized(new { success = false, message = "User not found" });
            }

            Console.WriteLine($"[GetCurrentUser] Found user: {user.Username}");
            return Ok(new
            {
                id = user.Id,
                email = user.Email,
                username = user.Username,
                firstName = user.FirstName,
                lastName = user.LastName,
                cookingLevel = user.CookingLevel,
                isAdmin = user.IsAdmin,
                createdAt = user.CreatedAt,
                bio = user.Bio,
                favoriteCuisine = user.FavoriteCuisine,
                location = user.Location,
                profileImageUrl = user.ProfileImageUrl,
                memberSince = user.CreatedAt.ToString("MMMM yyyy")
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetCurrentUser] Error: {ex.Message}");
            Console.WriteLine($"[GetCurrentUser] Error details: {ex}");
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving user information" });
        }
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        try
        {
            if (Request.Cookies.TryGetValue("session_id", out var sessionId))
            {
                Console.WriteLine("[Logout] Removing session");
                Sessions.Remove(sessionId);
                Response.Cookies.Delete("session_id");
            }
            return Ok(new { success = true, message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Logout] Error: {ex.Message}");
            return StatusCode(500, new { success = false, message = "An error occurred during logout" });
        }
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request)
    {
        try
        {
            Console.WriteLine("[UpdateProfile] Attempting to update user profile");
            
            // Check authentication
            if (!Request.Cookies.TryGetValue("session_id", out var sessionId))
            {
                Console.WriteLine("[UpdateProfile] No session cookie found");
                return Unauthorized(new { success = false, message = "Not authenticated" });
            }

            if (!Sessions.TryGetValue(sessionId, out var email))
            {
                Console.WriteLine("[UpdateProfile] Session ID not found in session store");
                return Unauthorized(new { success = false, message = "Session expired" });
            }

            // Find the user
            var users = await _supabase.From<User>()
                .Filter("email", Supabase.Postgrest.Constants.Operator.Equals, email)
                .Get();

            var user = users.Models.FirstOrDefault();
            if (user == null)
            {
                Console.WriteLine("[UpdateProfile] User not found in database");
                return Unauthorized(new { success = false, message = "User not found" });
            }

            // Check if username is being changed and if it's already taken
            if (request.Username != user.Username)
            {
                var existingUsername = await _supabase.From<User>()
                    .Filter("username", Supabase.Postgrest.Constants.Operator.Equals, request.Username)
                    .Get();
                
                if (existingUsername.Models.Any())
                {
                    Console.WriteLine("[UpdateProfile] Username already taken");
                    return Conflict(new { success = false, message = "Username already taken" });
                }
            }

            // Update user properties but leave created_at untouched
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.Username = request.Username;
            user.CookingLevel = request.CookingLevel ?? "Beginner";
            user.Bio = request.Bio;
            user.FavoriteCuisine = request.FavoriteCuisine;
            user.Location = request.Location;

            // Create a minimal user object with just the fields we want to update
            var updateUser = new UserUpdateDTO
            {
                Id = user.Id,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Username = request.Username,
                CookingLevel = request.CookingLevel ?? "Beginner",
                Bio = request.Bio,
                FavoriteCuisine = request.FavoriteCuisine,
                Location = request.Location
            };

            // Save changes
            Console.WriteLine($"[UpdateProfile] Updating user: {user.Email}, {user.Username}");
            var response = await _supabase.From<UserUpdateDTO>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, user.Id)
                .Update(updateUser);

            if (response.Models.Count == 0)
            {
                Console.WriteLine("[UpdateProfile] Failed to update user in Supabase");
                return StatusCode(500, new { success = false, message = "Failed to update profile" });
            }

            Console.WriteLine("[UpdateProfile] Success");
            return Ok(new { 
                success = true, 
                message = "Profile updated successfully",
                user = new {
                    id = user.Id,
                    email = user.Email,
                    username = user.Username,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    cookingLevel = user.CookingLevel,
                    bio = user.Bio,
                    favoriteCuisine = user.FavoriteCuisine,
                    location = user.Location
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UpdateProfile] Error: {ex.Message}");
            Console.WriteLine($"[UpdateProfile] Error details: {ex}");
            return StatusCode(500, new { success = false, message = "An error occurred while updating profile" });
        }
    }

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        try
        {
            Console.WriteLine("[ChangePassword] Attempting to change password");
            
            if (!ModelState.IsValid)
            {
                Console.WriteLine("[ChangePassword] Invalid model");
                return ValidationProblem(ModelState);
            }
            
            if (request.NewPassword != request.ConfirmPassword)
            {
                Console.WriteLine("[ChangePassword] New passwords do not match");
                return BadRequest(new { success = false, message = "New passwords do not match" });
            }
            
            if (request.NewPassword.Length < 8)
            {
                Console.WriteLine("[ChangePassword] New password too short");
                return BadRequest(new { success = false, message = "Password must be at least 8 characters" });
            }
            
            // Check authentication
            if (!Request.Cookies.TryGetValue("session_id", out var sessionId))
            {
                Console.WriteLine("[ChangePassword] No session cookie found");
                return Unauthorized(new { success = false, message = "Not authenticated" });
            }

            if (!Sessions.TryGetValue(sessionId, out var email))
            {
                Console.WriteLine("[ChangePassword] Session ID not found in session store");
                return Unauthorized(new { success = false, message = "Session expired" });
            }

            // Find the user
            var users = await _supabase.From<User>()
                .Filter("email", Supabase.Postgrest.Constants.Operator.Equals, email)
                .Get();

            var user = users.Models.FirstOrDefault();
            if (user == null)
            {
                Console.WriteLine("[ChangePassword] User not found in database");
                return Unauthorized(new { success = false, message = "User not found" });
            }

            // Verify current password
            var currentPasswordHash = HashPassword(request.CurrentPassword);
            if (currentPasswordHash != user.PasswordHash)
            {
                Console.WriteLine("[ChangePassword] Current password is incorrect");
                return BadRequest(new { success = false, message = "Current password is incorrect" });
            }

            // Create a minimal user object with just the password hash
            var passwordUpdateUser = new PasswordUpdateDTO
            {
                Id = user.Id,
                PasswordHash = HashPassword(request.NewPassword)
            };
            
            // Save changes
            Console.WriteLine($"[ChangePassword] Updating password for user: {user.Email}");
            var response = await _supabase.From<PasswordUpdateDTO>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, user.Id)
                .Update(passwordUpdateUser);

            if (response.Models.Count == 0)
            {
                Console.WriteLine("[ChangePassword] Failed to update password in Supabase");
                return StatusCode(500, new { success = false, message = "Failed to update password" });
            }

            Console.WriteLine("[ChangePassword] Success");
            return Ok(new { success = true, message = "Password changed successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ChangePassword] Error: {ex.Message}");
            Console.WriteLine($"[ChangePassword] Error details: {ex}");
            return StatusCode(500, new { success = false, message = "An error occurred while changing password" });
        }
    }

    [HttpPost("delete-account")]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest request)
    {
        try
        {
            Console.WriteLine("[DeleteAccount] Attempting to delete user account");
            Console.WriteLine($"[DeleteAccount] Request: {request?.Password ?? "null"}");
            
            // Log model state errors if any
            if (!ModelState.IsValid)
            {
                Console.WriteLine("[DeleteAccount] Invalid model");
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        Console.WriteLine($"[DeleteAccount] Model error - {state.Key}: {error.ErrorMessage}");
                    }
                }
                return ValidationProblem(ModelState);
            }
            
            // Check authentication
            if (!Request.Cookies.TryGetValue("session_id", out var sessionId))
            {
                Console.WriteLine("[DeleteAccount] No session cookie found");
                return Unauthorized(new { success = false, message = "Not authenticated" });
            }

            if (!Sessions.TryGetValue(sessionId, out var email))
            {
                Console.WriteLine("[DeleteAccount] Session ID not found in session store");
                return Unauthorized(new { success = false, message = "Session expired" });
            }

            // Find the user
            var users = await _supabase.From<User>()
                .Filter("email", Supabase.Postgrest.Constants.Operator.Equals, email)
                .Get();

            var user = users.Models.FirstOrDefault();
            if (user == null)
            {
                Console.WriteLine("[DeleteAccount] User not found in database");
                return Unauthorized(new { success = false, message = "User not found" });
            }

            // Verify password
            var passwordHash = HashPassword(request.Password);
            if (passwordHash != user.PasswordHash)
            {
                Console.WriteLine("[DeleteAccount] Password is incorrect");
                return BadRequest(new { success = false, message = "Password is incorrect" });
            }
            
            // Note: In a production environment, you would want to delete related data first
            // For this demo, we'll delete related data before deleting the user
            Console.WriteLine($"[DeleteAccount] Deleting user and their data: {user.Id}");
            
            // Delete the user account
            Console.WriteLine($"[DeleteAccount] Deleting user: {user.Email}, {user.Username}");
            // Convert Guid to string for the filter
            string userIdString = user.Id.ToString();
            Console.WriteLine($"[DeleteAccount] User ID as string: {userIdString}");
            
            // First delete related data
            try {
                // Execute direct SQL queries instead of RPC functions
                Console.WriteLine($"[DeleteAccount] Deleting user recipes directly");
                await _supabase.From<Models.Recipe>()
                    .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, userIdString)
                    .Delete();
                    
                Console.WriteLine($"[DeleteAccount] Deleting user saved recipes directly");
                await _supabase.From<SavedRecipe>()
                    .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, userIdString)
                    .Delete();
            }
            catch (Exception ex) {
                Console.WriteLine($"[DeleteAccount] Error deleting related data: {ex.Message}");
                Console.WriteLine($"[DeleteAccount] Error details: {ex}");
                // Continue with user deletion even if related data deletion fails
            }
            
            // Now delete the user
            await _supabase.From<User>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, userIdString)
                .Delete();
                
            // For checking if deletion was successful, we'll try to find the user again
            var checkUser = await _supabase.From<User>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, userIdString)
                .Get();
                
            bool deletionSuccessful = checkUser.Models.Count == 0;

            if (!deletionSuccessful)
            {
                Console.WriteLine("[DeleteAccount] Failed to delete user from Supabase");
                return StatusCode(500, new { success = false, message = "Failed to delete account" });
            }

            // Remove session
            Sessions.Remove(sessionId);
            Response.Cookies.Delete("session_id");

            Console.WriteLine("[DeleteAccount] Success");
            return Ok(new { success = true, message = "Account deleted successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DeleteAccount] Error: {ex.Message}");
            Console.WriteLine($"[DeleteAccount] Error details: {ex}");
            Console.WriteLine($"[DeleteAccount] Stack trace: {ex.StackTrace}");
            
            // Return a more detailed error message for debugging
            return StatusCode(500, new { success = false, message = $"An error occurred while deleting account: {ex.Message}" });
        }
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
        string? Bio,
        string? FavoriteCuisine,
        string? Location,
        [Required] bool Terms);

    public record LoginRequest(
        [Required] string LoginEmail,
        [Required] string LoginPassword);

    public record AdminLoginRequest(
        [Required] string AdminUsername,
        [Required] string AdminPassword,
        [Required, StringLength(6, MinimumLength = 6)] string AdminCode);
        
    public record UpdateProfileRequest(
        [Required] string FirstName,
        [Required] string LastName,
        [Required, MinLength(3)] string Username,
        string? CookingLevel,
        string? Bio,
        string? FavoriteCuisine,
        string? Location);
        
    public record ChangePasswordRequest(
        [Required] string CurrentPassword,
        [Required, MinLength(8)] string NewPassword,
        [Required] string ConfirmPassword);
        
    public record DeleteAccountRequest(
        [Required] string Password);

    // User model for Supabase
    [Table("users")]
    public class User : BaseModel
    {
        [PrimaryKey("id")]
        public Guid Id { get; set; }
        [Column("first_name")] public string FirstName { get; set; } = string.Empty;
        [Column("last_name")] public string LastName { get; set; } = string.Empty;
        [Column("email")] public string Email { get; set; } = string.Empty;
        [Column("username")] public string Username { get; set; } = string.Empty;
        [Column("password_hash")] public string PasswordHash { get; set; } = string.Empty;
        [Column("cooking_level")] public string CookingLevel { get; set; } = string.Empty;
        [Column("is_admin")] public bool IsAdmin { get; set; }
        [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Column("bio")] public string? Bio { get; set; }
        [Column("favorite_cuisine")] public string? FavoriteCuisine { get; set; }
        [Column("profile_image_url")] public string? ProfileImageUrl { get; set; }
        [Column("location")] public string? Location { get; set; }
    }
    
    // DTO for user updates to avoid created_at issues
    [Table("users")]
    public class UserUpdateDTO : BaseModel
    {
        [PrimaryKey("id")]
        public Guid Id { get; set; }
        [Column("first_name")] public string FirstName { get; set; } = string.Empty;
        [Column("last_name")] public string LastName { get; set; } = string.Empty;
        [Column("username")] public string Username { get; set; } = string.Empty;
        [Column("cooking_level")] public string CookingLevel { get; set; } = string.Empty;
        [Column("bio")] public string? Bio { get; set; }
        [Column("favorite_cuisine")] public string? FavoriteCuisine { get; set; }
        [Column("location")] public string? Location { get; set; }
    }
    
    // DTO for password updates to avoid created_at issues
    [Table("users")]
    public class PasswordUpdateDTO : BaseModel
    {
        [PrimaryKey("id")]
        public Guid Id { get; set; }
        [Column("password_hash")] public string PasswordHash { get; set; } = string.Empty;
    }
    
    // Model for saved recipes table
    [Table("saved_recipes")]
    public class SavedRecipe : BaseModel
    {
        [PrimaryKey("id")]
        public Guid Id { get; set; }
        [Column("user_id")] public string UserId { get; set; } = string.Empty;
        [Column("recipe_id")] public string RecipeId { get; set; } = string.Empty;
    }
} 