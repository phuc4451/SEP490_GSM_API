using Microsoft.AspNetCore.Mvc;
using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.AspNetCore.Authorization;
using Alpha_API.Temp;

[Route("api/[controller]")]
[ApiController]
//[Authorize(Policy = "AdminOnly")]
[Authorize(Roles = "admin")]
public class CourseController : ControllerBase
{
	private readonly FirebaseClient _firebaseClient;

	public CourseController()
	{
		_firebaseClient = new FirebaseClient("https://sgm-management-c98cd-default-rtdb.firebaseio.com/");
	}

	// GET: api/course
	[HttpGet]
	public async Task<ActionResult<IEnumerable<Course>>> GetCourses()
	{
		var courses = await _firebaseClient
			.Child("Courses")
			.OnceAsync<Course>();

		var courseList = new List<Course>();
		foreach (var course in courses)
		{
			var courseObject = course.Object;
			courseObject.CourseId = course.Key; // Set CourseId from Firebase key
			courseList.Add(courseObject);
		}

		return Ok(courseList);
	}

	// POST: api/course
	[HttpPost]
	public async Task<ActionResult<Course>> PostCourse([FromBody] Course course)
	{
		if (course == null)
		{
			return BadRequest();
		}

		// Add course to Firebase and use auto-generated key
		var result = await _firebaseClient
			.Child("Courses")
			.PostAsync(new
			{
				course.CourseName,
				course.CourseContent,
				course.CoursePrice,
				course.CourseDuration // Add CourseDuration field here
			});

		// Set the CourseId to Firebase's auto-generated key
		course.CourseId = result.Key;
		return CreatedAtAction(nameof(GetCourseById), new { id = course.CourseId }, course);
	}

	// GET: api/course/{id}
	[HttpGet("{id}")]
	public async Task<ActionResult<Course>> GetCourseById(string id)
	{
		var course = await _firebaseClient
			.Child("Courses")
			.Child(id.ToString())
			.OnceSingleAsync<Course>();

		if (course == null)
		{
			return NotFound();
		}

		course.CourseId = id; // Set CourseId based on the requested ID
		return Ok(course);
	}

	// PUT: api/course/{id}
	[HttpPut("{id}")]
	public async Task<ActionResult> PutCourse(string id, [FromBody] Course course)
	{
		if (id != course.CourseId)
		{
			return BadRequest();
		}

		await _firebaseClient
			.Child("Courses")
			.Child(id.ToString())
			.PutAsync(course);

		return NoContent(); // 204 No Content
	}

	// PATCH: api/course/{id}
	[HttpPatch("{id}")]
	public async Task<ActionResult> PatchCourse(string id, [FromBody] Course course)
	{
		var existingCourse = await _firebaseClient
			.Child("Courses")
			.Child(id.ToString())
			.OnceSingleAsync<Course>();

		if (existingCourse == null)
		{
			return NotFound();
		}

		// Update only the modified fields
		if (!string.IsNullOrEmpty(course.CourseName))
		{
			existingCourse.CourseName = course.CourseName;
		}
		if (!string.IsNullOrEmpty(course.CourseContent))
		{
			existingCourse.CourseContent = course.CourseContent;
		}
		if (course.CoursePrice > 0)
		{
			existingCourse.CoursePrice = course.CoursePrice;
		}
		//if (course.CourseDuration > 0)
		//{
		//	existingCourse.CourseDuration = course.CourseDuration;
		//}

		await _firebaseClient
			.Child("Courses")
			.Child(id.ToString())
			.PutAsync(existingCourse);

		return NoContent(); // 204 No Content
	}

	// DELETE: api/course/{id}
	[HttpDelete("{id}")]
	public async Task<ActionResult> DeleteCourse(string id)
	{
		var existingCourse = await _firebaseClient
			.Child("Courses")
			.Child(id.ToString())
			.OnceSingleAsync<Course>();

		if (existingCourse == null)
		{
			return NotFound();
		}

		await _firebaseClient
			.Child("Courses")
			.Child(id.ToString())
			.DeleteAsync();

		return NoContent(); // 204 No Content
	}
}
