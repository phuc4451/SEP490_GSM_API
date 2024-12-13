using Microsoft.AspNetCore.Mvc;
using Firebase.Database;
using Firebase.Database.Query;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alpha_API.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Alpha_API.ViewModel;
using Microsoft.AspNetCore.OData.Results;

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

    [HttpGet("FeedbackWithUserInfo")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<IEnumerable<Feedback>>> GetFeedbackWithUserInfo()
    {
        try
        {
            // Lấy tất cả feedback
            var feedbacks = await _firebaseClient
                .Child("Feedback")
                .OnceAsync<Feedback>();
            // Tạo danh sách để lưu kết quả
            var feedbackWithUserList = new List<FeedbackWithUserInfoDTO>();
            // Với mỗi feedback, lấy thông tin user tương ứng
            foreach (var f in feedbacks)
            {
                var feedback = f.Object;
                feedback.FeedbackId = f.Key;
                // Lấy thông tin user từ userId trong feedback
                var user = await _firebaseClient
                    .Child("users")
                    .Child(feedback.UserId)
                    .OnceSingleAsync<User>();
                if (user != null)
                {
                    user.UserId = feedback.UserId;
                }
                // Tạo đối tượng mới kết hợp feedback và user
                var feedbackWithUser = new FeedbackWithUserInfoDTO
                {
                    FeedbackId = feedback.FeedbackId,
                    UserId = feedback.UserId,
                    Message = feedback.Message,
                    Rating = feedback.Rating,
                    SubmittedAt = feedback.SubmittedAt,
                    User = user
                };
                feedbackWithUserList.Add(feedbackWithUser);
            }
            return Ok(feedbackWithUserList);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpGet("search")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<IEnumerable<FeedbackWithUserInfoDTO>>> SearchFeedbackByEmail([FromQuery] string email)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new { Message = "Email không được để trống" });
            }

            // Lấy tất cả user để tìm user có email phù hợp
            var users = await _firebaseClient
                .Child("users")
                .OnceAsync<User>();

            // Tìm user có email chứa chuỗi tìm kiếm (case-insensitive)
            var matchedUsers = users
                .Where(u => u.Object.Email.ToLower().Contains(email.ToLower()))
                .Select(u =>
                {
                    var user = u.Object;
                    user.UserId = u.Key;
                    return user;
                })
                .ToList();

            if (!matchedUsers.Any())
            {
                return NotFound(new { Message = $"Không tìm thấy feedback nào với email chứa '{email}'" });
            }

            // Lấy tất cả feedback
            var feedbacks = await _firebaseClient
                .Child("Feedback")
                .OnceAsync<Feedback>();

            // Tạo danh sách kết quả
            var result = new List<FeedbackWithUserInfoDTO>();

            // Lọc feedback của các user tìm được
            foreach (var user in matchedUsers)
            {
                var userFeedbacks = feedbacks
                    .Where(f => f.Object.UserId == user.UserId)
                    .Select(f =>
                    {
                        var feedback = f.Object;
                        feedback.FeedbackId = f.Key;
                        return new FeedbackWithUserInfoDTO
                        {
                            FeedbackId = feedback.FeedbackId,
                            UserId = feedback.UserId,
                            Message = feedback.Message,
                            Rating = feedback.Rating,
                            SubmittedAt = feedback.SubmittedAt,
                            User = user
                        };
                    });

                result.AddRange(userFeedbacks);
            }

            var orderedResult = result.OrderByDescending(f => f.SubmittedAt).ToList();

            if (!orderedResult.Any())
            {
                return NotFound(new { Message = $"Người dùng với email chứa '{email}' chưa có feedback nào" });
            }

            return Ok(new
            {
                Message = $"Tìm thấy {orderedResult.Count} feedback",
                Data = orderedResult
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = $"Lỗi server: {ex.Message}" });
        }
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

    //public async  Task<ActionResult<bool>> 

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
