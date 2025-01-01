using Alpha_API.ViewModel;

namespace Alpha_API.Wrapper.Interfaces
{
	public interface IQrCodeService
	{
		Task<List<object>> GenerateQrCodeAsync(RegisterPackageRequest request, string customerId);
	}
}
