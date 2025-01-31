using HomeSite.Entities;
using HomeSite.Managers;
using HomeSite.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace HomeSite.Controllers
{
    public class ServerController : Controller
    {
        private readonly UserDBContext _usersContext;
        private static readonly ConcurrentDictionary<string, List<HttpResponse>> _subscribers = new();

        public ServerController(UserDBContext userDBContext)
        {
            _usersContext = userDBContext;
        }

        public IActionResult Index()
        {
            string? username = HttpContext.User.Identity.Name;
            if (username == null)
            {
                ViewBag.Message = "Теперь, чтобы воспользоваться функциями сервера нужно зайти в аккаунт";
                return RedirectToAction("Login", "Account");
            }
            UserAccount user = _usersContext.UserAccounts.FirstOrDefault(x => x.Username == username)!;
            if (user.ServerID == "no")
            {
                return View(new ServerViewModel { ServerCreation = ServerCreation.notCreated, AllowedServers = null });
            }
            var specs = MinecraftServerManager.GetServerSpecs(user.ServerID);
            if (MinecraftServerManager.inCreation.ContainsKey(user.ServerID))
            {
                return View(new ServerViewModel { AllowedServers = null, OwnServer = new MinecraftServerWrap { Description = specs.Description, Id = user.ServerID, IsRunning = false, Name = specs.Name}, ServerCreation = ServerCreation.AddingMods });
            }
            //string logis = MinecraftServerManager.GetInstance().ConsoleLogs;
            return View(new ServerViewModel { AllowedServers = null, OwnServer = new MinecraftServerWrap { Description = specs.Description, Id = user.ServerID, IsRunning = MinecraftServerManager.serversOnline.Any(x => x.Id == user.ServerID), Name = specs.Name }, ServerCreation = ServerCreation.Created });
            //return View(new ServerViewModel { IsRunning = MinecraftServerManager.GetInstance().IsRunning, ServerState = MinecraftServerManager.GetInstance().ServerProcess == null ? ServerState.starting : ServerState.started, logs = logis});
        }

        public IActionResult create()
        {
            if (HttpContext.User.Identity.Name == null)
            {
                ViewBag.Message = "Теперь, чтобы воспользоваться функциями сервера нужно зайти в аккаунт";
                return RedirectToAction("Login", "Account");
            }
            if (_usersContext.UserAccounts.FirstOrDefault(x => x.Username == HttpContext.User.Identity.Name)!.ServerID != "no")
            {
                return RedirectToAction("Index");
            }
            return View();
        }

        [HttpPost]
        public IActionResult create(CreateServerViewModel model)
        {
            if (HttpContext.User.Identity.Name == null)
            {
                ViewBag.Message = "Теперь, чтобы воспользоваться функциями сервера нужно зайти в аккаунт";
                return RedirectToAction("Login", "Account");
            }
            if (_usersContext.UserAccounts.FirstOrDefault(x => x.Username == HttpContext.User.Identity.Name)!.ServerID != "no")
            {
                return RedirectToAction("Index");
            }
            if (ModelState.IsValid)
            {
                // create server
                return RedirectToAction("Index");
            }
            return View(model);
        }


        [Route("/Server/See/{Id}")]
        public IActionResult See(string Id)
        {
            if (HttpContext.User.Identity.Name == null)
            {
                ViewBag.Message = "Теперь, чтобы воспользоваться функциями сервера нужно зайти в аккаунт";
                return RedirectToAction("Login", "Account");
            }
            MinecraftServer thisServer = MinecraftServerManager.serversOnline.First(x => x.Id == Id);
            return View(new ServerIdViewModel { IsRunning = thisServer.IsRunning, logs = thisServer.ConsoleLogs, ServerDesc = new MinecraftServerWrap { Name = thisServer.Name, Description = thisServer.Description, Id = thisServer.Id}, ServerState = thisServer.ServerState });
        }

        public IActionResult See()
        {
            return RedirectToAction("Index");
        }

        [HttpGet("/Server/See/{Id}/sti")]
        public IActionResult GetServerStats(string Id)
        {
            MinecraftServer server = MinecraftServerManager.serversOnline.First(x => x.Id == Id);
            Response.ContentType = "text/event-stream";
            switch (server.ServerState)
            {
                //case ServerState.starting:
                //    await Response.WriteAsync($"data: {{" +
                //        $"\"Type\": \"Server\"," +
                //        $"\"State\": \"{ServerState.starting}\"" +
                //        $"}}\n\n");
                //    await Response.Body.FlushAsync();
                //    //Sendtype = SendType.Info;
                //    break;
                case ServerState.started:
                    return Ok(new
                    {
                        Type = "Info",
                        Players = server.Players,
                        MemoryUsage = server.RamUsage
                    });
                    //await Response.WriteAsync($"data: {{" +
                    //    $"\"Type\": \"Info\"," +
                    //    $"\"Players\": {server.Players}," +
                    //    $" \"MemoryUsage\": {server.RamUsage}" +
                    //    $"}}\n\n");
                    //await Response.Body.FlushAsync();
                case ServerState.starting:
                    return Ok();

                default:
                    return Ok();
            }
        }

        [HttpGet("/Server/See/{Id}/sti/subscribe")]
        public async Task SubscribeToServerStart(string Id)
        {
            Response.ContentType = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";

            if (!_subscribers.ContainsKey(Id))
            {
                _subscribers[Id] = new List<HttpResponse>();
            }

            _subscribers[Id].Add(Response);

            await Response.Body.FlushAsync();

            await Task.Delay(60000); // Ожидание максимум 1 минуту, потом клиент должен переподключиться

            _subscribers[Id].Remove(Response);
        }

        public static async Task NotifyServerStarted(string serverId)
        {
            if (_subscribers.TryGetValue(serverId, out var subscribers))
            {
                foreach (var response in subscribers)
                {
                    try
                    {
                        await response.WriteAsync("data: {\"Type\": \"ServerStarted\"}\n\n");
                        await response.Body.FlushAsync();
                    }
                    catch
                    {
                        // Если клиент отключился, просто игнорируем
                    }
                }
                _subscribers.Remove(serverId, out _);
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
