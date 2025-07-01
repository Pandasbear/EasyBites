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
    [HttpPost]
    public async Task<IActionResult> Submit(SubmitFeedbackRequest request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var feedback = new Models.Feedback
        {
            Name = request.Name,
            Email = request.Email,
            Type = request.Type,
            Subject = request.Subject,
            Message = request.Message,
            Rating = request.Rating,
            SubmittedAt = DateTime.UtcNow
        };

        var insertResp = await _supabase.From<Models.Feedback>().Insert(feedback);
        var row = insertResp.Models.FirstOrDefault();
        return Created($"/api/feedback/{row?.Id}", row);
    }

    // GET /api/feedback (admin use)
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? type)
    {
        dynamic qry = _supabase.From<Models.Feedback>();
        if (!string.IsNullOrWhiteSpace(type))
            qry = qry.Filter("type", Constants.Operator.Equals, type);
        var res = await qry.Get();
        return Ok(res.Models);
    }

    public record SubmitFeedbackRequest(
        string? Name,
        [EmailAddress] string? Email,
        [Required] string Type,
        [Required, MinLength(3)] string Subject,
        [Required, MinLength(10)] string Message,
        int? Rating);
} 