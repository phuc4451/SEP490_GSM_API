using Microsoft.AspNetCore.Mvc;
using Firebase.Database;
using System.Collections.Generic;
using System.Threading.Tasks;
using Alpha_API.Models;
using Firebase.Database.Query;

[Route("api/[controller]")]
[ApiController]
public class TrainingHistoryController : ControllerBase
{
	private readonly FirebaseClient _firebaseClient;

	public TrainingHistoryController()
	{
		_firebaseClient = new FirebaseClient("https://sgm-management-c98cd-default-rtdb.firebaseio.com/");
	}

	// GET: api/traininghistory
	[HttpGet]
	public async Task<ActionResult<IEnumerable<TrainingHistory>>> GetTrainingHistories()
	{
		var trainingHistories = await _firebaseClient
			.Child("TrainingHistory")
			.OnceAsync<TrainingHistory>();

		var historyList = new List<TrainingHistory>();
		foreach (var history in trainingHistories)
		{
			historyList.Add(history.Object);
		}

		return Ok(historyList);
	}

	// POST: api/traininghistory
	[HttpPost]
	public async Task<ActionResult<TrainingHistory>> PostTrainingHistory([FromBody] TrainingHistory trainingHistory)
	{
		if (trainingHistory == null)
		{
			return BadRequest();
		}

		// Add logic to check if MembershipId exists, if needed.

		var result = await _firebaseClient
			.Child("TrainingHistory")
			.PostAsync(new
			{
				trainingHistory.MembershipId,
				trainingHistory.CheckInTime,
				trainingHistory.CheckOutTime,
			});

		trainingHistory.HistoryId = result.Key; // Firebase generates a unique key

		return CreatedAtAction(nameof(GetTrainingHistoryById), new { id = trainingHistory.HistoryId }, trainingHistory);
	}

	// GET: api/traininghistory/{id}
	[HttpGet("{id}")]
	public async Task<ActionResult<TrainingHistory>> GetTrainingHistoryById(string id)
	{
		var trainingHistory = await _firebaseClient
			.Child("TrainingHistory")
			.Child(id)
			.OnceSingleAsync<TrainingHistory>();

		if (trainingHistory == null)
		{
			return NotFound();
		}

		return Ok(trainingHistory);
	}

	// PUT: api/traininghistory/{id}
	[HttpPut("{id}")]
	public async Task<ActionResult> PutTrainingHistory(string id, [FromBody] TrainingHistory trainingHistory)
	{
		if (id != trainingHistory.HistoryId.ToString())
		{
			return BadRequest();
		}

		await _firebaseClient
			.Child("TrainingHistory")
			.Child(id)
			.PutAsync(trainingHistory);

		return NoContent(); // 204 No Content
	}

//	// GET: api/traininghistory/today/{userId}
//	[HttpGet("today/{userId}")]
//	public async Task<ActionResult<IEnumerable<TrainingHistory>>> GetTodayTrainingHistoriesByUser(string userId)
//	{
//		var trainingHistories = await _firebaseClient
//			.Child("TrainingHistory")
//			.OnceAsync<TrainingHistory>();

//		var today = DateTime.UtcNow.Date; // Get current day in UTC
//		var todayHistories = new List<TrainingHistory>();

//		foreach (var history in trainingHistories)
//		{
//			string userid="";
//			string courseId="";
//			var parts = history.Object.MembershipId.Split('_');
//			if (parts.Length == 2)
//			{
//				courseId = parts[0];
//				userid = parts[1];
//			}

//			var existingMembership = await _firebaseClient
//.Child("Memberships")
//.Child(history.Key)
//.OnceSingleAsync<Membership>();
//			// Check if the UserId matches and if the CheckInTime or CheckOutTime is today
//			if (userid == userId &&
//				(history.Object.CheckInTime.Date == today || history.Object.CheckOutTime.Date == today))
//			{
//				todayHistories.Add(history.Object);
//			}
//		}

//		return Ok(todayHistories);
//	}


	// PATCH: api/traininghistory/{id}
	[HttpPatch("{id}")]
	public async Task<ActionResult> PatchTrainingHistory(string id, [FromBody] TrainingHistory trainingHistory)
	{
		var existingHistory = await _firebaseClient
			.Child("TrainingHistory")
			.Child(id)
			.OnceSingleAsync<TrainingHistory>();

		//if (existingHistory == null)
		//{
		//	return NotFound();
		//}

		//// Update fields selectively
		//if (trainingHistory.CheckInTime != default)
		//{
		//	existingHistory.CheckInTime = trainingHistory.CheckInTime;
		//}
		//if (trainingHistory.CheckOutTime != default)
		//{
		//	existingHistory.CheckOutTime = trainingHistory.CheckOutTime;
		//}


		await _firebaseClient
			.Child("TrainingHistory")
			.Child(id)
			.PutAsync(existingHistory);

		return NoContent(); // 204 No Content
	}

	// DELETE: api/traininghistory/{id}
	[HttpDelete("{id}")]
	public async Task<ActionResult> DeleteTrainingHistory(string id)
	{
		var existingHistory = await _firebaseClient
			.Child("TrainingHistory")
			.Child(id)
			.OnceSingleAsync<TrainingHistory>();

		if (existingHistory == null)
		{
			return NotFound();
		}

		await _firebaseClient
			.Child("TrainingHistory")
			.Child(id)
			.DeleteAsync();

		return NoContent(); // 204 No Content
	}
}
