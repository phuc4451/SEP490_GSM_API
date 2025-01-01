using Microsoft.AspNetCore.Mvc;
using Firebase.Database;
using Alpha_API.Models;
using Firebase.Database.Query;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirebaseAdmin.Auth;

[Route("api/[controller]")]
[ApiController]
public class RoleController : ControllerBase
{
	private FirebaseClient _firebaseClient;

	public RoleController(FirebaseClient firebaseClient)
	{
		_firebaseClient = firebaseClient;
	}

	// GET: api/roles
	[HttpGet]
	public async Task<ActionResult<IEnumerable<Role>>> GetRoles()
	{
		var roles = await _firebaseClient
			.Child("Roles")
			.OnceAsync<Role>();

		var roleList = roles.Select(r => r.Object).ToList(); // Flatten the roles

		return Ok(roleList);
	}

	// POST: api/roles
	[HttpPost]
	public async Task<ActionResult<Role>> AddRole([FromBody] Role role)
	{
		if (role == null)
		{
			return BadRequest();
		}

		// Save the role in the database without assigning to a variable
		var result = await _firebaseClient
			.Child("Roles")
			.PostAsync(new { 
				role.RoleName
             });

		// Return Created with the role URI and the created role object
		return CreatedAtAction(nameof(GetRoleById), new { id = result.Key }, role);
	}

	// GET: api/roles/{id}
	[HttpGet("GetRoleById/{id}")]
	public async Task<ActionResult<Role>> GetRoleById(string id)
	{
		var role = await _firebaseClient
			.Child("Roles")
			.Child(id)
			.OnceSingleAsync<Role>();

		if (role == null)
		{
			return NotFound();
		}

		return Ok(role);
	}


	// GET: api/roles/{id}
	[HttpGet("GetRoleNameById/{id}")]
	public async Task<ActionResult<string>> GetRoleNameById(string id)
	{
		var role = await _firebaseClient
			.Child("Roles")
			.Child(id)
			.OnceSingleAsync<Role>();

		if (role == null)
		{
			return NotFound();
		}

		return Ok(role.RoleName);
	}

	// PUT: api/roles/{id}
	[HttpPut("{id}")]
	public async Task<ActionResult> PutRole(string id, [FromBody] Role role)
	{
		var existingRole = await _firebaseClient
			.Child("Roles")
			.Child(id.ToString())
			.OnceSingleAsync<Role>();

		if (existingRole == null)
		{
			return NotFound();
		}

		// Update the role
		await _firebaseClient
			.Child("Roles")
			.Child(id.ToString())
			.PutAsync(role);

		return NoContent();
	}

	// DELETE: api/roles/{id}
	[HttpDelete("{id}")]
	public async Task<ActionResult> DeleteRole(string id)
	{
		var existingRole = await _firebaseClient
			.Child("Roles")
			.Child(id)
			.OnceSingleAsync<Role>();

		if (existingRole == null)
		{
			return NotFound();
		}

		await _firebaseClient
			.Child("Roles")
			.Child(id)
			.DeleteAsync();

		return NoContent();
	}
}
