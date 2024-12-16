using Alpha_API.Models;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Firebase.Database;
using Firebase.Database.Query;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.Json;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Math;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace Alpha_API.Services
{
	public class SalaryService
	{
		private FirebaseClient _firebaseClient;
		private readonly FirebaseClientProvider _firebaseClientProvider;
		private readonly TrainerService _trainerService;
		private readonly StaffService _staffService;
		private readonly TimeSlotService _timeSlotService;
		public SalaryService(FirebaseClient firebaseClient, FirebaseClientProvider firebaseClientProvider, StaffService staffService,
			TrainerService trainerService, TimeSlotService timeSlotService)
		{
			_firebaseClient = firebaseClient;
			_firebaseClientProvider = firebaseClientProvider;
			_trainerService = trainerService;
			_staffService = staffService;
			_timeSlotService = timeSlotService;
		}
		public async Task<(IEnumerable<SalaryReport>, IEnumerable<SalaryReport>)> CalculateStaffSalaryAsync(string staffId)
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			//// Fetch shift assignments and attendance records concurrently
			//var fetchShiftAssignmentsTask = _firebaseClient
			//	.Child("StaffShiftAssignments")
			//	.OrderBy("staffId")
			//	.EqualTo(staffId)
			//	.OnceAsync<StaffShiftAssignment>();

			var fetchAttendanceRecordsTask = _firebaseClient
				.Child("AttendanceRecords")
				.OrderBy("staffId")
				.EqualTo(staffId)
				.OnceAsync<AttendanceRecord>();

			var fetchSalaryReportsTask = _firebaseClient
				.Child("SalaryReports")
				.OrderBy("staffId")
				.EqualTo(staffId)
				.OnceAsync<SalaryReport>();

			var staffNameTask = _staffService.GetStaffName(staffId);

			await Task.WhenAll(staffNameTask, fetchSalaryReportsTask, fetchAttendanceRecordsTask);

			//var shiftAssignments = fetchShiftAssignmentsTask.Result;
			var attendanceRecords = fetchAttendanceRecordsTask.Result;
			var existedSalaryReports = fetchSalaryReportsTask.Result;

			// Filter relevant shift assignments locally
			var unpaidSalaryReports = existedSalaryReports
				.Where(report => !report.Object.IsBilled)
				.ToList();

			var paidSalaryReports = existedSalaryReports
				.Where(report => report.Object.IsBilled)
				.ToList();

			//// Filter relevant shift assignments locally
			//var relevantAssignments = shiftAssignments
			//	.Where(assignment => assignment.Object.AssignedDate.Date >= staffId.FromDate.Date &&
			//						 assignment.Object.EndDate.Date <= staffId.ToDate.Date)
			//	.ToList();

			if (!unpaidSalaryReports.Any() && !paidSalaryReports.Any())
				throw new Exception("No salary found.");

			var options = new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
			};

			// Pre-fetch all shifts and salary configurations in parallel
			var shiftIds = unpaidSalaryReports.Select(a => a.Object.ShiftId).Distinct().ToList();
			var configIds = unpaidSalaryReports.Select(a => a.Object.ConfigId).Distinct().ToList();

			//var fetchShiftsTask = Task.WhenAll(shiftIds.Select(id =>
			//	_firebaseClient.Child("Shifts").Child(id).OnceSingleAsync<Shift>()));

			var fetchShiftsTask = Task.WhenAll(shiftIds.Select(async id =>
			{
				var result = await _firebaseClient.Child("Shifts").Child(id).OnceSingleAsync<Shift>();
				var shift = result; // result is directly the Shift object when using OnceSingleAsync
				if (shift != null)
				{
					shift.ShiftId = id; // Assign the ShiftId based on the id passed
				}
				return shift;
			}));

			//var fetchConfigsTask = Task.WhenAll(configIds.Select(id =>
			//	_firebaseClient.Child("SalaryConfigurations").Child(id).OnceSingleAsync<SalaryConfiguration>()));


			var fetchConfigsTask = Task.WhenAll(configIds.Select(async id =>
			{
				var result = await _firebaseClient.Child("SalaryConfigurations").Child(id).OnceSingleAsync<SalaryConfiguration>();
				var shift = result; // result is directly the Shift object when using OnceSingleAsync
				if (shift != null)
				{
					shift.ConfigurationId = id; // Assign the ShiftId based on the id passed
				}
				return shift;
			}));

			await Task.WhenAll(fetchShiftsTask, fetchConfigsTask);

			// After completion, filter out null values and create a dictionary
			var shifts = fetchShiftsTask.Result
				.Where(s => s != null)  // Filter out any null shifts
				.ToDictionary(s => s.ShiftId);  // Create dictionary with ShiftId as the key
			var salaryConfigs = fetchConfigsTask.Result
				.Where(s => s != null)  // Filter out any null shifts
				.ToDictionary(s => s.ConfigurationId);  // Create dictionary with ShiftId as the key


			// Aggregate results
			var unpaidSalaryReportsList = unpaidSalaryReports.Select(report =>
			{
				var shift = shifts.GetValueOrDefault(report.Object.ShiftId)
					?? throw new Exception($"Shift not found for ID {report.Object.ShiftId}");

				var salaryConfig = salaryConfigs.GetValueOrDefault(report.Object.ConfigId)
					?? throw new Exception($"Salary configuration not found for ID {report.Object.ConfigId}");

				int totalShifts = (report.Object.ToDate - report.Object.FromDate).Days;

				var relevantRecords = attendanceRecords
					.Where(record => record.Object.Time.Date >= report.Object.FromDate &&
									 record.Object.Time.Date <= report.Object.ToDate &&
									 TimeOnly.FromDateTime(record.Object.Time) >= TimeOnly.FromDateTime(shift.StartTime) &&
									 TimeOnly.FromDateTime(record.Object.Time) <= TimeOnly.FromDateTime(shift.EndTime))
			  // Group by date to handle each day separately
			  .GroupBy(record => record.Object.Time.Date)
			  // For each date, select the earliest record within the shift time window
			  .Select(group =>
				  group.OrderBy(record => record.Object.Time)
					   .First().Object);

				report.Object.LateCount = relevantRecords.Count(record => record.IsLate);
				report.Object.TotalPresent = relevantRecords.Count(record => record.IsPresent);
				report.Object.AbsenceCount = totalShifts - report.Object.TotalPresent;
				report.Object.TotalFines = (report.Object.LateCount * salaryConfig.FinePerLate) + (report.Object.AbsenceCount * salaryConfig.FinePerAbsence);
				report.Object.TotalShiftsSalary = report.Object.TotalPresent * salaryConfig.PerShiftSalary;
				report.Object.FinalSalary = salaryConfig.BaseSalary + report.Object.TotalShiftsSalary - report.Object.TotalFines;

				return report.Object;
			}).ToList();

			var updateTasks = unpaidSalaryReportsList.Select(async report =>
			{
				try
				{
					var jsonString = JsonSerializer.Serialize(report, options);

					// Update the object in Firebase asynchronously
					await _firebaseClient.Child("SalaryReports").Child(report.ReportId).PatchAsync(jsonString);

					//Console.WriteLine($"Successfully updated report ID: {report.Id}");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error updating report ID: {report.ReportId}. Details: {ex.Message}");
					// Handle the error or log it as needed
				}
			});

			// Execute all tasks asynchronously
			await Task.WhenAll(updateTasks);

			//Console.WriteLine("All reports updated successfully.");


			var paidSalaryReportsList = paidSalaryReports.Select(report => report.Object).ToList();

			//// Combine aggregated results
			//var aggregatedReport = new
			//{
			//	TotalShifts = reportData.Sum(data => data.TotalShifts),
			//	LateCount = reportData.Sum(data => data.LateCount),
			//	AbsenceCount = reportData.Sum(data => data.AbsenceCount),
			//	TotalFines = reportData.Sum(data => data.TotalFines),
			//	FinalSalary = reportData.Sum(data => data.FinalSalary)
			//};

			//// Prepare and save the salary report
			//string id = string.IsNullOrEmpty(staffId.ReportId) ? Guid.NewGuid().ToString().Replace("-", "").Substring(0, 15) : staffId.ReportId;
			//var salaryReport = new SalaryReport
			//{
			//	ReportId = id,
			//	StaffId = staffId.StaffId,
			//	FullName = staffNameTask.Result,
			//	TotalShifts = aggregatedReport.TotalShifts,
			//	LateCount = aggregatedReport.LateCount,
			//	AbsenceCount = aggregatedReport.AbsenceCount,
			//	TotalFines = aggregatedReport.TotalFines,
			//	FinalSalary = aggregatedReport.FinalSalary,
			//	IsBilled = false,
			//	FromDate = staffId.FromDate,
			//	ToDate = staffId.ToDate,
			//	TotalSlots = 0,
			//	TrainerId = ""
			//};

			//var options = new JsonSerializerOptions
			//{
			//	PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			//	DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
			//};
			//await _firebaseClient.Child("SalaryReports").Child(id).PatchAsync(JsonSerializer.Serialize(salaryReport, options));

			return (unpaidSalaryReportsList, paidSalaryReportsList);

		}

		public async Task<(IEnumerable<SalaryReport>, IEnumerable<SalaryReport>)> CalculateTrainerSalaryAsync(string trainerId)
		{
			_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

			// Fetch required data in parallel
			var trainerNameTask = _trainerService.GetTrainerName(trainerId);
			var fetchAttendanceRecordsTask = _firebaseClient
				.Child("AttendanceRecords")
				.OrderBy("trainerId")
				.EqualTo(trainerId)
				.OnceAsync<AttendanceRecord>();
			//var fetchSalaryAssignmentTask = _firebaseClient
			//	.Child("TrainerSalaryAssignments")
			//	.OrderBy("trainerId")
			//	.EqualTo(trainerId.TrainerId)
			//	.OnceAsync<TrainerSalaryAssignment>();
			var fetchSchedulesTask = _firebaseClient
				.Child("Schedules")
				.OrderBy("trainerId")
				.EqualTo(trainerId)
				.OnceAsync<Schedule>();

			var fetchSalaryReportsTask = _firebaseClient
				.Child("SalaryReports")
				.OrderBy("trainerId")
				.EqualTo(trainerId)
				.OnceAsync<SalaryReport>();

			await Task.WhenAll(trainerNameTask, fetchAttendanceRecordsTask, fetchSalaryReportsTask, fetchSchedulesTask);

			// Process fetched data
			var attendanceRecords = fetchAttendanceRecordsTask.Result.Select(r => r.Object).ToList();
			var existedSalaryReports = fetchSalaryReportsTask.Result.ToList();

			// Filter relevant shift assignments locally
			var unpaidSalaryReports = existedSalaryReports
				.Where(report => !report.Object.IsBilled)
				.ToList();

			var paidSalaryReports = existedSalaryReports
				.Where(report => report.Object.IsBilled)
				.ToList();

			if (!unpaidSalaryReports.Any() && !paidSalaryReports.Any())
				throw new Exception("No salary found.");

			var options = new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
			};

			// Pre-fetch all shifts and salary configurations in parallel
			var shiftIds = unpaidSalaryReports.Select(a => a.Object.ShiftId).Distinct().ToList();
			var configIds = unpaidSalaryReports.Select(a => a.Object.ConfigId).Distinct().ToList();

			var fetchShiftsTask = Task.WhenAll(shiftIds.Select(async id =>
			{
				var result = await _firebaseClient.Child("Shifts").Child(id).OnceSingleAsync<Shift>();
				var shift = result; // result is directly the Shift object when using OnceSingleAsync
				if (shift != null)
				{
					shift.ShiftId = id; // Assign the ShiftId based on the id passed
				}
				return shift;
			}));

			var fetchConfigsTask = Task.WhenAll(configIds.Select(async id =>
			{
				var result = await _firebaseClient.Child("SalaryConfigurations").Child(id).OnceSingleAsync<SalaryConfiguration>();
				var shift = result; // result is directly the Shift object when using OnceSingleAsync
				if (shift != null)
				{
					shift.ConfigurationId = id; // Assign the ShiftId based on the id passed
				}
				return shift;
			}));

			await Task.WhenAll(fetchShiftsTask, fetchConfigsTask);

			// After completion, filter out null values and create a dictionary
			var shifts = fetchShiftsTask.Result
				.Where(s => s != null)  // Filter out any null shifts
				.ToDictionary(s => s.ShiftId);  // Create dictionary with ShiftId as the key
			var salaryConfigs = fetchConfigsTask.Result
				.Where(s => s != null)  // Filter out any null shifts
				.ToDictionary(s => s.ConfigurationId);  // Create dictionary with ShiftId as the key

			// Aggregate results
			var unpaidSalaryReportsTask = await Task.WhenAll(unpaidSalaryReports.Select(async report =>
			{
				var salaryConfig = salaryConfigs.GetValueOrDefault(report.Object.ConfigId)
					?? throw new Exception($"Salary configuration not found for ID {report.Object.ConfigId}");

				// Filter relevant schedules
				var schedules = fetchSchedulesTask.Result;
				var relevantSchedules = schedules
					.Where(s => s.Object.LastSlot >= DateOnly.FromDateTime(report.Object.FromDate) &&
								s.Object.LastSlot <= DateOnly.FromDateTime(report.Object.ToDate))
					.ToList();

				if (!relevantSchedules.Any())
					throw new Exception("No relevant schedules found.");

				var mostOldSlotDate = relevantSchedules.Min(s => s.Object.FirstSlot);
				var mostRecentSlotDate = relevantSchedules.Max(s => s.Object.LastSlot);

				// Fetch all slots for the relevant schedules in parallel
				var slotTasks = relevantSchedules.Select(schedule =>
					_firebaseClient
						.Child("Slots")
						.OrderBy("scheduleId")
						.EqualTo(schedule.Key)
						.OnceAsync<Slot>());

				var slots = (await Task.WhenAll(slotTasks)).SelectMany(s => s).Select(slot => slot.Object)
						.Where(s => s.Date >= DateOnly.FromDateTime(report.Object.FromDate) &&
						s.Date <= DateOnly.FromDateTime(report.Object.ToDate)).ToList();

				var slotTimeStrings = slots
					.Select(a =>
					{
						var timeSlot = _timeSlotService.GetTimeSlot(a.TimeSlotId).Split("-");
						var startTime = TimeOnly.ParseExact(timeSlot[0], "H:mm");
						var endTime = TimeOnly.ParseExact(timeSlot[1], "H:mm");
						var startSlotDate = a.Date.ToDateTime(startTime);
						var endSlotDate = a.Date.ToDateTime(endTime);
						return $"{startSlotDate:yyyy-MM-dd HH:mm}-{endSlotDate:yyyy-MM-dd HH:mm}";
					})
					.Distinct()
					.ToList();

				// Filter relevant attendance records
				var relevantRecords = attendanceRecords
					.Where(record =>
						DateOnly.FromDateTime(record.Time) >= DateOnly.FromDateTime(report.Object.FromDate) &&
						DateOnly.FromDateTime(record.Time) <= DateOnly.FromDateTime(report.Object.ToDate))
					.SelectMany(record => slotTimeStrings
						.Select(timeRange => new
						{
							Record = record,
							StartTime = DateTime.ParseExact(timeRange.Split("-")[0], "yyyy-MM-dd HH:mm", CultureInfo.CurrentCulture),
							EndTime = DateTime.ParseExact(timeRange.Split("-")[1], "yyyy-MM-dd HH:mm", CultureInfo.CurrentCulture)
						})
						.Where(x => record.Time >= x.StartTime && record.Time <= x.EndTime))
					.GroupBy(x => new { x.StartTime, x.EndTime }) // Group by time range
					.Select(g => g.OrderBy(x => x.Record.Time).First().Record) // Take the earliest record
					.ToList();

				// Calculate statistics
				int totalSlots = slots.Count;
				int lateCount = relevantRecords.Count(record => record.IsLate);
				int presenceCount = relevantRecords.Count(record => record.IsPresent);
				int absenceCount = totalSlots - presenceCount;
				decimal totalFines = (lateCount * salaryConfig.FinePerLate) + (absenceCount * salaryConfig.FinePerAbsence);
				decimal totalSlotSalary = presenceCount * salaryConfig.PerSlotSalary;
				decimal finalSalary = salaryConfig.BaseSalary + totalSlotSalary - totalFines;

				return report.Object;
			}));

			var updateTasks = unpaidSalaryReportsTask.Select(async report =>
			{
				try
				{
					var jsonString = JsonSerializer.Serialize(report, options);

					// Update the object in Firebase asynchronously
					await _firebaseClient.Child("SalaryReports").Child(report.ReportId).PatchAsync(jsonString);

					//Console.WriteLine($"Successfully updated report ID: {report.Id}");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error updating report ID: {report.ReportId}. Details: {ex.Message}");
					// Handle the error or log it as needed
				}
			});

			// Execute all tasks asynchronously
			await Task.WhenAll(updateTasks);

			//Console.WriteLine("All reports updated successfully.");


			var paidSalaryReportsList = paidSalaryReports.Select(report => report.Object).ToList();

			return (unpaidSalaryReportsTask.ToList(), paidSalaryReportsList);
		}

		public async Task CreateSalaryConfiguration(SalaryConfiguration salaryConfiguration)
		{
			try
			{
				// Initialize Firebase client (assuming it's being injected)
				_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

				// Check if a shift with the same details already exists (ShiftName, StartTime, EndTime)
				var existingConfig = await CheckIfConfigurationExistsAsync(salaryConfiguration);
				if (existingConfig != null)
				{
					throw new InvalidOperationException("A configuration with the same details already exists.");
				}

				// Create a new configuration with a unique ID
				var id = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 15);
				var newConfig = new SalaryConfiguration
				{
					BaseSalary = salaryConfiguration.BaseSalary,
					PerShiftSalary = salaryConfiguration.PerShiftSalary,
					PerSlotSalary = salaryConfiguration.PerSlotSalary,
					FinePerLate = salaryConfiguration.FinePerLate,
					FinePerAbsence = salaryConfiguration.FinePerAbsence,
				};

				var options = new JsonSerializerOptions
				{
					PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
					DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
				};

				var jsonString = JsonSerializer.Serialize(newConfig, options);

				// Store the new configuration in Firebase
				await _firebaseClient.Child("SalaryConfigurations").Child(id).PutAsync(jsonString);

				Console.WriteLine("SalaryConfiguration created successfully.");
			}
			catch (Exception ex)
			{
				// Log or handle the exception as needed
				Console.WriteLine($"Error creating shift: {ex.Message}");
				throw new InvalidOperationException("An error occurred while creating the salary configuration.", ex);
			}
		}

		private async Task<SalaryConfiguration> CheckIfConfigurationExistsAsync(SalaryConfiguration salaryConfiguration)
		{
			try
			{
				// Check for an existing configurations with the same fields
				var existingConfigs = await _firebaseClient.Child("SalaryConfigurations")
					.OnceAsync<SalaryConfiguration>();

				// Look for configurations that match the provided details
				foreach (var existingConfig in existingConfigs)
				{
					if (existingConfig.Object.BaseSalary == salaryConfiguration.BaseSalary
						&& existingConfig.Object.FinePerAbsence == salaryConfiguration.FinePerAbsence &&
						existingConfig.Object.FinePerLate == salaryConfiguration.FinePerLate &&
						existingConfig.Object.PerSlotSalary == salaryConfiguration.PerSlotSalary &&
						existingConfig.Object.PerShiftSalary == salaryConfiguration.PerShiftSalary
						)
					{
						return existingConfig.Object; // Return the existing configuration if found
					}
				}

				return null; // No matching configuration found
			}
			catch (Exception ex)
			{
				// Log or handle the exception as needed
				Console.WriteLine($"Error checking for existing salary configuration: {ex.Message}");
				return null;
			}
		}

		public async Task<IEnumerable<StaffShiftAssignment>> GetStaffAssignments()
		{
			try
			{
				// Initialize Firebase client (assuming it's being injected)
				_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

				// Retrieve shifts in Firebase
				var staffsSnapshot = await _firebaseClient.Child("StaffShiftAssignments").OnceAsync<StaffShiftAssignment>();

				// Create a list to store updated shifts with their ShiftId set
				var staffs = staffsSnapshot.Where(x => x.Object.EndDate >= DateTime.Now).Select(staffSnapshot =>
				{
					var staff = staffSnapshot.Object;
					staff.AssignmentId = staffSnapshot.Key; // Set ShiftId to the Firebase key
					return staff;
				}).ToList();

				return staffs;
			}
			catch (Exception ex)
			{
				// Log or handle the exception as needed
				Console.WriteLine($"Error retrieving staff assignments: {ex.Message}");
				throw new InvalidOperationException("An error occurred while retrieving the staff assignments.", ex);
			}
		}

		public async Task CompleteSalary(string reportId)
		{
			try
			{
				_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

				await _firebaseClient.Child("SalaryReports").Child(reportId).PatchAsync(new
				{
					isBilled = true,
				});
			}

			catch (Exception ex)
			{
				throw new InvalidOperationException(ex.Message);
			}
		}

		//public async Task<List<SalaryReport>> CalculateAllSalariesAsync(DateTime startDate, DateTime endDate)
		//{
		//	_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

		//	var salaryConfigs = await _firebaseClient
		//		.Child("SalaryConfigurations")
		//		.OnceAsync<SalaryConfiguration>();

		//	var staffTasks = salaryConfigs
		//		.Where(config => config.Object.ConfigurationId.StartsWith("staff"))
		//		.Select(config => CalculateStaffSalaryAsync(config.Key, startDate, endDate));

		//	var trainerTasks = salaryConfigs
		//		.Where(config => config.Object.ConfigurationId.StartsWith("trainer"))
		//		.Select(config => CalculateTrainerSalaryAsync(config.Key, startDate, endDate));

		//	var salaryReports = await Task.WhenAll(staffTasks.Concat(trainerTasks));

		//	return salaryReports.ToList();
		//}
	}
}
