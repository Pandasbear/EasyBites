using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Supabase;
using Supabase.Postgrest;

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
            qry = qry.Filter("name", Constants.Operator.ILike, $"%{search}%");

        if (!string.IsNullOrWhiteSpace(category))
            qry = qry.Filter("category", Constants.Operator.Equals, category);

        if (!string.IsNullOrWhiteSpace(difficulty))
            qry = qry.Filter("difficulty", Constants.Operator.Equals, difficulty);

        if (!string.IsNullOrWhiteSpace(time) && int.TryParse(time.TrimEnd('+'), out var minutes))
        {
            var op = time.EndsWith('+') ? Constants.Operator.GreaterThan : Constants.Operator.LessThanOrEqual;
            qry = qry.Filter("total_time", op, minutes);
        }

        var results = await qry.Get();
        return Ok(results.Models);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var singleResp = await _supabase.From<Models.Recipe>()
                                    .Filter("id", Constants.Operator.Equals, id)
                                    .Get();
        var result = singleResp.Models.FirstOrDefault();

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    // Recipe submission – based on submit-recipe.js
    [HttpPost]
    public async Task<IActionResult> Submit(SubmitRecipeRequest request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

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
            Author = request.Author ?? "Anonymous",
            Status = "pending",
            SubmittedAt = DateTime.UtcNow,
            TotalTime = request.PrepTime + request.CookTime
        };

        var insertResponse = await _supabase.From<Models.Recipe>().Insert(recipe);
        var created = insertResponse.Models.FirstOrDefault();
        return Created($"/api/recipes/{created?.Id}", created);
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
        string? Author);

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
    }
} 