using Alpha_API.Models;
using Alpha_API.Services;
using Alpha_API.Utils;
using Alpha_API.ViewModel;
using DocumentFormat.OpenXml.Spreadsheet;
using Firebase.Database;
using Firebase.Database.Query;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Alpha_API.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class AuthController : ControllerBase
	{
		private readonly IConfiguration _configuration;
		private readonly EmailService _emailService;
		private FirebaseClient _firebaseClient;
		private readonly FirebaseAuth _firebaseAuth;
		private readonly FirebaseClientProvider _firebaseClientProvider;
		private static readonly HttpClient _httpClient = new HttpClient();
		private readonly string _firebaseAuthUrl;
		private readonly string _firebaseApiKey;
		private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new ConcurrentDictionary<string, SemaphoreSlim>();

		public AuthController(IConfiguration configuration, EmailService emailService, FirebaseClientProvider firebaseClientProvider, FirebaseClient firebaseClient, FirebaseAuth firebaseAuth)
		{
			_configuration = configuration;
			_firebaseAuthUrl = configuration["Firebase:AuthUrl"];
			_firebaseApiKey = configuration["Firebase:ApiKey"];

			//// Initialize Firebase client (read/write permissions now open)
			_firebaseClient = firebaseClient;
			_firebaseAuth = firebaseAuth;
			_emailService = emailService;
			_firebaseClientProvider = firebaseClientProvider;
		}

		[HttpPost("register")]
		public async Task<ActionResult> Register([FromBody] RegisterUserDto registerUserDto)
		{
			var emailLock = _locks.GetOrAdd(registerUserDto.Email, _ => new SemaphoreSlim(1, 1));

			await emailLock.WaitAsync(); // Wait for the email-specific semaphore
			try
			{
				//// Check if the email already exists
				//var existingUser = await _firebaseAuth.GetUserByEmailAsync(registerUserDto.Email);
				//if (existingUser != null)
				//{
				//	return Conflict(new { message = "The email is already registered." });
				//}

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

				var roles = await GetAllRoles();

				User u = new User()
				{
					Name = registerUserDto.Name,
					Email = registerUserDto.Email,
					RoleId = "-O7s8sU2ZMyRWjrImzCO", // Customer role
					Phone = "empty",
					UserId = userId,
					Gender = "male",
					Address = "empty",
					UserAvatar = "",
					//IdCard= new CardId()
					//{
					//	Id="empty"
					//},
					IdCard = "empty",
					Dob = new CustomDateTime()
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
				var emailSent = _emailService.SendVerificationEmail(registerUserDto.Email, verificationLink);

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
			finally
			{
				emailLock.Release(); // Release the lock for this email
									 // Clean up unused locks
				if (emailLock.CurrentCount == 1) // No threads waiting
				{
					_locks.TryRemove(registerUserDto.Email, out _);
				}
			}
		}

		[HttpPost("login")]
		public async Task<ActionResult> Login([FromBody] LogUserDto logUser)
		{
			var requestUrl = $"{_firebaseAuthUrl}{_firebaseApiKey}";

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

				_firebaseClient = _firebaseClientProvider.GetFirebaseClient();


				//_firebaseClient = new FirebaseClient(_firebaseBaseUrl,
				//	new FirebaseOptions
				//	{
				//		AuthTokenAsyncFactory = () => Task.FromResult(signInResponse.IdToken)
				//	});

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
						var token = GenerateJwtToken(existingUser.Email, roleName, uid);

						return Ok(new { jwTtoken = token, firebaseToken = signInResponse.IdToken });
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

		[HttpPost("loginWithFirebaseToken")]
		public async Task<IActionResult> LoginWithFirebaseToken([FromBody] string firebaseToken)
		{
			// Verify Firebase ID token
			FirebaseToken decodedToken;
			try
			{
				decodedToken = await _firebaseAuth.VerifyIdTokenAsync(firebaseToken);
			}
			catch (Exception ex)
			{
				return Unauthorized("Invalid Firebase ID token");
			}

			// After signing in and receiving the ID token:
			HttpContext.Session.SetString("FirebaseIdToken", firebaseToken);

			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			// Extract user info
			string uid = decodedToken.Uid;

			var firebaseUser = await _firebaseClient
				.Child("users")
				.Child(uid)
				.OnceSingleAsync<Dictionary<string, object>>();

			// Now you can access fields dynamically
			if (firebaseUser.ContainsKey("roleId"))
			{
				var roleId = firebaseUser["roleId"].ToString();

				//var existingUser = await _firebaseAuth.GetUserAsync(uid);

				var email = decodedToken.Claims["email"]?.ToString();

				var verified = (bool)decodedToken.Claims["email_verified"];

				if (verified)
				{
					// Email is verified, proceed with login
					var roleName = await GetRoleNameFromFirebase(roleId);
					var jwtToken = GenerateJwtToken(email, roleName, uid);

					return Ok(new { jwTtoken = jwtToken });
				}
				else
				{
					// Email is not verified
					return BadRequest("Please verify your email before logging in.");
				}
			}

			return StatusCode(400, new { message = "No role found" });

			
		}

		private string GenerateJwtToken(string email, string role, string uid)
		{
			var claims = new[]
			{
			//new Claim(JwtRegisteredClaimNames.Sub, email),

			 new Claim(ClaimTypes.Email, email),  // Use ClaimTypes.Email for email claim
			 new Claim(ClaimTypes.NameIdentifier, uid),  // Use ClaimTypes.Email for email claim
			new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString().Replace("-", "").Substring(0, 15)),
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
}

