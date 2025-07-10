using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc;
using Supabase;
using Supabase.Postgrest;
using Supabase.Gotrue;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using EasyBites.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Json.Serialization; 
using System.Text.Json;

namespace EasyBites.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Apply Authorize attribute at the class level
public class RecipesController : ControllerBase
{
    private readonly Supabase.Client _supabase;
    private readonly RecipeImageService _imageService;
    private readonly GeminiService _geminiService; 
    private readonly ILogger<RecipesController> _logger; 

    public RecipesController(Supabase.Client supabase, RecipeImageService imageService, GeminiService geminiService, ILogger<RecipesController> logger)
    {
        _supabase = supabase;
        _imageService = imageService;
        _geminiService = geminiService; 
        _logger = logger; 
    }

    // Simplified DTO for current user claims
    private record CurrentUser(
        Guid Id,
        string Email,
        string Username,
        bool IsAdmin
    );

    // Helper method to get the current authenticated user from HttpContext.User.Claims
    private CurrentUser? GetCurrentUser()
    {
        if (!HttpContext.User.Identity?.IsAuthenticated == true)
        {
            Console.WriteLine("[RecipesController::GetCurrentUser] User is not authenticated.");
            return null;
        }

        var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        var usernameClaim = HttpContext.User.FindFirst(ClaimTypes.Name);
        var emailClaim = HttpContext.User.FindFirst(ClaimTypes.Email);
        var roleClaim = HttpContext.User.FindFirst(ClaimTypes.Role);

        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId) || 
            usernameClaim == null || emailClaim == null || roleClaim == null)
        {
            Console.WriteLine("[RecipesController::GetCurrentUser] Missing or invalid user claims.");
            return null;
        }

        return new CurrentUser(
            Id: userId,
            Email: emailClaim.Value,
            Username: usernameClaim.Value,
            IsAdmin: roleClaim.Value == "Admin"
        );
    }

    // DTO for recipe image generation request
    public record GenerateRecipeImageRequest(
        [Required] string RecipeName,
        [Required] string RecipeDescription
    );

    [AllowAnonymous] // Allow unauthenticated access for temporary image generation
    [HttpPost("temp-image-gen")]
    public async Task<IActionResult> GenerateTemporaryImage([FromBody] GenerateRecipeImageRequest request)
    {
        _logger.LogInformation("[RecipesController::GenerateTemporaryImage] Attempting to generate image for recipe: {RecipeName}", request.RecipeName);
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("[RecipesController::GenerateTemporaryImage] Invalid ModelState: {ModelStateErrors}",
                string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return BadRequest(ModelState);
        }

        if (!_geminiService.IsConfiguredAndAvailable())
        {
            _logger.LogError("[RecipesController::GenerateTemporaryImage] AI image generation service is not available or configured.");
            return StatusCode(503, new { error = "AI image generation service is not available or configured." });
        }

        try
        {
            var result = await _imageService.GenerateTemporaryImageUrlAsync(request.RecipeName, request.RecipeDescription);

            if (result.Success)
            {
                _logger.LogInformation("[RecipesController::GenerateTemporaryImage] Image generated successfully. URL: {ImageUrl}", result.ImageUrl);
                return Ok(new { success = true, imageUrl = result.ImageUrl });
            }
            else
            {
                _logger.LogError("[RecipesController::GenerateTemporaryImage] Failed to generate image: {ErrorMessage}", result.ErrorMessage);
                return StatusCode(500, new { success = false, error = result.ErrorMessage });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RecipesController::GenerateTemporaryImage] Error generating temporary image: {ErrorMessage}", ex.Message);
            return StatusCode(500, new { success = false, error = "An unexpected error occurred during image generation.", details = ex.Message });
        }
    }

    [HttpGet("debug/test")]
    public IActionResult Test()
    {
        var testDto = new RecipeDto
        {
            Id = Guid.NewGuid(),
            Name = "Test Recipe",
            Description = "Test Description",
            Category = "Test",
            Difficulty = "Easy",
            PrepTime = 10,
            CookTime = 20,
            Servings = 4,
            Ingredients = new List<string> { "Ingredient 1", "Ingredient 2" },
            Instructions = new List<string> { "Step 1", "Step 2" },
            Author = "Test Author",
            SubmittedAt = DateTime.UtcNow,
            Status = "pending",
            TotalTime = 30
        };
        
        return Ok(new List<RecipeDto> { testDto });
    }

    [AllowAnonymous] // Allow unauthenticated access
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] string? category,
                            [FromQuery] string? difficulty, [FromQuery] string? time)
    {
        try
        {
            // Only fetch publicly-visible recipes (approved & not draft)
            var response = await _supabase.From<Models.Recipe>()
                .Filter("status", Supabase.Postgrest.Constants.Operator.Equals, "approved")
                .Filter("is_draft", Supabase.Postgrest.Constants.Operator.Equals, "false")
                .Get();
            var recipes = response.Models.ToList();

            // Apply filters in memory (for simplicity, avoiding complex Supabase query chaining)
            if (!string.IsNullOrWhiteSpace(search))
            {
                recipes = recipes.Where(r => r.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                recipes = recipes.Where(r => string.Equals(r.Category, category, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(difficulty))
            {
                recipes = recipes.Where(r => string.Equals(r.Difficulty, difficulty, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(time) && int.TryParse(time.TrimEnd('+'), out var minutes))
            {
                if (time.EndsWith('+'))
                {
                    recipes = recipes.Where(r => r.TotalTime > minutes).ToList();
                }
                else
                {
                    recipes = recipes.Where(r => r.TotalTime <= minutes).ToList();
                }
            }

            var recipeDtos = recipes.Select(MapToDto).ToList();
            return Ok(recipeDtos);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetAll] Error: {ex.Message}");
            return StatusCode(500, new { error = "Failed to fetch recipes", details = ex.Message });
        }
    }
    
    [AllowAnonymous] // Allow unauthenticated access
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return await GetAll(null, null, null, null);
        
        try
        {
            // Search only among approved, non-draft recipes
            var response = await _supabase.From<Models.Recipe>()
                .Filter("status", Supabase.Postgrest.Constants.Operator.Equals, "approved")
                .Filter("is_draft", Supabase.Postgrest.Constants.Operator.Equals, "false")
                .Get();
            var recipes = response.Models.Where(r => r.Name.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
            
            var recipeDtos = recipes.Select(MapToDto).ToList();
            return Ok(recipeDtos);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Search] Error: {ex.Message}");
            return StatusCode(500, new { error = "Failed to search recipes", details = ex.Message });
        }
    }
    
    [AllowAnonymous] // Allow unauthenticated access
    [HttpGet("filter")]
    public async Task<IActionResult> Filter([FromQuery] string? difficulty, [FromQuery] string? time)
    {
        try
        {
            // Start with publicly-visible recipes only
            var response = await _supabase.From<Models.Recipe>()
                .Filter("status", Supabase.Postgrest.Constants.Operator.Equals, "approved")
                .Filter("is_draft", Supabase.Postgrest.Constants.Operator.Equals, "false")
                .Get();
            var recipes = response.Models.ToList();

            // Apply filters in memory
            if (!string.IsNullOrWhiteSpace(difficulty))
            {
                recipes = recipes.Where(r => string.Equals(r.Difficulty, difficulty, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(time) && int.TryParse(time, out var minutes))
            {
                recipes = recipes.Where(r => r.TotalTime <= minutes).ToList();
            }

            var recipeDtos = recipes.Select(MapToDto).ToList();
            return Ok(recipeDtos);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Filter] Error: {ex.Message}");
            return StatusCode(500, new { error = "Failed to filter recipes", details = ex.Message });
        }
    }

    [AllowAnonymous] // Allow unauthenticated access
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var singleResp = await _supabase.From<Models.Recipe>()
                                    .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id)
                                    .Get();
        var result = singleResp.Models.FirstOrDefault();

        if (result == null)
            return NotFound();

        // Enforce visibility: recipe must be approved & not a draft unless requester is owner or admin
        if (result.Status != "approved" || result.IsDraft)
        {
            var user = GetCurrentUser();
            var isOwner = user != null && user.Id.ToString() == result.UserId;
            var isAdmin = user != null && user.IsAdmin;

            if (!isOwner && !isAdmin)
            {
                return NotFound(); // Hide existence from other users
            }
        }

        var recipeDto = MapToDto(result);
        return Ok(recipeDto);
    }
    
    [HttpGet("saved")] // Already protected by class-level [Authorize]
    public async Task<IActionResult> GetSavedRecipes()
    {
        // Get the current user
        var user = GetCurrentUser(); // No await needed, claims are local
        if (user == null)
            return Unauthorized(new { error = "User not authenticated or claims missing" });
            
        try
        {
            // Get all saved recipe IDs for the user
            var savedRecipesResponse = await _supabase.From<SavedRecipe>()
                .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, user.Id.ToString())
                .Get();
                
            var savedRecipeIds = savedRecipesResponse.Models.Select(sr => sr.RecipeId).ToList();
            
            // Fetch the actual recipe details
            if (!savedRecipeIds.Any())
            {
                return Ok(new List<RecipeDto>());
            }
            
            var recipesResponse = await _supabase.From<Models.Recipe>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.In, savedRecipeIds)
                .Get();
                
            var recipeDtos = recipesResponse.Models.Select(MapToDto).ToList();
            return Ok(recipeDtos);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetSavedRecipes] Error: {ex.Message}");
            return StatusCode(500, new { error = "Failed to load saved recipes", details = ex.Message });
        }
    }
    
    // New endpoint for fetching user recipe progress
    private async Task<UserRecipeProgress?> FetchRecipeProgress(Guid userId, Guid recipeId, Guid? progressId = null)
    {
        var query = _supabase.From<UserRecipeProgress>()
            .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, userId.ToString())
            .Filter("recipe_id", Supabase.Postgrest.Constants.Operator.Equals, recipeId.ToString());

        if (progressId.HasValue)
        {
            query = query.Filter("id", Supabase.Postgrest.Constants.Operator.Equals, progressId.Value.ToString());
        }

        var response = await query.Limit(1).Get();
        return response.Models.FirstOrDefault();
    }

    [HttpGet("progress/{recipeId}")]
    public async Task<IActionResult> GetRecipeProgress(Guid recipeId)
    {
        var user = GetCurrentUser();
        if (user == null)
            return Unauthorized(new { error = "User not authenticated or claims missing" });

        try
        {
            var progress = await FetchRecipeProgress(user.Id, recipeId);

            if (progress == null)
                return NotFound(new { message = "No progress found for this recipe and user." });

            return Ok(MapToRecipeProgressDto(progress));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetRecipeProgress] Error: {ex.Message}");
            return StatusCode(500, new { error = "Failed to fetch recipe progress", details = ex.Message });
        }
    }

    // New endpoint for creating user recipe progress
    [HttpPost("progress")]
    public async Task<IActionResult> CreateRecipeProgress([FromBody] RecipeProgressRequest request)
    {
        var user = GetCurrentUser();
        if (user == null)
        {
            return Unauthorized(new { message = "User not authenticated." });
        }

        // Check if progress already exists for this user and recipe to avoid duplicates
        var existingProgress = await FetchRecipeProgress(user.Id, request.RecipeId);

        if (existingProgress != null)
        {
            // If progress exists, update it instead of creating a new one
            return await UpdateRecipeProgressInternal(existingProgress.Id, request, user.Id);
        }

        var newProgress = new UserRecipeProgress
        {
            UserId = user.Id,
            RecipeId = request.RecipeId,
            CurrentInstructionStep = request.CurrentInstructionStep,
            CheckedIngredients = Newtonsoft.Json.JsonConvert.SerializeObject(request.CheckedIngredients),
            UpdatedAt = DateTime.UtcNow
        };

        var response = await _supabase.From<UserRecipeProgress>().Insert(newProgress);

        if (response == null || !response.Models.Any())
        {
            Console.WriteLine($"[CreateRecipeProgress] Supabase insert returned no models for new progress for user {user.Id} and recipe {request.RecipeId}.");
            return StatusCode(500, new { message = "Failed to create recipe progress." });
        }

        _logger.LogInformation("[CreateRecipeProgress] New recipe progress created successfully for user {UserId} and recipe {RecipeId}.", user.Id, request.RecipeId);
        return Ok(MapToRecipeProgressDto(response.Models.First()));
    }

    // New endpoint for updating user recipe progress
    [HttpPut("progress/{id}")]
    public async Task<IActionResult> UpdateRecipeProgress(Guid id, [FromBody] RecipeProgressRequest request)
    {
        var user = GetCurrentUser();
        if (user == null)
        {
            return Unauthorized(new { message = "User not authenticated." });
        }

        // Ensure the user is updating their own progress
        var progressToVerify = await FetchRecipeProgress(user.Id, request.RecipeId, id);

        if (progressToVerify == null || progressToVerify.UserId != user.Id)
        {
            return Forbid("You do not have permission to update this recipe progress or it does not exist.");
        }

        return await UpdateRecipeProgressInternal(id, request, user.Id);
    }

    private async Task<IActionResult> UpdateRecipeProgressInternal(Guid progressId, RecipeProgressRequest request, Guid userId)
    {
        var progressToUpdate = await FetchRecipeProgress(userId, request.RecipeId, progressId);

        if (progressToUpdate == null)
            return NotFound(new { message = "Recipe progress not found for update." });

        progressToUpdate.CurrentInstructionStep = request.CurrentInstructionStep;
        progressToUpdate.CheckedIngredients = Newtonsoft.Json.JsonConvert.SerializeObject(request.CheckedIngredients);
        progressToUpdate.UpdatedAt = DateTime.UtcNow;

        // Explicitly update only the necessary columns using Filter and Set to avoid PGRST100 error
        var response = await _supabase.From<UserRecipeProgress>()
            .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, progressId.ToString())
            .Set(x => x.CurrentInstructionStep, progressToUpdate.CurrentInstructionStep)
            .Set(x => x.CheckedIngredients, progressToUpdate.CheckedIngredients) 
            .Set(x => x.UpdatedAt, progressToUpdate.UpdatedAt)
            .Update();

        if (response == null || !response.Models.Any())
        {
            Console.WriteLine($"[UpdateRecipeProgressInternal] Supabase update returned no models for recipe progress ID {progressId}.");
            _logger.LogError("[UpdateRecipeProgressInternal] Supabase update failed or returned no models for progress ID {ProgressId}. Request: {Request}.", progressId, JsonSerializer.Serialize(request));
            return StatusCode(500, new { message = "Failed to update recipe progress." });
        }

        _logger.LogInformation("[UpdateRecipeProgressInternal] Recipe progress updated successfully for progress ID {ProgressId}.", progressId);
        return Ok(MapToRecipeProgressDto(response.Models.First()));
    }

    private static RecipeProgressDto MapToRecipeProgressDto(UserRecipeProgress progress)
    {
        List<int> parsed = new List<int>();
        if (!string.IsNullOrWhiteSpace(progress.CheckedIngredients))
        {
            try
            {
                if (progress.CheckedIngredients.TrimStart().StartsWith("{"))
                {
                    var nums = progress.CheckedIngredients.Trim('{', '}').Split(',', StringSplitOptions.RemoveEmptyEntries);
                    parsed = nums.Select(n => int.TryParse(n, out var v) ? v : (int?)null).Where(v => v.HasValue).Select(v => v!.Value).ToList();
                }
                else
                {
                    parsed = Newtonsoft.Json.JsonConvert.DeserializeObject<List<int>>(progress.CheckedIngredients) ?? new List<int>();
                }
            }
            catch { /* ignore parse errors */ }
        }

        return new RecipeProgressDto
        {
            Id = progress.Id,
            UserId = progress.UserId,
            RecipeId = progress.RecipeId,
            CurrentInstructionStep = progress.CurrentInstructionStep,
            CheckedIngredients = parsed,
            CompletedAt = progress.CompletedAt,
            UpdatedAt = progress.UpdatedAt,
            CreatedAt = progress.CreatedAt ?? DateTime.MinValue
        };
    }
    
    [AllowAnonymous] // Allow unauthenticated access
    [HttpGet("saved/{recipeId}")] // Already protected by class-level [Authorize]
    public async Task<IActionResult> CheckSavedRecipe(string recipeId)
    {
        var user = GetCurrentUser(); 
        if (user == null)
            return Unauthorized(new { error = "User not authenticated or claims missing" });

        try
        {
            var savedRecipe = await _supabase.From<SavedRecipe>()
                .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, user.Id.ToString())
                .Filter("recipe_id", Supabase.Postgrest.Constants.Operator.Equals, recipeId)
                .Limit(1)
                .Get();

            return Ok(new { isSaved = savedRecipe.Models.Any() });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CheckSavedRecipe] Error: {ex.Message}");
            return StatusCode(500, new { error = "Failed to check saved recipe status", details = ex.Message });
        }
    }
    
    [HttpPost("saved")] // Already protected by class-level [Authorize]
    public async Task<IActionResult> SaveRecipe([FromBody] SaveRecipeRequest request)
    {
        var user = GetCurrentUser(); 
        if (user == null)
            return Unauthorized(new { error = "User not authenticated or claims missing" });

        // Check if already saved
        var existing = await _supabase.From<SavedRecipe>()
            .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, user.Id.ToString())
            .Filter("recipe_id", Supabase.Postgrest.Constants.Operator.Equals, request.RecipeId)
            .Limit(1)
            .Get();

        if (existing.Models.Any())
        {
            return Conflict(new { error = "Recipe already saved by this user" });
        }
        
        try
        {
            var savedRecipe = new SavedRecipe
            {
                UserId = user.Id.ToString(),
                RecipeId = request.RecipeId,
                SavedAt = DateTime.UtcNow
            };
            
            await _supabase.From<SavedRecipe>().Insert(savedRecipe);
            
            Console.WriteLine($"[SaveRecipe] User {user.Id} saved recipe {request.RecipeId}");
            return Ok(new { success = true, message = "Recipe saved successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SaveRecipe] Error: {ex.Message}");
            return StatusCode(500, new { error = "Failed to save recipe", details = ex.Message });
        }
    }
    
    [HttpDelete("saved/{recipeId}")] // Already protected by class-level [Authorize]
    public async Task<IActionResult> UnsaveRecipe(string recipeId)
    {
        var user = GetCurrentUser(); 
        if (user == null)
            return Unauthorized(new { error = "User not authenticated or claims missing" });

        try
        {
            await _supabase.From<SavedRecipe>()
                .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, user.Id.ToString())
                .Filter("recipe_id", Supabase.Postgrest.Constants.Operator.Equals, recipeId)
                .Delete();

            Console.WriteLine($"[UnsaveRecipe] User {user.Id} unsaved recipe {recipeId}");
            return Ok(new { success = true, message = "Recipe unsaved successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UnsaveRecipe] Error: {ex.Message}");
            return StatusCode(500, new { error = "Failed to unsave recipe", details = ex.Message });
        }
    }

    [HttpPost("submit")] // Changed from generic [HttpPost] to specific "submit" route
    public async Task<IActionResult> Submit(SubmitRecipeRequest request)
    {
        // Ensure submitted by an authenticated user
        var user = GetCurrentUser(); // No await needed
        if (user == null)
            return Unauthorized(new { error = "User not authenticated or claims missing" });

        // Assign the actual authenticated user's ID
        var userId = user.Id.ToString();

        // Use the user's username as the author if not provided
        var authorName = string.IsNullOrWhiteSpace(request.Author) ? user.Username : request.Author;

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var recipe = new Models.Recipe
            {
                Id = Guid.NewGuid(),
                Name = request.RecipeName,
                Description = request.RecipeDescription,
                Category = request.Category,
                Difficulty = request.Difficulty,
                PrepTime = request.PrepTime ?? 0,
                CookTime = request.CookTime ?? 0,
                Servings = request.Servings ?? 0,
                Ingredients = request.Ingredients,
                Instructions = request.Instructions,
                Tips = request.Tips,
                NutritionInfo = request.NutritionInfo,
                DietaryOptions = request.DietaryOptions,
                Author = authorName,
                SubmittedAt = DateTime.UtcNow,
                Status = request.IsDraft ? "draft" : "pending",
                UserId = userId // Assign the authenticated user's ID
            };

            var response = await _supabase.From<Models.Recipe>().Insert(recipe);
            var created = response.Models.First();

            // await _activityLog.LogRecipeSubmittedAsync(created.Id.ToString(), created.Name, userId, GetClientIp(), GetUserAgent());

            var createdDto = MapToDto(created);
            return Created($"/api/recipes/{createdDto.Id}", createdDto);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Submit] Error: {ex.Message}");
            return StatusCode(500, new { error = "Failed to submit recipe", details = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRecipe(string id, [FromBody] SubmitRecipeRequest request)
    {
        var user = GetCurrentUser();
        if (user == null)
            return Unauthorized(new { error = "User not authenticated" });

        try
        {
            var existingRecipeResponse = await _supabase.From<Models.Recipe>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id)
                .Get();

            var existingRecipe = existingRecipeResponse.Models.FirstOrDefault();
            if (existingRecipe == null)
                return NotFound(new { error = "Recipe not found" });

            // Authorization: Only owner or admin can update
            if (existingRecipe.UserId != user.Id.ToString() && !user.IsAdmin)
            {
                return Forbid("You are not authorized to update this recipe.");
            }

            // Update properties from request
            existingRecipe.Name = request.RecipeName;
            existingRecipe.Description = request.RecipeDescription;
            existingRecipe.Category = request.Category;
            existingRecipe.Difficulty = request.Difficulty;
            existingRecipe.PrepTime = request.PrepTime ?? 0;
            existingRecipe.CookTime = request.CookTime ?? 0;
            existingRecipe.Servings = request.Servings ?? 0;
            existingRecipe.Ingredients = request.Ingredients;
            existingRecipe.Instructions = request.Instructions;
            existingRecipe.Tips = request.Tips;
            existingRecipe.NutritionInfo = request.NutritionInfo;
            existingRecipe.DietaryOptions = request.DietaryOptions;
            existingRecipe.Author = request.Author ?? "Unknown";
            // Do not allow direct update of SubmittedAt or Status via this endpoint if it's meant for user editing.
            // Status changes should ideally go through a separate workflow (e.g., publish, admin review).
            // However, the frontend passes status, so for now, we'll allow it. If this causes issues, we can restrict it.
#pragma warning disable CS8601 // Possible null reference assignment.
            existingRecipe.Status = request.Status ?? existingRecipe.Status ?? "pending";
#pragma warning restore CS8601 // Possible null reference assignment. 
            existingRecipe.ImageUrl = request.ImageUrl ?? existingRecipe.ImageUrl;
            existingRecipe.IsDraft = request.IsDraft; // This is a boolean, no null coalescing needed from request

            // Recalculate total time
            existingRecipe.TotalTime = (request.PrepTime ?? 0) + (request.CookTime ?? 0); // Explicitly cast if needed, though ?? 0 handles null

            var response = await _supabase.From<Models.Recipe>().Update(existingRecipe);

            if (response.Models.Any())
            {
                // Log activity for recipe update
                // You might need an ActivityLogService injected here to record this
                return Ok(MapToDto(response.Models.First()));
            }
            else
            {
                return StatusCode(500, new { error = "Failed to update recipe." });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RecipesController::UpdateRecipe] Error updating recipe {RecipeId}: {ErrorMessage}", id, ex.Message);
            return StatusCode(500, new { error = "An unexpected error occurred while updating the recipe.", details = ex.Message });
        }
    }

    [HttpGet("user/{userId}")] // Already protected by class-level [Authorize]
    public async Task<IActionResult> GetByUserId(string userId)
    {
        var user = GetCurrentUser(); 
        if (user == null)
            return Unauthorized(new { error = "User not authenticated or claims missing" });

        // Ensure the current user is authorized to view these recipes (either self or admin)
        if (user.Id.ToString() != userId && !user.IsAdmin)
        {
            return Forbid("You are not authorized to view this user's recipes.");
        }

        try
        {
            var response = await _supabase.From<Models.Recipe>()
                .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, userId)
                .Get();
                
            var recipeDtos = response.Models.Select(r => MapToDto(r)).ToList();
            return Ok(recipeDtos);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetByUserId] Error: {ex.Message}");
            // Return empty list so frontend shows friendly empty-state instead of error
            return Ok(new List<RecipeDto>());
        }
    }

    // Image generation endpoints
    [HttpPost("{id}/generate-image")] // Already protected by class-level [Authorize]
    public async Task<IActionResult> GenerateRecipeImage(string id)
    {
        var user = GetCurrentUser(); 
        if (user == null)
            return Unauthorized(new { error = "User not authenticated or claims missing" });

        // Ensure the user is either an admin or the owner of the recipe
        if (!user.IsAdmin)
        {
            // Supabase C# Filter does not accept Guid type directly, so compare using the string representation
            var recipeResponse = await _supabase.From<Models.Recipe>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id)
                .Limit(1)
                .Get();
            var recipe = recipeResponse.Models.FirstOrDefault();

            if (recipe?.UserId != user.Id.ToString())
            {
                return Forbid("You are not authorized to generate an image for this recipe.");
            }
        }

        try
        {
            // Get the recipe
            var recipeResponse = await _supabase.From<Models.Recipe>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id)
                .Get();
                
            var recipe = recipeResponse.Models.FirstOrDefault();
            if (recipe == null)
                return NotFound(new { error = "Recipe not found" });

            // Generate and upload image
            var result = await _imageService.GenerateAndUploadRecipeImageAsync(
                recipe.Id.ToString(), 
                recipe.Name, 
                recipe.Description ?? "", 
                recipe.Category ?? "", 
                recipe.Difficulty ?? "", 
                recipe.Ingredients?.ToList() ?? new List<string>()
            );

            if (result.Success)
            {
                // Update recipe with image URL
                recipe.ImageUrl = result.ImageUrl;
                await _supabase.From<Models.Recipe>().Update(recipe);

                return Ok(new { success = true, imageUrl = result.ImageUrl });
            }
            else
            {
                return StatusCode(500, new { 
                    error = "Failed to generate image", 
                    details = result.ErrorMessage
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GenerateRecipeImage] Error: {ex.Message}");
            return StatusCode(500, new { error = "Failed to generate recipe image", details = ex.Message });
        }
    }

    [HttpPost("{id}/regenerate-image")] // Already protected by class-level [Authorize]
    public async Task<IActionResult> RegenerateRecipeImage(string id)
    {
        var user = GetCurrentUser(); 
        if (user == null)
            return Unauthorized(new { error = "User not authenticated or claims missing" });

        // Ensure the user is either an admin or the owner of the recipe
        if (!user.IsAdmin)
        {
            // Supabase C# Filter does not accept Guid type directly, so compare using the string representation
            var recipeResponse = await _supabase.From<Models.Recipe>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id)
                .Limit(1)
                .Get();
            var recipe = recipeResponse.Models.FirstOrDefault();

            if (recipe?.UserId != user.Id.ToString())
            {
                return Forbid("You are not authorized to regenerate an image for this recipe.");
            }
        }

        try
        {
            // Get the recipe
            var recipeResponse = await _supabase.From<Models.Recipe>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id)
                .Get();

            var recipe = recipeResponse.Models.FirstOrDefault();
            if (recipe == null)
                return NotFound(new { error = "Recipe not found" });

            // Regenerate and upload image
            var result = await _imageService.RegenerateRecipeImageAsync(
                recipe.Id.ToString(),
                recipe.Name,
                recipe.Description ?? "",
                recipe.Category ?? "",
                recipe.Difficulty ?? "",
                recipe.Ingredients?.ToList() ?? new List<string>(),
                recipe.ImageUrl // Pass existing image URL for deletion
            );

            if (result.Success)
            {
                // Update recipe with new image URL
                recipe.ImageUrl = result.ImageUrl; 
                await _supabase.From<Models.Recipe>().Update(recipe); // Update the image URL in the database

                return Ok(new { success = true, imageUrl = result.ImageUrl });
            }
            else
            {
                return StatusCode(500, new { 
                    error = "Failed to regenerate image", 
                    details = result.ErrorMessage
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RegenerateRecipeImage] Error: {ex.Message}");
            return StatusCode(500, new { error = "Failed to regenerate recipe image", details = ex.Message });
        }
    }

    [Authorize] // Only authenticated users can publish recipes
    [HttpPut("{id}/publish")]
    public async Task<IActionResult> PublishRecipe(string id)
    {
        var user = GetCurrentUser();
        if (user == null)
            return Unauthorized(new { error = "User not authenticated or claims missing" });

        try
        {
            var recipeToUpdate = await _supabase.From<Models.Recipe>()
                .Where(r => r.Id.ToString() == id)
                .Single();

            if (recipeToUpdate == null)
                return NotFound(new { error = "Recipe not found" });

            // Ensure the current user is the owner of the recipe or an admin
            if (recipeToUpdate.UserId != user.Id.ToString() && !user.IsAdmin)
            {
                return Forbid("You are not authorized to publish this recipe.");
            }

            if (!recipeToUpdate.IsDraft)
            {
                return BadRequest(new { error = "Recipe is not a draft and cannot be published." });
            }

            recipeToUpdate.IsDraft = false;
            recipeToUpdate.Status = "pending"; // Set to pending for admin review

            var response = await _supabase.From<Models.Recipe>().Update(recipeToUpdate);

            if (response.Models.Any())
            {
                return Ok(new { success = true, message = "Recipe published successfully and sent for admin review." });
            }
            else
            {
                return StatusCode(500, new { error = "Failed to publish recipe." });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RecipesController::PublishRecipe] Error publishing recipe {RecipeId}: {ErrorMessage}", id, ex.Message);
            return StatusCode(500, new { error = "An unexpected error occurred while publishing the recipe.", details = ex.Message });
        }
    }

    [Authorize(Roles = "Admin")] // Only admins can delete images
    [HttpDelete("{id}/image")]
    public async Task<IActionResult> DeleteRecipeImage(string id)
    {
        var user = GetCurrentUser(); 
        if (user == null || !user.IsAdmin)
        {
            return Unauthorized(new { error = "Admin authentication required." });
        }

        try
        {
            var recipe = await _supabase.From<Models.Recipe>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id)
                .Limit(1)
                .Get();

            var existingRecipe = recipe.Models.FirstOrDefault();
            if (existingRecipe == null)
                return NotFound(new { error = "Recipe not found" });

            if (string.IsNullOrEmpty(existingRecipe.ImageUrl))
                return Ok(new { success = true, message = "No image to delete." });

            // Clear the image URL in the database
            existingRecipe.ImageUrl = null;
            await _supabase.From<Models.Recipe>().Update(existingRecipe);

            return Ok(new { success = true, message = "Recipe image deleted successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DeleteRecipeImage] Error: {ex.Message}");
            return StatusCode(500, new { error = "Failed to delete recipe image", details = ex.Message });
        }
    }

    // Helper method to convert Recipe model to DTO (prevents Supabase serialization issues)
    private static RecipeDto MapToDto(Models.Recipe recipe)
    {
        return new RecipeDto
        {
            Id = recipe.Id,
            Name = recipe.Name,
            Description = recipe.Description,
            Category = recipe.Category,
            Difficulty = recipe.Difficulty,
            PrepTime = recipe.PrepTime,
            CookTime = recipe.CookTime,
            Servings = recipe.Servings,
            Ingredients = recipe.Ingredients,
            Instructions = recipe.Instructions,
            Tips = recipe.Tips,
            NutritionInfo = recipe.NutritionInfo,
            DietaryOptions = recipe.DietaryOptions,
            Author = recipe.Author,
            SubmittedAt = recipe.SubmittedAt,
            Status = recipe.Status,
            UserId = recipe.UserId,
            TotalTime = recipe.TotalTime,
            ImageUrl = recipe.ImageUrl,
            IsDraft = recipe.IsDraft 
        };
    }

    [AllowAnonymous] // Allow unauthenticated access
    [HttpGet("by-user/{userId}")]
    public Task<IActionResult> GetByUserIdAlt(string userId) => GetByUserId(userId);

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRecipe(string id)
    {
        var user = GetCurrentUser();
        if (user == null)
            return Unauthorized(new { error = "User not authenticated" });

        try
        {
            // Get the recipe to check ownership
            var recipeResponse = await _supabase.From<Models.Recipe>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id)
                .Limit(1)
                .Get();
                
            var recipe = recipeResponse.Models.FirstOrDefault();
            if (recipe == null)
                return NotFound(new { error = "Recipe not found" });
                
            // Ensure the current user is the owner of the recipe or an admin
            if (recipe.UserId != user.Id.ToString() && !user.IsAdmin)
            {
                return Forbid("You are not authorized to delete this recipe.");
            }
            
            // First, delete any progress records for this recipe
            try {
                await _supabase.From<UserRecipeProgress>()
                    .Filter("recipe_id", Supabase.Postgrest.Constants.Operator.Equals, id)
                    .Delete();
            } catch (Exception ex) {
                _logger.LogError(ex, "Failed to delete recipe progress records for recipe {RecipeId}", id);
                return StatusCode(500, new { error = "Failed to delete recipe progress records", details = ex.Message });
            }
            
            // Second, delete any saved references to this recipe
            try {
                await _supabase.From<SavedRecipe>()
                    .Filter("recipe_id", Supabase.Postgrest.Constants.Operator.Equals, id)
                    .Delete();
            } catch (Exception ex) {
                _logger.LogError(ex, "Failed to delete saved recipe references for recipe {RecipeId}", id);
                return StatusCode(500, new { error = "Failed to delete saved recipe references", details = ex.Message });
            }
            
            // Finally, delete the recipe itself
            await _supabase.From<Models.Recipe>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id)
                .Delete();

            return Ok(new { success = true, message = "Recipe deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting recipe {RecipeId}: {ErrorMessage}", id, ex.Message);
            return StatusCode(500, new { error = "Failed to delete recipe", details = ex.Message });
        }
    }

    // DTOs -----------------------------------------------------------

    public record SubmitRecipeRequest(
        [Required] string RecipeName,
        [Required] string RecipeDescription,
        [Required] string Category,
        [Required] string Difficulty,
        [Range(1, int.MaxValue)] int? PrepTime,
        [Range(1, int.MaxValue)] int? CookTime,
        [Range(1, int.MaxValue)] int? Servings,
        [Required] List<string> Ingredients,
        [Required] List<string> Instructions,
        string? Tips,
        string? NutritionInfo,
        List<string>? DietaryOptions,
        string? Author,
        string? UserId,
        string? ImageUrl, 
        string? Status, 
        bool IsDraft = false 
    );
        
    public record SaveRecipeRequest(
        [Required] string RecipeId);

    public class RecipeDto
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public int PrepTime { get; set; }
        public int CookTime { get; set; }
        public int Servings { get; set; }
        public List<string> Ingredients { get; set; } = new();
        public List<string> Instructions { get; set; } = new();
        public string? Tips { get; set; }
        public string? NutritionInfo { get; set; }
        public List<string>? DietaryOptions { get; set; }
        public string Author { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public string Status { get; set; } = "pending";
        public string? UserId { get; set; }
        public int? TotalTime { get; set; }
        public string? ImageUrl { get; set; } 
        public bool IsDraft { get; set; } 
    }
    
    [Supabase.Postgrest.Attributes.Table("saved_recipes")]
    public class SavedRecipe : BaseModel
    {
        [Supabase.Postgrest.Attributes.PrimaryKey("id")]
        public Guid Id { get; set; }
        
        [Supabase.Postgrest.Attributes.Column("user_id")]
        public string UserId { get; set; } = null!; 
        
        [Supabase.Postgrest.Attributes.Column("recipe_id")]
        public string RecipeId { get; set; } = null!; 
        
        [Supabase.Postgrest.Attributes.Column("saved_at")]
        public DateTime SavedAt { get; set; } = DateTime.UtcNow;
    }

    // Model for user_recipe_progress table
    [Supabase.Postgrest.Attributes.Table("user_recipe_progress")]
    public class UserRecipeProgress : BaseModel
    {
        [Supabase.Postgrest.Attributes.PrimaryKey("id")]
        public Guid Id { get; set; }
        [Supabase.Postgrest.Attributes.Column("user_id")]
        public Guid UserId { get; set; }
        [Supabase.Postgrest.Attributes.Column("recipe_id")]
        public Guid RecipeId { get; set; }
        [Supabase.Postgrest.Attributes.Column("current_instruction_step")]
        public int CurrentInstructionStep { get; set; }
        [Supabase.Postgrest.Attributes.Column("checked_ingredients")]
        public string CheckedIngredients { get; set; } = "[]";

        [Supabase.Postgrest.Attributes.Column("completed_at")]
        public DateTime? CompletedAt { get; set; }
        [Supabase.Postgrest.Attributes.Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
        [Supabase.Postgrest.Attributes.Column("created_at")]
        public DateTime? CreatedAt { get; set; }
    }

    // DTO for incoming recipe progress data
    public record RecipeProgressRequest(
        [Required] Guid RecipeId,
        [Required] int CurrentInstructionStep,
        [Required] List<int> CheckedIngredients
    );

    // DTO for outgoing recipe progress data
    public class RecipeProgressDto
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
        [JsonPropertyName("userId")]
        public Guid? UserId { get; set; }
        [JsonPropertyName("recipeId")]
        public Guid RecipeId { get; set; }
        [JsonPropertyName("currentInstructionStep")]
        public int CurrentInstructionStep { get; set; }
        [JsonPropertyName("checkedIngredients")]
        public List<int> CheckedIngredients { get; set; } = new List<int>();
        [JsonPropertyName("completedAt")]
        public DateTime? CompletedAt { get; set; }
        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }
        [JsonPropertyName("createdAt")] 
        public DateTime? CreatedAt { get; set; } 
    }
} 