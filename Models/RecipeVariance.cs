using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EasyBites.Models;

[Table("recipe_variances")]
public class RecipeVariance : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }

    [Column("original_recipe_id")] public Guid OriginalRecipeId { get; set; }
    [Column("name")] public string Name { get; set; } = string.Empty;
    [Column("description")] public string Description { get; set; } = string.Empty;
    [Column("category")] public string Category { get; set; } = string.Empty;
    [Column("difficulty")] public string Difficulty { get; set; } = string.Empty;

    [Column("prep_time")] public int PrepTime { get; set; }
    [Column("cook_time")] public int CookTime { get; set; }
    [Column("servings")] public int Servings { get; set; }
    [Column("original_servings")] public int OriginalServings { get; set; }

    [Column("ingredients")] public List<string> Ingredients { get; set; } = new();
    [Column("instructions")] public List<string> Instructions { get; set; } = new();
    [Column("tips")] public string? Tips { get; set; }
    [Column("nutrition_info")] public string? NutritionInfo { get; set; }
    [Column("dietary_options")] public List<string>? DietaryOptions { get; set; }
    [Column("author")] public string Author { get; set; } = string.Empty;
    [Column("created_at")] public DateTime CreatedAt { get; set; }
    [Column("created_by_user_id")] public string? CreatedByUserId { get; set; }
    [Column("image_url")] public string? ImageUrl { get; set; }
    
    [Column("total_time", ignoreOnInsert: true, ignoreOnUpdate: true)]
    [JsonIgnore]
    public int? TotalTime { get; set; }
}