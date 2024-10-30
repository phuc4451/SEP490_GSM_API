using Microsoft.AspNetCore.Mvc;
using Firebase.Database;
using Firebase.Database.Query;
using Alpha_API.Models;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "admin")]
    public class OtherServiceController : ControllerBase
    {
        private readonly FirebaseClient _firebaseClient;

    public OtherServiceController()
    {
        _firebaseClient = new FirebaseClient("https://sgm-management-c98cd-default-rtdb.firebaseio.com/");
    }

    // GET: api/otherservice
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OtherService>>> GetOtherServices()
    {
        var services = await _firebaseClient
            .Child("OtherServices")
            .OnceAsync<OtherService>();

        var serviceList = new List<OtherService>();
        foreach (var service in services)
        {
            var serviceObject = service.Object;
            serviceObject.ServiceId = service.Key; // Set ServiceId from Firebase key
            serviceList.Add(serviceObject);
        }

        return Ok(serviceList);
    }

    // POST: api/otherservice
    [HttpPost]
    public async Task<ActionResult<OtherService>> PostOtherService([FromBody] OtherService service)
    {
        if (service == null)
        {
            return BadRequest();
        }

        // Tính toán TotalPrice dựa trên Quantity, Price và Discount
        service.TotalPrice = service.Quantity * service.Price * (1 - service.Discount / 100);

        var result = await _firebaseClient
            .Child("OtherServices")
            .PostAsync(new
            {
                service.ServiceName,
                service.Quantity,
                service.Price,
                service.Discount,
                service.TotalPrice
            });

        service.ServiceId = result.Key;
        return CreatedAtAction(nameof(GetOtherServiceById), new { id = service.ServiceId }, service);
    }

    // GET: api/otherservice/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<OtherService>> GetOtherServiceById(string id)
    {
        var service = await _firebaseClient
            .Child("OtherServices")
            .Child(id)
            .OnceSingleAsync<OtherService>();

        if (service == null)
        {
            return NotFound();
        }

        service.ServiceId = id;
        return Ok(service);
    }

    // PUT: api/otherservice/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult> PutOtherService(string id, [FromBody] OtherService service)
    {
        if (id != service.ServiceId)
        {
            return BadRequest();
        }

        // Tính toán TotalPrice
        service.TotalPrice = service.Quantity * service.Price * (1 - service.Discount / 100);

        await _firebaseClient
            .Child("OtherServices")
            .Child(id)
            .PutAsync(service);

        return NoContent();
    }

    // PATCH: api/otherservice/{id}
    [HttpPatch("{id}")]
    public async Task<ActionResult> PatchOtherService(string id, [FromBody] OtherService service)
    {
        var existingService = await _firebaseClient
            .Child("OtherServices")
            .Child(id)
            .OnceSingleAsync<OtherService>();

        if (existingService == null)
        {
            return NotFound();
        }

        // Cập nhật các trường đã thay đổi
        if (!string.IsNullOrEmpty(service.ServiceName))
        {
            existingService.ServiceName = service.ServiceName;
        }
        if (service.Quantity > 0)
        {
            existingService.Quantity = service.Quantity;
        }
        if (service.Price > 0)
        {
            existingService.Price = service.Price;
        }
        if (service.Discount >= 0)
        {
            existingService.Discount = service.Discount;
        }

        // Tính toán lại TotalPrice
        existingService.TotalPrice = existingService.Quantity * existingService.Price 
            * (1 - existingService.Discount / 100);

        await _firebaseClient
            .Child("OtherServices")
            .Child(id)
            .PutAsync(existingService);

        return NoContent();
    }

    // DELETE: api/otherservice/{id}
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteOtherService(string id)
    {
        var existingService = await _firebaseClient
            .Child("OtherServices")
            .Child(id)
            .OnceSingleAsync<OtherService>();

        if (existingService == null)
        {
            return NotFound();
        }

        await _firebaseClient
            .Child("OtherServices")
            .Child(id)
            .DeleteAsync();

        return NoContent();
    }
}
