using Alpha_API.Models;

namespace Alpha_API.ViewModel
{
	public class TrainerDto
	{
		public string Name { get; set; }
		public bool IsTrainerGym { get; set; }
		public bool IsTrainerBoxing { get; set; }
		public string Email { get; set; }
		public string Gender { get; set; }
		public CustomDateTime Dob { get; set; }
		public string Address { get; set; }
		public string Phone { get; set; }
		public string IdCard { get; set; }
	}
}
