using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Supabase;
using Supabase.Postgrest;

namespace EasyBites.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class FeedbackController : ControllerBase
{
    private readonly Supabase.Client _supabase;

    public FeedbackController(Supabase.Client supabase)
    {
        _supabase = supabase;
    }

    // POST /api/feedback
    [HttpPost("submit")]
    public async Task<IActionResult> Submit(SubmitFeedbackRequest request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        try
        {
            // Get current user ID from claims if authenticated
            var userIdClaim = HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            Guid? userId = null;
            if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var parsedUserId))
            {
                userId = parsedUserId;
            }

            var feedback = new Models.Feedback
            {
                UserId = userId,
                Name = request.Name,
                Email = request.Email,
                Type = request.Type,
                Subject = request.Subject,
                Message = request.Message,
                Rating = request.Rating
                // SubmittedAt will be set automatically by Supabase DEFAULT now()
            };

            var insertResp = await _supabase.From<Models.Feedback>().Insert(feedback);
            var row = insertResp.Models.FirstOrDefault();
            
            if (row == null)
                return StatusCode(500, new { error = "Failed to submit feedback" });
                
            // Return a simple response instead of the raw Supabase model
            return Ok(new { 
                message = "Feedback submitted successfully",
                id = row.Id,
                submittedAt = row.SubmittedAt
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to submit feedback", details = ex.Message });
        }
    }

    // GET /api/feedback/user (get feedback for current user)
    [HttpGet("user")]
    public async Task<IActionResult> GetUserFeedback()
    {
        try
        {
            var userIdClaim = HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            // Get feedback submitted by the current user
            var res = await _supabase.From<Models.Feedback>()
                .Where(f => f.UserId == userId)
                .Order("submitted_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();
            
            var feedbackList = res.Models.ToList();
            
            // Convert to DTOs to avoid serialization issues
            var feedbackDtos = feedbackList.Select(f => new FeedbackDto
            {
                Id = f.Id,
                Name = f.Name,
                Email = f.Email,
                Type = f.Type,
                Subject = f.Subject,
                Message = f.Message,
                Rating = f.Rating,
                Status = f.Status,
                SubmittedAt = f.SubmittedAt,
                AdminResponse = f.AdminResponse,
                ReviewedAt = f.ReviewedAt
            }).ToList();
            
            return Ok(feedbackDtos);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to fetch user feedback", details = ex.Message });
        }
    }

    // GET /api/feedback (admin use)
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? type)
    {
        try
        {
            // Get all feedback
            var res = await _supabase.From<Models.Feedback>().Get();
            var feedbackList = res.Models.ToList();
            
            // Filter in memory if type is specified
            if (!string.IsNullOrWhiteSpace(type))
            {
                feedbackList = feedbackList.Where(f => string.Equals(f.Type, type, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            
            // Convert to DTOs to avoid serialization issues
            var feedbackDtos = feedbackList.Select(f => new FeedbackDto
            {
                Id = f.Id,
                Name = f.Name,
                Email = f.Email,
                Type = f.Type,
                Subject = f.Subject,
                Message = f.Message,
                Rating = f.Rating,
                SubmittedAt = f.SubmittedAt
            }).ToList();
            
            return Ok(feedbackDtos);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to fetch feedback", details = ex.Message });
        }
    }

    public record SubmitFeedbackRequest(
        string? Name,
        [EmailAddress] string? Email,
        [Required] string Type,
        [Required, MinLength(3)] string Subject,
        [Required, MinLength(5)] string Message,
        int? Rating);
        
    public class FeedbackDto
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int? Rating { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public string? AdminResponse { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }
}