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
using System.IdentityModel.Tokens.Jwt;
using System.Collections;
using Alpha_API.Services;
using ClosedXML.Excel;


[Route("api/[controller]")]
[ApiController]

public class UsersController : ControllerBase
{
	private readonly EmailService _emailService;
	private readonly FirebaseAuth _firebaseAuth;
	private FirebaseClient _firebaseClient;
	private readonly RoleService _roleService;
	private readonly TrainerService _trainerService;
	private readonly FirebaseClientProvider _firebaseClientProvider;

	public UsersController(EmailService emailService, FirebaseClient firebaseClient, FirebaseAuth firebaseAuth, RoleService roleService,
		FirebaseClientProvider firebaseClientProvider, TrainerService trainerService)
	{
		_firebaseClient = firebaseClient;
		_emailService = emailService;
		_firebaseAuth = firebaseAuth;
		_roleService = roleService;
		_firebaseClientProvider = firebaseClientProvider;
		_trainerService = trainerService;
	}

	// GET: api/users/GetUsers
	[Authorize(Roles = "admin")]
	[HttpGet("GetUsers")]
	public async Task<ActionResult<IEnumerable<User>>> GetUsers()
	{
		_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

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

	// GET: api/users/GetStaffs
	[Authorize(Roles = "admin")]
	[HttpGet("GetStaffAccounts")]
	public async Task<ActionResult<IEnumerable<User>>> GetStaffs()
	{
		_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

		var roles = await _roleService.GetAllRoles();

		var staffRoleId = roles.FirstOrDefault(role => role.RoleName == "staff")?.RoleId;

		var users = await _firebaseClient
			.Child("users")
			.OrderBy("roleId")
			.EqualTo(staffRoleId)
			.OnceAsync<User>();

		var userList = users
			.Select(user => user.Object)
			.ToList();



		//var userList = (await Task.WhenAll(users.Select(async user =>
		//	new { User = user.Object, RoleName = await _roleService.GetRoleName(user.Object.RoleId) })))
		//	.Where(result => result.RoleName == "staff")
		//	.Select(result => result.User)
		//	.ToList();


		//var userList = new List<User>();
		//foreach (var user in users)
		//{
		//	var roleName = await _roleService.GetRoleName(user.Object.RoleId);

		//	if (roleName == "staff")
		//		userList.Add(user.Object);
		//}

		return Ok(userList);
	}

	// GET: api/users/GetTrainers
	[Authorize(Roles = "admin,staff")]
	[HttpGet("GetTrainerAccounts")]
	public async Task<ActionResult<IEnumerable<User>>> GetTrainers()
	{
		_firebaseClient = _firebaseClientProvider.GetFirebaseClient();
		var users = await _firebaseClient
			.Child("users")
			.OnceAsync<User>();

		var trainers = await _firebaseClient
			.Child("Trainers")
			.OnceAsync<Trainer>();

		var userList = new List<User>();
		foreach (var user in users)
		{
			var roleName = await _roleService.GetRoleName(user.Object.RoleId);
			if (roleName == "pt")
			{
				foreach (var trainer in trainers)
				{
					if (trainer.Object.UserId == user.Object.UserId)
					{
						var pt = new
						{
							trainerId = trainer.Object.TrainerId,

						};
						userList.Add(user.Object);
					}

				}
			}
		}
		return Ok(userList);
	}

	// GET: api/users/GetCustomers
	[Authorize(Roles = "admin,staff")]
	[HttpGet("GetCustomerAccounts")]
	public async Task<ActionResult<IEnumerable<User>>> GetCustomers()
	{
		_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

		var roles = await _roleService.GetAllRoles();

		var customerRoleId = roles.FirstOrDefault(role => role.RoleName == "customer")?.RoleId;

		var users = await _firebaseClient
			.Child("users")
			.OrderBy("roleId")
			.EqualTo(customerRoleId)
			.OnceAsync<User>();

		var userList = users
			.Select(user => user.Object)
			.ToList();

		//var userList = new List<User>();
		//foreach (var user in users)
		//{
		//	var roleName = await _roleService.GetRoleName(user.Object.RoleId);
		//	if (roleName == "customer")
		//		userList.Add(user.Object);
		//}

		return Ok(userList);
	}

	// GET: api/users/ExportStaffsToExcel
	[HttpGet("ExportStaffsToExcel")]
	[Authorize(Roles = "admin")]
	public async Task<IActionResult> ExportStaffsToExcel()
	{
		_firebaseClient = _firebaseClientProvider.GetFirebaseClient();
		var users = await _firebaseClient
			.Child("users")
			.OnceAsync<User>();

		var staffList = new List<User>();
		foreach (var user in users)
		{
			var roleName = await _roleService.GetRoleName(user.Object.RoleId);
			if (roleName == "staff")
				staffList.Add(user.Object);
		}

		// Generate Excel file
		using (var workbook = new XLWorkbook())
		{
			var worksheet = workbook.Worksheets.Add("Staffs");
			worksheet.Cell(1, 1).Value = "Name";
			worksheet.Cell(1, 2).Value = "Email";
			worksheet.Cell(1, 3).Value = "Phone";
			worksheet.Cell(1, 4).Value = "Address";
			worksheet.Cell(1, 5).Value = "Gender";

			for (int i = 0; i < staffList.Count; i++)
			{
				worksheet.Cell(i + 2, 1).Value = staffList[i].Name;
				worksheet.Cell(i + 2, 2).Value = staffList[i].Email;
				worksheet.Cell(i + 2, 3).Value = staffList[i].Phone;
				worksheet.Cell(i + 2, 4).Value = staffList[i].Address;
				worksheet.Cell(i + 2, 5).Value = staffList[i].Gender;
			}

			using (var stream = new MemoryStream())
			{
				workbook.SaveAs(stream);
				stream.Seek(0, SeekOrigin.Begin);

				return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Staffs.xlsx");
			}
		}
	}

	// GET: api/users/ExportCustomersToExcel
	[HttpGet("ExportCustomersToExcel")]
	[Authorize(Roles = "admin,staff")]
	public async Task<IActionResult> ExportCustomersToExcel()
	{
		_firebaseClient = _firebaseClientProvider.GetFirebaseClient();
		var users = await _firebaseClient
			.Child("users")
			.OnceAsync<User>();

		var customerList = new List<User>();
		foreach (var user in users)
		{
			var roleName = await _roleService.GetRoleName(user.Object.RoleId);
			if (roleName == "customer")
				customerList.Add(user.Object);
		}

		// Generate Excel file
		using (var workbook = new XLWorkbook())
		{
			var worksheet = workbook.Worksheets.Add("Customers");
			worksheet.Cell(1, 1).Value = "Name";
			worksheet.Cell(1, 2).Value = "Email";
			worksheet.Cell(1, 3).Value = "Phone";
			worksheet.Cell(1, 4).Value = "Address";
			worksheet.Cell(1, 5).Value = "Gender";

			for (int i = 0; i < customerList.Count; i++)
			{
				worksheet.Cell(i + 2, 1).Value = customerList[i].Name;
				worksheet.Cell(i + 2, 2).Value = customerList[i].Email;
				worksheet.Cell(i + 2, 3).Value = customerList[i].Phone;
				worksheet.Cell(i + 2, 4).Value = customerList[i].Address;
				worksheet.Cell(i + 2, 5).Value = customerList[i].Gender;
			}

			using (var stream = new MemoryStream())
			{
				workbook.SaveAs(stream);
				stream.Seek(0, SeekOrigin.Begin);

				return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Customers.xlsx");
			}
		}
	}

	// GET: api/users/getcurrentuser
	[Authorize(Roles = "admin,staff,customer,pt")]
	[HttpGet("GetCurrentUser")]
	public async Task<ActionResult<User>> GetCurrentUser()
	{
		_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

		// Retrieve the email claim
		var email = HttpContext.User.FindFirst(ClaimTypes.Email)?.Value;

		foreach (var claim in HttpContext.User.Claims)
		{
			Console.WriteLine($"{claim.Type}: {claim.Value}");
		}

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

	// GET: api/users/GetUserById/{id}
	[Authorize(Roles = "admin")]
	[HttpGet("GetUserById/{id}")]
	public async Task<ActionResult<User>> GetUserById(string id)
	{
		_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

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

	// GET: api/users/GetUserByEmail/{email}
	[Authorize(Roles = "admin")]
	[HttpGet("GetUserByEmail/{email}")]
	public async Task<ActionResult<object>> GetUserByEmail(string email)
	{
		_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

		// Truy vấn trực tiếp để tìm người dùng có email khớp
		var users = await _firebaseClient
			.Child("users")
			.OrderBy("email")
			.EqualTo(email)
			.OnceAsync<User>();

		var matchingUser = users.FirstOrDefault();

		if (matchingUser == null)
		{
			return NotFound($"No user found with the email {email}");
		}

		// Trả về chỉ các trường cần thiết
		var result = new
		{
			UserId = matchingUser.Key,
			Name = matchingUser.Object.Name,
			Email = matchingUser.Object.Email
		};

		return Ok(result);
	}

	// POST: api/users/updatestaff/{id}
	[Authorize(Roles = "admin,staff,customer")]
	[HttpPatch("updateStaff/{id}")]
	public async Task<ActionResult> UpdateStaff(string id, [FromBody] User u)
	{
		_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

		var existingUser = await _firebaseClient
			.Child("users")
			.Child(id)
			.OnceSingleAsync<User>();

		if (existingUser == null)
		{
			return NotFound("User not found");
		}

		if (string.IsNullOrEmpty(existingUser.Email) || string.IsNullOrEmpty(existingUser.RoleId))
		{
			return BadRequest("User is missing required fields: Email, RoleId");
		}

		// Call GetRoleName method to get the role name
		string roleName = await _roleService.GetRoleName(existingUser.RoleId);

		if (roleName != "staff")
		{
			return Forbid();
		}

		User user = new User()
		{
			Address = u.Address,
			Dob = u.Dob,
			Email = u.Email,
			Gender = u.Gender,
			IdCard = u.IdCard,
			Name = u.Name,
			Phone = u.Phone,
			UserAvatar = u.UserAvatar,
		};

		var options = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
		};

		var jsonString = JsonSerializer.Serialize(user, options);

		await _firebaseClient
			.Child("users")
			.Child(id)
			.PatchAsync(jsonString);

		return NoContent();
	}

	// PUT: api/users/updatecustomer/{id}
	[Authorize(Roles = "admin,staff")]
	[HttpPatch("updateCustomer/{id}")]
	public async Task<ActionResult> UpdateCustomer(string id, [FromBody] User u)
	{
		_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

		var existingUser = await _firebaseClient
			.Child("users")
			.Child(id)
			.OnceSingleAsync<User>();

		if (existingUser == null)
		{
			return NotFound("User not found");
		}

		if (string.IsNullOrEmpty(existingUser.Email) || string.IsNullOrEmpty(existingUser.RoleId))
		{
			return BadRequest("User is missing required fields: Email, RoleId");
		}

		// Call GetRoleName method to get the role name
		string roleName = await _roleService.GetRoleName(existingUser.RoleId);

		if (roleName != "customer")
		{
			return Forbid();
		}

		User user = new User()
		{
			Address = u.Address,
			Dob = u.Dob,
			Email = u.Email,
			Gender = u.Gender,
			IdCard = u.IdCard,
			Name = u.Name,
			Phone = u.Phone,
			UserAvatar = u.UserAvatar,
		};

		var options = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
		};

		var jsonString = JsonSerializer.Serialize(user, options);

		await _firebaseClient
			.Child("users")
			.Child(id)
			.PatchAsync(jsonString);

		return NoContent();
	}

	// POST: api/users/addstaff
	[Authorize(Roles = "admin")]
	[HttpPost("addStaff")]
	public async Task<ActionResult> AddStaff([FromBody] RegisterStaffDto staff)
	{
		if (staff == null || string.IsNullOrEmpty(staff.IdCard) || string.IsNullOrEmpty(staff.Phone) || string.IsNullOrEmpty(staff.Name))
		{
			return BadRequest("Missing required fields");
		}
		_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

		try
		{
			string password = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8);
			// Create a Firebase Auth user
			var createUserResponse = await _firebaseAuth.CreateUserAsync(new UserRecordArgs
			{
				Email = staff.Email,
				Password = password,
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

			var roles = await _roleService.GetAllRoles();
			string roleStaffId = roles.FirstOrDefault(role => role.RoleName == "staff")?.RoleId;

			string id = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 15);

			User u = new User()
			{
				Name = staff.Name,
				Email = staff.Email,
				RoleId = roleStaffId, // Staff role
				Phone = staff.Phone,
				UserId = id,
				Gender = staff.Gender,
				Address = staff.Address,
				UserAvatar = staff.UserAvatar,
				IdCard = staff.IdCard,
				Dob = staff.Dob
			};

			Staff newStaff = new Staff()
			{
				FullName = staff.Name,
				Position = staff.Position,
				UserId = userId,
			};
			var options = new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
			};

			var jsonString = JsonSerializer.Serialize(u, options);

			// Send the verification email using a third-party email service
			var emailSent = _emailService.SendVerificationEmail(staff.Email, verificationLink);
			_emailService.SendEmailMessage(staff.Email, password, "Here is your password");

			if (!emailSent)
			{
				return BadRequest("Failed to send email verification.");
			}

			var userTask = _firebaseClient
				.Child("users")
				.Child(id)
				.PutAsync(jsonString);

			jsonString = JsonSerializer.Serialize(newStaff, options);

			var staffTask = _firebaseClient
				.Child("Staffs")
				.PostAsync(jsonString);

			await Task.WhenAll(userTask, staffTask);

			return Ok("Staff added sucessfully. Please verify email.");
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

	// POST: api/users/addTrainer
	[Authorize(Roles = "admin,staff")]
	[HttpPost("addTrainer")]
	public async Task<ActionResult> AddTrainer([FromBody] RegisterTrainerDto trainer)
	{
		if (trainer == null || string.IsNullOrEmpty(trainer.IdCard) || string.IsNullOrEmpty(trainer.Email) || string.IsNullOrEmpty(trainer.Name))
		{
			return BadRequest("Missing required fields");
		}
		_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

		try
		{
			string password = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8);
			// Create a Firebase Auth user
			var createUserResponse = await _firebaseAuth.CreateUserAsync(new UserRecordArgs
			{
				Email = trainer.Email,
				Password = password,
				DisplayName = trainer.Name,
				EmailVerified = false,
				Disabled = false
			});

			if (createUserResponse == null)
			{
				return BadRequest("User registration failed.");
			}
			var userId = createUserResponse.Uid;

			// Generate email verification link
			var verificationLink = await _firebaseAuth.GenerateEmailVerificationLinkAsync(trainer.Email);

			var roles = await _roleService.GetAllRoles();
			string roleTrainerId = roles.FirstOrDefault(role => role.RoleName == "pt")?.RoleId;

			User u = new User()
			{
				Name = trainer.Name,
				Email = trainer.Email,
				RoleId = roleTrainerId, // Trainer role
				Phone = trainer.Phone,
				UserId = userId,
				Gender = trainer.Gender,
				Address = trainer.Address,
				UserAvatar = trainer.UserAvatar,
				IdCard = trainer.IdCard,
				Dob = trainer.Dob
			};

			var options = new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
			};

			var jsonString = JsonSerializer.Serialize(u, options);

			// Send the verification email using a third-party email service
			var emailSent = _emailService.SendVerificationEmail(trainer.Email, verificationLink);
			_emailService.SendEmailMessage(trainer.Email, password, "Here is your password");
			if (!emailSent)
			{
				return BadRequest("Failed to send email verification.");
			}

			var userTask = _firebaseClient
				.Child("users")
				.Child(userId)
				.PutAsync(jsonString);


			Trainer newTrainer = new Trainer()
			{
				Name = trainer.Name,
				UserId = userId,
				IsTrainerBoxing = trainer.IsBoxer,
				IsTrainerGym = trainer.IsGymer,
			};


			var trainerNameTask = _trainerService.AddTrainerAsync(newTrainer);

			await Task.WhenAll(userTask, trainerNameTask);

			return Ok($"Trainer {trainerNameTask.Result} created. Please verify your email.");
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
	[HttpPost("addCustomer")]
	public async Task<ActionResult> AddCustomer([FromBody] RegisterCustomerDto customer)
	{
		if (customer == null)
		{
			return BadRequest();
		}

		if (string.IsNullOrEmpty(customer.Phone) || string.IsNullOrEmpty(customer.Name))
		{
			return BadRequest("Missing phone and name");
		}

		_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

		try
		{
			string password = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8);
			// Create a Firebase Auth user
			var createUserResponse = await _firebaseAuth.CreateUserAsync(new UserRecordArgs
			{
				Email = customer.Email,
				Password = password,
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
			var roles = await _roleService.GetAllRoles();
			string roleCustomerId = roles.FirstOrDefault(role => role.RoleName == "customer")?.RoleId;


			User u = new User()
			{
				Name = customer.Name,
				Email = customer.Email,
				RoleId = roleCustomerId, // Customer role
				Phone = customer.Phone,
				UserId = userId,
				Gender = customer.Gender,
				Address = customer.Address,
				UserAvatar = customer.UserAvatar,
				IdCard = customer.IdCard,
				Dob = customer.Dob
			};

			// Send the verification email using a third-party email service
			var emailSent = _emailService.SendVerificationEmail(customer.Email, verificationLink);
			_emailService.SendEmailMessage(customer.Email, password, "Here is your password");
			if (!emailSent)
			{
				return BadRequest("Failed to send email verification.");
			}

			var options = new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
			};

			var jsonString = JsonSerializer.Serialize(u, options);

			await _firebaseClient
				.Child("users")
				.Child(userId)
				.PutAsync(jsonString);

			return Ok("Customer added sucessfully. Please verify email.");

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
		_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

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

	// PATCH: api/users/{id}
	[HttpPatch("{id}")]
	public async Task<ActionResult> PatchUser(string id, [FromBody] User user)
	{
		_firebaseClient = _firebaseClientProvider.GetFirebaseClient();
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
		_firebaseClient = _firebaseClientProvider.GetFirebaseClient();
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

	// GET: api/users/GetAllMemberships
	[HttpGet("GetAllMemberships")]
	public async Task<ActionResult<Object>> GetAllMemberships()
	{
		_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

		// Fetch BoxingRegistrations and GymRegistrations concurrently
		var boxingTask = _firebaseClient.Child("BoxingRegistrations").OnceAsync<BoxingRegistration>();
		var gymTask = _firebaseClient.Child("GymRegistrations").OnceAsync<GymRegistration>();


		await Task.WhenAll(boxingTask, gymTask);

		var boxingRegistrations = boxingTask.Result;
		var gymRegistrations = gymTask.Result;

		// Add RegistrationId to each record
		foreach (var r in boxingRegistrations)
		{
			r.Object.RegistrationId = r.Key;
		}

		foreach (var r in gymRegistrations)
		{
			r.Object.RegistrationId = r.Key;
		}


		// Create lookup maps for registrations by user
		var gymRegistrationLookup = gymRegistrations
			.GroupBy(x => x.Object.UserId)
			.ToDictionary(g => g.Key, g => g.Select(x => x.Object).ToList());

		var boxingRegistrationLookup = boxingRegistrations
			.SelectMany(r => r.Object.UserIds.Split(",").Select(userId => new { UserId = userId, Registration = r.Object }))
			.GroupBy(x => x.UserId)
			.ToDictionary(g => g.Key, g => g.Select(x => x.Registration).ToList());

		// Combine user IDs from both sources
		var allUserIds = gymRegistrationLookup.Keys
			.Concat(boxingRegistrationLookup.Keys)
			.Distinct()
			.ToList();

		// Fetch all user details in bulk
		var userTasks = allUserIds.Select(userId =>
			_firebaseClient.Child("users").Child(userId).OnceSingleAsync<User>()
		);
		var users = await Task.WhenAll(userTasks);

		// Map users by UserId for quick lookup
		var userMap = users.ToDictionary(user => user.UserId, user => user);

		// Build the final result
		var result = allUserIds.Select(userId =>
		{
			var user = userMap.GetValueOrDefault(userId);
			var userGymRegistrations = gymRegistrationLookup.GetValueOrDefault(userId, new List<GymRegistration>());
			var userBoxingRegistrations = boxingRegistrationLookup.GetValueOrDefault(userId, new List<BoxingRegistration>());

			return new
			{
				User = user,
				GymRegistrations = userGymRegistrations,
				BoxingRegistrations = userBoxingRegistrations
			};
		}).ToList();

		return result;

	}
}
