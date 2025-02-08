using HomeSite.Entities;
using HomeSite.Helpers;
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
                return View(new ServerViewModel { AllowedServers = null, OwnServer = new MinecraftServerWrap { Description = specs.Description, Id = user.ServerID, ServerState = ServerState.stopped, Name = specs.Name}, ServerCreation = ServerCreation.AddingMods });
            }
            //string logis = MinecraftServerManager.GetInstance().ConsoleLogs;
            MinecraftServer? server = MinecraftServerManager.serversOnline.FirstOrDefault(x => x.Id == user.ServerID);
            return View(new ServerViewModel { AllowedServers = null, OwnServer = new MinecraftServerWrap { Description = specs.Description, Id = user.ServerID, ServerState = server == null ? ServerState.stopped : server.ServerState, Name = specs.Name }, ServerCreation = ServerCreation.Created });
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
                string id = MinecraftServerManager.CreateServer(model.Name, HttpContext.User.Identity.Name,model.Version, model.Description).Result;
                _usersContext.UserAccounts.FirstOrDefault(x => x.Username == HttpContext.User.Identity.Name)!.ServerID = id;
                _usersContext.SaveChanges();
                return RedirectToAction($"configure", new { Id = id});
            }
            return View(model);
        }

        [Route("/Server/configure/{Id}")]
        public async Task<IActionResult> Configure(string Id)
        {
            string filepath = Path.Combine(MinecraftServerManager.folder, Id, "server.properties");
            string modsPath = Path.Combine(MinecraftServerManager.folder, Id, "mods");
            return View(new ConfigureServerViewModel
            {
                CommandBlock = await ServerPropertiesManager.GetProperty<bool>(filepath, "enable-command-block"),
                Difficulty = await ServerPropertiesManager.GetProperty<Difficulty>(filepath, "difficulty"),
                GameMode = await ServerPropertiesManager.GetProperty<GameMode>(filepath, "gamemode"),
                Flight = await ServerPropertiesManager.GetProperty<bool>(filepath, "allow-flight"),
                ForceGM = await ServerPropertiesManager.GetProperty<bool>(filepath, "force-gamemode"),
                MaxPlayers = await ServerPropertiesManager.GetProperty<int>(filepath, "max-players"),
                Nether = await ServerPropertiesManager.GetProperty<bool>(filepath, "allow-nether"),
                OnlineMode = await ServerPropertiesManager.GetProperty<bool>(filepath, "online-mode"),
                Pvp = await ServerPropertiesManager.GetProperty<bool>(filepath, "pvp"),
                SpawnMonsters = await ServerPropertiesManager.GetProperty<bool>(filepath, "spawn-monsters"),
                SpawnProtection = await ServerPropertiesManager.GetProperty<int>(filepath, "spawn-protection"),
                Whitelist = await ServerPropertiesManager.GetProperty<bool>(filepath, "white-list"),
                IsConfigured = MinecraftServerManager.inCreation.Any(x => x.Key == Id),
                ModsInstalled = Directory.Exists(modsPath) ? Directory.GetFiles(modsPath).Length > 0 : false,
            });
        }

        [HttpPost]
        [Route("/Server/configure/{Id}/set")]
        public async Task<IActionResult> Set(string Id, [FromBody] PreferenceRequest request)
        {
            if (HttpContext.User.Identity?.Name == null)
            {
                return Unauthorized();
            }

            if (await ServerPropertiesManager.EditProperty(Path.Combine(MinecraftServerManager.folder, Id, "server.properties"), request.Preference, request.Value))
            {
                return Ok();
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpPost]
        [Route("/Server/configure/{Id}/finish")]
        public async Task<IActionResult> FinishServer(string Id)
        {
            if (HttpContext.User.Identity?.IsAuthenticated != true)
            {
                return Unauthorized("Вы не авторизованы");
            }

            bool isCreated = await  MinecraftServerManager.FinishServerCreation(Id);

            return Ok(isCreated); // Возвращает true или false
        }

        [Route("/Server/See/{Id}")]
        public IActionResult See(string Id)
        {
            if (HttpContext.User.Identity.Name == null)
            {
                ViewBag.Message = "Теперь, чтобы воспользоваться функциями сервера нужно зайти в аккаунт";
                return RedirectToAction("Login", "Account");
            }
            MinecraftServer? thisServer = MinecraftServerManager.serversOnline.FirstOrDefault(x => x.Id == Id);
            if (thisServer == null)
            {
                MinecraftServerSpecifications specs = MinecraftServerManager.GetServerSpecs(Id);
                return View(new ServerIdViewModel { IsRunning = false, logs = "Последние 10 логов\n" + MinecraftServerManager.GetLastLogs(Id), ServerDesc = new MinecraftServerWrap { Description = specs.Description, Name = specs.Name, Id = Id, ServerState = ServerState.stopped}, ServerState = ServerState.stopped});
            }
            else
            {
                return View(new ServerIdViewModel { IsRunning = thisServer.IsRunning, logs = thisServer.ConsoleLogs, ServerDesc = new MinecraftServerWrap { Name = thisServer.Name, Description = thisServer.Description, Id = thisServer.Id }, ServerState = thisServer.ServerState });
            }       
        }

        public IActionResult See()
        {
            return RedirectToAction("Index");
        }

        [Route("/Server/configure/{Id}/mods")]
        public IActionResult Mods(string Id)
        {
            if (HttpContext.User.Identity?.Name == null)
            {
                return RedirectToAction("Login");
            }

            string modsFolder = Path.Combine(MinecraftServerManager.folder, Id, "mods");

            if (!Directory.Exists(modsFolder))
            {
                return View(new ModsViewModel
                {
                    Files = null,
                    ModsInstalled = false,
                    Name = MinecraftServerManager.GetServerSpecs(Id).Name,
                    ServerId = Id
                });
            }

            var files = Directory.GetFiles(modsFolder)
                                 .Select(Path.GetFileName)
                                 .ToList();

            return View(new ModsViewModel { ServerId = Id, ModsInstalled = true, Files = files, Name = MinecraftServerManager.GetServerSpecs(Id).Name });
        }

        [HttpGet("/Server/See/{Id}/sti")]
        public IActionResult GetServerStats(string Id)
        {
            MinecraftServer? server = MinecraftServerManager.serversOnline.FirstOrDefault(x => x.Id == Id);
            if (server == null)
            {
                return Ok(new
                {
                    Type = "Stop"
                });
            }
            //Response.ContentType = "text/event-stream";
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
        public static async Task NotifyServerCrashed(string serverId)
        {
            if (_subscribers.TryGetValue(serverId, out var subscribers))
            {
                foreach (var response in subscribers)
                {
                    try
                    {
                        await response.WriteAsync("data: {\"Type\": \"ServerCrashed\"}\n\n");
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
