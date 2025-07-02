using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;
using System;
using System.Collections.Generic;

namespace EasyBites.Models;

[Table("recipes")]
public class Recipe : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }

    [Column("name")] public string Name { get; set; } = string.Empty;
    [Column("description")] public string Description { get; set; } = string.Empty;
    [Column("category")] public string Category { get; set; } = string.Empty;
    [Column("difficulty")] public string Difficulty { get; set; } = string.Empty;

    [Column("prep_time")] public int PrepTime { get; set; }
    [Column("cook_time")] public int CookTime { get; set; }
    [Column("servings")] public int Servings { get; set; }

    [Column("ingredients")] public List<string> Ingredients { get; set; } = new();
    [Column("instructions")] public List<string> Instructions { get; set; } = new();
    [Column("tips")] public string? Tips { get; set; }
    [Column("nutrition_info")] public string? NutritionInfo { get; set; }
    [Column("dietary_options")] public List<string>? DietaryOptions { get; set; }
    [Column("author")] public string Author { get; set; } = string.Empty;
    [Column("submitted_at")] public DateTime SubmittedAt { get; set; }
    [Column("status")] public string Status { get; set; } = string.Empty;

    [Column("total_time")] public int? TotalTime { get; set; }
    [Column("user_id")] public string? UserId { get; set; }
} 