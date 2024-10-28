using Microsoft.AspNetCore.Mvc;
using Firebase.Database;
using Alpha_API.Models;
using Firebase.Database.Query;
using Microsoft.AspNetCore.Authorization;
using FirebaseAdmin.Auth;
using System.Security.Claims;
using Alpha_API.ViewModel;
using System.Text.Json.Serialization;
using System.Text.Json;
using Alpha_API.Utils;


[Route("api/[controller]")]
[ApiController]
//[Authorize(Policy = "AdminOnly")]

public class UsersController : ControllerBase
{
	private readonly EmailService _emailService;
	private readonly FirebaseAuth _firebaseAuth;
	private FirebaseClient _firebaseClient;
	private const string FirebaseBaseUrl = "https://sgm-management-c98cd-default-rtdb.firebaseio.com/";

	public UsersController(EmailService emailService)
	{
		_firebaseClient = new FirebaseClient(FirebaseBaseUrl);
		_emailService = emailService;
		_firebaseAuth = FirebaseAuth.DefaultInstance;
	}

	// GET: api/users
	[Authorize(Roles = "admin")]
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
			userList.Add(user.Object);
		}

		return Ok(userList);
	}

	// GET: api/users/staffs
	[Authorize(Roles = "admin")]
	[HttpGet("staffs")]
	public async Task<ActionResult<IEnumerable<User>>> GetStaffs()
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
			if (user.Object.RoleId == "-O7s8orgirC-Hqcoa7xR")
				userList.Add(user.Object);
		}

		return Ok(userList);
	}

	// GET: api/users/customers
	[Authorize(Roles = "admin,staff")]
	[HttpGet("customers")]
	public async Task<ActionResult<IEnumerable<User>>> GetCustomers()
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
			if (user.Object.RoleId == "-O7s8sU2ZMyRWjrImzCO")
				userList.Add(user.Object);
		}

		return Ok(userList);
	}

	// GET: api/users/getcurrentuser
	[Authorize(Roles = "admin,staff,customer,pt")]
	[HttpGet("GetCurrentUser")]
	public async Task<ActionResult<User>> GetCurrentUser()
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

		// Retrieve the email claim
		var email = HttpContext.User.FindFirst(ClaimTypes.Email)?.Value;

		// Retrieve the uid claim
		var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);

		if (userIdClaim == null)
		{
			return Unauthorized("User not authenticated.");
		}

		var userId = userIdClaim.Value; // This is the Firebase UID

		var user = await _firebaseClient
	.Child("users")
	.Child(userId)
	.OnceSingleAsync<User>();

		if (user == null)
		{
			return NotFound();
		}

		return Ok(user);
	}

	// GET: api/users/{id}
	[Authorize(Roles = "admin")]
	[HttpGet("{id}")]
	public async Task<ActionResult<User>> GetUserById(string id)
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

	//// POST: api/users/updatestaffinfo
	//[Authorize(Roles = "admin,staff,customer,pt")]
	//[HttpPost("updateinfo")]
	//public async Task<ActionResult> UpdateInfo([FromBody] User u)
	//{
	//	var idToken = HttpContext.Session.GetString("FirebaseIdToken");

	//	if (!string.IsNullOrEmpty(idToken))
	//	{
	//		// Use the token in your database query
	//		_firebaseClient = new FirebaseClient(FirebaseBaseUrl,
	//			new FirebaseOptions
	//			{
	//				AuthTokenAsyncFactory = () => Task.FromResult(idToken)
	//			});
	//	}

	//	var result = await _firebaseClient
	//		.Child("users")
	//	.PutAsync(u);

	//	user.UserId = result.Key; // Firebase generates a unique key
	//	return CreatedAtAction(nameof(GetUserById), new { id = result.Key }, user);
	//}

	// POST: api/users/addstaff
	[Authorize(Roles = "admin")]
	[HttpPost("addstaff")]
	public async Task<ActionResult> AddStaff([FromBody] RegisterStaffDto staff)
	{
		if (staff == null)
		{
			return BadRequest();
		}
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

		try
		{
			// Create a Firebase Auth user
			var createUserResponse = await _firebaseAuth.CreateUserAsync(new UserRecordArgs
			{
				Email = staff.Email,
				Password = staff.Password,
				DisplayName = staff.Name,
				EmailVerified = false,
				Disabled = false
			});

			if (createUserResponse == null)
			{
				return BadRequest("User registration failed.");
			}
			var userId = createUserResponse.Uid;

			// Generate email verification link
			var verificationLink = await _firebaseAuth.GenerateEmailVerificationLinkAsync(staff.Email);

			User u = new User()
			{
				Name = staff.Name,
				Email = staff.Email,
				RoleId = "-O7s8orgirC-Hqcoa7xR", // Staff role
				Phone = staff.Phone,
				UserId = userId,
				Gender = staff.Gender,
				Address = staff.Address,
				UserAvatar = staff.UserAvatar,
				IdCard = staff.IdCard,
				Dob = staff.Dob
			};

			var options = new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
			};

			var jsonString = JsonSerializer.Serialize(u, options);

			// Send the verification email using a third-party email service
			var emailSent = _emailService.SendVerificationEmail(staff.Email, verificationLink);
			if (!emailSent)
			{
				return BadRequest("Failed to send email verification.");
			}

			await _firebaseClient
	.Child("users")
	.Child(userId)
	.PutAsync(jsonString);

			return Ok("User created. Please verify your email.");
		}
		catch (FirebaseAuthException ex)
		{
			// Check if the error is related to an existing email
			if (ex.Message.Contains("EMAIL_EXISTS"))
			{
				// Return 409 Conflict status with error message
				return Conflict(new { message = "The email is already registered." });
			}

			// For other Firebase errors, return a generic internal server error
			return StatusCode(500, new { message = "An error occurred during registration." });
		}
	}

	// POST: api/users/addcustomer
	[Authorize(Roles = "admin,staff")]
	[HttpPost("addcustomer")]
	public async Task<ActionResult> AddCustomer([FromBody] RegisterStaffDto customer)
	{
		if (customer == null)
		{
			return BadRequest();
		}
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

		try
		{
			// Create a Firebase Auth user
			var createUserResponse = await _firebaseAuth.CreateUserAsync(new UserRecordArgs
			{
				Email = customer.Email,
				Password = customer.Password,
				DisplayName = customer.Name,
				EmailVerified = false,
				Disabled = false
			});

			if (createUserResponse == null)
			{
				return BadRequest("User registration failed.");
			}
			var userId = createUserResponse.Uid;

			// Generate email verification link
			var verificationLink = await _firebaseAuth.GenerateEmailVerificationLinkAsync(customer.Email);

			User u = new User()
			{
				Name = customer.Name,
				Email = customer.Email,
				RoleId = "-O7s8sU2ZMyRWjrImzCO", // Customer role
				Phone = customer.Phone,
				UserId = userId,
				Gender = customer.Gender,
				Address = customer.Address,
				UserAvatar = customer.UserAvatar,
				IdCard = customer.IdCard,
				Dob = customer.Dob
			};

			var options = new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
			};

			var jsonString = JsonSerializer.Serialize(u, options);

			// Send the verification email using a third-party email service
			var emailSent = _emailService.SendVerificationEmail(customer.Email, verificationLink);
			if (!emailSent)
			{
				return BadRequest("Failed to send email verification.");
			}

			await _firebaseClient
	.Child("users")
	.Child(userId)
	.PutAsync(jsonString);

			return Ok("User created. Please verify your email.");
		}
		catch (FirebaseAuthException ex)
		{
			// Check if the error is related to an existing email
			if (ex.Message.Contains("EMAIL_EXISTS"))
			{
				// Return 409 Conflict status with error message
				return Conflict(new { message = "The email is already registered." });
			}

			// For other Firebase errors, return a generic internal server error
			return StatusCode(500, new { message = "An error occurred during registration." });
		}
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
			user.Gender,
			user.Dob,
			user.Address,
			user.Phone,
			user.RoleId,
			user.UserAvatar,
		});

		user.UserId = result.Key; // Firebase generates a unique key
		return CreatedAtAction(nameof(GetUserById), new { id = result.Key }, user);
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
