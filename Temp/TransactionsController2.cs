using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Text;

namespace Alpha_API.Temp
{
    [ApiController]
    [Route("api/mbbank")]

    public class TransactionsController2 : ControllerBase
    {
        protected readonly string access_token = "EXO3SBW0CFF4PWJ7CCW9K6YGVS62Z0U5ERZAUB1VMNLNBDZJAR2BE3IXWFJMI7HY";
        //private readonly TwoSportDBContext _context; // Add database context here


        public TransactionsController2(IHttpClientFactory httpClientFactory)
        {

        }
        [HttpGet("check-transactions")]
        public async Task<IActionResult> CheckTransactions(string accountNumber = "0978788128",
        string? transactionDateMin = "", string? transactionDateMax = "",
        string? sinceId = "",
        string? limit = "5000",
    string? referenceNumber = "",
    string? amountIn = "",
    string? amountOut = "")
        {
            try
            {
                var json = await GetListTransactionsAsync(access_token, accountNumber, transactionDateMin, transactionDateMax,
                    sinceId, limit, referenceNumber, amountIn, amountOut);
                return Ok(json);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error checking transactions: {ex.Message}");
            }
        }


        static async Task<string> GetListTransactionsAsync(string accessToken, string accountNumber = "",
    string transactionDateMin = "", string transactionDateMax = "",
    string sinceId = "",
    string limit = "",
    string referenceNumber = "",
    string amountIn = "",
    string amountOut = "")
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Build the endpoint with query parameters
                    string dataEndpoint = "https://my.sepay.vn/userapi/transactions/list?";

                    if (!string.IsNullOrEmpty(accountNumber))
                        dataEndpoint += $"account_number={accountNumber}&";
                    if (!string.IsNullOrEmpty(transactionDateMin))
                        dataEndpoint += $"transaction_date_min={transactionDateMin}&";
                    if (!string.IsNullOrEmpty(transactionDateMax))
                        dataEndpoint += $"transaction_date_max={transactionDateMax}&";
                    if (!string.IsNullOrEmpty(sinceId))
                        dataEndpoint += $"since_id={sinceId}&";
                    if (!string.IsNullOrEmpty(limit))
                        dataEndpoint += $"limit={limit}&";
                    if (!string.IsNullOrEmpty(referenceNumber))
                        dataEndpoint += $"reference_number={referenceNumber}&";
                    if (!string.IsNullOrEmpty(amountIn))
                        dataEndpoint += $"amount_in={amountIn}&";
                    if (!string.IsNullOrEmpty(amountOut))
                        dataEndpoint += $"amount_out={amountOut}&";

                    // Remove the last '&' if it exists
                    if (dataEndpoint.EndsWith("&"))
                        dataEndpoint = dataEndpoint.Substring(0, dataEndpoint.Length - 1);

                    // Set authorization header
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
                    client.DefaultRequestHeaders.Add("Accept", "application/json");

                    // Send a GET request to the data endpoint
                    HttpResponseMessage response = await client.GetAsync(dataEndpoint);

                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        Console.WriteLine("Data retrieval failed. Status Code: " + response.StatusCode);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred during data retrieval: " + ex.Message);
            }
            return null;
        }

    }
}




