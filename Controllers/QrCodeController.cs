using Alpha_API.Models;
using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Text;

namespace Alpha_API.Controllers
{
	[ApiController]
	[Route("api/qr")]
	public class QrCodeController : ControllerBase
	{
		private readonly IHttpClientFactory _httpClientFactory;
		private FirebaseClient _firebaseClient;
		private const string FirebaseBaseUrl = "https://sgm-management-c98cd-default-rtdb.firebaseio.com/";

		public QrCodeController(IHttpClientFactory httpClientFactory)
		{
			_httpClientFactory = httpClientFactory;
		}

		[HttpPost("generate")]
		public async Task<IActionResult> GenerateQrCode([FromBody] QrCodeRequest request)
		{
			var idToken = HttpContext.Session.GetString("FirebaseIdToken");
			_firebaseClient = new FirebaseClient(FirebaseBaseUrl,
	new FirebaseOptions
	{
		AuthTokenAsyncFactory = () => Task.FromResult(idToken)
	});

			//get available courses
			var courses = await _firebaseClient
				.Child("Courses")
				.OnceAsync<Course>();

			var courseList = new List<Course>();
			foreach (var course in courses)
			{
				course.Object.CourseId = course.Key;
				courseList.Add(course.Object);
			}

			if (request == null)
			{
				return BadRequest("Request cannot be null.");
			}

			var qrList = new List<object>(); // List to hold both qrDataUrl and course details

			foreach (Course course in courseList)
			{
				try
				{
					var client = _httpClientFactory.CreateClient();
					var apiUrl = "https://api.vietqr.io/v2/generate";

					// Prepare the request data
					var info = "DK" + Guid.NewGuid().ToString();

					// Save membership to Firebase
					await _firebaseClient
						.Child("Memberships")
						.Child(info)
						.PutAsync(new
						{
							MembershipStartDate=DateTime.Now,
							//MembershipEndDate = DateTime.Now.AddDays(course.CourseDuration),
							MembershipEndDate = DateTime.Now.AddDays(0),
							course.CourseId,
							UserId=request.Uid
						});


					var jsonData = new
					{
						accountNo = "0978788128",
						accountName = "DINH DAI DUONG",
						acqId = "970422",
						addInfo = info,
						amount = course.CoursePrice,
						template = "compact"
					};

					// Serialize request data to JSON
					var jsonContent = Newtonsoft.Json.JsonConvert.SerializeObject(jsonData);
					var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

					// Add headers
					client.DefaultRequestHeaders.Add("x-client-id", "e27b68d6-aadc-44b8-bee3-9b77f39e9e0e");
					client.DefaultRequestHeaders.Add("x-api-key", "3163d46b-a727-4cc8-a841-fd8e0910dd57");

					// Send POST request to VietQR API
					HttpResponseMessage response = await client.PostAsync(apiUrl, content);

					if (response.IsSuccessStatusCode)
					{
						var responseData = await response.Content.ReadAsStringAsync();
						var jsonResponse = JObject.Parse(responseData);

						// Extract qrDataURL from the response
						var qrDataUrl = jsonResponse["data"]?["qrDataURL"]?.ToString();

						if (!string.IsNullOrEmpty(qrDataUrl))
						{
							// Add both the QR data and course details to the response
							qrList.Add(new
							{
								qrDataUrl,
								courseDetails = new
								{
									course.CourseName,
									course.CourseContent,
									course.CourseDuration,
									course.CoursePrice
								}
							});
						}
						else
						{
							return BadRequest("QR data URL not found in response.");
						}
					}
					else
					{
						return StatusCode((int)response.StatusCode, $"Error: {response.ReasonPhrase}");
					}
				}
				catch (Exception ex)
				{
					return StatusCode(500, $"Internal server error: {ex.Message}");
				}
			}

			// Optionally, you can convert this Data URI to an image file or return it directly
			return Ok(new { qrList }); // Return the Data URI
		}
	}

	public class QrCodeRequest
	{
		public string Uid { get; set; }
	}
}
