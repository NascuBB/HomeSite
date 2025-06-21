using HomeSite.Entities;
using HomeSite.Generated;
using HomeSite.Helpers;
using HomeSite.Managers;
using HomeSite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace HomeSite.Controllers
{
    [Route("server")]
    public class ServerController : Controller
    {
        private readonly UserDBContext _usersContext;
        private readonly ServerDBContext _serverContext;
        private readonly ISharedAdministrationManager _sharedManager;
        private readonly IUserHelper _userHelper;
        private readonly IMinecraftServerManager _minecraftServerManager;
        private static readonly ConcurrentDictionary<string, List<HttpResponse>> _subscribers = new();

        public ServerController(UserDBContext userDBContext, ServerDBContext serverContext, ISharedAdministrationManager sharedAdministration, IUserHelper userHelper, IMinecraftServerManager minecraftServerManager)
        {
            _usersContext = userDBContext;
            _serverContext = serverContext;
            _sharedManager = sharedAdministration;
            _userHelper = userHelper;
            _minecraftServerManager = minecraftServerManager;
        }

        public IActionResult Index()
        {
            string? username = HttpContext.User.Identity.Name;
            if (username == null)
            {
                ViewBag.Message = "Теперь, чтобы воспользоваться функциями сервера нужно зайти в аккаунт";
                return RedirectToAction("Login", "Account");
            }
			List<MinecraftServerWrap> allowedWraps = new List<MinecraftServerWrap>();
			foreach (var allowedServer in _sharedManager.GetAllowedServers(username) ?? new())
			{
                if(!_minecraftServerManager.ServerExists(allowedServer.ServerId).Result)
                {
                    _sharedManager.DeleteSharedUser(allowedServer.ServerId, username);
                    continue;
                }
				Server allowedSpecs = _serverContext.Servers.First(x => x.Id == allowedServer.ServerId);
				allowedWraps.Add(new MinecraftServerWrap
				{
					Description = allowedSpecs.Description,
					Id = allowedServer.ServerId,
					ServerState = MinecraftServerManager.serversOnline.Any(x => x.Id == allowedServer.ServerId) ? MinecraftServerManager.serversOnline.First(x => x.Id == allowedServer.ServerId).ServerState : ServerState.stopped,
					Name = allowedSpecs.Name
				});
			}
			UserAccount user = _usersContext.UserAccounts.FirstOrDefault(x => x.Username == username)!;
            if (user.ServerId == "no" || user.ServerId == null)
            {
                return View(new ServerViewModel { ServerCreation = ServerCreation.notCreated, AllowedServers = allowedWraps });
            }
            var specs = _serverContext.Servers.First(x => x.Id == user.ServerId);
            if (MinecraftServerManager.inCreation.ContainsKey(user.ServerId))
            {
                return View(new ServerViewModel { AllowedServers = allowedWraps, OwnServer = new MinecraftServerWrap { Description = specs.Description, Id = user.ServerId, ServerState = ServerState.stopped, Name = specs.Name}, ServerCreation = ServerCreation.AddingMods });
            }
            //string logis = MinecraftServerManager.GetInstance().ConsoleLogs;
            MinecraftServer? server = MinecraftServerManager.serversOnline.FirstOrDefault(x => x.Id == user.ServerId);
            return View(new ServerViewModel { AllowedServers = allowedWraps, OwnServer = new MinecraftServerWrap { Description = specs.Description, Id = user.ServerId, ServerState = server == null ? ServerState.stopped : server.ServerState, Name = specs.Name }, ServerCreation = ServerCreation.Created });
            //return View(new ServerViewModel { IsRunning = MinecraftServerManager.GetInstance().IsRunning, ServerState = MinecraftServerManager.GetInstance().ServerProcess == null ? ServerState.starting : ServerState.started, logs = logis});
        }
        [Route("create")]
        public IActionResult create()
        {
            if (HttpContext.User.Identity.Name == null)
            {
                ViewBag.Message = "Теперь, чтобы воспользоваться функциями сервера нужно зайти в аккаунт";
                return RedirectToAction("Login", "Account");
            }
            if (_usersContext.UserAccounts.FirstOrDefault(x => x.Username == HttpContext.User.Identity.Name)!.ServerId != "no")
            {
                return RedirectToAction("Index");
            }
            return View(new CreateServerViewModel());
        }

        [Route("create")]
        [HttpPost]
        public IActionResult create(CreateServerViewModel model)
        {
            if (HttpContext.User.Identity.Name == null)
            {
                ViewBag.Message = "Теперь, чтобы воспользоваться функциями сервера нужно зайти в аккаунт";
                return RedirectToAction("Login", "Account");
            }
            if (_usersContext.UserAccounts.FirstOrDefault(x => x.Username == HttpContext.User.Identity.Name)!.ServerId != "no")
            {
                return RedirectToAction("Index");
            }
            if (ModelState.IsValid)
            {
                string id = _minecraftServerManager.CreateServer(model.Name, HttpContext.User.Identity.Name, model.ServerCore ,model.Version, model.Description ?? "A Minecraft server").Result;
                _usersContext.UserAccounts.FirstOrDefault(x => x.Username == HttpContext.User.Identity.Name)!.ServerId = id;
                _usersContext.SaveChanges();
                return RedirectToAction($"configure", new { Id = id});
            }
            return View(model);
        }

        [Route("/server/configure/{Id}")]
        public async Task<IActionResult> Configure(string Id)
        {
            if (HttpContext.User.Identity.Name == null || _usersContext.UserAccounts.Find(_userHelper.GetUserId(HttpContext.User.Identity.Name)).ServerId != Id)
                if (HttpContext.User.Identity.Name == null || !_sharedManager.HasSharedThisServer(Id, HttpContext.User.Identity.Name) 
                    || !_sharedManager.GetUserSharedRights(HttpContext.User.Identity.Name, Id).EditServerPreferences)
                    return RedirectToAction("Index");
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
                UploadMods = (_sharedManager.GetUserSharedRights(HttpContext.User.Identity.Name, Id)
                ?? (_usersContext.UserAccounts.Any(x => x.ServerId == Id)
                    ? SharedAdministrationManager.allRights(_userHelper.GetUserId(HttpContext.User.Identity.Name), Id)
                    : SharedAdministrationManager.defaultRights(_userHelper.GetUserId(HttpContext.User.Identity.Name), Id))).UploadServer,
                ServerCore = _minecraftServerManager.GetServerSpecs(Id).Result.ServerCore
            });
        }

        [HttpPost]
        [Route("/server/configure/{Id}/set")]
        public async Task<IActionResult> Set(string Id, [FromBody] PreferenceRequest request)
        {
            if (HttpContext.User.Identity.Name == null || _usersContext.UserAccounts.Find(_userHelper.GetUserId(HttpContext.User.Identity.Name)).ServerId != Id)
                if (HttpContext.User.Identity.Name == null || !_sharedManager.HasSharedThisServer(Id, HttpContext.User.Identity.Name)
                    || !_sharedManager.GetUserSharedRights(HttpContext.User.Identity.Name, Id).EditServerPreferences)
                    return Unauthorized();
            if(request.Preference == "motd")
            {
                await _minecraftServerManager.SetServerDesc(Id, request.Value);
            }
            else if(request.Preference == "name")
            {
                await _minecraftServerManager.SetServerName(Id, request.Value);
				return Ok();
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
        [Route("/server/configure/{Id}/finish")]
        public async Task<IActionResult> FinishServer(string Id)
        {
            if (HttpContext.User.Identity?.IsAuthenticated != true)
            {
                return Unauthorized("Вы не авторизованы");
            }

            bool isCreated = await  MinecraftServerManager.FinishServerCreation(Id);

            return Ok(isCreated); // Возвращает true или false
        }

        [HttpPost]
        [Route("/server/delete/{Id}")]
        public async Task<IActionResult> Delete(string Id)
        {
            if (HttpContext.User.Identity.Name == null)
            {
                return RedirectToAction("Index", "Home");
            }
            var user = _usersContext.UserAccounts.First(x => x.Username == HttpContext.User.Identity.Name);
            if(user.ServerId != Id)
            {
				return RedirectToAction("Index", "Home");
			}

            if (await _minecraftServerManager.DeleteServer(Id))
            {
                user.ServerId = "no";
                _usersContext.SaveChanges();
                _sharedManager.DeleteServer(Id);
				return RedirectToAction("Index");
            }
            else
            {
                return RedirectToAction("Index", "Account");
            }
        }

        [Route("/server/see/{Id}")]
        public async Task<IActionResult> See(string Id)
        {
            var username = HttpContext.User.Identity?.Name;

            if (username == null)
            {
                ViewBag.Message = "Теперь, чтобы воспользоваться функциями сервера нужно зайти в аккаунт";
                return RedirectToAction("Login", "Account");
            }

            var userId = _userHelper.GetUserId(username);
            var userAccount = await _usersContext.UserAccounts.FindAsync(userId);

            bool isOwner = userAccount?.ServerId == Id;
            bool isShared = _sharedManager.HasSharedThisServer(Id, username);

            if (!isOwner && !isShared)
                return RedirectToAction("Index");

            ViewBag.ThisId = Id;

            MinecraftServer? thisServer = MinecraftServerManager.serversOnline.FirstOrDefault(x => x.Id == Id);

            SharedRightsDBO rights = isOwner
                ? SharedAdministrationManager.allRightsDBO
                : isShared
                    ? (SharedRightsDBO)_sharedManager.GetUserSharedRights(username, Id)!
                    : SharedAdministrationManager.defaultRightsDBO;

            List<string> allowedUsers = _sharedManager.GetAllowedUsernames(Id);

            if (thisServer == null)
            {
                var specs = await _serverContext.Servers.FirstAsync(x => x.Id == Id);

                return View(new ServerIdViewModel
                {
                    SharedRights = rights,
                    AllowedUsers = allowedUsers,
                    IsRunning = false,
                    logs = "Последние 10 логов\n" + MinecraftServerManager.GetLastLogs(Id),
                    ServerDesc = new MinecraftServerWrap
                    {
                        Description = specs.Description,
                        Name = specs.Name,
                        Id = Id,
                        ServerState = ServerState.stopped
                    },
                    ServerState = ServerState.stopped,
                    PublicAddress = ConfigManager.Domain + ":" + specs.PublicPort,
                    Version = VersionHelperGenerated.GetVersionDBO(specs.Version),
                    Core = specs.ServerCore.ToString()!
                });
            }
            else
            {
                string logs = userAccount!.ShortLogs
                    ? "Сокращенные логи:\n" + Helper.GetTrimmedLogs(thisServer.ConsoleLogs)
                    : thisServer.ConsoleLogs;

                return View(new ServerIdViewModel
                {
                    SharedRights = rights,
                    AllowedUsers = allowedUsers,
                    IsRunning = thisServer.IsRunning,
                    logs = logs,
                    ServerDesc = new MinecraftServerWrap
                    {
                        Name = thisServer.Name,
                        Description = thisServer.Description,
                        Id = thisServer.Id
                    },
                    ServerState = thisServer.ServerState,
                    PublicAddress = ConfigManager.Domain + ":" + thisServer.PublicPort,
                    Version = VersionHelperGenerated.GetVersionDBO(thisServer.Version),
                    Core = thisServer.ServerCore.ToString()!
                });
            }
        }

        [Route("/server/see")]
        public IActionResult See()
        {
            return RedirectToAction("Index");
        }

        [Route("/server/see/{Id}/allow")]
        public IActionResult Allow(string Id,[FromQuery] string user)
        {
          
            if (HttpContext.User.Identity.Name == null || !_minecraftServerManager.ServerExists(Id).Result)
                return RedirectToAction("Index");
            if(_usersContext.UserAccounts.Find(_userHelper.GetUserId(HttpContext.User.Identity.Name)).ServerId != Id)
                if(!_sharedManager.HasSharedThisServer(Id ,HttpContext.User.Identity.Name) 
                    || !_sharedManager.GetUserSharedRights(HttpContext.User.Identity.Name, Id).AddShareds)
                    return RedirectToAction("Index");
            ViewBag.AllowName = user;
            return View((SharedRightsDBO)_sharedManager.GetUserSharedRights(user, Id));
        }

        [HttpGet("/server/see/{Id}/allow/add")]
        public IActionResult AddAllow(string Id, [FromQuery] string user)
        {
            if(!_minecraftServerManager.ServerExists(Id).Result)
            {
                return NotFound();
            }

            if (HttpContext.User.Identity.Name == null || _usersContext.UserAccounts.Find(_userHelper.GetUserId(HttpContext.User.Identity.Name)).ServerId != Id)
                if (HttpContext.User.Identity.Name == null || !_sharedManager.HasSharedThisServer(Id, HttpContext.User.Identity.Name) 
                    || !_sharedManager.GetUserSharedRights(HttpContext.User.Identity.Name, Id).AddShareds)
                    return Unauthorized();
            if (HttpContext.User.Identity.Name == user)
            {
                return Ok(new
                {
                    result = "self"
                });
            }
            if(!_usersContext.UserAccounts.Any(x => x.Username == user))
            {
                return Ok(new
                {
                    result = "usernotfound"
                });
            }
            if(_sharedManager.HasSharedThisServer(Id, user))
            {
                return Ok(new
                {
                    result = "alreadyshared"
                });
            }
            _sharedManager.SetUserSharedRights(SharedAdministrationManager.defaultRights(_userHelper.GetUserId(user), Id));
            return Ok(new
            {
                result = "done"
            });
        }

        [HttpPost("/server/see/{Id}/allow/delete")]
        public IActionResult DeleteAllow(string Id, [FromQuery] string user)
        {
            if (!_minecraftServerManager.ServerExists(Id).Result)
            {
                return NotFound();
            }

            if (HttpContext.User.Identity.Name == null || _usersContext.UserAccounts.Find(_userHelper.GetUserId(HttpContext.User.Identity.Name)).ServerId != Id)
                if (HttpContext.User.Identity.Name == null || !_sharedManager.HasSharedThisServer(Id, HttpContext.User.Identity.Name) 
                    || !_sharedManager.GetUserSharedRights(HttpContext.User.Identity.Name, Id).AddShareds)
                    return Unauthorized();
            _sharedManager.DeleteSharedUser(Id, user);
            return Ok();
        }

        [HttpPost("/server/see/{Id}/allow/save")]
        public IActionResult SetSharedRights(string Id, [FromQuery] string user, [FromBody] SharedRightsDBO rights)
        {
            if (HttpContext.User.Identity.Name == null || _usersContext.UserAccounts.Find(_userHelper.GetUserId(HttpContext.User.Identity.Name)).ServerId != Id)
                if (HttpContext.User.Identity.Name == null || !_sharedManager.HasSharedThisServer(Id, HttpContext.User.Identity.Name) 
                    || !_sharedManager.GetUserSharedRights(HttpContext.User.Identity.Name, Id).AddShareds)
                    return Unauthorized();

            if (rights == null)
            {
                return BadRequest("Данные не получены.");
            }

            SharedRights newRights = new SharedRights
            {
                AddShareds = rights.AddShareds,
                EditMods = rights.EditMods,
                EditServerPreferences = rights.EditServerPreferences,
                SendCommands = rights.SendCommands,
                SeeServerFiles = rights.SeeServerFiles,
                ServerId = Id,
                StartStopServer = rights.StartStopServer,
                UploadServer = rights.UploadMods,
                UserId = _userHelper.GetUserId(user)
            };

            _sharedManager.SetUserSharedRights(newRights);
            return Ok("Права успешно обновлены.");
        }

        [Route("/server/configure/{Id}/mods")]
        public IActionResult Mods(string Id)
        {
            if (HttpContext.User.Identity?.Name == null)
            {
                return RedirectToAction("Login");
            }
            var server = _minecraftServerManager.GetServerSpecs(Id).Result;
            if (server.ServerCore == ServerCore.Paper)
            {
                return RedirectToAction("See", "Server", new { Id = Id });
            }
            var user = _usersContext.UserAccounts.First(x => x.Username == HttpContext.User.Identity.Name);
            if (user.ServerId != Id)
                if (!_sharedManager.HasSharedThisServer(Id, user.Username) || !_sharedManager.GetUserSharedRights(HttpContext.User.Identity.Name, Id).EditMods)
                    return RedirectToAction("Index");

            string modsFolder = Path.Combine(MinecraftServerManager.folder, Id, "mods");

            if (!Directory.Exists(modsFolder))
            {
                return View(new ModsViewModel
                {
                    Files = null,
                    ModsInstalled = false,
                    Name = server.Name,
                    ServerId = Id
                });
            }

            var files = Directory.GetFiles(modsFolder)
                                 .Select(Path.GetFileName)
                                 .ToList();

            return View(new ModsViewModel { ServerId = Id, ModsInstalled = true, Files = files, Name = _minecraftServerManager.GetServerSpecs(Id).Result.Name });
        }
        [Route("/server/configure/{Id}/plugins")]
        public IActionResult Plugins(string Id)
        {
            if (HttpContext.User.Identity?.Name == null)
            {
                return RedirectToAction("Login");
            }
            var server = _minecraftServerManager.GetServerSpecs(Id).Result;
            if(server.ServerCore != ServerCore.Paper)
            {
                return RedirectToAction("See", "Server", new { Id = Id });
            }
            var user = _usersContext.UserAccounts.First(x => x.Username == HttpContext.User.Identity.Name);
            if (user.ServerId != Id)
                if (!_sharedManager.HasSharedThisServer(Id, user.Username) || !_sharedManager.GetUserSharedRights(HttpContext.User.Identity.Name, Id).EditMods)
                    return RedirectToAction("Index");

            string modsFolder = Path.Combine(MinecraftServerManager.folder, Id, "plugins");

            if (!Directory.Exists(modsFolder))
            {
                return View(new PluginsViewModel
                {
                    Files = null,
                    Name = server.Name,
                    ServerId = Id
                });
            }

            var files = Directory.GetFiles(modsFolder)
                                 .Select(Path.GetFileName)
                                 .ToList();

            return View(new PluginsViewModel { ServerId = Id, Files = files, Name = _minecraftServerManager.GetServerSpecs(Id).Result.Name });
        }

        private readonly string _serversBasePath = Path.Combine(Directory.GetCurrentDirectory(), "servers");

        [HttpGet("/server/see/{id}/files")]
        public IActionResult Files(string id, string? path)
        {
            if (HttpContext.User.Identity?.Name == null)
            {
                return RedirectToAction("Login");
            }
            var user = _usersContext.UserAccounts.First(x => x.Username == HttpContext.User.Identity.Name);
            if (user.ServerId != id)
                if (!_sharedManager.HasSharedThisServer(id, user.Username) || !_sharedManager.GetUserSharedRights(HttpContext.User.Identity.Name, id).SeeServerFiles)
                    return RedirectToAction("See", "Server", new { Id = id });

            var serverPath = Path.Combine(_serversBasePath, id);
            var relativePath = path ?? "";
            var fullPath = Path.Combine(serverPath, relativePath);

            if (!Path.GetFullPath(fullPath).StartsWith(serverPath))
                return BadRequest("Недопустимый путь.");

            if (!Directory.Exists(fullPath))
                return NotFound("Папка не найдена.");

            var items = new List<DirectoryItem>();

            foreach (var dir in Directory.GetDirectories(fullPath))
            {
                items.Add(new DirectoryItem
                {
                    Name = Path.GetFileName(dir),
                    RelativePath = Path.Combine(relativePath, Path.GetFileName(dir)),
                    IsDirectory = true
                });
            }

            foreach (var file in Directory.GetFiles(fullPath))
            {
                items.Add(new DirectoryItem
                {
                    Name = Path.GetFileName(file),
                    RelativePath = Path.Combine(relativePath, Path.GetFileName(file)),
                    IsDirectory = false
                });
            }

            ViewBag.ServerId = id;
            ViewBag.ServerName = _serverContext.Servers.First(x => x.Id == id).Name;
            ViewBag.CurrentPath = relativePath;
            ViewBag.ParentPath = GetParentPath(relativePath);

            return View("Files", items);
        }

        private string? GetParentPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            var dir = Path.GetDirectoryName(path);
            return dir?.Replace("\\", "/");
        }

        [HttpGet("/server/see/{id}/view")]
        public IActionResult ViewFile(string id, string path)
        {
            if (HttpContext.User.Identity?.Name == null)
            {
                return RedirectToAction("Login");
            }
            var user = _usersContext.UserAccounts.First(x => x.Username == HttpContext.User.Identity.Name);
            if (user.ServerId != id)
                if (!_sharedManager.HasSharedThisServer(id, user.Username) || !_sharedManager.GetUserSharedRights(HttpContext.User.Identity.Name, id).SeeServerFiles)
                    return RedirectToAction("See", "Server", new { Id = id });

            var filePath = Path.Combine(_serversBasePath, id, path);

            if (!System.IO.File.Exists(filePath))
                return NotFound("Файл не найден.");

            var ext = Path.GetExtension(filePath).ToLower();
            var supported = new[] { ".txt", ".cfg", ".ini", ".log", ".json", ".yml", ".yaml", ".toml", ".properties" };

            if (!supported.Contains(ext))
                return BadRequest("Формат файла не поддерживается для просмотра.");

            var content = System.IO.File.ReadAllText(filePath);

            ViewBag.ServerId = id;
            ViewBag.RelativePath = path;
            ViewBag.FileName = Path.GetFileName(filePath);

            return View("ViewFile", content);
        }

        [HttpPost("/server/see/{id}/edit")]
        [ValidateAntiForgeryToken]
        public IActionResult EditFile(string id, string path, string content)
        {
            if (HttpContext.User.Identity?.Name == null)
            {
                return RedirectToAction("Login");
            }
            var user = _usersContext.UserAccounts.First(x => x.Username == HttpContext.User.Identity.Name);
            if (user.ServerId != id)
                if (!_sharedManager.HasSharedThisServer(id, user.Username) || !_sharedManager.GetUserSharedRights(HttpContext.User.Identity.Name, id).SeeServerFiles)
                    return RedirectToAction("See", "Server", new { Id = id });

            var filePath = Path.Combine(_serversBasePath, id, path);

            if (!System.IO.File.Exists(filePath))
                return NotFound("Файл не найден.");

            System.IO.File.WriteAllText(filePath, content);

            TempData["Message"] = "Файл успешно сохранён.";
            return RedirectToAction("ViewFile", new { id = id, path = path });
        }


        [HttpGet("/server/see/{Id}/sti")]
        public IActionResult GetServerStats(string Id)
        {
            if (HttpContext.User.Identity.Name == null || _usersContext.UserAccounts.Find(_userHelper.GetUserId(HttpContext.User.Identity.Name)).ServerId != Id)
                if (HttpContext.User.Identity.Name == null || !_sharedManager.HasSharedThisServer(Id, HttpContext.User.Identity.Name))
                    return Unauthorized();
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

        [HttpGet("/server/see/{Id}/sti/subscribe")]
        public async Task SubscribeToServerStart(string Id)
        {
            if (HttpContext.User.Identity.Name == null || _usersContext.UserAccounts.Find(_userHelper.GetUserId(HttpContext.User.Identity.Name)).ServerId != Id)
                if (HttpContext.User.Identity.Name == null || !_sharedManager.HasSharedThisServer(Id, HttpContext.User.Identity.Name))
                    return;

            Response.ContentType = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";

            if (!_subscribers.ContainsKey(Id))
            {
                _subscribers[Id] = new List<HttpResponse>();
            }

            _subscribers[Id].Add(Response);

            await Response.Body.FlushAsync();

            await Task.Delay(60000); // Ожидание максимум 1 минуту, потом клиент должен переподключиться
            try
            {
                _subscribers[Id].Remove(Response);
            }
            catch { }
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
