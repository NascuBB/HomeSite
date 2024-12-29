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
            return View(new ServerViewModel { IsRunning = MinecraftServerManager.GetInstance().IsRunning, ServerState = MinecraftServerManager.GetInstance().ServerProcess == null ? ServerState.starting : ServerState.started, logs = MinecraftServerManager.GetInstance().ConsoleLogs});
        }
    }
}
