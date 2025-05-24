using System.Diagnostics;
using Glucolnsight.Models;
using Microsoft.AspNetCore.Mvc;

namespace Glucolnsight.Controllers
{
    public class HomeController : Controller
    {
        // �o�� Action �^�� SPA �� Razor ����
        [HttpGet("/prediction")]
        public IActionResult Index()
        {
            return View();  // Views/Home/Index.cshtml
        }
    }
}
