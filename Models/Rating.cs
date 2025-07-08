
using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;
using System;

namespace EasyBites.Models;

[Table("ratings")]
public class Rating : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }

    [Column("recipe_id")]
    public Guid RecipeId { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("score")]
    public int Score { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
} 