using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;
using System;

namespace EasyBites.Models;

[Table("feedback")]
public class Feedback : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }

    [Column("name")] public string? Name { get; set; }
    [Column("email")] public string? Email { get; set; }
    [Column("type")] public string Type { get; set; } = string.Empty;
    [Column("subject")] public string Subject { get; set; } = string.Empty;
    [Column("message")] public string Message { get; set; } = string.Empty;
    [Column("rating")] public int? Rating { get; set; }
    [Column("submitted_at")] public DateTime SubmittedAt { get; set; }
} 