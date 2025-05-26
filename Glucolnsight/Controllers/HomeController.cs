using System.Diagnostics;
using Glucolnsight.Models;
using Microsoft.AspNetCore.Mvc;

namespace Glucolnsight.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();  // Views/Home/Index.cshtml
        }
    }
}
