using Alpha_API.ViewModel;

namespace Alpha_API.Wrapper.Interfaces
{
	public interface IRegisterService
	{
		Task<RegisterResult> RegisterGym(RegisterPackageRequest request, bool qrPayment, string customerId);
		Task<RegisterResult> RegisterTrainerRental(RegisterPackageRequest request,
	bool qrPayment, string customerId);
		Task<RegisterResult> RegisterBoxing(RegisterPackageRequest request,
			bool qrPayment, string customerId);
	}
}
