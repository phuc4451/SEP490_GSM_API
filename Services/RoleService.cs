namespace Alpha_API.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alpha_API.Models;
using Alpha_API.Wrapper.Interfaces;
using Firebase.Database;
using Firebase.Database.Query;

public class RoleService :IRoleService
{
	private readonly FirebaseClient _firebaseClient;

	public RoleService(FirebaseClient firebaseClient)
	{
		_firebaseClient = firebaseClient;
	}

	// Method to get all roles
	public async Task<List<Role>> GetAllRoles()
	{
		try
		{
			// Query Firebase to get all roles from the "Roles" node
			var roleQuery = await _firebaseClient
				.Child("Roles")
				.OnceAsync<Role>();

			// Convert the Firebase query results into a list of roles
			var roles = roleQuery.Select(item => new Role
			{
				RoleId = item.Key, 
				RoleName = item.Object.RoleName
			}).ToList();

			return roles;
		}
		catch (Exception ex)
		{
			// Log exception or handle it appropriately
			Console.WriteLine($"Error fetching roles: {ex.Message}");
			return new List<Role>(); // Return an empty list in case of error
		}
	}

	// Method to get a role name by its roleId
	public async Task<string> GetRoleName(string roleId)
	{
		try
		{
			// Query Firebase to get a specific role by roleId from the "Roles" node
			var role = await _firebaseClient
				.Child("Roles")
				.Child(roleId) // Use roleId directly as the key
				.OnceSingleAsync<Role>();

			if (role == null)
			{
				return "no role match";
			}

			return role.RoleName;
		}
		catch (Exception ex)
		{
			// Log exception or handle it appropriately
			Console.WriteLine($"Error fetching role by ID: {ex.Message}");
			return ""; // Return an empty string in case of error
		}
	}

	// Method to get the role of a user by their userId
	public async Task<string> GetRoleOfUser(string userId)
	{
		try
		{
			// Query Firebase to get the user by userId from the "Users" node
			var user = await _firebaseClient
				.Child("users")
				.Child(userId) // Use userId directly as the key
				.OnceSingleAsync<User>();

			if (user == null || string.IsNullOrEmpty(user.RoleId))
			{
				return "User not found or no role assigned"; // User not found or no role assigned
			}

			// Now use the roleId from the user to get the role name
			var roleName = await GetRoleName(user.RoleId);

			return roleName;
		}
		catch (Exception ex)
		{
			// Log exception or handle it appropriately
			Console.WriteLine($"Error fetching role of user {userId}: {ex.Message}");
			return "error"; // Return error message in case of exception
		}
	}
}
