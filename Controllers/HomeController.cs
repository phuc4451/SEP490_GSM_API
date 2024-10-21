using Microsoft.AspNetCore.Mvc;

namespace Alpha_API.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
