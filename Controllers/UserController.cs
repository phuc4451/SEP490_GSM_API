using Microsoft.AspNetCore.Mvc;
using Firebase.Database;
using Alpha_API.Models;
using Firebase.Database.Query;
using Microsoft.AspNetCore.Authorization;
using FirebaseAdmin.Auth;


[Route("api/[controller]")]
[ApiController]
//[Authorize(Policy = "AdminOnly")]
[Authorize(Roles = "admin")]
public class UsersController : ControllerBase
{
	private FirebaseClient _firebaseClient;
	private const string FirebaseBaseUrl = "https://sgm-management-c98cd-default-rtdb.firebaseio.com/";

	public UsersController()
	{
		_firebaseClient = new FirebaseClient(FirebaseBaseUrl);
	}

	// GET: api/users

	[HttpGet]
	public async Task<ActionResult<IEnumerable<User>>> GetUsers()
	{
		var idToken = HttpContext.Session.GetString("FirebaseIdToken");

		if (!string.IsNullOrEmpty(idToken))
		{
			// Use the token in your database query
			_firebaseClient = new FirebaseClient(FirebaseBaseUrl,
				new FirebaseOptions
				{
					AuthTokenAsyncFactory = () => Task.FromResult(idToken)
				});
		}

		var users = await _firebaseClient
			.Child("users")
			.OnceAsync<User>();

		var userList = new List<User>();
		foreach (var user in users)
		{
			user.Object.UserId = user.Key;
			userList.Add(user.Object);
		}

		return Ok(userList);
	}

	// POST: api/users
	[HttpPost]
	public async Task<ActionResult<User>> PostUser([FromBody] User user)
	{
		if (user == null)
		{
			return BadRequest();
		}

		var result = await _firebaseClient
			.Child("users")
		.PostAsync(new
		{
			//user.UserId,
			user.Name,
			user.Email,
			user.Password,
			user.Gender,
			user.Dob,
			user.Address,
			user.Phone,
			user.RoleId,
			user.UserAvatar,
		});

		//user.UserId = result.Key; // Firebase generates a unique key
		return CreatedAtAction(nameof(GetUserById), new { id = result.Key }, user);
	}

	// GET: api/users/{id}
	[HttpGet("{id}")]
	public async Task<ActionResult<User>> GetUserById(string id)
	{
		var user = await _firebaseClient
			.Child("users")
			.Child(id)
			.OnceSingleAsync<User>();

		user.UserId = id;

		if (user == null)
		{
			return NotFound();
		}

		return Ok(user);
	}

	// PUT: api/users/{id}
	[HttpPut("{id}")]
	public async Task<ActionResult> PutUser(string id, [FromBody] User user)
	{
		if (id != user.UserId)
		{
			return BadRequest();
		}

		await _firebaseClient
			.Child("users")
			.Child(id)
			.PutAsync(user);

		return NoContent(); // 204 No Content
	}

	// PATCH: api/users/{id}
	[HttpPatch("{id}")]
	public async Task<ActionResult> PatchUser(string id, [FromBody] User user)
	{
		var existingUser = await _firebaseClient
			.Child("users")
			.Child(id)
			.OnceSingleAsync<User>();

		if (existingUser == null)
		{
			return NotFound();
		}

		//// Update fields selectively
		//if (!string.IsNullOrEmpty(user.UserEmail))
		//{
		//	existingUser.UserEmail = user.UserEmail;
		//}

		await _firebaseClient
			.Child("users")
			.Child(id)
			.PutAsync(existingUser);

		return NoContent(); // 204 No Content
	}

	// DELETE: api/users/{id}
	[HttpDelete("{id}")]
	public async Task<ActionResult> DeleteUser(string id)
	{
		var existingUser = await _firebaseClient
			.Child("users")
			.Child(id)
			.OnceSingleAsync<User>();

		if (existingUser == null)
		{
			return NotFound();
		}

		await _firebaseClient
			.Child("users")
			.Child(id)
			.DeleteAsync();

		return NoContent(); // 204 No Content
	}
}
