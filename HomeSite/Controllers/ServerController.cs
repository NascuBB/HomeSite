using HomeSite.Helpers;
using HomeSite.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace HomeSite.Controllers
{
    public class ServerController : Controller
    {
        public static SendType Sendtype { get; set; } = SendType.Skip;
        private int adaptivePing = 1000;
        public IActionResult Index()
        {
            string logis = MinecraftServerManager.GetInstance().ConsoleLogs;
            return View(new ServerViewModel { IsRunning = MinecraftServerManager.GetInstance().IsRunning, ServerState = MinecraftServerManager.GetInstance().ServerProcess == null ? ServerState.starting : ServerState.started, logs = logis});
        }

        [HttpGet("/Server/sti")]
        public async Task GetServerStats()
        {
            Response.ContentType = "text/event-stream";
            while (true)
            {
                switch (Sendtype)
                {
                    case SendType.Server:
                        await Response.WriteAsync($"data: {{" +
                            $"\"Type\": \"Server\"," +
                            $"\"State\": \"{MinecraftServerManager.GetInstance().ServerState}\"" +
                            $"}}\n\n");
                        await Response.Body.FlushAsync();
                        Sendtype = SendType.Info;
                    break;
                    case SendType.Info:
                        adaptivePing = 5000;
                        await Response.WriteAsync($"data: {{" +
                            $"\"Type\": \"Info\"," +
                            $"\"Players\": {MinecraftServer.GetInstance()!.Players}," +
                            $" \"MemoryUsage\": {(int)MinecraftServer.GetInstance()!.RamUsage}" +
                            $"}}\n\n");
                        await Response.Body.FlushAsync();
                    break;
                    case SendType.Skip:
                    break;
                }
                await Task.Delay(adaptivePing);
            }
        }
    }

    public enum SendType
    {
        Info,
        Server,
        Skip
    }
}
