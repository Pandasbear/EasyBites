using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc;
using Supabase;
using Supabase.Postgrest;
using Supabase.Gotrue;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using EasyBites.Services;
using EasyBites.Models;
using System.Collections.Concurrent;
using static EasyBites.Controllers.Api.RecipesController; 

namespace EasyBites.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly Supabase.Client _supabase;
    private readonly ActivityLogService _activityLog;
    private readonly RecipeImageService _recipeImageService;

    public AdminController(Supabase.Client supabase, ActivityLogService activityLog, RecipeImageService recipeImageService)
    {
        _supabase = supabase;
        _activityLog = activityLog;
        _recipeImageService = recipeImageService;
    }

    // Helper method to get the current authenticated user
    private async Task<UserDto?> GetCurrentUser()
    {
        if (!Request.Cookies.TryGetValue("session_id", out var sessionId))
        {
            Console.WriteLine("[GetCurrentUser - AdminController] No session cookie found.");
            return null;
        }

        Console.WriteLine($"[GetCurrentUser - AdminController] Session ID found: {sessionId}");

        // Retrieve session from database
        var session = (await _supabase.From<AuthController.UserSession>()
                                    .Where(s => s.SessionId == sessionId)
                                    .Limit(1)
                                    .Get()).Models.FirstOrDefault();

        if (session == null)
        {
            Console.WriteLine($"[GetCurrentUser - AdminController] Session {sessionId} not found in DB.");
            Response.Cookies.Delete("session_id");
            return null;
        }

        if (session.ExpiresAt < DateTime.UtcNow)
        {
            Console.WriteLine($"[GetCurrentUser - AdminController] Session {sessionId} expired at {session.ExpiresAt}.");
            await _supabase.From<AuthController.UserSession>().Where(s => s.SessionId == sessionId).Delete();
            Response.Cookies.Delete("session_id");
            return null;
        }

        Console.WriteLine($"[GetCurrentUser - AdminController] Session {sessionId} found in DB, UserId: {session.UserId}, IsAdmin: {session.IsAdmin}.");

        // Fetch the user details from the 'users' table
        var userResponse = await _supabase.From<AuthController.User>()
                .Where(u => u.Id == session.UserId)
                .Get();
        var user = userResponse.Models.FirstOrDefault();

        if (user == null)
        {
            Console.WriteLine($"[GetCurrentUser - AdminController] User not found in database for session {sessionId}. Deleting session.");
            await _supabase.From<AuthController.UserSession>().Where(s => s.SessionId == sessionId).Delete();
            Response.Cookies.Delete("session_id");
            return null;
        }

        Console.WriteLine($"[GetCurrentUser - AdminController] User found: {user.Username}, DB IsAdmin: {user.IsAdmin}.");

        bool isAdminSession = session.IsAdmin; // Use isAdmin flag from the database session

        return new UserDto(
            user.Id,
            user.Email,
            user.Username,
            user.FirstName,
            user.LastName,
            user.CookingLevel,
            user.IsAdmin,
            user.CreatedAt,
            user.Bio,
            user.FavoriteCuisine,
            user.Location,
            user.ProfileImageUrl,
            user.CreatedAt.ToString("MMMM yyyy"),
            isAdminSession
        );
    }

    // DTO for user information (matching AuthController.UserDto)
    public record UserDto(
        Guid Id,
        string Email,
        string Username,
        string FirstName,
        string LastName,
        string? CookingLevel,
        bool IsAdmin,
        DateTime CreatedAt,
        string? Bio,
        string? FavoriteCuisine,
        string? Location,
        string? ProfileImageUrl,
        string MemberSince,
        bool IsAdminSession
    );

    // Helper method to check admin authentication
    private async Task<AuthController.User?> GetCurrentAdminUser()
    {
        try
        {
            if (!Request.Cookies.TryGetValue("session_id", out var sessionId))
            {
                Console.WriteLine("[GetCurrentAdminUser - AdminController] No session cookie found.");
                return null;
            }
            Console.WriteLine($"[GetCurrentAdminUser - AdminController] Session ID found: {sessionId}.");

            // Retrieve session from database
            var session = (await _supabase.From<AuthController.UserSession>()
                                        .Where(s => s.SessionId == sessionId)
                                        .Limit(1)
                                        .Get()).Models.FirstOrDefault();

            if (session == null)
            {
                Console.WriteLine($"[GetCurrentAdminUser - AdminController] Session {sessionId} not found in DB.");
                Response.Cookies.Delete("session_id");
                return null;
            }

            if (session.ExpiresAt < DateTime.UtcNow)
            {
                Console.WriteLine($"[GetCurrentAdminUser - AdminController] Session {sessionId} expired at {session.ExpiresAt}.");
                await _supabase.From<AuthController.UserSession>().Where(s => s.SessionId == sessionId).Delete();
                Response.Cookies.Delete("session_id");
                return null;
            }

            if (!session.IsAdmin)
            {
                Console.WriteLine($"[GetCurrentAdminUser - AdminController] Session {sessionId} is not marked as admin. Deleting session.");
                await _supabase.From<AuthController.UserSession>().Where(s => s.SessionId == sessionId).Delete();
                Response.Cookies.Delete("session_id");
                return null;
            }

            Console.WriteLine($"[GetCurrentAdminUser - AdminController] Session {sessionId} is an admin session, UserId: {session.UserId}.");

            // Fetch the user from Supabase to ensure they still have admin privileges
            var userResponse = await _supabase.From<AuthController.User>()
                .Where(u => u.Id == session.UserId)
                .Get();

            var user = userResponse.Models.FirstOrDefault();

            if (user == null)
            {
                Console.WriteLine($"[GetCurrentAdminUser - AdminController] User not found for session {sessionId}. Deleting session.");
                await _supabase.From<AuthController.UserSession>().Where(s => s.SessionId == sessionId).Delete();
                Response.Cookies.Delete("session_id");
                return null;
            }

            Console.WriteLine($"[GetCurrentAdminUser - AdminController] User {user.Username} found from DB, IsAdmin: {user.IsAdmin}.");

            // Ensure the user exists and their IsAdmin flag is true in the users table
            if (user.IsAdmin == true)
            {
                Console.WriteLine($"[GetCurrentAdminUser - AdminController] User {user.Username} is confirmed as admin.");
                return user;
            }
            else
            {
                Console.WriteLine($"[GetCurrentAdminUser - AdminController] User {user.Username} is not an admin in the users table. Deleting session.");
                await _supabase.From<AuthController.UserSession>().Where(s => s.SessionId == sessionId).Delete();
                Response.Cookies.Delete("session_id");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetCurrentAdminUser - AdminController] Error: {ex.Message}. Details: {ex}");
            return null;
        }
    }

    private string GetClientIp()
    {
        return Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? 
               Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private string GetUserAgent()
    {
        return Request.Headers["User-Agent"].FirstOrDefault() ?? "unknown";
    }

    // Dashboard Stats
    [HttpGet("dashboard/stats")]
    public async Task<IActionResult> GetDashboardStats()
    {
        var admin = await GetCurrentAdminUser();
        if (admin == null) return Unauthorized();

        try
        {
            // Get total users
            var usersResponse = await _supabase.From<AuthController.User>().Get();
            var totalUsers = usersResponse.Models.Count;

            // Get total recipes
            var recipesResponse = await _supabase.From<Models.Recipe>().Get();
            var totalRecipes = recipesResponse.Models.Count;

            // Get pending recipes
            var pendingRecipes = recipesResponse.Models.Count(r => r.Status == "pending");

            // Calculate average rating dynamically
            var ratingsResponse = await _supabase.From<Models.Rating>().Get();
            var avgRating = ratingsResponse.Models.Any() ? ratingsResponse.Models.Average(r => r.Score) : 0.0;

            return Ok(new
            {
                totalUsers,
                totalRecipes,
                pendingRecipes,
                avgRating
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetDashboardStats] Error: {ex.Message}\nStack Trace: {ex.StackTrace}");
            return StatusCode(500, new { error = "Failed to get dashboard stats", details = ex.Message });
        }
    }

    // Recent Activities
    [HttpGet("dashboard/activities")]
    public async Task<IActionResult> GetRecentActivities()
    {
        var admin = await GetCurrentAdminUser();
        if (admin == null) return Unauthorized();

        try
        {
            var activities = await _activityLog.GetRecentActivitiesAsync(10);
            return Ok(activities);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to get recent activities", details = ex.Message });
        }
    }

    // Popular Categories
    [HttpGet("dashboard/popular-categories")]
    public async Task<IActionResult> GetPopularCategories()
    {
        var admin = await GetCurrentAdminUser();
        if (admin == null) return Unauthorized();

        try
        {
            // Get all recipes and group by category
            var recipesResponse = await _supabase.From<Models.Recipe>()
                .Filter("status", Supabase.Postgrest.Constants.Operator.Equals, "approved")
                .Get();

            var categories = recipesResponse.Models
                .Where(r => !string.IsNullOrEmpty(r.Category))
                .GroupBy(r => r.Category)
                .Select(g => new { name = g.Key, count = g.Count() })
                .OrderByDescending(c => c.count)
                .Take(5)
                .ToList();

            // If no categories found, return default categories with zero counts
            if (!categories.Any())
            {
                categories = new[]
                {
                    new { name = "Dinner", count = 0 },
                    new { name = "Breakfast", count = 0 },
                    new { name = "Desserts", count = 0 },
                    new { name = "Lunch", count = 0 },
                    new { name = "Snacks", count = 0 }
                }.ToList();
            }

            return Ok(categories);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to get popular categories", details = ex.Message });
        }
    }

    // Pending Actions
    [HttpGet("dashboard/pending-actions")]
    public async Task<IActionResult> GetPendingActions()
    {
        var admin = await GetCurrentAdminUser();
        if (admin == null) return Unauthorized();

        try
        {
            // Get pending recipes count
            var recipesResponse = await _supabase.From<Models.Recipe>().Get();
            var pendingRecipes = recipesResponse.Models.Count(r => r.Status == "pending");

            // Get flagged users count (inactive users)
            var usersResponse = await _supabase.From<AuthController.User>().Get();
            var flaggedUsers = usersResponse.Models.Count(u => !u.Active);

            // Get unread feedback count (new feedback)
            var feedbackResponse = await _supabase.From<Models.Feedback>().Get();
            var unreadFeedback = feedbackResponse.Models.Count(f => f.Status == "new");

            return Ok(new
            {
                pendingRecipes,
                flaggedUsers,
                unreadFeedback
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to get pending actions", details = ex.Message });
        }
    }

    // Recipe Management
    [HttpGet("recipes")]
    public async Task<IActionResult> GetAllRecipes([FromQuery] string? status = null, [FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        var admin = await GetCurrentAdminUser();
        if (admin == null) return Unauthorized();

        try
        {
            // Get all recipes, then filter in memory to avoid Supabase query reassignment issues
            var allRecipesResponse = await _supabase.From<Models.Recipe>()
                .Order("submitted_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            var recipes = allRecipesResponse.Models.ToList();
            
            // Apply status filter in memory if needed
            if (!string.IsNullOrEmpty(status))
            {
                recipes = recipes.Where(r => r.Status == status).ToList();
            }
            
            // Apply pagination
            recipes = recipes
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToList();

            var recipeData = recipes.Select(r => new
            {
                id = r.Id,
                name = r.Name,
                description = r.Description,
                author = r.Author,
                status = r.Status,
                submittedAt = r.SubmittedAt,
                category = r.Category,
                difficulty = r.Difficulty
            }).ToList();

            return Ok(recipeData);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetAllRecipes] Error: {ex.Message}\n{ex}");
            return StatusCode(500, new { error = "Failed to get recipes", details = ex.Message });
        }
    }

    [HttpGet("recipes/{id}")]
    public async Task<IActionResult> GetRecipe(string id)
    {
        // Check for session ID directly
        if (!Request.Cookies.TryGetValue("session_id", out var sessionId))
        {
            Console.WriteLine("[GetRecipe] No session cookie found - unauthorized");
            return Unauthorized(new { message = "User not authenticated." });
        }

        var currentUser = await GetCurrentUser();
        if (currentUser == null || !currentUser.IsAdmin || !currentUser.IsAdminSession)
        {
            Console.WriteLine("[GetRecipe] No admin user found or session not active - unauthorized");
            return Unauthorized(new { message = "Unauthorized access." });
        }

        Console.WriteLine($"[GetRecipe] Called for ID: {id}");

        try
        {
            if (!Guid.TryParse(id, out Guid recipeGuid))
            {
                Console.WriteLine($"[GetRecipe] Invalid GUID format for ID: {id}");
                return BadRequest(new { message = "Invalid recipe ID format." });
            }

            var response = await _supabase.From<Recipe>()
                .Where(r => r.Id == recipeGuid)
                .Single();

            if (response == null)
            {
                Console.WriteLine($"[GetRecipe] Recipe not found for ID: {id}");
                return NotFound(new { message = "Recipe not found." });
            }

            var recipe = response;
            Console.WriteLine($"[GetRecipe] Found recipe: {recipe.Name}");
            return Ok(new RecipeDto(
                recipe.Id,
                recipe.Name ?? string.Empty,
                recipe.Description ?? string.Empty,
                recipe.Category ?? string.Empty,
                recipe.Difficulty ?? string.Empty,
                recipe.PrepTime,
                recipe.CookTime,
                recipe.Servings,
                recipe.Ingredients ?? new List<string>(),
                recipe.Instructions ?? new List<string>(),
                recipe.Tips,
                recipe.NutritionInfo,
                recipe.DietaryOptions,
                recipe.Author ?? string.Empty,
                recipe.SubmittedAt,
                recipe.Status ?? string.Empty,
                recipe.TotalTime,
                recipe.UserId,
                recipe.ImageUrl
            ));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetRecipe] Error fetching recipe: {ex.Message}\n{ex}");
            return StatusCode(500, new { message = "Failed to get recipe details", details = ex.Message });
        }
    }

    [HttpPut("recipes/{id}")]
    public async Task<IActionResult> UpdateRecipe(string id, [FromBody] UpdateRecipeRequest request)
    {
        Console.WriteLine($"[UpdateRecipe] Called for ID: {id}");

        var admin = await GetCurrentAdminUser();
        if (admin == null)
        {
            Console.WriteLine("[UpdateRecipe] No admin user found - unauthorized");
            return Unauthorized();
        }

        try
        {
            Console.WriteLine($"[UpdateRecipe] Request object received: {System.Text.Json.JsonSerializer.Serialize(request)}");

            // Ensure the ID is a properly formatted GUID string
            if (!Guid.TryParse(id, out var parsedGuid))
            {
                Console.WriteLine($"[UpdateRecipe] Invalid GUID format for ID: {id}");
                return BadRequest(new { error = "Invalid recipe ID format" });
            }
            // var formattedId = parsedGuid.ToString(); // Convert Guid back to string

            // Update properties using Set to explicitly update allowed fields
            var updateQuery = _supabase.From<Models.Recipe>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id);

#pragma warning disable CS8603 // Possible null reference return.

            updateQuery = updateQuery.Set(r => r.Name!, request.Name ?? "Untitled Recipe");
            updateQuery = updateQuery.Set(r => r.Description!, request.Description ?? "No description");
            updateQuery = updateQuery.Set(r => r.Category!, request.Category ?? "Other");
            updateQuery = updateQuery.Set(r => r.Difficulty!, request.Difficulty ?? "Easy");
            updateQuery = updateQuery.Set(r => r.PrepTime, request.PrepTime);
            updateQuery = updateQuery.Set(r => r.CookTime, request.CookTime);
            updateQuery = updateQuery.Set(r => r.Servings, request.Servings);
            updateQuery = updateQuery.Set(r => r.Ingredients!, request.Ingredients ?? new List<string>());
            updateQuery = updateQuery.Set(r => r.Instructions!, request.Instructions ?? new List<string>());
            updateQuery = updateQuery.Set(r => r.Tips, request.Tips);
            updateQuery = updateQuery.Set(r => r.NutritionInfo, request.NutritionInfo);
            updateQuery = updateQuery.Set(r => r.DietaryOptions!, request.DietaryOptions ?? new List<string>());
            updateQuery = updateQuery.Set(r => r.Author!, request.Author ?? "Unknown");
            updateQuery = updateQuery.Set(r => r.Status!, request.Status ?? "pending");
            updateQuery = updateQuery.Set(r => r.ImageUrl, request.ImageUrl);

#pragma warning restore CS8603 // Possible null reference return.
            
            var response = await updateQuery.Update();

            Console.WriteLine($"[UpdateRecipe] Update response received. Models count: {response.Models.Count}");

            if (response.Models.Count == 0)
            {
                Console.WriteLine($"[UpdateRecipe] No rows updated for ID: {id}");
                return StatusCode(500, new { error = "Failed to update recipe: No rows updated" });
            }

            Console.WriteLine($"[UpdateRecipe] Recipe {id} updated successfully.");
            await _activityLog.LogActivityAsync(admin.Id.ToString(), "recipe_updated", id, "recipe",
                new { recipeName = request.Name, updatedBy = admin.Email },
                GetClientIp(), GetUserAgent());

            return Ok(new { message = "Recipe updated successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UpdateRecipe] Error updating recipe: {ex.Message}\n{ex}");
            return StatusCode(500, new { error = "Failed to update recipe", details = ex.Message });
        }
    }

    [HttpPost("recipes/{id}/generate-image")]
    public async Task<IActionResult> GenerateRecipeImage(string id)
    {
        var admin = await GetCurrentAdminUser();
        if (admin == null) return Unauthorized();

        try
        {
            var recipe = (await _supabase.From<Recipe>().Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id).Get()).Models.FirstOrDefault();

            if (recipe == null) return NotFound(new { error = "Recipe not found" });

            var imageGenerationResult = await _recipeImageService.GenerateAndUploadRecipeImageAsync(
                recipe.Id.ToString(), recipe.Name, recipe.Description, recipe.Category, recipe.Difficulty, recipe.Ingredients
            );

            if (!imageGenerationResult.Success || string.IsNullOrEmpty(imageGenerationResult.ImageUrl))
            {
                throw new Exception(imageGenerationResult.ErrorMessage ?? "Failed to generate image");
            }

            // Call the new dedicated endpoint to update the image URL
            var updateResult = await UpdateRecipeImageUrl(recipe.Id.ToString(), new UpdateImageUrlRequest(imageGenerationResult.ImageUrl));

            if (updateResult is ObjectResult objectResult && objectResult.StatusCode != 200)
            {
                // Handle error from update endpoint if necessary
                throw new Exception("Failed to save generated image URL to database.");
            }

            await _activityLog.LogActivityAsync(admin.Id.ToString(), "recipe_image_generated", id, "recipe",
                new { recipeName = recipe.Name, imageUrl = imageGenerationResult.ImageUrl },
                GetClientIp(), GetUserAgent());

            return Ok(new { success = true, imageUrl = imageGenerationResult.ImageUrl });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GenerateRecipeImage] Error generating image for recipe {id}: {ex.Message}\nStack Trace: {ex.StackTrace}");
            return StatusCode(500, new { success = false, error = "Failed to generate image", details = ex.Message });
        }
    }

    [HttpPut("recipes/{id}/image-url")]
    public async Task<IActionResult> UpdateRecipeImageUrl(string id, [FromBody] UpdateImageUrlRequest request)
    {
        var admin = await GetCurrentAdminUser();
        if (admin == null) return Unauthorized();

        try
        {
            if (string.IsNullOrEmpty(request.ImageUrl))
            {
                return BadRequest(new { error = "Image URL cannot be empty" });
            }

            // Parse the ID string to Guid for proper filtering
            if (!Guid.TryParse(id, out var recipeGuid))
            {
                return BadRequest(new { error = "Invalid recipe ID format" });
            }

            Console.WriteLine($"[UpdateRecipeImageUrl] Attempting to update recipe {id} with ImageUrl: {request.ImageUrl}");

            await _supabase.From<Models.Recipe>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id)
                .Set(r => r.ImageUrl!, request.ImageUrl)
                .Update();

            await _activityLog.LogActivityAsync(admin.Id.ToString(), "recipe_image_updated", id, "recipe",
                new { newImageUrl = request.ImageUrl },
                GetClientIp(), GetUserAgent());

            return Ok(new { success = true, message = "Recipe image URL updated successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UpdateRecipeImageUrl] Error updating recipe image URL: {ex.Message}");
            return StatusCode(500, new { success = false, error = "Failed to update image URL", details = ex.Message });
        }
    }

    [HttpPut("recipes/{id}/status")]
    public async Task<IActionResult> UpdateRecipeStatus(string id, [FromBody] UpdateStatusRequest request)
    {
        var admin = await GetCurrentAdminUser();
        if (admin == null) return Unauthorized();

        try
        {
            await _supabase.From<Models.Recipe>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id)
                .Set(r => r.Status!, request.Status)
                .Update();

            // Log the activity
            var recipeResponse = await _supabase.From<Models.Recipe>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id)
                .Get();
            
            var recipe = recipeResponse.Models.FirstOrDefault();
            if (recipe != null)
            {
                await _activityLog.LogActivityAsync(admin.Id.ToString(), "recipe_status_updated", id, "recipe",
                    new { recipe_name = recipe.Name, old_status = recipe.Status, new_status = request.Status, admin_email = admin.Email },
                    GetClientIp(), GetUserAgent());
            }

            return Ok(new { message = "Recipe status updated successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to update recipe status", details = ex.Message });
        }
    }

    [HttpDelete("recipes/{id}")]
    public async Task<IActionResult> DeleteRecipe(string id)
    {
        var admin = await GetCurrentAdminUser();
        if (admin == null) return Unauthorized();

        try
        {
            // Get recipe info for logging
            var recipeResponse = await _supabase.From<Models.Recipe>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id)
                .Get();
            
            var recipe = recipeResponse.Models.FirstOrDefault();
            if (recipe == null) return NotFound();

            // First, delete any progress records for this recipe to handle foreign key constraints
            try {
                await _supabase.From<UserRecipeProgress>()
                    .Filter("recipe_id", Supabase.Postgrest.Constants.Operator.Equals, id)
                    .Delete();
                Console.WriteLine($"[DeleteRecipe] Deleted progress records for recipe {id}");
            } catch (Exception ex) {
                Console.WriteLine($"[DeleteRecipe] Error deleting progress records: {ex.Message}");
                return StatusCode(500, new { error = "Failed to delete recipe progress records", details = ex.Message });
            }
            
            // Second, delete any saved references to this recipe
            try {
                await _supabase.From<SavedRecipe>()
                    .Filter("recipe_id", Supabase.Postgrest.Constants.Operator.Equals, id)
                    .Delete();
                Console.WriteLine($"[DeleteRecipe] Deleted saved references for recipe {id}");
            } catch (Exception ex) {
                Console.WriteLine($"[DeleteRecipe] Error deleting saved references: {ex.Message}");
                return StatusCode(500, new { error = "Failed to delete saved recipe references", details = ex.Message });
            }
            
            // Finally, delete the recipe itself
            await _supabase.From<Models.Recipe>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id)
                .Delete();

            // Log the activity
            await _activityLog.LogActivityAsync(admin.Id.ToString(), "recipe_deleted_by_admin", id, "recipe",
                new { recipe_name = recipe.Name, admin_email = admin.Email },
                GetClientIp(), GetUserAgent());

            return Ok(new { message = "Recipe deleted successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DeleteRecipe] Error: {ex.Message}");
            return StatusCode(500, new { error = "Failed to delete recipe", details = ex.Message });
        }
    }

    // User Management
    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var admin = await GetCurrentAdminUser();
        if (admin == null) return Unauthorized();

        try
        {
            Console.WriteLine($"[CreateUser] Admin {admin.Email} creating user: {request.Email}");

            // Validate input
            if (string.IsNullOrWhiteSpace(request.FirstName) || 
                string.IsNullOrWhiteSpace(request.LastName) ||
                string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { error = "All required fields must be provided" });
            }

            if (request.Password.Length < 6)
            {
                return BadRequest(new { error = "Password must be at least 6 characters long" });
            }

            // Check if email already exists
            var existingEmailUser = await _supabase.From<AuthController.User>()
                .Filter("email", Supabase.Postgrest.Constants.Operator.Equals, request.Email)
                .Get();

            if (existingEmailUser.Models.Any())
            {
                return Conflict(new { error = "Email address is already registered" });
            }

            // Check if username already exists
            var existingUsernameUser = await _supabase.From<AuthController.User>()
                .Filter("username", Supabase.Postgrest.Constants.Operator.Equals, request.Username)
                .Get();

            if (existingUsernameUser.Models.Any())
            {
                return Conflict(new { error = "Username is already taken" });
            }

            // Create new user
            var newUser = new AuthController.User
            {
                Id = Guid.NewGuid(),
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Username = request.Username,
                PasswordHash = AuthController.HashPassword(request.Password),
                CookingLevel = "Beginner", // Default value
                IsAdmin = request.IsAdmin,
                Active = true, // New users are active by default
                CreatedAt = DateTime.UtcNow,
                Bio = request.Bio ?? "",
                FavoriteCuisine = null,
                Location = null,
                ProfileImageUrl = null,
                AdminSecurityCode = request.IsAdmin ? "SEC123" : null // Default admin code if admin
            };

            // Insert user into users table
            var userResponse = await _supabase.From<AuthController.User>().Insert(newUser);
            var createdUser = userResponse.Models.FirstOrDefault();

            if (createdUser == null)
            {
                return StatusCode(500, new { error = "Failed to create user" });
            }

            // Create corresponding user profile
            try
            {
                var userProfile = new AuthController.UserProfile
                {
                    Id = newUser.Id,
                    FirstName = newUser.FirstName,
                    LastName = newUser.LastName,
                    Username = newUser.Username,
                    FavoriteCuisine = null,
                    Location = null,
                    Bio = newUser.Bio,
                    AdminVerified = newUser.IsAdmin,
                    Active = true,
                    CookingLevel = newUser.CookingLevel,
                    CreatedAt = newUser.CreatedAt,
                    UpdatedAt = DateTime.UtcNow,
                    LastLogin = null
                };

                await _supabase.From<AuthController.UserProfile>().Insert(userProfile);
                Console.WriteLine($"[CreateUser] Created user profile for user {newUser.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CreateUser] Warning: Failed to create user profile: {ex.Message}");
                // Don't fail user creation if profile creation fails
            }

            // Log the activity
            await _activityLog.LogActivityAsync(admin.Id.ToString(), "user_created_by_admin", newUser.Id.ToString(), "user",
                new { 
                    created_email = newUser.Email, 
                    created_username = newUser.Username,
                    is_admin = newUser.IsAdmin,
                    admin_email = admin.Email 
                },
                GetClientIp(), GetUserAgent());

            Console.WriteLine($"[CreateUser] Successfully created user {newUser.Email} with ID {newUser.Id}");

            return Created($"/api/admin/users/{newUser.Id}", new
            {
                id = newUser.Id,
                email = newUser.Email,
                username = newUser.Username,
                firstName = newUser.FirstName,
                lastName = newUser.LastName,
                isAdmin = newUser.IsAdmin,
                active = newUser.Active,
                createdAt = newUser.CreatedAt
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CreateUser] Error: {ex.Message}\n{ex}");
            return StatusCode(500, new { error = "Failed to create user", details = ex.Message });
        }
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers([FromQuery] string? status = null, [FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        var admin = await GetCurrentAdminUser();
        if (admin == null) return Unauthorized();

        try
        {
            Console.WriteLine($"[GetAllUsers] Called with status='{status}', search='{search}', page={page}, limit={limit}");
            
            // Get all users, then filter in memory to avoid Supabase query reassignment issues
            var allUsersResponse = await _supabase.From<AuthController.User>()
                .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            var users = allUsersResponse.Models.ToList();
            
            Console.WriteLine($"[GetAllUsers] Found {users.Count} total users");
            
            // Apply status filter in memory if needed (convert status string to boolean)
            if (!string.IsNullOrEmpty(status))
            {
                bool isActiveFilter = status.ToLower() == "active";
                users = users.Where(u => u.Active == isActiveFilter).ToList();
                Console.WriteLine($"[GetAllUsers] After status filter '{status}': {users.Count} users");
            }
            
            // Apply search filter in memory if needed
            if (!string.IsNullOrEmpty(search))
            {
                var searchTerm = search.ToLower();
                users = users.Where(u => 
                    (!string.IsNullOrEmpty(u.FirstName) && u.FirstName.ToLower().Contains(searchTerm)) ||
                    (!string.IsNullOrEmpty(u.LastName) && u.LastName.ToLower().Contains(searchTerm)) ||
                    (!string.IsNullOrEmpty(u.Email) && u.Email.ToLower().Contains(searchTerm)) ||
                    (!string.IsNullOrEmpty(u.Username) && u.Username.ToLower().Contains(searchTerm)) ||
                    (u.Id.ToString().ToLower().Contains(searchTerm))
                ).ToList();
                Console.WriteLine($"[GetAllUsers] After search filter '{search}': {users.Count} users");
            }
            
            // Store total count before pagination for frontend
            var totalCount = users.Count;
            
            // Apply pagination
            users = users
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToList();

            Console.WriteLine($"[GetAllUsers] After pagination: {users.Count} users");

            var userData = users.Select(u => new
            {
                id = u.Id,
                email = u.Email,
                firstName = u.FirstName,
                lastName = u.LastName,
                username = u.Username,
                isAdmin = u.IsAdmin,
                active = u.Active,
                accountStatus = u.Active ? "Active" : "Suspended", // Convert boolean to string for frontend compatibility
                lastLogin = (DateTime?)null, // Get from user_profiles if needed
                createdAt = u.CreatedAt,
                totalCount = totalCount // Include total count for pagination
            }).ToList();

            return Ok(userData);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetAllUsers] Error: {ex.Message}\n{ex}");
            return StatusCode(500, new { error = "Failed to get users", details = ex.Message });
        }
    }

    [HttpGet("users/{id}/username")]
    public async Task<IActionResult> GetUsername(string id)
    {
        var admin = await GetCurrentAdminUser();
        if (admin == null) return Unauthorized();

        try
        {
            if (!Guid.TryParse(id, out var userId))
            {
                return BadRequest(new { error = "Invalid user ID format" });
            }

            var user = (await _supabase.From<AuthController.User>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, userId.ToString())
                .Get()).Models.FirstOrDefault();

            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            return Ok(new { username = user.Username });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetUsername] Error fetching username: {ex.Message}\n{ex}");
            return StatusCode(500, new { error = "Failed to get username", details = ex.Message });
        }
    }

    [HttpPut("users/{id}/status")]
    public async Task<IActionResult> UpdateUserStatus(string id, [FromBody] UpdateUserStatusRequest request)
    {
        var admin = await GetCurrentAdminUser();
        if (admin == null) return Unauthorized();

        try
        {
            // Convert string id to Guid for proper filtering
            if (!Guid.TryParse(id, out var userId))
            {
                return BadRequest(new { error = "Invalid user ID format" });
            }
            
            // Get user details for logging before potential deletion
            var userResponse = await _supabase.From<AuthController.User>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, userId.ToString())
                .Get();
            
            var user = userResponse.Models.FirstOrDefault();
            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            string successMessage;
            string activityType;

            switch (request.Action.ToLower())
            {
                case "suspend":
                    // Set active = false for both users and user_profiles
                    await _supabase.From<AuthController.User>()
                        .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, userId.ToString())
                        .Set(u => u.Active!, false)
                        .Update();
                        
                    await _supabase.From<AuthController.UserProfile>()
                        .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, userId.ToString())
                        .Set(p => p.Active!, false)
                        .Set(p => p.UpdatedAt!, DateTime.UtcNow)
                        .Update();

                    successMessage = "User has been suspended successfully";
                    activityType = "user_suspended";
                    break;

                case "activate":
                    // Set active = true for both users and user_profiles
                    await _supabase.From<AuthController.User>()
                        .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, userId.ToString())
                        .Set(u => u.Active!, true)
                        .Update();
                        
                    await _supabase.From<AuthController.UserProfile>()
                        .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, userId.ToString())
                        .Set(p => p.Active!, true)
                        .Set(p => p.UpdatedAt!, DateTime.UtcNow)
                        .Update();

                    successMessage = "User has been activated successfully";
                    activityType = "user_activated";
                    break;

                case "ban":
                    // Delete user from both tables completely
                    await _supabase.From<AuthController.UserProfile>()
                        .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, userId.ToString())
                        .Delete();
                        
                    await _supabase.From<AuthController.User>()
                        .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, userId.ToString())
                        .Delete();

                    successMessage = "User has been banned permanently";
                    activityType = "user_banned";
                    break;

                default:
                    return BadRequest(new { error = "Invalid action. Valid actions are: suspend, activate, ban" });
            }

            // Log the activity
            object activityData;
            if (!string.IsNullOrEmpty(request.Reason))
            {
                activityData = new { 
                    target_email = user.Email, 
                    action = request.Action, 
                    admin_email = admin.Email,
                    reason = request.Reason 
                };
            }
            else
            {
                activityData = new { 
                    target_email = user.Email, 
                    action = request.Action, 
                    admin_email = admin.Email 
                };
            }
            
            await _activityLog.LogActivityAsync(admin.Id.ToString(), activityType, id, "user",
                activityData, GetClientIp(), GetUserAgent());

            return Ok(new { message = successMessage });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UpdateUserStatus] Error: {ex.Message}\n{ex}");
            return StatusCode(500, new { error = "Failed to update user status", details = ex.Message });
        }
    }

    // Feedback Management
    [HttpGet("feedback")]
    public async Task<IActionResult> GetAllFeedback([FromQuery] string? status = null, [FromQuery] string? type = null, [FromQuery] string? rating = null, [FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        Console.WriteLine($"[GetAllFeedback] Called with status='{status}', type='{type}', rating='{rating}', page={page}, limit={limit}");
        
        var admin = await GetCurrentAdminUser();
        if (admin == null) 
        {
            Console.WriteLine("[GetAllFeedback] No admin user found - unauthorized");
            return Unauthorized();
        }

        Console.WriteLine($"[GetAllFeedback] Admin user: {admin.Email}");

        try
        {
            Console.WriteLine("[GetAllFeedback] Querying feedback table...");
            
            // Get all feedback, then filter in memory to avoid Supabase query reassignment issues
            var allFeedbackResponse = await _supabase.From<Feedback>()
                .Order("submitted_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            Console.WriteLine($"[GetAllFeedback] Found {allFeedbackResponse.Models.Count} total feedback records");

            var feedback = allFeedbackResponse.Models.ToList();
            
            // Debug: Show unique type values in the database
            var uniqueTypes = feedback.Select(f => f.Type).Distinct().ToList();
            Console.WriteLine($"[GetAllFeedback] Unique types in database: [{string.Join(", ", uniqueTypes.Select(t => $"'{t}'"))}]");
            
            // Apply status filter in memory if needed
            if (!string.IsNullOrEmpty(status))
            {
                feedback = feedback.Where(f => f.Status == status).ToList();
                Console.WriteLine($"[GetAllFeedback] After status filter '{status}': {feedback.Count} records");
            }
            
            // Apply type filter in memory if needed
            if (!string.IsNullOrEmpty(type))
            {
                Console.WriteLine($"[GetAllFeedback] Filtering by type: '{type}' (case insensitive)");
                var beforeCount = feedback.Count;
                feedback = feedback.Where(f => string.Equals(f.Type, type, StringComparison.OrdinalIgnoreCase)).ToList();
                Console.WriteLine($"[GetAllFeedback] After type filter '{type}': {feedback.Count} records (was {beforeCount})");
                
                // Debug: Show what types were excluded
                if (feedback.Count == 0 && beforeCount > 0)
                {
                    var actualTypes = allFeedbackResponse.Models.Select(f => f.Type).Distinct().ToList();
                    Console.WriteLine($"[GetAllFeedback] No matches found. Available types: [{string.Join(", ", actualTypes.Select(t => $"'{t}'"))}]");
                }
            }
            
            // Apply rating filter in memory if needed
            if (!string.IsNullOrEmpty(rating))
            {
                Console.WriteLine($"[GetAllFeedback] Filtering by rating: '{rating}'");
                var beforeCount = feedback.Count;
                
                // Debug: Show unique rating values in the database
                var uniqueRatings = feedback.Select(f => f.Rating).Distinct().ToList();
                Console.WriteLine($"[GetAllFeedback] Unique ratings in database: [{string.Join(", ", uniqueRatings.Select(r => r?.ToString() ?? "null"))}]");
                
                if (int.TryParse(rating, out var ratingValue))
                {
                    if (ratingValue == 0)
                    {
                        // Filter for no rating (null or 0)
                        feedback = feedback.Where(f => f.Rating == null || f.Rating == 0).ToList();
                        Console.WriteLine($"[GetAllFeedback] Filtering for no rating (null or 0)");
                    }
                    else
                    {
                        // Filter for specific rating value
                        feedback = feedback.Where(f => f.Rating == ratingValue).ToList();
                        Console.WriteLine($"[GetAllFeedback] Filtering for rating = {ratingValue}");
                    }
                    Console.WriteLine($"[GetAllFeedback] After rating filter '{rating}': {feedback.Count} records (was {beforeCount})");
                    
                    // Debug: Show what ratings were excluded
                    if (feedback.Count == 0 && beforeCount > 0)
                    {
                        var actualRatings = allFeedbackResponse.Models.Select(f => f.Rating).Distinct().ToList();
                        Console.WriteLine($"[GetAllFeedback] No matches found. Available ratings: [{string.Join(", ", actualRatings.Select(r => r?.ToString() ?? "null"))}]");
                    }
                }
                else
                {
                    Console.WriteLine($"[GetAllFeedback] Failed to parse rating '{rating}' as integer");
                }
            }
            
            // Apply pagination
            feedback = feedback
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToList();

            Console.WriteLine($"[GetAllFeedback] After pagination: {feedback.Count} records");

            var feedbackData = feedback.Select(f => new
            {
                id = f.Id,
                name = f.Name,
                email = f.Email,
                type = f.Type,
                subject = f.Subject,
                message = f.Message,
                rating = f.Rating,
                status = f.Status,
                submittedAt = f.SubmittedAt,
                // reviewedAt = f.ReviewedAt // Commented out due to schema cache issue
            }).ToList();

            Console.WriteLine($"[GetAllFeedback] Returning {feedbackData.Count} feedback records");
            return Ok(feedbackData);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetAllFeedback] Error: {ex.Message}\n{ex}");
            return StatusCode(500, new { error = "Failed to get feedback", details = ex.Message });
        }
    }

    [HttpPut("feedback/{id}")]
    public async Task<IActionResult> UpdateFeedback(string id, [FromBody] UpdateFeedbackRequest request)
    {
        var admin = await GetCurrentAdminUser();
        if (admin == null) return Unauthorized();

        try
        {
            var updateQuery = _supabase.From<Feedback>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id)
                .Set(f => f.Status!, request.Status)
                .Set(f => f.ReviewedByAdminId!, admin.Id.ToString())
                .Set(f => f.ReviewedAt!, DateTime.UtcNow);

            // Only set admin response if provided
            if (!string.IsNullOrWhiteSpace(request.AdminResponse))
            {
                updateQuery = updateQuery.Set(f => f.AdminResponse!, request.AdminResponse);
            }

            await updateQuery.Update();

            // Log the activity
            await _activityLog.LogActivityAsync(admin.Id.ToString(), "feedback_reviewed", id, "feedback",
                new { status = request.Status, admin_response = request.AdminResponse, admin_email = admin.Email },
                GetClientIp(), GetUserAgent());

            return Ok(new { message = "Feedback updated successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to update feedback", details = ex.Message });
        }
    }

    // Reports Management
    [HttpGet("reports")]
    public async Task<IActionResult> GetAllReports([FromQuery] string? status = null, [FromQuery] string? type = null, [FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        Console.WriteLine($"[GetAllReports] Called with status='{status}', type='{type}', page={page}, limit={limit}");
        
        var admin = await GetCurrentAdminUser();
        if (admin == null) 
        {
            Console.WriteLine("[GetAllReports] No admin user found - unauthorized");
            return Unauthorized();
        }

        Console.WriteLine($"[GetAllReports] Admin user: {admin.Email}");

        try
        {
            Console.WriteLine("[GetAllReports] Querying reports table...");
            
            // Get all reports, then filter in memory to avoid Supabase query reassignment issues
            var allReportsResponse = await _supabase.From<Report>()
                .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            Console.WriteLine($"[GetAllReports] Found {allReportsResponse.Models.Count} total report records");

            var reports = allReportsResponse.Models.ToList();
            
            // Apply status filter in memory if needed
            if (!string.IsNullOrEmpty(status))
            {
                reports = reports.Where(r => r.Status == status).ToList();
                Console.WriteLine($"[GetAllReports] After status filter '{status}': {reports.Count} records");
            }
            
            // Apply type filter in memory if needed
            if (!string.IsNullOrEmpty(type))
            {
                reports = reports.Where(r => r.ReportType == type).ToList();
                Console.WriteLine($"[GetAllReports] After type filter '{type}': {reports.Count} records");
            }
            
            // Apply pagination
            reports = reports
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToList();

            Console.WriteLine($"[GetAllReports] After pagination: {reports.Count} records");

            var reportsData = reports.Select(r => new
            {
                id = r.Id,
                reporterUserId = r.ReporterUserId,
                reportedUserId = r.ReportedUserId,
                reportedRecipeId = r.ReportedRecipeId,
                reportType = r.ReportType,
                description = r.Description,
                status = r.Status,
                createdAt = r.CreatedAt,
                // reviewedAt = r.ReviewedAt, // Commented out due to schema cache issue
                adminNotes = r.AdminNotes
            }).ToList();

            Console.WriteLine($"[GetAllReports] Returning {reportsData.Count} report records");
            return Ok(reportsData);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetAllReports] Error: {ex.Message}\n{ex}");
            return StatusCode(500, new { error = "Failed to get reports", details = ex.Message });
        }
    }

    [HttpGet("reports/{id}")]
    public async Task<IActionResult> GetReport(string id)
    {
        Console.WriteLine($"[GetReport] Called with id='{id}' (type: {id.GetType()})");
        
        var admin = await GetCurrentAdminUser();
        if (admin == null) 
        {
            Console.WriteLine("[GetReport] No admin user found - unauthorized");
            return Unauthorized();
        }

        Console.WriteLine($"[GetReport] Admin user: {admin.Email}");

        try
        {
            Console.WriteLine("[GetReport] Querying single report...");
            
            // Try to parse as long (since Report.Id is long)
            if (!long.TryParse(id, out _))
            {
                Console.WriteLine($"[GetReport] Invalid id format: '{id}' – not a number");
                return BadRequest(new { error = "Invalid report ID format" });
            }

            var reportResponse = await _supabase.From<Report>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id)
                .Get();

            var report = reportResponse.Models.FirstOrDefault();
            
            if (report == null)
            {
                Console.WriteLine($"[GetReport] Report not found with id: {id} (long: {id})");
                return NotFound(new { error = "Report not found", requestedId = id });
            }

            Console.WriteLine($"[GetReport] Found report: {report.Id}");

            var reportData = new
            {
                id = report.Id,
                reporterUserId = report.ReporterUserId,
                reportedUserId = report.ReportedUserId,
                reportedRecipeId = report.ReportedRecipeId,
                reportType = report.ReportType,
                description = report.Description,
                status = report.Status,
                createdAt = report.CreatedAt,
                // reviewedAt = report.ReviewedAt, // Commented out due to schema cache issue
                adminNotes = report.AdminNotes,
                reviewedByAdminId = report.ReviewedByAdminId
            };

            Console.WriteLine($"[GetReport] Returning report data for id: {id}");
            return Ok(reportData);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetReport] Error: {ex.Message}\n{ex}");
            return StatusCode(500, new { error = "Failed to get report", details = ex.Message });
        }
    }

    [HttpPut("reports/{id}")]
    public async Task<IActionResult> UpdateReport(string id, [FromBody] UpdateReportRequest request)
    {
        var admin = await GetCurrentAdminUser();
        if (admin == null) return Unauthorized();

        try
        {
            // Ensure we filter by numeric id
            if (!long.TryParse(id, out var longId))
            {
                return BadRequest(new { error = "Invalid report ID format" });
            }

            int intId;
            try
            {
                intId = checked((int)longId);
            }
            catch (OverflowException)
            {
                // Fallback to string filter if id exceeds int range
                intId = -1;
            }

            var table = _supabase.From<Report>();

            // Build the filter using the appropriate criterion type to satisfy Supabase generics
            var updateQuery = (intId != -1) // If it fits in int and is not the fallback value
                ? table.Filter("id", Supabase.Postgrest.Constants.Operator.Equals, intId)
                : table.Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id); 

            Console.WriteLine($"[UpdateReport] Received Status: '{request.Status}' (Type: {request.Status.GetType().Name})");
            Console.WriteLine($"[UpdateReport] Received AdminNotes: '{request.AdminNotes}' (Type: {request.AdminNotes?.GetType().Name ?? "null"})");

            // Apply the updates
            await updateQuery
                .Set(r => r.Status!, request.Status)
                .Set(r => r.AdminNotes!, request.AdminNotes ?? string.Empty)
                // .Set(r => r.ReviewedAt!, DateTime.UtcNow) // Commented out due to schema cache issue
                .Update();

            // Log the activity
            await _activityLog.LogActivityAsync(admin.Id.ToString(), "report_reviewed", id, "report",
                new { status = request.Status, admin_email = admin.Email },
                GetClientIp(), GetUserAgent());

            return Ok(new { message = "Report updated successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to update report", details = ex.Message });
        }
    }

    [HttpPost("reports")]
    public async Task<IActionResult> CreateReport([FromBody] CreateReportRequest request)
    {
        // This endpoint can be used by regular users to create reports
        try
        {
            var report = new Report
            {
                ReporterUserId = string.IsNullOrEmpty(request.ReporterUserId) ? null : Guid.Parse(request.ReporterUserId),
                ReportedUserId = string.IsNullOrEmpty(request.ReportedUserId) ? null : Guid.Parse(request.ReportedUserId),
                ReportedRecipeId = string.IsNullOrEmpty(request.ReportedRecipeId) ? null : Guid.Parse(request.ReportedRecipeId),
                ReportType = request.ReportType,
                Description = request.Description,
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            };

            var response = await _supabase.From<Report>().Insert(report);
            var created = response.Models.FirstOrDefault();

            if (created != null)
            {
                // Log the activity
                await _activityLog.LogReportCreatedAsync(request.ReporterUserId, created.Id.ToString(), 
                    request.ReportType, GetClientIp(), GetUserAgent());
            }

            return Ok(new { message = "Report submitted successfully", id = created?.Id });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to create report", details = ex.Message });
        }
    }

    // Test endpoint to create sample reports for testing
    [HttpPost("reports/test-data")]
    public async Task<IActionResult> CreateTestReports()
    {
        var admin = await GetCurrentAdminUser();
        if (admin == null) return Unauthorized();

        try
        {
            var testReports = new[]
            {
                new Report
                {
                    ReportType = "inappropriate_content",
                    Description = "This recipe contains inappropriate content that violates community guidelines.",
                    Status = "pending",
                    CreatedAt = DateTime.UtcNow.AddHours(-2),
                    ReportedRecipeId = Guid.NewGuid()
                },
                new Report
                {
                    ReportType = "spam",
                    Description = "User is posting spam recipes repeatedly.",
                    Status = "pending",
                    CreatedAt = DateTime.UtcNow.AddHours(-1),
                    ReportedUserId = Guid.NewGuid(),
                    ReporterUserId = admin.Id
                },
                new Report
                {
                    ReportType = "harassment",
                    Description = "User is harassing other community members in recipe comments.",
                    Status = "reviewed",
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    // ReviewedAt = DateTime.UtcNow.AddHours(-3), // Commented out due to schema cache issue
                    AdminNotes = "Investigated - evidence found. Taking action.",
                    ReviewedByAdminId = admin.Id,
                    ReportedUserId = Guid.NewGuid()
                }
            };

            foreach (var report in testReports)
            {
                await _supabase.From<Report>().Insert(report);
            }

            return Ok(new { message = $"Created {testReports.Length} test reports successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to create test reports", details = ex.Message });
        }
    }
}

// Request DTOs
public record UpdateStatusRequest([Required] string Status);
public record UpdateUserStatusRequest([Required] string Action, string? Reason);
public record UpdateUserActiveRequest([Required] bool Active, string? Reason);
public record CreateUserRequest(
    [Required] string FirstName,
    [Required] string LastName,
    [Required] string Username,
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password,
    [Required] bool IsAdmin,
    string? Bio,
    bool SendWelcomeEmail);
public record UpdateFeedbackRequest([Required] string Status, string? AdminResponse);
public record UpdateReportRequest([Required] string Status, string? AdminNotes);
public record CreateReportRequest(
    string? ReporterUserId,
    string? ReportedUserId,
    string? ReportedRecipeId,
    [Required] string ReportType,
    [Required] string Description);

public record UpdateRecipeRequest(
    [Required] string Name,
    string Description,
    [Required] string Category,
    [Required] string Difficulty,
    [Required] int PrepTime,
    [Required] int CookTime,
    [Required] int Servings,
    [Required] List<string> Ingredients,
    [Required] List<string> Instructions,
    string? Tips,
    string? NutritionInfo,
    List<string>? DietaryOptions,
    [Required] string Author,
    [Required] string Status,
    string? ImageUrl
); 

public record RecipeDto(
    Guid Id,
    string Name,
    string Description,
    string Category,
    string Difficulty,
    int PrepTime,
    int CookTime,
    int Servings,
    List<string> Ingredients,
    List<string> Instructions,
    string? Tips,
    string? NutritionInfo,
    List<string>? DietaryOptions,
    string Author,
    DateTime SubmittedAt,
    string Status,
    int? TotalTime,
    string? UserId,
    string? ImageUrl
); 

public record UpdateImageUrlRequest([Required] string ImageUrl);