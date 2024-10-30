using Alpha_API.Models;
using Firebase.Database;
using Firebase.Storage;
using Firebase.Database.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Alpha_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostsController : ControllerBase
    {
        private readonly FirebaseClient _firebaseClient;

        public PostsController()
        {
            _firebaseClient = new FirebaseClient("https://sgm-management-c98cd-default-rtdb.firebaseio.com/"); // Firebase URL
        }

        // 1. Lấy danh sách tất cả danh mục
        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<PostCategory>>> GetCategories()
        {
            var categories = await _firebaseClient
                .Child("post_category")
                .OnceAsync<PostCategory>();

            var categoryList = categories.Select(c => new PostCategory
            {
                CategoryId = c.Key,
                Name = c.Object.Name
            }).ToList();

            return Ok(categoryList);
        }

        // 2. Lấy danh sách tất cả bài viết (cho RecyclerView)
        [HttpGet("posts")]
        public async Task<ActionResult<IEnumerable<Post>>> GetPosts()
        {
            var posts = await _firebaseClient
                .Child("posts")
                .OnceAsync<Post>();

            var postList = posts.Select(p => new Post
            {
                PostId = p.Key,
                Title = p.Object.Title,
                ThumbnailUrl = p.Object.ThumbnailUrl,
                Date = p.Object.Date,
                UserId = p.Object.UserId,
                Content = null,  // Không trả về content để tối ưu hóa cho danh sách
                CategoryId = p.Object.CategoryId
            }).ToList();

            return Ok(postList);
        }
        // 2. Lấy 5 bài viết mới nhất
        [HttpGet("posts/latest")]
        public async Task<ActionResult<IEnumerable<Post>>> GetLatestPosts()
        {
            var posts = await _firebaseClient
                .Child("posts")
                .OnceAsync<Post>();

            // Sắp xếp các bài viết theo Date giảm dần và lấy 5 bài viết mới nhất
            var latestPosts = posts
                .OrderByDescending(p => p.Object.Date)
                .Take(5)
                .Select(p => new Post
                {
                    PostId = p.Key,
                    Title = p.Object.Title,
                    ThumbnailUrl = p.Object.ThumbnailUrl,
                    Date = p.Object.Date,
                    UserId = p.Object.UserId,
                    Content = null,  // Không trả về content để tối ưu hóa cho danh sách
                    CategoryId = p.Object.CategoryId
                }).ToList();

            return Ok(latestPosts);
        }


        // 3. Lấy chi tiết một bài viết
        [HttpGet("posts/{postId}")]
        public async Task<ActionResult<Post>> GetPostById(string postId)
        {
            var post = await _firebaseClient
                .Child("posts")
                .Child(postId)
                .OnceSingleAsync<Post>();

            if (post == null)
            {
                return NotFound();
            }

            post.PostId = postId;
            return Ok(post);
        }

        // 4. Đăng bài viết mới
        [HttpPost("posts")]
        public async Task<ActionResult<Post>> CreatePostWithImage([FromForm] IFormFile image, [FromForm] string title, [FromForm] string content, [FromForm] string categoryId)
        {
            if (image == null || image.Length == 0)
            {
                return BadRequest("Image file is required.");
            }

            // Lưu ảnh vào Firebase Storage và nhận URL
            string imageUrl;
            using (var stream = image.OpenReadStream())
            {
                var task = new FirebaseStorage("sgm-management-c98cd.appspot.com")
                    .Child("post_images")                    // Thư mục lưu ảnh
                    .Child(image.FileName)                   // Tên file ảnh
                    .PutAsync(stream);

                imageUrl = await task;                       // URL của ảnh sau khi lưu
            }

            // Tạo bài viết mới với thông tin và URL ảnh
            var post = new Post
            {
                Title = title,
                ThumbnailUrl = imageUrl,
                Date = DateTime.UtcNow,
                UserId = HttpContext.User.Identity.Name,      // Giả định UserId từ User đang đăng nhập
                Content = content,
                CategoryId = categoryId
            };

            var result = await _firebaseClient
                .Child("posts")
                .PostAsync(post);

            post.PostId = result.Key;

            return CreatedAtAction(nameof(GetPostById), new { postId = post.PostId }, post);
        }

        // 5. Xóa bài viết
        [HttpDelete("posts/{postId}")]
        public async Task<IActionResult> DeletePost(string postId)
        {
            var post = await _firebaseClient
                .Child("posts")
                .Child(postId)
                .OnceSingleAsync<Post>();

            if (post == null)
            {
                return NotFound();
            }

            await _firebaseClient
                .Child("posts")
                .Child(postId)
                .DeleteAsync();

            return NoContent(); // 204 No Content
        }

        // 6. Cập nhật bài viết
        [HttpPut("posts/{postId}")]
        public async Task<IActionResult> UpdatePost(string postId, [FromBody] Post updatedPost)
        {
            var post = await _firebaseClient
                .Child("posts")
                .Child(postId)
                .OnceSingleAsync<Post>();

            if (post == null)
            {
                return NotFound();
            }

            updatedPost.PostId = postId;
            updatedPost.Date = post.Date;  // Giữ nguyên ngày tạo ban đầu

            await _firebaseClient
                .Child("posts")
                .Child(postId)
                .PutAsync(updatedPost);

            return NoContent(); // 204 No Content
        }
    }
}
