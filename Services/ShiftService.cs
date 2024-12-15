using Alpha_API.Models;
using DocumentFormat.OpenXml.Spreadsheet;
using Firebase.Database;
using Firebase.Database.Query;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Alpha_API.Services
{
	public class ShiftService
	{
		private FirebaseClient _firebaseClient;
		private readonly FirebaseClientProvider _firebaseClientProvider;

		public ShiftService(FirebaseClient firebaseClient, FirebaseClientProvider firebaseClientProvider)
		{
			_firebaseClient = firebaseClient;
			_firebaseClientProvider = firebaseClientProvider;
		}

		// Create a new shift with recurrence options
		public async Task CreateShiftAsync(Shift shift)
		{
			try
			{
				// Initialize Firebase client (assuming it's being injected)
				_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

				// Check if a shift with the same details already exists (ShiftName, StartTime, EndTime)
				var existingShift = await CheckIfShiftExistsAsync(shift);
				if (existingShift != null)
				{
					throw new InvalidOperationException("A shift with the same details already exists.");
				}

				// Create a new shift with a unique ID
				var id = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 15);
				var newShift = new Shift
				{
					ShiftName = shift.ShiftName,
					StartTime = shift.StartTime,
					EndTime = shift.EndTime,
					Location = shift.Location,
					ShiftType = shift.ShiftType,
				};

				var options = new JsonSerializerOptions
				{
					PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
					DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
				};

				var jsonString = JsonSerializer.Serialize(newShift, options);

				// Store the new shift in Firebase
				await _firebaseClient.Child("Shifts").Child(id).PutAsync(jsonString);

				Console.WriteLine("Shift created successfully.");
			}
			catch (Exception ex)
			{
				// Log or handle the exception as needed
				Console.WriteLine($"Error creating shift: {ex.Message}");
				throw;
			}
		}

		public async Task<IEnumerable<SalaryConfiguration>> GetSalaryConfigAsync()
		{
			try
			{
				// Initialize Firebase client (assuming it's being injected)
				_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

				// Retrieve shifts in Firebase
				var configsSnapshot = await _firebaseClient.Child("SalaryConfigurations").OnceAsync<SalaryConfiguration>();

				// Create a list to store updated shifts with their ShiftId set
				var configs = configsSnapshot.Select(configSnapshot =>
				{
					var config = configSnapshot.Object;
					config.ConfigurationId = configSnapshot.Key; // Set ShiftId to the Firebase key
					return config;
				}).ToList();

				return configs;

				//Console.WriteLine("Shift created successfully.");
			}
			catch (Exception ex)
			{
				// Log or handle the exception as needed
				Console.WriteLine($"Error retrieving salary configs: {ex.Message}");
				throw new InvalidOperationException("An error occurred while retrieving the salary configs.", ex);
			}
		}

		public async Task<IEnumerable<Shift>> GetShiftAsync()
		{
			try
			{
				// Initialize Firebase client (assuming it's being injected)
				_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

				// Retrieve shifts in Firebase
				var shiftsSnapshot = await _firebaseClient.Child("Shifts").OnceAsync<Shift>();

				// Create a list to store updated shifts with their ShiftId set
				var shifts = shiftsSnapshot.Select(shiftSnapshot =>
				{
					var shift = shiftSnapshot.Object;
					shift.ShiftId = shiftSnapshot.Key; // Set ShiftId to the Firebase key
					return shift;
				}).ToList();

				return shifts;

				//Console.WriteLine("Shift created successfully.");
			}
			catch (Exception ex)
			{
				// Log or handle the exception as needed
				Console.WriteLine($"Error retrieving shifts: {ex.Message}");
				throw new InvalidOperationException("An error occurred while retrieving the shifts.", ex);
			}
		}

		private async Task<Shift> CheckIfShiftExistsAsync(Shift shift)
		{
			try
			{
				// Check for an existing shift with the same ShiftName, StartTime, and EndTime
				var existingShifts = await _firebaseClient.Child("Shifts")
					.OnceAsync<Shift>();

				// Look for shifts that match the provided details
				foreach (var existingShift in existingShifts)
				{
					if (existingShift.Object.StartTime.TimeOfDay == shift.StartTime.TimeOfDay
						&& existingShift.Object.EndTime.TimeOfDay == shift.EndTime.TimeOfDay &&
						existingShift.Object.Location == shift.Location &&
						existingShift.Object.ShiftType == shift.ShiftType)
					{
						return existingShift.Object; // Return the existing shift if found
					}
				}

				return null; // No matching shift found
			}
			catch (Exception ex)
			{
				// Log or handle the exception as needed
				Console.WriteLine($"Error checking for existing shift: {ex.Message}");
				return null;
			}
		}

		public async Task AssignStaffToShiftAsync(StaffShiftAssignment shiftAssignment)
		{
			try
			{
				// Initialize Firebase client
				_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

				// Retrieve the shift details using the shiftId
				var shift = await _firebaseClient.Child("Shifts").Child(shiftAssignment.ShiftId).OnceSingleAsync<Shift>();
				if (shift == null)
				{
					throw new InvalidOperationException("Shift not found.");
				}

				// Check if the staff member already has an overlapping shift
				var hasOverlap = await CheckForOverlappingShiftsAsync(shiftAssignment.StaffId, shiftAssignment.AssignedDate, shiftAssignment.EndDate, shift.StartTime, shift.EndTime);
				if (hasOverlap)
				{
					throw new InvalidOperationException("The staff member already has an overlapping shift.");
				}

				// Create a new staff shift assignment
				var id = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 15);
				var staffShiftAssignment = new StaffShiftAssignment
				{
					StaffId = shiftAssignment.StaffId,
					ShiftId = shiftAssignment.ShiftId,
					AssignedDate = shiftAssignment.AssignedDate,
					EndDate = shiftAssignment.EndDate,
					ConfigurationId = shiftAssignment.ConfigurationId,
				};

				string reportId = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 15);

				var salaryReport = new SalaryReport
				{
					ReportId = reportId,
					StaffId = shiftAssignment.StaffId,
					AssignmentId = shiftAssignment.AssignmentId,
					FullName = "",
					ShiftName = shift.ShiftName,
					ShiftId = shiftAssignment.ShiftId,
					ConfigId = shiftAssignment.ConfigurationId,
					TotalShifts = 0,
					TotalPresent = 0,
					LateCount = 0,
					AbsenceCount = 0,
					TotalFines = 0,
					FinalSalary = 0,
					IsBilled = false,
					FromDate = shiftAssignment.AssignedDate,
					ToDate = shiftAssignment.EndDate,
					TotalSlots = 0,
					TrainerId = ""
				};

				var options = new JsonSerializerOptions
				{
					PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
					DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
				};

				var jsonString = JsonSerializer.Serialize(staffShiftAssignment, options);

				// Assign the staff to the shift
				var assignmentTask = _firebaseClient.Child("StaffShiftAssignments").Child(id).PutAsync(jsonString);

				jsonString = JsonSerializer.Serialize(salaryReport, options);

				var reportTask = _firebaseClient.Child("SalaryReports").Child(reportId).PutAsync(jsonString);

				await Task.WhenAll(assignmentTask, reportTask);

				Console.WriteLine("Staff assigned to shift successfully.");
			}
			catch (Exception ex)
			{
				// Log or handle the exception as needed
				Console.WriteLine($"Error assigning staff to shift: {ex.Message}");
				throw;
			}
		}

		private async Task<bool> CheckForOverlappingShiftsAsync(string staffId, DateTime newAssignedDate, DateTime newEndDate, DateTime newShiftStartTime, DateTime newShiftEndTime)
		{
			try
			{
				// Get all shift assignments for the given staff member
				var existedAssignments = await _firebaseClient
					.Child("StaffShiftAssignments")
					.OrderBy("staffId")
					.EqualTo(staffId)
					.OnceAsync<StaffShiftAssignment>();

				// Filter assignments based on the overlap with the new shift date range
				var relatedAssignments = existedAssignments
					.Where(a => a.Object.AssignedDate < newEndDate && a.Object.EndDate > newAssignedDate)
					.ToList();

				// Extract all shift IDs from the filtered assignments
				var shiftIds = relatedAssignments.Select(a => a.Object.ShiftId).ToList();

				// Fetch all shifts in a single call
				var allShifts = await Task.WhenAll(
					shiftIds.Select(async shiftId =>
					{
						return await _firebaseClient.Child("Shifts").Child(shiftId).OnceSingleAsync<Shift>();
					})
				);

				// Check for overlaps with the new shift (same day and overlapping time)
				foreach (var shift in allShifts)
				{
					if (shift != null &&
						TimeOnly.FromDateTime(shift.StartTime) < TimeOnly.FromDateTime(newShiftEndTime) &&
						TimeOnly.FromDateTime(shift.EndTime) > TimeOnly.FromDateTime(newShiftStartTime))
					{
						return true; // There is an overlap
					}
				}

				return false; // No overlap found

			}
			catch (Exception ex)
			{
				// Log or handle the exception as needed
				Console.WriteLine($"Error checking for overlapping shifts: {ex.Message}");
				return false; // Return false if there was an error checking for overlaps
			}
		}

		public async Task<Shift> ShiftAtTimeAsync(string staffId, DateTime checkTime)
		{
			try
			{
				_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

				// Get all the staff's shift assignments
				var staffAssignments = await _firebaseClient
					.Child("StaffShiftAssignments")
					.OrderBy("staffId")
					.EqualTo(staffId)
					.OnceAsync<StaffShiftAssignment>();

				var relatedAssignments = staffAssignments
					.Where(a => a.Object.AssignedDate <= checkTime && a.Object.EndDate >= checkTime);

				// Create a list of tasks to fetch shifts in parallel
				var shiftTasks = relatedAssignments.Select(async assignment =>
				{
					var staffShift = await _firebaseClient
						.Child("Shifts")
						.Child(assignment.Object.ShiftId)
						.OnceSingleAsync<Shift>();

					if (staffShift != null)
					{
						// Compare the checkTime with the shift's start and end time
						if (TimeOnly.FromDateTime(checkTime) >= TimeOnly.FromDateTime(staffShift.StartTime)
						&& TimeOnly.FromDateTime(checkTime) < TimeOnly.FromDateTime(staffShift.EndTime))
						{
							return staffShift; // Return the matching shift
						}
					}

					// Return null if no matching shift is found
					return null;
				}).ToList();

				// Await all the tasks and filter out null results
				var validShifts = (await Task.WhenAll(shiftTasks)).Where(shift => shift != null).ToList();

				// Ensure there’s only one valid shift
				if (validShifts.Count > 1)
				{
					throw new InvalidOperationException("Multiple valid shifts found for the given checkTime.");
				}
				else if (validShifts.Count == 0)
				{
					return null;
				}

				// Return the valid shift or null if none found
				var foundShift = validShifts.FirstOrDefault();

				// Return true if any shift assignment matches the time
				return foundShift;
			}
			catch (Exception ex)
			{
				// Handle exceptions (e.g., logging or rethrowing)
				Console.WriteLine($"Error checking staff shift: {ex.Message}");
				throw;
			}
		}

	}

}
