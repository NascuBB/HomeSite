using HomeSite.Entities;
using HomeSite.Managers;
using HomeSite.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace HomeSite.Controllers
{
    public class ServerController : Controller
    {
        public static SendType Sendtype { get; set; } = SendType.Skip;
        private int adaptivePing = 1000;
        private readonly UserDBContext _usersContext;

        public ServerController(UserDBContext userDBContext)
        {
            _usersContext = userDBContext;
        }

        public IActionResult Index()
        {
            if (HttpContext.User.Identity.Name == null)
            {
                ViewBag.Message = "Теперь, чтобы воспользоваться функциями сервера нужно зайти в аккаунт";
                return RedirectToAction("Login", "Account");
            }
            //if(_usersContext.UserAccounts.FirstOrDefault(x => x.Username == HttpContext.User.Identity.Name)!.ServerID == "no")
            //{
            //    return View(new ServerViewModel { ServerCreation = ServerCreation.notCreated});
            //}
            //string logis = MinecraftServerManager.GetInstance().ConsoleLogs;
            return View(new ServerViewModel { AllowedServers = new List<MinecraftServerWrap>
            {
                new MinecraftServerWrap { IsRunning = true, Description = "Desc 11", Id="asdfghj", Name = "Friend's server"},
                new MinecraftServerWrap { IsRunning = false, Description = "zhopa sdfdgffg sdfdfgdfhdghfgh оооочень длинное описание", Id="poiuytrewq", Name = "ЖОООПАААААААААААААА"},
                new MinecraftServerWrap { IsRunning = false, Description = "heheheha lmao lol", Id="zxcvbnm", Name = "Popapisa"},
                
            }, OwnServer = new MinecraftServerWrap {Description = "Ponos1k server of just1x", Name = "My server", Id = "qwertyuio"}, ServerCreation = ServerCreation.Created });
            //return View(new ServerViewModel { IsRunning = MinecraftServerManager.GetInstance().IsRunning, ServerState = MinecraftServerManager.GetInstance().ServerProcess == null ? ServerState.starting : ServerState.started, logs = logis});
        }

        [Route("/Server/See/{Id}")]
        public IActionResult See(string Id)
        {
            if (HttpContext.User.Identity.Name == null)
            {
                ViewBag.Message = "Теперь, чтобы воспользоваться функциями сервера нужно зайти в аккаунт";
                return RedirectToAction("Login", "Account");
            }
            return View(new ServerIdViewModel { IsRunning = false, logs = "", ServerDesc = new MinecraftServerWrap { Name = "My server", Description = "Ta pohui", Id="qwertyuio"}, ServerState = ServerState.starting });
        }

        public IActionResult See()
        {
            return RedirectToAction("Index");
        }

        [HttpGet("/Server/See/{Id}/sti")]
        public async Task GetServerStats(string Id)
        {
            Response.ContentType = "text/event-stream";
            while (true)
            {
                switch (Sendtype)
                {
                    case SendType.Server:
                        await Response.WriteAsync($"data: {{" +
                            $"\"Type\": \"Server\"," +
                            $"\"State\": \"{ServerState.starting}\"" +
                            $"}}\n\n");
                        await Response.Body.FlushAsync();
                        Sendtype = SendType.Info;
                    break;
                    case SendType.Info:
                        adaptivePing = 5000;
                        await Response.WriteAsync($"data: {{" +
                            $"\"Type\": \"Info\"," +
                            $"\"Players\": 0," +
                            $" \"MemoryUsage\": 228" +
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
