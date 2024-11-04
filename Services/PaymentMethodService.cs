namespace Alpha_API.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alpha_API.Models;
using Firebase.Database;
using Firebase.Database.Query;

public class PaymentMethodService
{
	private readonly FirebaseClient _firebaseClient;

	public PaymentMethodService(FirebaseClient firebaseClient)
	{
		_firebaseClient = firebaseClient;
	}

	// Method to get all payment methods
	public async Task<List<PaymentMethod>> GetAllPaymentMethods()
	{
		try
		{
			// Query Firebase to get all methods from the "PaymentMethods" node
			var methodQuery = await _firebaseClient
				.Child("PaymentMethods")
				.OnceAsync<PaymentMethod>();

			// Convert the Firebase query results into a list of methods
			var methods = methodQuery.Select(item => new PaymentMethod
			{
				PaymentMethodId = item.Key,
				MethodName = item.Object.MethodName
			}).ToList();

			return methods;
		}
		catch (Exception ex)
		{
			// Log exception or handle it appropriately
			Console.WriteLine($"Error fetching roles: {ex.Message}");
			return new List<PaymentMethod>(); // Return an empty list in case of error
		}
	}

	// Method to get a role name by its roleId
	public async Task<string> GetPaymentMethodName(string methodId)
	{
		try
		{
			// Query Firebase to get a specific method by methodId from the "PaymentMethods" node
			var method = await _firebaseClient
				.Child("PaymentMethods")
				.Child(methodId) // Use methodId directly as the key
				.OnceSingleAsync<PaymentMethod>();

			if (method == null)
			{
				return "no method match";
			}

			return method.MethodName;
		}
		catch (Exception ex)
		{
			// Log exception or handle it appropriately
			Console.WriteLine($"Error fetching payment method by ID: {ex.Message}");
			return ""; // Return an empty string in case of error
		}
	}
}
