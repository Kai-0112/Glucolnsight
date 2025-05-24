using System.Diagnostics;
using Glucolnsight.Models;
using Microsoft.AspNetCore.Mvc;

namespace Glucolnsight.Controllers
{
    public class HomeController : Controller
    {
        // 這支 Action 回傳 SPA 的 Razor 視圖
        [HttpGet("/prediction")]
        public IActionResult Index()
        {
            return View();  // Views/Home/Index.cshtml
        }
    }
}
