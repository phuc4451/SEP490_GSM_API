using Alpha_API.Models;
using Alpha_API.Services;
using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Alpha_API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class StaffController : ControllerBase
	{
		private FirebaseClient _firebaseClient;
		private readonly FirebaseClientProvider _firebaseClientProvider;
		private readonly StaffService _staffService;
		private readonly RoleService _roleService;
		private readonly SalaryService _salaryService;

		public StaffController(FirebaseClient firebaseClient,
			FirebaseClientProvider firebaseClientProvider,
			StaffService staffService, RoleService roleService, SalaryService salaryService)
		{
			_firebaseClient = firebaseClient;
			_firebaseClientProvider = firebaseClientProvider;
			_staffService = staffService;
			_roleService = roleService;
			_salaryService = salaryService;
		}


		// GET: api/staff/GetAllStaffs
		[Authorize(Roles = "admin")]
		[HttpGet("GetAllStaffs")]
		public async Task<ActionResult<List<Object>>> GetAllStaffs()
		{
			try
			{
				_firebaseClient = _firebaseClientProvider.GetFirebaseClient();
				var staffs = _staffService.GetStaffAsync();
				var roles = _roleService.GetAllRoles();

				await Task.WhenAll(staffs, roles);

				var roleStaffId = roles.Result.FirstOrDefault(role => role.RoleName == "staff")?.RoleId;

				if (staffs == null)
				{
					return NotFound("No staffs found");
				}
				List<Object> list = new List<Object>();

				foreach (var staff in staffs.Result)
				{
					var user = await _firebaseClient.Child("users").Child(staff.UserId).OnceSingleAsync<User>();

					if (user.RoleId != roleStaffId)
					{
						return Conflict("Staff account is not staff role");
					}

					var staffWithInfo = new
					{
						staff.StaffId,
						staff.Position,
						staff.FullName,
						staff.UserId,
						user.IdCard,
						user.Name,
						user.Email,
						user.Phone,
						user.Address,
						user.UserAvatar,
						user.Dob,
						user.Gender
					};

					list.Add(staffWithInfo);
				}

				return Ok(list);
			}
			catch (InvalidOperationException ex)
			{
				return Conflict(ex.Message);
			}
			catch (Exception ex)
			{
				return Conflict(ex.Message);
			}
		}


		// GET: api/staff/GetStaffAssignments
		[HttpGet("GetStaffAssignments")]
		[Authorize(Roles = "admin")]
		public async Task<ActionResult> GetStaffAssignments()
		{
			try
			{
				var assignments = await _salaryService.GetStaffAssignments();
				return Ok(assignments);

			}
			catch (InvalidOperationException ex)
			{
				return Conflict(ex.Message);
			}
			catch (Exception ex)
			{
				return Conflict(ex.Message);
			}
		}
	}
}
