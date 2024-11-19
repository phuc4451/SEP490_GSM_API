using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Alpha_API.Models;
using Alpha_API.ViewModel;
using Firebase.Database;
using Firebase.Database.Query;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Net;
using System.Security.Claims;
using System.Text;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Text.Json;
using Alpha_API.Utils;
using System.Collections.Concurrent;
using Alpha_API.Services;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Alpha_API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class FaceDataController : ControllerBase
	{
		private FirebaseClient _firebaseClient;
		private readonly FirebaseClientProvider _firebaseClientProvider;
		public FaceDataController(FirebaseClient firebaseClient, FirebaseClientProvider firebaseClientProvider)
		{
			_firebaseClientProvider = firebaseClientProvider;
			_firebaseClient = firebaseClient;
		}

		[HttpPost("RegisterFace")]
		public async Task<IActionResult> AddFace([FromBody] FaceData faceData)
		{
			try
			{
				if (faceData == null)
				{
					return BadRequest("Invalid data.");
				}

				_firebaseClient = _firebaseClientProvider.GetFirebaseClient();

				var user = await _firebaseClient
					.Child("users")
					.OrderBy("email")
					.EqualTo(faceData.Email)
					.OnceSingleAsync<User>();

				if (user == null)
				{
					return BadRequest("Email not exist.");
				}

				var embeddings = JsonConvert.SerializeObject(faceData.Embeddings);

				await _firebaseClient
					.Child("faces")
					.Child(user.Email)
					.PutAsync(embeddings);

				return Ok();
			}
			catch(Exception e)
			{
				return StatusCode(500, new { message = $"An error occurred during face registration. {e.Message}" });
			}
		}

		public class FaceData
		{
			public string Email { get; set; }
			public List<List<float>> Embeddings { get; set; }
		}

	}
}
