using Alpha_API.ViewModel;
using Alpha_API.Wrapper.Interfaces;
using Newtonsoft.Json.Linq;
using System.Text;

namespace Alpha_API.Services
{
	public class QrCodeService : IQrCodeService
	{
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IRegisterService _registerService;

		public QrCodeService(IHttpClientFactory httpClientFactory,
							 IRegisterService registerService)
		{
			_httpClientFactory = httpClientFactory;
			_registerService = registerService;
		}

		public async Task<List<object>> GenerateQrCodeAsync(RegisterPackageRequest request, string customerId)
		{
			if (request == null)
			{
				throw new ArgumentNullException(nameof(request), "Request cannot be null.");
			}

			var client = _httpClientFactory.CreateClient();
			client.DefaultRequestHeaders.Add("x-client-id", "e27b68d6-aadc-44b8-bee3-9b77f39e9e0e");
			client.DefaultRequestHeaders.Add("x-api-key", "3163d46b-a727-4cc8-a841-fd8e0910dd57");

			var qrList = new List<object>();

			if (!string.IsNullOrEmpty(request.GymMembershipId))
			{
				var qrData = await ProcessRequestAsync(request, customerId, client, _registerService.RegisterGym);
				if (qrData != null) qrList.Add(qrData);
			}
			else if (!string.IsNullOrEmpty(request.TrainerRentalPlanId))
			{
				var qrData = await ProcessRequestAsync(request, customerId, client, _registerService.RegisterTrainerRental);
				if (qrData != null) qrList.Add(qrData);
			}
			else if (!string.IsNullOrEmpty(request.BoxingMembershipPlanId))
			{
				var qrData = await ProcessRequestAsync(request, customerId, client, _registerService.RegisterBoxing);
				if (qrData != null) qrList.Add(qrData);
			}

			return qrList;
		}

		private async Task<object> ProcessRequestAsync(RegisterPackageRequest request, string customerId, HttpClient client, Func<RegisterPackageRequest, bool, string, Task<RegisterResult>> registerFunc)
		{
			bool qrPayment = request.QRPayment;
			if (!qrPayment)
			{
				throw new InvalidOperationException("This request doesn't use qr payment.");
			}

			var registerResult = await registerFunc(request, qrPayment, customerId);

			#region MBBank
			//var jsonData = new
			//{
			//	accountNo = "0978788128",
			//	accountName = "DINH DAI DUONG",
			//	acqId = "970422",
			//	addInfo = registerResult.Info,
			//	amount = registerResult.Payment.Amount,
			//	template = "compact"
			//};
			#endregion

			var jsonData = new
			{
				accountNo = "105874147288",
				accountName = "VU HONG PHUC",
				acqId = "970415",
				addInfo = registerResult.TransactionContent,
				amount = registerResult.MoneyToPay,
				template = "compact2"
			};

			var jsonContent = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(jsonData), Encoding.UTF8, "application/json");
			var apiUrl = "https://api.vietqr.io/v2/generate";
			HttpResponseMessage response = await client.PostAsync(apiUrl, jsonContent);

			if (response.IsSuccessStatusCode)
			{
				var responseData = await response.Content.ReadAsStringAsync();
				var jsonResponse = JObject.Parse(responseData);
				var qrDataUrl = jsonResponse["data"]?["qrDataURL"]?.ToString();

				if (!string.IsNullOrEmpty(qrDataUrl))
				{
					return new
					{
						qrDataUrl,
						details = registerResult.ToQrDetails()
					};
				}
				else
				{
					throw new InvalidOperationException("QR data URL not found in the response.");
				}
			}
			else
			{
				throw new HttpRequestException($"Error: {response.ReasonPhrase} (Status Code: {response.StatusCode})");
			}
		}

	}
}
