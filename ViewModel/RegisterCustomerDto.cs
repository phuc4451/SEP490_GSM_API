using Alpha_API.Models;

namespace Alpha_API.ViewModel
{
	public class RegisterCustomerDto
	{
		public string Email { get; set; }
		public string Name { get; set; }
		public string Gender { get; set; }
		public CustomDateTime Dob { get; set; }
		public string Address { get; set; }
		public string Phone { get; set; }
		public string UserAvatar { get; set; }
		public string IdCard { get; set; }
	}
}
