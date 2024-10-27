using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Alpha_API.Models
{
	public class User
	{
		public string UserId { get; set; }
		public string Name { get; set; }
		public string Email { get; set; }
		public string Gender { get; set; }
		public CustomDateTime Dob { get; set; }
		public string Address { get; set; }
		public string Phone { get; set; }
		public string RoleId { get; set; }
		public string UserAvatar { get; set; }
		public CardId IdCard { get; set; }

		public CustomDateTime MapDateTimeToCustomFormat(DateTime dateTime)
		{
			return new CustomDateTime
			{
				Date = dateTime.Day,
				Month = dateTime.Month,
				Year = dateTime.Year
			};
		}

	}


	public class CardId
	{
		[JsonPropertyName("id")]
		public string Id { get; set; }
	}

	public class CustomDateTime
	{
		[JsonPropertyName("date")]
		public int Date { get; set; }

		[JsonPropertyName("month")]
		public int Month { get; set; }

		[JsonPropertyName("year")]
		public int Year { get; set; }
	}

}
