using Alpha_API.Models;

namespace Alpha_API.ViewModel
{
    public class FeedbackWithUserInfoDTO
    {
        public string FeedbackId { get; set; }
        public string UserId { get; set; }
        public string Message { get; set; }
        public int Rating { get; set; }
        public DateTime SubmittedAt { get; set; }
        public User User { get; set; }
    }
}
