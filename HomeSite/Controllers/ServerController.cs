using HomeSite.Helpers;
using HomeSite.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace HomeSite.Controllers
{
    public class ServerController : Controller
    {
        public IActionResult Index()
        {
            string logis = MinecraftServerManager.GetInstance().consoleLogs;
            return View(new ServerViewModel { IsRunning = MinecraftServerManager.GetInstance().IsRunning, ServerState = MinecraftServerManager.GetInstance().ServerProcess == null ? ServerState.starting : ServerState.started, logs = logis});
        }
    }
}
