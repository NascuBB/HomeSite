using HomeSite.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using HomeSite.Helpers;

namespace HomeSite.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet("/sse")]
        public async Task GetServerStats()
        {
            Response.ContentType = "text/event-stream";
            while(true)
            {
                await Response.WriteAsync($"data: {{\"CpuUsage\": {(int)ServerInfo.CpuPercentage},\"GpuUsage\": {(int)ServerInfo.GpuPercentage}, \"MemoryUsage\": {(int)ServerInfo.MemFree}}}\n\n");
                await Response.Body.FlushAsync();
                await Task.Delay(5000);
            }
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
