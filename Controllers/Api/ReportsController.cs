using Microsoft.AspNetCore.Mvc;
using EasyBites.Models;
using EasyBites.Services;
using Supabase;
using System.ComponentModel.DataAnnotations;

namespace EasyBites.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly Client _supabaseClient;
    private readonly ActivityLogService _activityLogService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(Client supabaseClient, ActivityLogService activityLogService, ILogger<ReportsController> logger)
    {
        _supabaseClient = supabaseClient;
        _activityLogService = activityLogService;
        _logger = logger;
    }

    // Helper method to get current user ID from claims
    private Guid? GetAuthenticatedUserIdFromClaims()
    {
        try
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return null;
            }

            if (Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user ID from claims");
            return null;
        }
    }

    // Submit a report (for regular users)
    [HttpPost("submit")]
    public async Task<IActionResult> SubmitReport([FromBody] SubmitReportRequest request)
    {
        try
        {
            Console.WriteLine("[SubmitReport] Starting report submission");
            
            // Get current user ID from claims
            var currentUserId = GetAuthenticatedUserIdFromClaims();
            if (currentUserId == null)
            {
                Console.WriteLine("[SubmitReport] User not authenticated");
                return Unauthorized(new { error = "User not authenticated" });
            }

            Console.WriteLine($"[SubmitReport] Current user ID: {currentUserId}");

            // Validate request
            if (string.IsNullOrEmpty(request.ReportType) || string.IsNullOrEmpty(request.Description))
            {
                return BadRequest(new { error = "Report type and description are required" });
            }

            // Validate optional GUIDs if provided
            Guid? reportedUserId = null;
            if (!string.IsNullOrEmpty(request.ReportedUserId))
            {
                if (!Guid.TryParse(request.ReportedUserId, out var parsedUserId))
                {
                    return BadRequest(new { error = "Invalid reported user ID format" });
                }
                reportedUserId = parsedUserId;
            }
            
            Guid? reportedRecipeId = null;
            if (!string.IsNullOrEmpty(request.ReportedRecipeId))
            {
                if (!Guid.TryParse(request.ReportedRecipeId, out var parsedRecipeId))
                {
                    return BadRequest(new { error = "Invalid reported recipe ID format" });
                }
                reportedRecipeId = parsedRecipeId;
            }

            // Create report object - only type and description are required
            var report = new Report
            {
                ReporterUserId = currentUserId.Value,
                ReportedUserId = reportedUserId,
                ReportedRecipeId = reportedRecipeId,
                ReportType = request.ReportType,
                Description = request.Description,
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            };
            
            Console.WriteLine($"[SubmitReport] Creating report for user: {currentUserId.Value}");
            
            // Insert into database using Supabase ORM
            var response = await _supabaseClient.From<Report>().Insert(report);
            
            Console.WriteLine($"[SubmitReport] Report created successfully");
            return Ok(new { success = true, message = "Report submitted successfully", reportId = report.Id });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SubmitReport] Error: {ex.Message}");
            Console.WriteLine($"[SubmitReport] Stack trace: {ex.StackTrace}");
            
            // Handle specific database constraint violations
            if (ex.Message.Contains("foreign key constraint") || ex.Message.Contains("violates foreign key"))
            {
                return BadRequest(new { error = "Invalid reference: One or more referenced entities do not exist" });
            }
            
            return StatusCode(500, new { error = "Failed to submit report", details = ex.Message });
        }
    }

    // Get user's own reports
    [HttpGet("my-reports")]
    public async Task<IActionResult> GetMyReports()
    {
        try
        {
            // Get current user ID from claims
            var currentUserId = GetAuthenticatedUserIdFromClaims();
            if (currentUserId == null)
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var reports = await _supabaseClient
                .From<Report>()
                .Select("*")
                .Where(r => r.ReporterUserId == currentUserId.Value)
                .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            // Convert to DTOs to avoid serialization issues with Supabase attributes
            var reportDtos = reports.Models.Select(r => new ReportDto
            {
                Id = r.Id,
                ReporterUserId = r.ReporterUserId,
                ReportedUserId = r.ReportedUserId,
                ReportedRecipeId = r.ReportedRecipeId,
                ReportType = r.ReportType,
                Description = r.Description,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                ReviewedAt = r.ReviewedAt,
                ReviewedByAdminId = r.ReviewedByAdminId,
                AdminNotes = r.AdminNotes
            }).ToList();

            return Ok(reportDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user reports");
            return StatusCode(500, new { error = "Failed to get reports", details = ex.Message });
        }
    }
}

public record SubmitReportRequest(
    string ReportType,
    string Description,
    string? ReportedUserId = null,
    string? ReportedRecipeId = null
);

public class ReportDto
{
    public long Id { get; set; }
    public Guid? ReporterUserId { get; set; }
    public Guid? ReportedUserId { get; set; }
    public Guid? ReportedRecipeId { get; set; }
    public string ReportType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedByAdminId { get; set; }
    public string? AdminNotes { get; set; }
}