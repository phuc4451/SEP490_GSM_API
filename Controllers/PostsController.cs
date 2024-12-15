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
using Alpha_API.Services;

namespace Alpha_API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class PostsController : ControllerBase
	{
		private FirebaseClient _firebaseClient;
		private readonly FirebaseClientProvider _firebaseClientProvider;

		public PostsController(FirebaseClientProvider firebaseClientProvider, FirebaseClient firebaseClient)
		{
			_firebaseClient = firebaseClient;
			_firebaseClientProvider = firebaseClientProvider;
		}

		// 1. Lấy danh sách tất cả danh mục
		[HttpGet("categories")]
		public async Task<ActionResult<IEnumerable<PostCategory>>> GetCategories()
		{
			var categories = await _firebaseClient
				.Child("post_category")
				.OnceAsync<PostCategory>();

			// Filter out any categories with a null or empty Name
			var categoryList = categories
				.Where(c => !string.IsNullOrEmpty(c.Object.Name)) // Filter for non-null, non-empty names
				.Select(c => new PostCategory
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
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			var posts = await _firebaseClient
				.Child("Posts")
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
				.Child("Posts")
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
		public async Task<ActionResult> GetPostById(string postId)
		{
			try
			{
				// Fetch the post by ID
				var post = await _firebaseClient
					.Child("Posts")
					.Child(postId)
					.OnceSingleAsync<Post>();

				if (post == null)
				{
					return NotFound($"Post with ID {postId} not found.");
				}

				// Fetch the user's information
				var user = await _firebaseClient
					.Child("users") // Đảm bảo rằng bạn đang truy cập đúng node
					.Child(post.UserId) // Lấy userId từ post
					.OnceSingleAsync<User>();

				if (user == null)
				{
					return NotFound($"User with ID {post.UserId} not found.");
				}

				// Prepare a custom response including post details and the author's name
				var response = new
				{
					PostId = post.PostId,
					Title = post.Title,
					ThumbnailUrl = post.ThumbnailUrl,
					Date = post.Date,
					Content = post.Content,
					CategoryId = post.CategoryId,
					UserId = post.UserId,
					AuthorName = user.Name // Lấy tên từ đối tượng user
				};

				return Ok(response);
			}
			catch (FirebaseException ex)
			{
				// Trả về thông báo lỗi chi tiết
				return BadRequest($"Firebase error: {ex.Message}");
			}
			catch (Exception ex)
			{
				// Xử lý các ngoại lệ khác
				return BadRequest($"General error: {ex.Message}");
			}
		}


        [HttpPost("posts")]
        public async Task<ActionResult<Post>> CreatePost([FromForm] string title, [FromForm] string content, [FromForm] string categoryId)
        {
            try
            {
                // Kiểm tra các tham số bắt buộc
                if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content) || string.IsNullOrWhiteSpace(categoryId))
                {
                    return BadRequest("Title, content, and categoryId are required fields.");
                }

                // Tạo bài viết mới với thông tin nhận được
                var post = new Post
                {
                    Title = title,
                    ThumbnailUrl = null, // Không có ảnh, để trống URL ảnh
                    Date = DateTime.UtcNow, // Lưu trữ dưới dạng DateTime
                    UserId = HttpContext.User.Identity.Name, // Lấy UserId từ người dùng đang đăng nhập
                    Content = content,
                    CategoryId = categoryId
                };

                // Lưu bài viết vào Firebase Realtime Database
                var result = await _firebaseClient
                    .Child("posts")
                    .PostAsync(post);

                // Thiết lập PostId cho bài viết mới tạo
                post.PostId = result.Key;

                // Trả về thông báo thành công và thông tin bài viết vừa tạo
                return CreatedAtAction(nameof(GetPostById), new { postId = post.PostId }, post);
            }
            catch (Exception ex)
            {
                // Trả về thông báo lỗi chi tiết nếu có lỗi
                return StatusCode(500, $"Error creating post: {ex.Message}");
            }
        }




        [HttpDelete("posts/{postId}")]
        public async Task<IActionResult> DeletePost(string postId)
        {
            var post = await _firebaseClient
                .Child("Posts")
                .Child(postId)
                .OnceSingleAsync<Post>();

            if (post == null)
            {
                return NotFound(new { message = "Post not found" });
            }

            try
            {
                await _firebaseClient
                    .Child("Posts")
                    .Child(postId)
                    .DeleteAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the post", error = ex.Message });
            }

            return Ok(new { message = "Post deleted successfully", postId = postId });
        }



        // 6. Cập nhật bài viết
        [HttpPut("posts/{postId}")]
        public async Task<IActionResult> UpdatePost(string postId, [FromBody] Post updatedPost)
        {
            // Lấy thông tin bài đăng từ Firebase
            var post = await _firebaseClient
                .Child("Posts")
                .Child(postId)
                .OnceSingleAsync<Post>();

            // Nếu không tìm thấy bài đăng, trả về 404
            if (post == null)
            {
                return NotFound(new { message = "Post not found" });
            }

            // Cập nhật thông tin bài đăng
            updatedPost.PostId = postId; // Giữ nguyên PostId
            updatedPost.Date = post.Date; // Giữ nguyên ngày tạo ban đầu

            try
            {
                // Cập nhật bài đăng vào Firebase
                await _firebaseClient
                    .Child("Posts")
                    .Child(postId)
                    .PutAsync(updatedPost);
            }
            catch (Exception ex)
            {
                // Nếu có lỗi khi cập nhật, trả về lỗi 500
                return StatusCode(500, new { message = "An error occurred while updating the post", error = ex.Message });
            }

            // Trả về thông tin bài đăng đã cập nhật
            return Ok(new { message = "Post updated successfully", updatedPost });
        }


        // 7. Search posts by category
        [HttpGet("posts/category/{categoryId}")]
		public async Task<ActionResult<IEnumerable<Post>>> GetPostsByCategory(string categoryId)
		{
			var posts = await _firebaseClient
				.Child("Posts")
				.OnceAsync<Post>();

			// Filter posts by categoryId
			var filteredPosts = posts
				.Where(p => p.Object.CategoryId == categoryId)
				.Select(p => new Post
				{
					PostId = p.Key,
					Title = p.Object.Title,
					ThumbnailUrl = p.Object.ThumbnailUrl,
					Date = p.Object.Date,
					UserId = p.Object.UserId,
					Content = null,  // Exclude content for optimization
					CategoryId = p.Object.CategoryId
				})
				.ToList();

			return Ok(filteredPosts);
		}

		[HttpGet("posts/user/{userId}")]
		public async Task<ActionResult<IEnumerable<Post>>> GetPostsByUserId(string userId)
		{
			try
			{
				// Truy vấn Firebase để lấy tất cả bài viết
				var posts = await _firebaseClient
					.Child("Posts")
					.OnceAsync<Post>();

				// Lọc bài viết theo `UserId` và chọn các trường cần thiết
				var userPosts = posts
					.Where(p => p.Object.UserId == userId)
					.Select(p => new Post
					{
						PostId = p.Key,
						Title = p.Object.Title,
						ThumbnailUrl = p.Object.ThumbnailUrl,
						Date = p.Object.Date,
						UserId = p.Object.UserId,
						CategoryId = p.Object.CategoryId
						// Có thể thêm các trường khác nếu cần
					})
					.ToList();

				return Ok(userPosts);
			}
			catch (FirebaseException ex)
			{
				// Xử lý lỗi Firebase
				return BadRequest($"Firebase error: {ex.Message}");
			}
			catch (Exception ex)
			{
				// Xử lý các lỗi chung
				return BadRequest($"Error: {ex.Message}");
			}
		}

		//commet 12345

	}
}
