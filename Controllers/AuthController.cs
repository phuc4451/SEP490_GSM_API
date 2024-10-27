using Alpha_API.Models;
using Alpha_API.ViewModel;
using Firebase.Database;
using Firebase.Database.Query;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Net;
using System.Security.Claims;
using System.Text;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Text.Json;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
	private readonly IConfiguration _configuration;
	private FirebaseClient _firebaseClient;
	private readonly FirebaseAuth _firebaseAuth;
	private static readonly HttpClient _httpClient = new HttpClient();
	private const string FirebaseApiKey = "AIzaSyDuRSqvyEO2Do04yj716Jq67e_iOcrvfNo";  // Replace with your Firebase API Key
	private const string FirebaseAuthUrl = "https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key=";
	private const string FirebaseBaseUrl = "https://sgm-management-c98cd-default-rtdb.firebaseio.com/";

	public AuthController(IConfiguration configuration)
	{
		_configuration = configuration;

		//// Initialize Firebase client (read/write permissions now open)
		_firebaseClient = new FirebaseClient(FirebaseBaseUrl);
		_firebaseAuth = FirebaseAuth.DefaultInstance;
	}

	[HttpPost("register")]
	public async Task<ActionResult> Register([FromBody] RegisterUserDto registerUserDto)
	{
		try
		{
			// Create a Firebase Auth user
			var createUserResponse = await _firebaseAuth.CreateUserAsync(new UserRecordArgs
			{
				Email = registerUserDto.Email,
				Password = registerUserDto.Password,
				DisplayName = registerUserDto.Name,
				EmailVerified = false,
				Disabled = false
			});

			if (createUserResponse == null)
			{
				return BadRequest("User registration failed.");
			}

			var userId = createUserResponse.Uid;

			// Generate email verification link
			var verificationLink = await _firebaseAuth.GenerateEmailVerificationLinkAsync(registerUserDto.Email);

			var roles =await GetAllRoles();

			User u = new User()
			{
				Name=registerUserDto.Name,
				Email = registerUserDto.Email,
				RoleId= "-O7s8sU2ZMyRWjrImzCO", // Customer role
				Phone="empty",
				UserId=userId,
				Gender="Male",
				Address="empty",
				UserAvatar="",
				IdCard= new CardId()
				{
					Id="empty"
				},
				Dob= new CustomDateTime()
				{

				}
			};
			u.Dob = u.MapDateTimeToCustomFormat(DateTime.Now);

			//// Save user details to Firebase Realtime Database
			//var user = new
			//{
			//	name = u.Name,
			//	email = u.Email,
			//	roleId = u.RoleId,
			//	phone = u.Phone,
			//	userId=u.UserId,
			//	gender = u.Gender,
			//	address = u.Address,
			//	userAvatar= "",
			//	idCard= u.IdCard,
			//	dob= u.Dob
			//};

			var options = new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
			};

			var jsonString = System.Text.Json.JsonSerializer.Serialize(u, options);

			// Send the verification email using a third-party email service
			var emailSent = SendVerificationEmail(registerUserDto.Email, verificationLink);
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


	private bool SendVerificationEmail(string email, string verificationLink)
	{
		try
		{
			MailMessage mail = new MailMessage();
			mail.From = new MailAddress("phucvu159753@gmail.com");
			mail.To.Add(email);
			mail.Subject = "Verify your email";
			mail.Body = $"Please verify your email by clicking on the link: {verificationLink}";

			SmtpClient smtpServer = new SmtpClient("smtp.gmail.com");  // Replace with your SMTP server
			smtpServer.Port = 587;  // or other port depending on your SMTP server
			smtpServer.Credentials = new NetworkCredential("phucvu159753@gmail.com", "tlpmzxpedrnlkfnn");  // Replace with your email credentials
			smtpServer.EnableSsl = true;

			smtpServer.Send(mail);
			return true;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error sending email: {ex.Message}");
			return false;
		}
	}


	//[HttpPost("login")]
	//public async Task<ActionResult> Login([FromBody] LogUserDto logUser)
	//{
	//	// Get the user record from Firebase Auth
	//	var firebaseUser = await _firebaseAuth.GetUserByEmailAsync(logUser.Email);

	//	// Query Firebase Realtime Database for user information

	//	//var userQuery = await _firebaseClient
	//	//	.Child("Users")
	//	//	.OrderBy("Email")
	//	//	.EqualTo(logUser.Email)
	//	//	.OnceAsync<User>();

	//	var userQuery = await _firebaseClient
	//.Child("Users")
	//.Child(firebaseUser.Uid)
	//.OnceAsync<User>();

	//	if (userQuery.Any())
	//	{
	//		var existingUser = userQuery.First().Object;
	//		if (true)
	//		{
	//			//check if email is verified
	//			if (firebaseUser.EmailVerified)
	//			{
	//				// Email is verified, proceed with login
	//				var roleName = await GetRoleNameFromFirebase(existingUser.RoleId);
	//				var token = GenerateJwtToken(existingUser.Email, roleName);

	//				return Ok(new { token });
	//			}
	//			else
	//			{
	//				// Email is not verified
	//				return BadRequest("Please verify your email before logging in.");
	//			}
	//		}

	//	}
	//	// If no matching user or password is incorrect
	//	return Unauthorized();

	//	////await _firebaseAuth.


	//	//	// Ensure password matches
	//	//	if (logUser.Password == existingUser.Password)
	//	//	{

	//	//	}
	//	//}


	//}


	[HttpPost("login")]
	public async Task<ActionResult> Login([FromBody] LogUserDto logUser)
	{
		var requestUrl = $"{FirebaseAuthUrl}{FirebaseApiKey}";

		var requestBody = new
		{
			email = logUser.Email,
			password = logUser.Password,
			returnSecureToken = true
		};

		var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

		try
		{
			var response = await _httpClient.PostAsync(requestUrl, content);

			if (!response.IsSuccessStatusCode)
			{
				var errorResponse = await response.Content.ReadAsStringAsync();
				return BadRequest(new { message = "Error signing in", details = errorResponse });
			}

			var responseBody = await response.Content.ReadAsStringAsync();
			var signInResponse = JsonConvert.DeserializeObject<FirebaseSignInResponse>(responseBody);

			// After signing in and receiving the ID token:
			HttpContext.Session.SetString("FirebaseIdToken", signInResponse.IdToken);

			var decodedToken = await _firebaseAuth.VerifyIdTokenAsync(signInResponse.IdToken);
			var uid = decodedToken.Uid;


			_firebaseClient = new FirebaseClient(FirebaseBaseUrl,
				new FirebaseOptions
				{
					AuthTokenAsyncFactory = () => Task.FromResult(signInResponse.IdToken)
				});

			var firebaseUser = await _firebaseClient
				.Child("users")
				.Child(uid)
				.OnceSingleAsync<Dictionary<string, object>>();


			// Now you can access fields dynamically
			if (firebaseUser.ContainsKey("roleId"))
			{
				var roleId = firebaseUser["roleId"].ToString();

				var existingUser = await _firebaseAuth.GetUserByEmailAsync(signInResponse.Email);

				//check if email is verified
				if (existingUser.EmailVerified)
				{
					// Email is verified, proceed with login
					var roleName = await GetRoleNameFromFirebase(roleId);
					var token = GenerateJwtToken(existingUser.Email, roleName);

					return Ok(new { JWTtoken = token, FirebaseToken = signInResponse.IdToken });
				}
				else
				{
					// Email is not verified
					return BadRequest("Please verify your email before logging in.");
				}


				//// Return the Firebase ID token, and additional details as JSON result
				//return Ok(new
				//{
				//	token = signInResponse.IdToken,
				//	email = signInResponse.Email,
				//	refreshToken = signInResponse.RefreshToken,
				//	expiresIn = signInResponse.ExpiresIn
				//});
			}

			return StatusCode(400, new { message = "No role found" });

		}
		catch (Exception ex)
		{
			return StatusCode(500, new { message = "Exception occurred during sign-in", details = ex.Message });
		}
	}


	// GET: api/Users/GetUserIdByEmail?email=user@example.com
	[HttpGet("GetUserIdByEmail")]
	public async Task<IActionResult> GetUserIdByEmail(string email)
	{
		try
		{
			// Use FirebaseAuth to get the user by email
			UserRecord userRecord = await _firebaseAuth.GetUserByEmailAsync(email);

			// Return the userId (Firebase UID)
			return Ok(new { userId = userRecord.Uid });
		}
		catch (FirebaseAuthException ex)
		{
			// Handle cases where the user is not found
			if (ex.AuthErrorCode == AuthErrorCode.UserNotFound)
			{
				return NotFound("User with the specified email not found.");
			}

			// Handle other Firebase authentication errors
			return BadRequest(ex.Message);
		}
	}

	private string GenerateJwtToken(string email, string role)
	{
		var claims = new[]
		{
			//new Claim(JwtRegisteredClaimNames.Sub, email),

			 new Claim(ClaimTypes.Email, email),  // Use ClaimTypes.Email for email claim
			new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
			new Claim(ClaimTypes.Role, role)  // Include the user's role in the token
        };

		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
		var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

		var token = new JwtSecurityToken(
			issuer: _configuration["Jwt:Issuer"],
			audience: _configuration["Jwt:Audience"],
			claims: claims,
			expires: DateTime.Now.AddMinutes(30),
			signingCredentials: creds);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}

	[HttpGet("role/{roleId}")]
	public async Task<string> GetRoleNameFromFirebase(string? roleId)
	{
		// Directly access the role using the unique key (roleId)
		var roleQuery = await _firebaseClient
			.Child("Roles")
			.Child(roleId) // Use roleId directly as the key
			.OnceSingleAsync<Role>();

		// If the role is found, return its name; otherwise, default to "guest"
		return roleQuery?.RoleName ?? "guest"; // Default to "guest" if no match found
	}


	[HttpPost("logout")]
	public IActionResult Logout()
	{
		return Ok(new { message = "Logged out successfully" });
	}

	[HttpGet("roles")]
	public async Task<ActionResult<List<Role>>> GetAllRoles()
	{
		// Query Firebase to get all roles from the "Roles" node
		var roleQuery = await _firebaseClient
			.Child("Roles")
			.OnceAsync<Role>();

		// If there are no roles in the database, return an empty list
		if (roleQuery == null || !roleQuery.Any())
		{
			return NotFound("No roles found.");
		}

		// Convert the Firebase query results into a list of roles
		var roles = roleQuery.Select(item => new Role
		{
			RoleId = item.Key,
			RoleName = item.Object.RoleName
		}).ToList();

		return Ok(roles);
	}

}

public class FirebaseSignInResponse
{
	[JsonProperty("idToken")]
	public string IdToken { get; set; }

	[JsonProperty("email")]
	public string Email { get; set; }

	[JsonProperty("refreshToken")]
	public string RefreshToken { get; set; }

	[JsonProperty("expiresIn")]
	public string ExpiresIn { get; set; }

	[JsonProperty("localId")]
	public string LocalId { get; set; }

	[JsonProperty("registered")]
	public bool Registered { get; set; }
}
