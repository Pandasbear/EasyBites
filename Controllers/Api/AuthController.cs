using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using EasyBites.Services;
using Supabase.Postgrest.Exceptions;
using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace EasyBites.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly Supabase.Client _supabase;
    private readonly ActivityLogService _activityLog;
    private readonly SupabaseStorageService _supabaseStorageService;

    internal static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        var hash = BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        Console.WriteLine($"[HashPassword] Input: {password}, Output hash: {hash}");
        return hash;
    }

    private static string GenerateSessionId() => Guid.NewGuid().ToString();

    public AuthController(Supabase.Client supabase, ActivityLogService activityLog, SupabaseStorageService supabaseStorageService)
    {
        _supabase = supabase;
        _activityLog = activityLog;
        _supabaseStorageService = supabaseStorageService;
        Console.WriteLine("[AuthController] Initialized with Supabase client and ActivityLogService");
    }

    private string GetClientIp()
    {
        return Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? 
               Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    public string GetUserAgent()
    {
        return Request.Headers["User-Agent"].FirstOrDefault() ?? "unknown";
    }

    [HttpPost("bootstrap-admin")]
    public async Task<IActionResult> BootstrapAdmin()
    {
        Console.WriteLine("[BootstrapAdmin] Creating initial admin user");
        
        try {
            // Check if any admin users already exist
            var existingAdmins = await _supabase.From<User>()
                .Filter("is_admin", Supabase.Postgrest.Constants.Operator.Equals, "true")
                .Get();
            
            if (existingAdmins.Models.Any())
            {
                Console.WriteLine("[BootstrapAdmin] Admin user already exists");
                return Conflict("Admin user already exists");
            }
            
            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Admin",
                LastName = "User",
                Email = "admin@easybites.com",
                Username = "admin",
                PasswordHash = HashPassword("admin123"),
                CookingLevel = "Expert",
                IsAdmin = true,
                Active = true,
                CreatedAt = DateTime.UtcNow,
                Bio = "System Administrator",
                FavoriteCuisine = null,
                Location = null,
                ProfileImageUrl = null,
                AdminSecurityCode = "SEC123"
            };
            
            Console.WriteLine($"[BootstrapAdmin] Creating admin user: {adminUser.Email}");
            var response = await _supabase.From<User>().Insert(adminUser);
            
            if (response.Models.Count == 0) {
                Console.WriteLine("[BootstrapAdmin] Failed to create admin user");
                return StatusCode(500, "Failed to create admin user");
            }
            
            // Create corresponding user profile
            var userProfile = new UserProfile
            {
                Id = adminUser.Id,
                FirstName = adminUser.FirstName,
                LastName = adminUser.LastName,
                Username = adminUser.Username,
                FavoriteCuisine = null,
                Location = null,
                Bio = adminUser.Bio,
                AdminVerified = true,
                Active = true,
                CookingLevel = adminUser.CookingLevel,
                CreatedAt = adminUser.CreatedAt,
                UpdatedAt = DateTime.UtcNow,
                LastLogin = null
            };
            
            await _supabase.From<UserProfile>().Insert(userProfile);
            
            Console.WriteLine("[BootstrapAdmin] Admin user created successfully");
            return Ok(new { 
                message = "Admin user created successfully",
                credentials = new {
                    username = "admin",
                    password = "admin123",
                    securityCode = "SEC123"
                }
            });
        }
        catch (Exception ex) {
            Console.WriteLine($"[BootstrapAdmin] Error: {ex.Message}");
            return StatusCode(500, "An error occurred while creating admin user");
        }
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
            var emailFilters = new List<Supabase.Postgrest.Interfaces.IPostgrestQueryFilter>
            {
                new Supabase.Postgrest.QueryFilter("email", Supabase.Postgrest.Constants.Operator.Equals, request.Email)
            };
            var existing = await _supabase.From<User>().Or(emailFilters).Get();
            Console.WriteLine($"[Register] Existing email count: {existing.Models.Count}");
            if (existing.Models.Any()) { Console.WriteLine("[Register] Email already registered"); return Conflict("Email already registered"); }
            
            var usernameFilters = new List<Supabase.Postgrest.Interfaces.IPostgrestQueryFilter>
            {
                new Supabase.Postgrest.QueryFilter("username", Supabase.Postgrest.Constants.Operator.Equals, request.Username)
            };
            var existingUsername = await _supabase.From<User>().Or(usernameFilters).Get();
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
                Active = true, // New users are active by default
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

            // Also create corresponding user profile
            try
            {
                var userProfile = new UserProfile
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Username = user.Username,
                    FavoriteCuisine = user.FavoriteCuisine,
                    Location = user.Location,
                    Bio = user.Bio,
                    AdminVerified = user.IsAdmin,
                    Active = true,
                    CookingLevel = user.CookingLevel ?? "Beginner",
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = DateTime.UtcNow,
                    LastLogin = null // Will be set on first login
                };

                await _supabase.From<UserProfile>().Insert(userProfile);
                Console.WriteLine($"[Register] Created user profile for user {user.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Register] Failed to create user profile: {ex.Message}");
                // Don't fail registration if profile creation fails - user can still login
                // The login process will handle creating the profile if missing
            }
            
            // Log the registration activity
            await _activityLog.LogUserRegisteredAsync(user.Id.ToString(), user.Email, GetClientIp(), GetUserAgent());
            
            Console.WriteLine("[Register] Success");
            return Created("/api/auth/me", new { 
                id = user.Id, 
                email = user.Email, 
                username = user.Username 
            });
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

            // Check if user account is active
            if (!user.Active)
            {
                Console.WriteLine($"[Login] User account is suspended: {user.Email}");
                return Unauthorized(new { success = false, message = "Your account has been suspended. Please contact support for assistance." });
            }
            
            // Create claims for the authenticated user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.IsAdmin ? "Admin" : "User")
            };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) // Session expiry
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Log the cookie being set
            Console.WriteLine($"[Login] Attempted to sign in user {user.Username} ({user.Id}).");
            Console.WriteLine($"[Login] Cookie authentication scheme used: {CookieAuthenticationDefaults.AuthenticationScheme}");
            // Note: HttpContext.Response.Headers["Set-Cookie"] can show the actual cookie header sent
            foreach (var header in HttpContext.Response.Headers["Set-Cookie"])
            {
                Console.WriteLine($"[Login] Set-Cookie header: {header}");
            }

            // Update last login time in user profile
            try
            {
                var userProfileToUpdate = (await _supabase.From<UserProfile>()
                    .Where(p => p.Id == user.Id)
                    .Limit(1)
                    .Get()).Models.FirstOrDefault();

                if (userProfileToUpdate != null)
                {
                    userProfileToUpdate.LastLogin = DateTime.UtcNow;
                    await _supabase.From<UserProfile>().Update(userProfileToUpdate);
                    Console.WriteLine($"[Login] Updated last login for user profile {user.Id}");
                }
                else
                {
                    // If profile doesn't exist, create it (should ideally exist from registration)
                    var newUserProfile = new UserProfile
                    {
                        Id = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Username = user.Username,
                        FavoriteCuisine = user.FavoriteCuisine,
                        Location = user.Location,
                        Bio = user.Bio,
                        AdminVerified = user.IsAdmin,
                        Active = user.Active,
                        CookingLevel = user.CookingLevel,
                        CreatedAt = user.CreatedAt,
                        UpdatedAt = DateTime.UtcNow,
                        LastLogin = DateTime.UtcNow
                    };
                    await _supabase.From<UserProfile>().Insert(newUserProfile);
                    Console.WriteLine($"[Login] Created missing user profile for {user.Id}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Login] Failed to update/create user profile last login: {ex.Message}");
            }

            // Log user activity
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = GetUserAgent();
            await _activityLog.LogUserLoginAsync(user.Id.ToString(), user.Email, ipAddress, userAgent);

            Console.WriteLine("[Login] Success");
            return Ok(new { message = "Login successful!" });
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
            // Use the same QueryFilter pattern as the Login method
            var filters = new List<Supabase.Postgrest.Interfaces.IPostgrestQueryFilter>
            {
                new Supabase.Postgrest.QueryFilter("username", Supabase.Postgrest.Constants.Operator.Equals, request.AdminUsername)
            };
            
            var adminUser = await _supabase.From<User>().Or(filters).Get();
                
            Console.WriteLine($"[AdminLogin] Users found: {adminUser.Models.Count}");
            var user = adminUser.Models.FirstOrDefault(u => u.IsAdmin);
            if (user == null)
            {
                Console.WriteLine("[AdminLogin] Admin user not found");
                return Unauthorized("Invalid admin credentials");
            }
            
            // Check password
            var inputHash = HashPassword(request.AdminPassword);
            Console.WriteLine($"[AdminLogin] Input hash: {inputHash}, DB hash: {user.PasswordHash}");
            if (user.PasswordHash != inputHash)
            {
                Console.WriteLine("[AdminLogin] Password hash mismatch");
                return Unauthorized("Invalid admin credentials");
            }
            
            // Check security code
            if (string.IsNullOrEmpty(user.AdminSecurityCode) || user.AdminSecurityCode != request.AdminCode)
            {
                Console.WriteLine($"[AdminLogin] Security code mismatch. Expected: {user.AdminSecurityCode}, Got: {request.AdminCode}");
                return Unauthorized("Invalid admin credentials");
            }
            
            var sessionId = GenerateSessionId();
            var expiresAt = DateTime.Now.AddDays(1); // Admin sessions expire sooner

            var adminSession = new UserSession
            {
                SessionId = sessionId,
                UserId = user.Id,
                IsAdmin = true, 
                ExpiresAt = expiresAt
            };

            await _supabase.From<UserSession>().Insert(adminSession);

            Response.Cookies.Append("session_id", sessionId, new CookieOptions { 
                HttpOnly = true, 
                SameSite = SameSiteMode.Strict,
                Expires = expiresAt
            });
            
            // Create claims for the authenticated user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("isAdmin", user.IsAdmin.ToString()), // Custom claim for admin status
                new Claim("sessionId", sessionId) // Store session_id as a claim
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true, // Remember me
                ExpiresUtc = expiresAt
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            // Log the sign-in success
            Console.WriteLine($"[AdminLogin] Attempted to sign in admin {user.Username} ({user.Id}).");
            Console.WriteLine($"[AdminLogin] Cookie authentication scheme used: {CookieAuthenticationDefaults.AuthenticationScheme}");
            foreach (var header in HttpContext.Response.Headers["Set-Cookie"])
            {
                Console.WriteLine($"[AdminLogin] Set-Cookie header: {header}");
            }
            Console.WriteLine($"[AdminLogin] Success: {user.Email} (Admin Session)");
            return Ok(new { 
                success = true,
                user = new {
                    id = user.Id, 
                    email = user.Email, 
                    username = user.Username, 
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    isAdmin = true,
                    isAdminSession = true
                }
            });
        }
        catch (Exception ex) {
            Console.WriteLine($"[AdminLogin] Error: {ex.Message}");
            Console.WriteLine($"[AdminLogin] Error details: {ex.ToString()}");
            return StatusCode(500, "An error occurred during admin login");
        }
    }

    // Helper method to get the current authenticated user from HttpContext.User.Claims
    private async Task<User?> GetUserFromDatabase(Guid userId)
    {
        Console.WriteLine($"[GetUserFromDatabase] Looking up user with ID: {userId}");
        var user = (await _supabase.From<User>().Where(u => u.Id == userId).Limit(1).Get()).Models.FirstOrDefault();
        if (user != null)
        {
            Console.WriteLine($"[GetUserFromDatabase] Found user: {user.Username}");
        }
        return user;
    }

    // New helper to retrieve userId from cookie-auth claims
    private Guid? GetAuthenticatedUserIdFromClaims()
    {
        var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return null;
        if (Guid.TryParse(userIdClaim.Value, out var uid)) return uid;
        return null;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        // Get the current user from claims (set during login)
        var userClaims = HttpContext.User.Claims;
        var userIdClaim = userClaims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            Console.WriteLine("[GetCurrentUser] No authenticated user found or invalid user ID.");
            if (userIdClaim == null)
            {
                Console.WriteLine("[GetCurrentUser] ClaimTypes.NameIdentifier not found in claims.");
            }
            else
            {
                Console.WriteLine($"[GetCurrentUser] Failed to parse userIdClaim.Value: '{userIdClaim.Value}' into GUID.");
            }
            Console.WriteLine("[GetCurrentUser] All claims:");
            foreach (var claim in userClaims)
            {
                Console.WriteLine($"  Type: {claim.Type}, Value: {claim.Value}");
            }
            return Unauthorized(new { error = "User not authenticated" });
        }

        // Optionally, fetch full user details from the database if needed
        var user = await GetUserFromDatabase(userId);
        if (user == null)
        {
            Console.WriteLine($"[GetCurrentUser] User profile not found for ID: {userId}");
            return NotFound(new { error = "User profile not found" });
        }

        // Check if there's an admin session for this user
        var isAdminSession = await _supabase.From<UserSession>()
            .Where(s => s.UserId == userId)
            .Where(s => s.IsAdmin == true)
            .Where(s => s.ExpiresAt > DateTime.UtcNow)
            .Limit(1)
            .Get();

        var userDto = new UserDto(
            Id: user.Id,
            Email: user.Email,
            Username: user.Username,
            FirstName: user.FirstName,
            LastName: user.LastName,
            CookingLevel: user.CookingLevel,
            IsAdmin: user.IsAdmin,
            CreatedAt: user.CreatedAt,
            Bio: user.Bio,
            FavoriteCuisine: user.FavoriteCuisine,
            Location: user.Location,
            ProfileImageUrl: user.ProfileImageUrl,
            MemberSince: user.CreatedAt.ToShortDateString(),
            IsAdminSession: isAdminSession.Models.Any() // Check if admin session exists
        );

        return Ok(userDto);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        // Sign out the user using cookie authentication
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        Console.WriteLine("[Logout] User logged out successfully.");
        return Ok(new { message = "Logged out successfully." });
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request)
    {
        try
        {
            Console.WriteLine("[UpdateProfile] Attempting to update user profile");

            // Use cookie-based authentication claims instead of the custom session cookie
            var authenticatedUserId = GetAuthenticatedUserIdFromClaims();
            if (authenticatedUserId == null)
            {
                Console.WriteLine("[UpdateProfile] No authenticated user id found in claims");
                return Unauthorized(new { success = false, message = "Not authenticated" });
            }

            // Retrieve the user from the database
            var user = (await _supabase.From<User>()
                                       .Where(u => u.Id == authenticatedUserId)
                                       .Limit(1)
                                       .Get()).Models.FirstOrDefault();
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

            // Update fields
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.Username = request.Username;
            user.CookingLevel = request.CookingLevel ?? "Beginner";
            user.Bio = request.Bio;
            user.FavoriteCuisine = request.FavoriteCuisine;
            user.Location = request.Location;

            var updateUser = new UserUpdateDTO
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Username = user.Username,
                CookingLevel = user.CookingLevel,
                Bio = user.Bio,
                FavoriteCuisine = user.FavoriteCuisine,
                Location = user.Location
            };

            var response = await _supabase.From<UserUpdateDTO>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, user.Id.ToString())
                .Update(updateUser);

            if (response.Models.Count == 0)
            {
                Console.WriteLine("[UpdateProfile] Failed to update user in Supabase");
                return StatusCode(500, new { success = false, message = "Failed to update profile" });
            }

            // Update user_profiles as before (unchanged)
            try
            {
                var userProfile = new UserProfile
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Username = user.Username,
                    FavoriteCuisine = user.FavoriteCuisine,
                    Location = user.Location,
                    Bio = user.Bio,
                    CookingLevel = user.CookingLevel,
                    UpdatedAt = DateTime.UtcNow
                };

                await _supabase.From<UserProfile>().Upsert(userProfile);
                Console.WriteLine("[UpdateProfile] Updated user_profiles table");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UpdateProfile] Could not update user_profiles: {ex.Message}");
            }

            Console.WriteLine("[UpdateProfile] Success");
            return Ok(new { success = true, message = "Profile updated successfully" });
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
            
            var authenticatedUserId = GetAuthenticatedUserIdFromClaims();
            if (authenticatedUserId == null)
            {
                Console.WriteLine("[ChangePassword] No authenticated user id found in claims");
                return Unauthorized(new { success = false, message = "Not authenticated" });
            }

            var user = (await _supabase.From<User>()
                                       .Where(u => u.Id == authenticatedUserId)
                                       .Limit(1)
                                       .Get()).Models.FirstOrDefault();
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

            var passwordUpdateUser = new PasswordUpdateDTO
            {
                Id = user.Id,
                PasswordHash = HashPassword(request.NewPassword)
            };
            
            var response = await _supabase.From<PasswordUpdateDTO>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, user.Id.ToString())
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
            
            if (!ModelState.IsValid)
            {
                Console.WriteLine("[DeleteAccount] Invalid model");
                return ValidationProblem(ModelState);
            }

            var authenticatedUserId = GetAuthenticatedUserIdFromClaims();
            if (authenticatedUserId == null)
            {
                Console.WriteLine("[DeleteAccount] No authenticated user id found in claims");
                return Unauthorized(new { success = false, message = "Not authenticated" });
            }

            var user = (await _supabase.From<User>()
                                       .Where(u => u.Id == authenticatedUserId)
                                       .Limit(1)
                                       .Get()).Models.FirstOrDefault();
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

            Console.WriteLine($"[DeleteAccount] Deleting user and their data: {user.Id}");
            string userIdString = user.Id.ToString();

            try {
                await _supabase.From<Models.Recipe>()
                    .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, userIdString)
                    .Delete();
                await _supabase.From<SavedRecipe>()
                    .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, userIdString)
                    .Delete();
            }
            catch (Exception ex) {
                Console.WriteLine($"[DeleteAccount] Error deleting related data: {ex.Message}");
            }

            await _supabase.From<User>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, userIdString)
                .Delete();

            var checkUser = await _supabase.From<User>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, userIdString)
                .Get();
            bool deletionSuccessful = checkUser?.Models?.Count == 0;

            if (!deletionSuccessful)
            {
                Console.WriteLine("[DeleteAccount] Failed to delete user from Supabase");
                return StatusCode(500, new { success = false, message = "Failed to delete account" });
            }

            // Sign the user out of cookie auth as well
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            Console.WriteLine("[DeleteAccount] Account deleted successfully");
            return Ok(new { success = true, message = "Account deleted successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DeleteAccount] Error deleting account: {ex.Message}");
            return StatusCode(500, new { success = false, message = "An error occurred while deleting account" });
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

    // User DTO for API responses (prevents Supabase serialization issues)
    public record UserDto(
        Guid Id,
        string Email,
        string Username,
        string FirstName,
        string LastName,
        string CookingLevel,
        bool IsAdmin,
        DateTime CreatedAt,
        string? Bio,
        string? FavoriteCuisine,
        string? Location,
        string? ProfileImageUrl,
        string MemberSince,
        bool IsAdminSession);

    // User model for Supabase
    [Table("users")]
    public new class User : BaseModel
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
        [Column("admin_security_code")] public string? AdminSecurityCode { get; set; }
        [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Column("bio")] public string? Bio { get; set; }
        [Column("favorite_cuisine")] public string? FavoriteCuisine { get; set; }
        [Column("profile_image_url")] public string? ProfileImageUrl { get; set; }
        [Column("location")] public string? Location { get; set; }
        [Column("active")] public bool Active { get; set; } = true;
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

    // Model for user_profiles table
    [Table("user_profiles")]
    public class UserProfile : BaseModel
    {
        [PrimaryKey("id")]
        public Guid Id { get; set; }
        [Column("last_login")] public DateTime? LastLogin { get; set; }
        [Column("first_name")] public string? FirstName { get; set; }
        [Column("last_name")] public string? LastName { get; set; }
        [Column("username")] public string? Username { get; set; }
        [Column("favorite_cuisine")] public string? FavoriteCuisine { get; set; }
        [Column("location")] public string? Location { get; set; }
        [Column("bio")] public string? Bio { get; set; }
        [Column("admin_verified")] public bool AdminVerified { get; set; } = false;
        [Column("active")] public bool Active { get; set; } = true;
        [Column("cooking_level")] public string CookingLevel { get; set; } = "Beginner";
        [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Column("updated_at")] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("user_sessions")]
    public class UserSession : BaseModel
    {
        [Column("session_id")]
        public string SessionId { get; set; } = Guid.NewGuid().ToString();

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("is_admin")]
        public bool IsAdmin { get; set; }

        [Column("expires_at")]
        public DateTime ExpiresAt { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}