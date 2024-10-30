using Microsoft.AspNetCore.Mvc;
using Firebase.Database;
using Firebase.Database.Query;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alpha_API.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

[Route("api/[controller]")]
[ApiController]
public class FeedbackController : ControllerBase
{
    private readonly FirebaseClient _firebaseClient;

    public FeedbackController()
    {
        _firebaseClient = new FirebaseClient("https://sgm-management-c98cd-default-rtdb.firebaseio.com/");
    }

    // GET: api/feedback
    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<IEnumerable<Feedback>>> GetAllFeedback()
    {
        var feedbacks = await _firebaseClient
            .Child("Feedback")
            .OnceAsync<Feedback>();

        var feedbackList = feedbacks.Select(f =>
        {
            var fb = f.Object;
            fb.FeedbackId = f.Key; // Gán FeedbackId là khóa Firebase
            return fb;
        }).ToList();

        return Ok(feedbackList);
    }

    // POST: api/feedback
    [HttpPost]
    [Authorize(Roles = "customer")]
    public async Task<ActionResult<Feedback>> PostFeedback([FromBody] Feedback feedback)
    {
        var userId = HttpContext.User.Claims.FirstOrDefault(c => c.Type == 
        ClaimTypes.NameIdentifier)?.Value;
        feedback.SubmittedAt = DateTime.Now;
        feedback.UserId = userId;

        var feed = new
        {
            feedback.UserId,
            feedback.Rating,
            feedback.Message,
            feedback.SubmittedAt,
        };

        var result = await _firebaseClient
            .Child("Feedback")
            .PostAsync(feed);

        feedback.FeedbackId = result.Key;
        return CreatedAtAction(nameof(GetFeedbackById), new { id = feedback.FeedbackId }, feedback);
    }

    // GET: api/feedback/{id}
    [HttpGet("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<Feedback>> GetFeedbackById(string id)
    {
        var feedback = await _firebaseClient
            .Child("Feedback")
            .Child(id)
            .OnceSingleAsync<Feedback>();

        if (feedback == null)
        {
            return NotFound();
        }

        feedback.FeedbackId = id;
        return Ok(feedback);
    }

    // DELETE: api/feedback/{id}
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DeleteFeedback(string id)
    {
        var existingFeedback = await _firebaseClient
            .Child("Feedback")
            .Child(id)
            .OnceSingleAsync<Feedback>();

        if (existingFeedback == null)
        {
            return NotFound();
        }

        await _firebaseClient
            .Child("Feedback")
            .Child(id)
            .DeleteAsync();

        return NoContent();
    }
}
