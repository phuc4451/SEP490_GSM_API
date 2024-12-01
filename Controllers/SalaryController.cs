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
	public class SalaryController : ControllerBase
	{
		private FirebaseClient _firebaseClient;
		private readonly FirebaseClientProvider _firebaseClientProvider;
		private readonly TrainerService _trainerService;
		private readonly ShiftService _shiftService;
		private readonly SalaryService _salaryService;
		public SalaryController(FirebaseClient firebaseClient, FirebaseClientProvider firebaseClientProvider, TrainerService trainerService,
			ShiftService shiftService, SalaryService salaryService)
		{
			_firebaseClient = firebaseClient;
			_firebaseClientProvider = firebaseClientProvider;
			_trainerService = trainerService;
			_shiftService = shiftService;
			_salaryService = salaryService;
		}

		[HttpPost("AddShift")]
		[Authorize(Roles = "admin")]
		public async Task<ActionResult> AddShift(Shift shift)
		{
			try
			{
				await _shiftService.CreateShiftAsync(shift);
				return Ok();

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

		[HttpPost("AddSalaryConfig")]
		[Authorize(Roles = "admin")]
		public async Task<ActionResult> AddSalaryConfig(SalaryConfiguration config)
		{
			try
			{
				await _salaryService.CreateSalaryConfiguration(config);
				return Ok();

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

		[HttpPost("AssignShift")]
		[Authorize(Roles = "admin")]

		public async Task<ActionResult> AssignShift(StaffShiftAssignment assignment)
		{
			try
			{
				await _shiftService.AssignStaffToShiftAsync(assignment);
				return Ok();

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

		[HttpPost("AssignTrainerSalaryConfig")]
		[Authorize(Roles = "admin")]

		public async Task<ActionResult> AssignTrainerSalaryConfig(TrainerSalaryAssignment assignment)
		{
			try
			{
				await _trainerService.AssignSalaryConfigToTrainerAsync(assignment);
				return Ok();

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

		[HttpPost("CalculateStaffSalary")]
		[Authorize(Roles = "admin")]

		public async Task<ActionResult> CalculateStaffSalary(SalaryReport report)
		{
			try
			{
				var salaryReport = await _salaryService.CalculateStaffSalaryAsync(report);
				return Ok(salaryReport);
			}
			catch (Exception ex)
			{
				return Conflict(ex.Message);
			}
		}

		[HttpPost("CalculateTrainerSalary")]
		[Authorize(Roles = "admin")]

		public async Task<ActionResult> CalculateTrainerSalary(SalaryReport report)
		{
			try
			{
				var salaryReport = await _salaryService.CalculateTrainerSalaryAsync(report);
				return Ok(salaryReport);
			}
			catch (Exception ex)
			{
				return Conflict(ex.Message);
			}
		}

		[HttpGet("GetStaffSalaryReport/{id}")]
		[Authorize(Roles = "admin")]

		public async Task<ActionResult> GetStaffSalary(string id)
		{
			try
			{
				_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

				var salaryReports = await _firebaseClient
					.Child("SalaryReports")
					.OrderBy("staffId")
					.EqualTo(id)
					.OnceAsync<SalaryReport>();

				if (salaryReports.Count == 0)
				{
					return NotFound("No salary reports for this staff");
				}

				return Ok(salaryReports);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "An unexpected error occurred.", details = ex.Message });
			}
		}

		[HttpGet("GetTrainerSalaryReport/{id}")]
		[Authorize(Roles = "admin")]

		public async Task<ActionResult> GetTrainerSalaryReport(string id)
		{
			try
			{
				_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

				var salaryReports = await _firebaseClient
					.Child("SalaryReports")
					.OrderBy("trainerId")
					.EqualTo(id)
					.OnceAsync<SalaryReport>();

				if (salaryReports.Count == 0)
				{
					return NotFound("No salary reports for this trainer");
				}

				return Ok(salaryReports);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "An unexpected error occurred.", details = ex.Message });
			}
		}

	}
}
