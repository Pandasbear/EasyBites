using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;
using System;

namespace EasyBites.Models;

[Table("reports")]
public class Report : BaseModel
{
    [PrimaryKey("id")]
    public long Id { get; set; }

    [Column("reporter_user_id")] public Guid? ReporterUserId { get; set; }
    [Column("reported_user_id")] public Guid? ReportedUserId { get; set; }
    [Column("reported_recipe_id")] public Guid? ReportedRecipeId { get; set; }
    [Column("report_type")] public string ReportType { get; set; } = string.Empty;
    [Column("description")] public string Description { get; set; } = string.Empty;
    [Column("status")] public string Status { get; set; } = "pending";
    [Column("created_at")] public DateTime CreatedAt { get; set; }
    [Column("reviewed_at")] public DateTime? ReviewedAt { get; set; }
    [Column("reviewed_by_admin_id")] public Guid? ReviewedByAdminId { get; set; }
    [Column("admin_notes")] public string? AdminNotes { get; set; }
} 