using Alpha_API.Temp;
using Microsoft.AspNetCore.Mvc;

namespace Alpha_API.Temp
{
    [Route("api/[controller]")]
    [ApiController]
    public class GymPackageController : ControllerBase
    {
        private readonly IGymPackageService _packageService;

        public GymPackageController(IGymPackageService packageService)
        {
            _packageService = packageService;
        }

        [HttpGet]
        public IActionResult GetAllPackages()
        {
            var packages = _packageService.GetAll();
            return Ok(packages);
        }

        [HttpGet("{id}")]
        public IActionResult GetPackageById(int id)
        {
            var package = _packageService.GetById(id);
            if (package == null)
            {
                return NotFound("Gym package not found.");
            }
            return Ok(package);
        }

        [HttpPost]
        public IActionResult CreatePackage([FromBody] GymPackage package)
        {
            if (package == null)
            {
                return BadRequest("Gym package is null.");
            }

            _packageService.Add(package);
            return CreatedAtAction(nameof(GetPackageById), new { id = package.Id }, package);
        }

        [HttpPut("{id}")]
        public IActionResult UpdatePackage(int id, [FromBody] GymPackage package)
        {
            if (package == null || package.Id != id)
            {
                return BadRequest("Package data is invalid.");
            }

            var existingPackage = _packageService.GetById(id);
            if (existingPackage == null)
            {
                return NotFound("Gym package not found.");
            }

            _packageService.Update(package);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult DeletePackage(int id)
        {
            var existingPackage = _packageService.GetById(id);
            if (existingPackage == null)
            {
                return NotFound("Gym package not found.");
            }

            _packageService.Delete(id);
            return NoContent();
        }
    }
}





































































public interface IGymPackageService
{
    IEnumerable<GymPackage> GetAll();
    GymPackage GetById(int id);
    void Add(GymPackage package);
    void Update(GymPackage package);
    void Delete(int id);
}

public class GymPackageService : IGymPackageService
{
    private readonly List<GymPackage> _packages = new List<GymPackage>();

    public IEnumerable<GymPackage> GetAll()
    {
        return _packages;
    }

    public GymPackage GetById(int id)
    {
        return _packages.FirstOrDefault(p => p.Id == id);
    }

    public void Add(GymPackage package)
    {
        package.Id = _packages.Count > 0 ? _packages.Max(p => p.Id) + 1 : 1;
        _packages.Add(package);
    }

    public void Update(GymPackage package)
    {
        var existingPackage = GetById(package.Id);
        if (existingPackage != null)
        {
            existingPackage.Name = package.Name;
            existingPackage.Description = package.Description;
            existingPackage.Price = package.Price;
            existingPackage.DurationInDays = package.DurationInDays;
        }
    }

    public void Delete(int id)
    {
        var package = GetById(id);
        if (package != null)
        {
            _packages.Remove(package);
        }
    }
}
