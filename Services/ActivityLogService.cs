using Supabase;
using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;
using System.ComponentModel.DataAnnotations;

namespace EasyBites.Services;

public class ActivityLogService
{
    private readonly Supabase.Client _supabase;

    public ActivityLogService(Supabase.Client supabase)
    {
        _supabase = supabase;
    }

    public async Task LogActivityAsync(string? userId, string actionType, string? targetId = null, 
        string? targetType = null, object? details = null, string? ipAddress = null, string? userAgent = null)
    {
        try
        {
            var log = new ActivityLog
            {
                UserId = userId,
                ActionType = actionType,
                TargetId = targetId,
                TargetType = targetType,
                Details = details != null ? System.Text.Json.JsonSerializer.Serialize(details) : null,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CreatedAt = DateTime.UtcNow
            };

            await _supabase.From<ActivityLog>().Insert(log);
        }
        catch (Exception ex)
        {
            // Log error but don't throw to avoid breaking main functionality
            Console.WriteLine($"Failed to log activity: {ex.Message}");
        }
    }

    public async Task<List<ActivityLogDto>> GetRecentActivitiesAsync(int limit = 50)
    {
        try
        {
            var response = await _supabase.From<ActivityLog>()
                .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Limit(limit)
                .Get();

            return response.Models.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get recent activities: {ex.Message}");
            return new List<ActivityLogDto>();
        }
    }

    public async Task LogRecipeCreatedAsync(string? userId, string recipeId, string recipeName, string? ipAddress = null, string? userAgent = null)
    {
        await LogActivityAsync(userId, "recipe_created", recipeId, "recipe", 
            new { recipe_name = recipeName }, ipAddress, userAgent);
    }

    public async Task LogRecipeDeletedAsync(string? userId, string recipeId, string recipeName, string? ipAddress = null, string? userAgent = null)
    {
        await LogActivityAsync(userId, "recipe_deleted", recipeId, "recipe", 
            new { recipe_name = recipeName }, ipAddress, userAgent);
    }

    public async Task LogRecipeSavedAsync(string userId, string recipeId, string recipeName, string? ipAddress = null, string? userAgent = null)
    {
        await LogActivityAsync(userId, "recipe_saved", recipeId, "recipe", 
            new { recipe_name = recipeName }, ipAddress, userAgent);
    }

    public async Task LogRecipeUnsavedAsync(string userId, string recipeId, string recipeName, string? ipAddress = null, string? userAgent = null)
    {
        await LogActivityAsync(userId, "recipe_unsaved", recipeId, "recipe", 
            new { recipe_name = recipeName }, ipAddress, userAgent);
    }

    public async Task LogUserRegisteredAsync(string userId, string email, string? ipAddress = null, string? userAgent = null)
    {
        await LogActivityAsync(userId, "user_registered", userId, "user", 
            new { email }, ipAddress, userAgent);
    }

    public async Task LogUserLoginAsync(string userId, string email, string? ipAddress = null, string? userAgent = null)
    {
        await LogActivityAsync(userId, "user_login", userId, "user", 
            new { email }, ipAddress, userAgent);
    }

    public async Task LogAdminLoginAsync(string userId, string email, string? ipAddress = null, string? userAgent = null)
    {
        await LogActivityAsync(userId, "admin_login", userId, "user", 
            new { email }, ipAddress, userAgent);
    }

    public async Task LogReportCreatedAsync(string? reporterId, string reportId, string reportType, string? ipAddress = null, string? userAgent = null)
    {
        await LogActivityAsync(reporterId, "report_created", reportId, "report", 
            new { report_type = reportType }, ipAddress, userAgent);
    }

    public async Task LogFeedbackSubmittedAsync(string? userId, string feedbackId, string feedbackType, string? ipAddress = null, string? userAgent = null)
    {
        await LogActivityAsync(userId, "feedback_submitted", feedbackId, "feedback", 
            new { feedback_type = feedbackType }, ipAddress, userAgent);
    }

    private static ActivityLogDto MapToDto(ActivityLog log)
    {
        return new ActivityLogDto
        {
            Id = log.Id,
            UserId = log.UserId,
            ActionType = log.ActionType,
            TargetId = log.TargetId,
            TargetType = log.TargetType,
            Details = log.Details,
            IpAddress = log.IpAddress,
            UserAgent = log.UserAgent,
            CreatedAt = log.CreatedAt
        };
    }
}

[Table("activity_logs")]
public class ActivityLog : BaseModel
{
    [PrimaryKey("id")]
    public long Id { get; set; }

    [Column("user_id")]
    public string? UserId { get; set; }

    [Column("action_type")]
    public string ActionType { get; set; } = string.Empty;

    [Column("target_id")]
    public string? TargetId { get; set; }

    [Column("target_type")]
    public string? TargetType { get; set; }

    [Column("details")]
    public string? Details { get; set; }

    [Column("ip_address")]
    public string? IpAddress { get; set; }

    [Column("user_agent")]
    public string? UserAgent { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}

public class ActivityLogDto
{
    public long Id { get; set; }
    public string? UserId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string? TargetId { get; set; }
    public string? TargetType { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; }
} 