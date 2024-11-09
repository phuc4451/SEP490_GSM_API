using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
namespace Alpha_API.Models
{
	public class ImportEquipment
	{
		public string ImportEquipmentId { get; set; }
		public CustomDateTimeIE ImportDate { get; set; }
		public int ImportQuantity { get; set; }
		public decimal ImportTotalPrice{ get; set; }
        public string EquipmentId { get; set; }

        public decimal ImportPrice { get; set; }
        public CustomDateTimeIE MapDateTimeToCustomFormat(DateTime dateTime)
        {
            return new CustomDateTimeIE
            {
                Date = dateTime.Day,
                Month = dateTime.Month,
                Year = dateTime.Year
            };
        }
    }

    	public class CustomDateTimeIE
    {
		[JsonPropertyName("date")]
		public int Date { get; set; }

		[JsonPropertyName("month")]
		public int Month { get; set; }

		[JsonPropertyName("year")]
		public int Year { get; set; }
	}
}
