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
        private readonly ServerDBContext _serverContext;
        private static readonly ConcurrentDictionary<string, List<HttpResponse>> _subscribers = new();

        public ServerController(UserDBContext userDBContext, ServerDBContext serverContext)
        {
            _usersContext = userDBContext;
            _serverContext = serverContext;
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
			foreach (var allowedServer in SharedAdministrationManager.GetAllowedServers(username) ?? new())
			{
                if(!MinecraftServerManager.ServerExists(allowedServer.serverid))
                {
                    SharedAdministrationManager.DeleteSharedUser(allowedServer.serverid, username);
                    continue;
                }
				Server allowedSpecs = _serverContext.Servers.First(x => x.id == allowedServer.serverid);
				allowedWraps.Add(new MinecraftServerWrap
				{
					Description = allowedSpecs.description,
					Id = allowedServer.serverid,
					ServerState = MinecraftServerManager.serversOnline.Any(x => x.Id == allowedServer.serverid) ? MinecraftServerManager.serversOnline.First(x => x.Id == allowedServer.serverid).ServerState : ServerState.stopped,
					Name = allowedSpecs.name
				});
			}
			UserAccount user = _usersContext.UserAccounts.FirstOrDefault(x => x.username == username)!;
            if (user.serverid == "no" || user.serverid == null)
            {
                return View(new ServerViewModel { ServerCreation = ServerCreation.notCreated, AllowedServers = allowedWraps });
            }
            var specs = _serverContext.Servers.First(x => x.id == user.serverid);
            if (MinecraftServerManager.inCreation.ContainsKey(user.serverid))
            {
                return View(new ServerViewModel { AllowedServers = allowedWraps, OwnServer = new MinecraftServerWrap { Description = specs.description, Id = user.serverid, ServerState = ServerState.stopped, Name = specs.name}, ServerCreation = ServerCreation.AddingMods });
            }
            //string logis = MinecraftServerManager.GetInstance().ConsoleLogs;
            MinecraftServer? server = MinecraftServerManager.serversOnline.FirstOrDefault(x => x.Id == user.serverid);
            return View(new ServerViewModel { AllowedServers = allowedWraps, OwnServer = new MinecraftServerWrap { Description = specs.description, Id = user.serverid, ServerState = server == null ? ServerState.stopped : server.ServerState, Name = specs.name }, ServerCreation = ServerCreation.Created });
            //return View(new ServerViewModel { IsRunning = MinecraftServerManager.GetInstance().IsRunning, ServerState = MinecraftServerManager.GetInstance().ServerProcess == null ? ServerState.starting : ServerState.started, logs = logis});
        }

        public IActionResult create()
        {
            if (HttpContext.User.Identity.Name == null)
            {
                ViewBag.Message = "Теперь, чтобы воспользоваться функциями сервера нужно зайти в аккаунт";
                return RedirectToAction("Login", "Account");
            }
            if (_usersContext.UserAccounts.FirstOrDefault(x => x.username == HttpContext.User.Identity.Name)!.serverid != null)
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
            if (_usersContext.UserAccounts.FirstOrDefault(x => x.username == HttpContext.User.Identity.Name)!.serverid != null)
            {
                return RedirectToAction("Index");
            }
            if (ModelState.IsValid)
            {
                string id = MinecraftServerManager.CreateServer(model.Name, HttpContext.User.Identity.Name, model.Version, model.Description).Result;
                _usersContext.UserAccounts.FirstOrDefault(x => x.username == HttpContext.User.Identity.Name)!.serverid = id;
                _usersContext.SaveChanges();
                return RedirectToAction($"configure", new { Id = id});
            }
            return View(model);
        }

        [Route("/Server/configure/{Id}")]
        public async Task<IActionResult> Configure(string Id)
        {
            if (HttpContext.User.Identity.Name == null || _usersContext.UserAccounts.Find(UserHelper.GetUserId(HttpContext.User.Identity.Name)).serverid != Id)
                if (HttpContext.User.Identity.Name == null || !SharedAdministrationManager.HasSharedThisServer(Id, HttpContext.User.Identity.Name) 
                    || !SharedAdministrationManager.GetUserSharedRights(HttpContext.User.Identity.Name, Id).editserverpreferences)
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
                UploadMods = (SharedAdministrationManager.GetUserSharedRights(HttpContext.User.Identity.Name, Id)
                ?? (_usersContext.UserAccounts.Any(x => x.serverid == Id)
                    ? SharedAdministrationManager.allRights(UserHelper.GetUserId(HttpContext.User.Identity.Name), Id)
                    : SharedAdministrationManager.defaultRights(UserHelper.GetUserId(HttpContext.User.Identity.Name), Id))).uploadmods
            });
        }

        [HttpPost]
        [Route("/Server/configure/{Id}/set")]
        public async Task<IActionResult> Set(string Id, [FromBody] PreferenceRequest request)
        {
            if (HttpContext.User.Identity.Name == null || _usersContext.UserAccounts.Find(UserHelper.GetUserId(HttpContext.User.Identity.Name)).serverid != Id)
                if (HttpContext.User.Identity.Name == null || !SharedAdministrationManager.HasSharedThisServer(Id, HttpContext.User.Identity.Name)
                    || !SharedAdministrationManager.GetUserSharedRights(HttpContext.User.Identity.Name, Id).editserverpreferences)
                    return Unauthorized();
            if(request.Preference == "motd")
            {
                await MinecraftServerManager.SetServerDesc(Id, request.Value);
            }
            else if(request.Preference == "name")
            {
                await MinecraftServerManager.SetServerName(Id, request.Value);
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

        [HttpPost]
        [Route("/Server/Delete/{Id}")]
        public async Task<IActionResult> Delete(string Id)
        {
            if(HttpContext.User.Identity.Name == null || _usersContext.UserAccounts.First(x => x.username == HttpContext.User.Identity.Name).serverid != Id)
            {
				return RedirectToAction("Index", "Home");
			}

            if (await MinecraftServerManager.DeleteServer(Id))
            {
                _usersContext.UserAccounts.First(x => x.username == HttpContext.User.Identity.Name).serverid = "no";
                _usersContext.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                return RedirectToAction("Index", "Account");
            }
        }

        [Route("/Server/See/{Id}")]
        public IActionResult See(string Id)
        {
            if (HttpContext.User.Identity.Name == null)
            {
                ViewBag.Message = "Теперь, чтобы воспользоваться функциями сервера нужно зайти в аккаунт";
                return RedirectToAction("Login", "Account");
            }
            if (_usersContext.UserAccounts.Find(UserHelper.GetUserId(HttpContext.User.Identity.Name)).serverid == Id ? false : !SharedAdministrationManager.HasSharedThisServer(Id, HttpContext.User.Identity.Name))
            {
                return RedirectToAction("Index");
            }
            ViewBag.ThisId = Id;
            MinecraftServer? thisServer = MinecraftServerManager.serversOnline.FirstOrDefault(x => x.Id == Id);
            if (thisServer == null)
            {
                Server specs = _serverContext.Servers.First(x => x.id == Id);

                SharedRightsDBO rrr = _usersContext.UserAccounts.Find(UserHelper.GetUserId(HttpContext.User.Identity.Name)).serverid == Id
                    ? SharedAdministrationManager.allRightsDBO
                    : SharedAdministrationManager.HasSharedThisServer(Id, HttpContext.User.Identity.Name)
                        ? (SharedRightsDBO)SharedAdministrationManager.GetUserSharedRights(HttpContext.User.Identity.Name, Id)!
                        : SharedAdministrationManager.defaultRightsDBO;
                return View(new ServerIdViewModel 
                { 
                    SharedRights = rrr,
                    AllowedUsers = SharedAdministrationManager.GetAllowedUsernames(Id), 
                    IsRunning = false, logs = "Последние 10 логов\n" + MinecraftServerManager.GetLastLogs(Id),
                    ServerDesc = new MinecraftServerWrap 
                    { 
                        Description = specs.description, Name = specs.name,
                        Id = Id, ServerState = ServerState.stopped
                    },
                    ServerState = ServerState.stopped,
                    PublicAddress = "just1x.hopto.org:" + specs.publicport,
                    Version = MinecraftServerManager.GetVersionDBO(specs.version),
                });
            }
            else
            {
                string logs = _usersContext.UserAccounts.First(x => x.username == HttpContext.User.Identity.Name).shortlogs
                    ? "Сокращенные логи:\n" + Helper.GetTrimmedLogs(thisServer.ConsoleLogs)
                    : thisServer.ConsoleLogs;
                return View(new ServerIdViewModel 
                {
                    SharedRights = _usersContext.UserAccounts.Find(UserHelper.GetUserId(HttpContext.User.Identity.Name)).serverid == Id
                    ? SharedAdministrationManager.allRightsDBO
                    : SharedAdministrationManager.HasSharedThisServer(Id, HttpContext.User.Identity.Name)
                        ? (SharedRightsDBO)SharedAdministrationManager.GetUserSharedRights(HttpContext.User.Identity.Name, Id)!
                        : SharedAdministrationManager.defaultRightsDBO,
                    AllowedUsers = SharedAdministrationManager.GetAllowedUsernames(Id),
                    IsRunning = thisServer.IsRunning,
                    logs = logs,
                    ServerDesc = new MinecraftServerWrap
                    {
                        Name = thisServer.Name,
                        Description = thisServer.Description,
                        Id = thisServer.Id
                    },
                    ServerState = thisServer.ServerState,
                    PublicAddress = "just1x.hopto.org:" + thisServer.PublicPort,
                    Version = MinecraftServerManager.GetVersionDBO(thisServer.Version),
                });
            }       
        }

        public IActionResult See()
        {
            return RedirectToAction("Index");
        }

        [Route("/Server/See/{Id}/allow")]
        public IActionResult Allow(string Id,[FromQuery] string user)
        {
          
            if (HttpContext.User.Identity.Name == null || !MinecraftServerManager.ServerExists(Id))
                return RedirectToAction("Index");
            if(_usersContext.UserAccounts.Find(UserHelper.GetUserId(HttpContext.User.Identity.Name)).serverid != Id)
                if(!SharedAdministrationManager.HasSharedThisServer(Id ,HttpContext.User.Identity.Name) 
                    || !SharedAdministrationManager.GetUserSharedRights(HttpContext.User.Identity.Name, Id).addshareds)
                    return RedirectToAction("Index");
            ViewBag.AllowName = user;
            return View((SharedRightsDBO)SharedAdministrationManager.GetUserSharedRights(user, Id));
        }

        [HttpGet("/Server/See/{Id}/allow/add")]
        public IActionResult AddAllow(string Id, [FromQuery] string user)
        {
            if(!MinecraftServerManager.ServerExists(Id))
            {
                return NotFound();
            }

            if (HttpContext.User.Identity.Name == null || _usersContext.UserAccounts.Find(UserHelper.GetUserId(HttpContext.User.Identity.Name)).serverid != Id)
                if (HttpContext.User.Identity.Name == null || !SharedAdministrationManager.HasSharedThisServer(Id, HttpContext.User.Identity.Name) 
                    || !SharedAdministrationManager.GetUserSharedRights(HttpContext.User.Identity.Name, Id).addshareds)
                    return Unauthorized();
            if (HttpContext.User.Identity.Name == user)
            {
                return Ok(new
                {
                    result = "self"
                });
            }
            if(!_usersContext.UserAccounts.Any(x => x.username == user))
            {
                return Ok(new
                {
                    result = "usernotfound"
                });
            }
            if(SharedAdministrationManager.HasSharedThisServer(Id, user))
            {
                return Ok(new
                {
                    result = "alreadyshared"
                });
            }
            SharedAdministrationManager.SetUserSharedRights(SharedAdministrationManager.defaultRights(UserHelper.GetUserId(user), Id));
            return Ok(new
            {
                result = "done"
            });
        }

        [HttpPost("/Server/See/{Id}/allow/delete")]
        public IActionResult DeleteAllow(string Id, [FromQuery] string user)
        {
            if (!MinecraftServerManager.ServerExists(Id))
            {
                return NotFound();
            }

            if (HttpContext.User.Identity.Name == null || _usersContext.UserAccounts.Find(UserHelper.GetUserId(HttpContext.User.Identity.Name)).serverid != Id)
                if (HttpContext.User.Identity.Name == null || !SharedAdministrationManager.HasSharedThisServer(Id, HttpContext.User.Identity.Name) 
                    || !SharedAdministrationManager.GetUserSharedRights(HttpContext.User.Identity.Name, Id).addshareds)
                    return Unauthorized();
            SharedAdministrationManager.DeleteSharedUser(Id, user);
            return Ok();
        }

        [HttpPost("/Server/See/{Id}/allow/save")]
        public IActionResult SetSharedRights(string Id, [FromQuery] string user, [FromBody] SharedRightsDBO rights)
        {
            if (HttpContext.User.Identity.Name == null || _usersContext.UserAccounts.Find(UserHelper.GetUserId(HttpContext.User.Identity.Name)).serverid != Id)
                if (HttpContext.User.Identity.Name == null || !SharedAdministrationManager.HasSharedThisServer(Id, HttpContext.User.Identity.Name) 
                    || !SharedAdministrationManager.GetUserSharedRights(HttpContext.User.Identity.Name, Id).addshareds)
                    return Unauthorized();

            if (rights == null)
            {
                return BadRequest("Данные не получены.");
            }

            SharedRights newRights = new SharedRights
            {
                addshareds = rights.AddShareds,
                editmods = rights.EditMods,
                editserverpreferences = rights.EditServerPreferences,
                sendcommands = rights.SendCommands,
                serverid = Id,
                startstopserver = rights.StartStopServer,
                uploadmods = rights.UploadMods,
                userid = UserHelper.GetUserId(user)
            };

            SharedAdministrationManager.SetUserSharedRights(newRights);
            return Ok("Права успешно обновлены.");
        }

        [Route("/Server/configure/{Id}/mods")]
        public IActionResult Mods(string Id)
        {
            if (HttpContext.User.Identity?.Name == null)
            {
                return RedirectToAction("Login");
            }
            if (HttpContext.User.Identity.Name == null || _usersContext.UserAccounts.Find(UserHelper.GetUserId(HttpContext.User.Identity.Name)).serverid != Id)
                if (HttpContext.User.Identity.Name == null || !SharedAdministrationManager.HasSharedThisServer(Id, HttpContext.User.Identity.Name) || !SharedAdministrationManager.GetUserSharedRights(HttpContext.User.Identity.Name, Id).editmods)
                    return RedirectToAction("Index");

            string modsFolder = Path.Combine(MinecraftServerManager.folder, Id, "mods");

            if (!Directory.Exists(modsFolder))
            {
                return View(new ModsViewModel
                {
                    Files = null,
                    ModsInstalled = false,
                    Name = MinecraftServerManager.GetServerSpecs(Id).name,
                    ServerId = Id
                });
            }

            var files = Directory.GetFiles(modsFolder)
                                 .Select(Path.GetFileName)
                                 .ToList();

            return View(new ModsViewModel { ServerId = Id, ModsInstalled = true, Files = files, Name = MinecraftServerManager.GetServerSpecs(Id).name });
        }

        [HttpGet("/Server/See/{Id}/sti")]
        public IActionResult GetServerStats(string Id)
        {
            if (HttpContext.User.Identity.Name == null || _usersContext.UserAccounts.Find(UserHelper.GetUserId(HttpContext.User.Identity.Name)).serverid != Id)
                if (HttpContext.User.Identity.Name == null || !SharedAdministrationManager.HasSharedThisServer(Id, HttpContext.User.Identity.Name))
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

        [HttpGet("/Server/See/{Id}/sti/subscribe")]
        public async Task SubscribeToServerStart(string Id)
        {
            if (HttpContext.User.Identity.Name == null || _usersContext.UserAccounts.Find(UserHelper.GetUserId(HttpContext.User.Identity.Name)).serverid != Id)
                if (HttpContext.User.Identity.Name == null || !SharedAdministrationManager.HasSharedThisServer(Id, HttpContext.User.Identity.Name))
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
