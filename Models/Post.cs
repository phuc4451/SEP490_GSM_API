namespace Alpha_API.Models
{
    public class Post
    {
        public string PostId { get; set; }           
        public string Title { get; set; }            
        public string ThumbnailUrl { get; set; }     
        public DateTime Date { get; set; }           
        public string UserId { get; set; }           
        public string Content { get; set; }          
        public string CategoryId { get; set; }
    }
}
