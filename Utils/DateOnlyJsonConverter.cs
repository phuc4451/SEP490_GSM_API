using System.Text.Json;
using System.Text.Json.Serialization;

namespace Alpha_API.Utils
{
	public class DateOnlyJsonConverter : JsonConverter<DateOnly>
	{
		private const string DateFormat = "yyyy-MM-dd"; // Adjust the format if needed

		public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var dateString = reader.GetString();
			return DateOnly.ParseExact(dateString, DateFormat);
		}

		public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
		{
			writer.WriteStringValue(value.ToString(DateFormat));
		}
	}

}
