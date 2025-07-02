using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc;
using Supabase;
using Supabase.Postgrest;
using Supabase.Gotrue;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace EasyBites.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class RecipesController : ControllerBase
{
    private readonly Supabase.Client _supabase;

    public RecipesController(Supabase.Client supabase)
    {
        _supabase = supabase;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] string? category,
                                [FromQuery] string? difficulty, [FromQuery] string? time)
    {
        dynamic qry = _supabase.From<Models.Recipe>();

        if (!string.IsNullOrWhiteSpace(search))
            qry = qry.Filter("name", Supabase.Postgrest.Constants.Operator.ILike, $"%{search}%");

        if (!string.IsNullOrWhiteSpace(category))
            qry = qry.Filter("category", Supabase.Postgrest.Constants.Operator.Equals, category);

        if (!string.IsNullOrWhiteSpace(difficulty))
            qry = qry.Filter("difficulty", Supabase.Postgrest.Constants.Operator.Equals, difficulty);

        if (!string.IsNullOrWhiteSpace(time) && int.TryParse(time.TrimEnd('+'), out var minutes))
        {
            var op = time.EndsWith('+') ? Supabase.Postgrest.Constants.Operator.GreaterThan : Supabase.Postgrest.Constants.Operator.LessThanOrEqual;
            qry = qry.Filter("total_time", op, minutes);
        }

        var results = await qry.Get();
        return Ok(results.Models);
    }
    
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return await GetAll(null, null, null, null);
        
        // For simple search, use the raw query string approach
        var results = await _supabase.From<Models.Recipe>()
            .Select("*")
            .Filter("name", Supabase.Postgrest.Constants.Operator.ILike, $"%{q}%")
            .Get();
        
        return Ok(results.Models);
    }
    
    [HttpGet("filter")]
    public async Task<IActionResult> Filter([FromQuery] string? difficulty, [FromQuery] string? time)
    {
        dynamic qry = _supabase.From<Models.Recipe>();

        if (!string.IsNullOrWhiteSpace(difficulty))
            qry = qry.Filter("difficulty", Supabase.Postgrest.Constants.Operator.Equals, difficulty);

        if (!string.IsNullOrWhiteSpace(time) && int.TryParse(time, out var minutes))
        {
            qry = qry.Filter("total_time", Supabase.Postgrest.Constants.Operator.LessThanOrEqual, minutes);
        }

        var results = await qry.Get();
        return Ok(results.Models);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var singleResp = await _supabase.From<Models.Recipe>()
                                    .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id)
                                    .Get();
        var result = singleResp.Models.FirstOrDefault();

        if (result == null)
            return NotFound();

        return Ok(result);
    }
    
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUserId(string userId)
    {
        try
        {
            var response = await _supabase.From<Models.Recipe>()
                .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, userId)
                .Get();
                
            return Ok(response.Models);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to fetch user recipes", details = ex.Message });
        }
    }
    
    [HttpGet("saved")]
    public async Task<IActionResult> GetSavedRecipes()
    {
        // Get the current user
        var user = await GetCurrentUser();
        if (user == null)
            return Unauthorized(new { error = "User not authenticated" });
            
        try
        {
            // Get all saved recipe IDs for the user
            var savedRecipesResponse = await _supabase.From<SavedRecipe>()
                .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, user.Id.ToString())
                .Get();
                
            var savedRecipeIds = savedRecipesResponse.Models.Select(sr => sr.RecipeId).ToList();
            
            if (savedRecipeIds.Count == 0)
                return Ok(new List<Models.Recipe>());
                
            // Get all saved recipes
            var recipesResponse = await _supabase.From<Models.Recipe>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.In, savedRecipeIds)
                .Get();
                
            return Ok(recipesResponse.Models);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to fetch saved recipes", details = ex.Message });
        }
    }
    
    [HttpGet("saved/{recipeId}")]
    public async Task<IActionResult> CheckSavedRecipe(string recipeId)
    {
        // Get the current user
        var user = await GetCurrentUser();
        if (user == null)
            return Unauthorized(new { error = "User not authenticated" });
            
        try
        {
            // Check if recipe is saved by user
            var savedRecipeResponse = await _supabase.From<SavedRecipe>()
                .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, user.Id.ToString())
                .Filter("recipe_id", Supabase.Postgrest.Constants.Operator.Equals, recipeId)
                .Get();
                
            var isSaved = savedRecipeResponse.Models.Any();
            
            return Ok(new { isSaved });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to check saved recipe status", details = ex.Message });
        }
    }
    
    [HttpPost("saved")]
    public async Task<IActionResult> SaveRecipe([FromBody] SaveRecipeRequest request)
    {
        // Get the current user
        var user = await GetCurrentUser();
        if (user == null)
            return Unauthorized(new { error = "User not authenticated" });
            
        try
        {
            // Check if recipe exists
            var recipeResponse = await _supabase.From<Models.Recipe>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, request.RecipeId)
                .Get();
                
            if (!recipeResponse.Models.Any())
                return NotFound(new { error = "Recipe not found" });
                
            // Check if already saved
            var savedRecipeResponse = await _supabase.From<SavedRecipe>()
                .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, user.Id.ToString())
                .Filter("recipe_id", Supabase.Postgrest.Constants.Operator.Equals, request.RecipeId)
                .Get();
                
            if (savedRecipeResponse.Models.Any())
                return Ok(new { message = "Recipe already saved" });
                
            // Save the recipe
            var savedRecipe = new SavedRecipe
            {
                UserId = user.Id.ToString(),
                RecipeId = request.RecipeId,
                SavedAt = DateTime.UtcNow
            };
            
            var insertResponse = await _supabase.From<SavedRecipe>().Insert(savedRecipe);
            var created = insertResponse.Models.FirstOrDefault();
            
            return Created($"/api/recipes/saved/{request.RecipeId}", created);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to save recipe", details = ex.Message });
        }
    }
    
    [HttpDelete("saved/{recipeId}")]
    public async Task<IActionResult> UnsaveRecipe(string recipeId)
    {
        // Get the current user
        var user = await GetCurrentUser();
        if (user == null)
            return Unauthorized(new { error = "User not authenticated" });
            
        try
        {
            // Delete the saved recipe
            await _supabase.From<SavedRecipe>()
                .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, user.Id.ToString())
                .Filter("recipe_id", Supabase.Postgrest.Constants.Operator.Equals, recipeId)
                .Delete();
                
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to unsave recipe", details = ex.Message });
        }
    }

    // Recipe submission – based on submit-recipe.js
    [HttpPost]
    public async Task<IActionResult> Submit(SubmitRecipeRequest request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        
        // Get the current user if available
        var user = await GetCurrentUser();
        var userId = user != null ? user.Id.ToString() : request.UserId;

        var recipe = new Models.Recipe
        {
            Name = request.RecipeName,
            Description = request.RecipeDescription,
            Category = request.Category,
            Difficulty = request.Difficulty,
            PrepTime = request.PrepTime,
            CookTime = request.CookTime,
            Servings = request.Servings,
            Ingredients = request.Ingredients,
            Instructions = request.Instructions,
            Tips = request.Tips,
            NutritionInfo = request.NutritionInfo,
            DietaryOptions = request.DietaryOptions,
            Author = request.Author ?? user?.Email ?? "Anonymous",
            Status = "pending",
            SubmittedAt = DateTime.UtcNow,
            TotalTime = request.PrepTime + request.CookTime,
            UserId = userId
        };

        var insertResponse = await _supabase.From<Models.Recipe>().Insert(recipe);
        var created = insertResponse.Models.FirstOrDefault();
        return Created($"/api/recipes/{created?.Id}", created);
    }
    
    // Helper method to get the current authenticated user
    private async Task<AuthController.User?> GetCurrentUser()
    {
        try
        {
            if (!Request.Cookies.TryGetValue("session_id", out var sessionId))
            {
                return null;
            }

            // Use the session ID to find the user's email from the AuthController's Sessions dictionary
            // For simplicity, we'll just query the user directly from the database
            var userResponse = await _supabase.From<AuthController.User>()
                .Get();
                
            return userResponse.Models.FirstOrDefault();
        }
        catch
        {
            // Session not available or invalid
            return null;
        }
    }

    // DTOs -----------------------------------------------------------

    public record SubmitRecipeRequest(
        [Required] string RecipeName,
        [Required] string RecipeDescription,
        [Required] string Category,
        [Required] string Difficulty,
        [Range(1, int.MaxValue)] int PrepTime,
        [Range(1, int.MaxValue)] int CookTime,
        [Range(1, int.MaxValue)] int Servings,
        [Required] List<string> Ingredients,
        [Required] List<string> Instructions,
        string? Tips,
        string? NutritionInfo,
        List<string>? DietaryOptions,
        string? Author,
        string? UserId);
        
    public record SaveRecipeRequest(
        [Required] string RecipeId);

    public class RecipeDto
    {
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
    }
    
    [Supabase.Postgrest.Attributes.Table("saved_recipes")]
    public class SavedRecipe : BaseModel
    {
        [Supabase.Postgrest.Attributes.PrimaryKey("id")]
        public Guid Id { get; set; }
        
        [Supabase.Postgrest.Attributes.Column("user_id")]
        public string UserId { get; set; } = string.Empty;
        
        [Supabase.Postgrest.Attributes.Column("recipe_id")]
        public string RecipeId { get; set; } = string.Empty;
        
        [Supabase.Postgrest.Attributes.Column("saved_at")]
        public DateTime SavedAt { get; set; }
    }
} 