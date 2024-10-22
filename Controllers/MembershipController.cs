using Microsoft.AspNetCore.Mvc;
using Firebase.Database;
using Firebase.Database.Query;
using Alpha_API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class MembershipController : ControllerBase
{
	private readonly FirebaseClient _firebaseClient;

	public MembershipController()
	{
		_firebaseClient = new FirebaseClient("https://sgm-management-c98cd-default-rtdb.firebaseio.com/");
	}

	// GET: api/membership
	[HttpGet]
	public async Task<ActionResult<IEnumerable<Membership>>> GetMemberships()
	{
		var memberships = await _firebaseClient
			.Child("Memberships")
			.OnceAsync<Membership>();

		var membershipList = new List<Membership>();
		foreach (var membership in memberships)
		{
			var membershipWithId = membership.Object;
			//membershipWithId.MembershipId = int.Parse(membership.Key); // Assign key as MembershipId
			membershipList.Add(membershipWithId);
		}

		return Ok(membershipList);
	}

	[HttpGet("membership/{courseId}/{userId}")]
	public async Task<ActionResult<Membership>> GetMembership(string courseId, string userId)
	{
		string compositeKey = $"{courseId}_{userId}";

		var membership = await _firebaseClient
			.Child("Memberships")
			.Child(compositeKey)
			.OnceSingleAsync<Membership>();

		if (membership == null)
		{
			return NotFound();
		}

		membership.CourseId = courseId;
		membership.UserId = userId;

		return Ok(membership);
	}

	//// GET: api/membership/{id}
	//[HttpGet("{id}")]
	//public async Task<ActionResult<Membership>> GetMembershipById(string id)
	//{
	//	var membership = await _firebaseClient
	//		.Child("Memberships")
	//		.Child(id)
	//		.OnceSingleAsync<Membership>();

	//	if (membership == null)
	//	{
	//		return NotFound();
	//	}

	//	membership.MembershipId = int.Parse(id); // Assign key as MembershipId
	//	return Ok(membership);
	//}

	// POST: api/membership
	[HttpPost]
	public async Task<ActionResult> PostMembership([FromBody] Membership membership)
	{
		if (membership == null)
		{
			return BadRequest();
		}

		// Create a composite key using CourseId and UserId
		string compositeKey = $"{membership.CourseId}_{membership.UserId}";


		// Retrieve the course based on CourseId to get the CourseDuration
		var course = await _firebaseClient
			.Child("Courses")
			.Child(membership.CourseId) // Assuming CourseId is stored with the course as the key
			.OnceSingleAsync<Course>();

		if (course == null)
		{
			return NotFound("Course not found.");
		}

		var existingMembership = await _firebaseClient
	.Child("Memberships")
	.Child(compositeKey)
	.OnceSingleAsync<Membership>();

		if (membership == null)
		{

			// Calculate the MembershipEndDate
			//membership.MembershipEndDate = membership.MembershipStartDate.AddDays(course.CourseDuration);
			membership.MembershipEndDate = membership.MembershipStartDate.AddDays(0);

			// Save membership to Firebase
			await _firebaseClient
				.Child("Memberships")
				.Child(compositeKey)
				.PutAsync(new
				{
					membership.MembershipStartDate,
					membership.MembershipEndDate,
				});

			return CreatedAtAction(nameof(GetMembership), new { courseId = membership.CourseId, userId = membership.UserId }, membership);
		}

		else
		{
			return Conflict(new { message = "A membership for this user and course already exists." });
		}

	}


	// PUT: api/membership/{id}
	[HttpPut("{id}")]
	public async Task<ActionResult> PutMembership(string id, [FromBody] Membership membership)
	{
		var existingMembership = await _firebaseClient
			.Child("Memberships")
			.Child(id)
			.OnceSingleAsync<Membership>();

		if (existingMembership == null)
		{
			return NotFound();
		}

		await _firebaseClient
			.Child("Memberships")
			.Child(id)
			.PutAsync(membership);

		return NoContent();
	}

	// PATCH: api/membership/{id}
	[HttpPatch("{id}")]
	public async Task<ActionResult> PatchMembership(string id, [FromBody] Membership membership)
	{
		var existingMembership = await _firebaseClient
			.Child("Memberships")
			.Child(id)
			.OnceSingleAsync<Membership>();

		if (existingMembership == null)
		{
			return NotFound();
		}

		//if (membership.MembershipStartDate != default(DateTime))
		//{
		//	existingMembership.MembershipStartDate = membership.MembershipStartDate;
		//}

		//if (membership.MembershipEndDate != default(DateTime))
		//{
		//	existingMembership.MembershipEndDate = membership.MembershipEndDate;
		//}

		//if (membership.CourseId != null)
		//{
		//	existingMembership.CourseId = membership.CourseId;
		//}

		//if (membership.UserId != null)
		//{
		//	existingMembership.UserId = membership.UserId;
		//}

		await _firebaseClient
			.Child("Memberships")
			.Child(id)
			.PutAsync(existingMembership);

		return NoContent();
	}

	// DELETE: api/membership/{id}
	[HttpDelete("{id}")]
	public async Task<ActionResult> DeleteMembership(string id)
	{
		var existingMembership = await _firebaseClient
			.Child("Memberships")
			.Child(id)
			.OnceSingleAsync<Membership>();

		if (existingMembership == null)
		{
			return NotFound();
		}

		await _firebaseClient
			.Child("Memberships")
			.Child(id)
			.DeleteAsync();

		return NoContent();
	}
}
