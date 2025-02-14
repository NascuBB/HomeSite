using HomeSite.Managers;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace HomeSite.Controllers
{
    [ApiController]
    [Route("Server/See/{Id}/api")]
    public class ServerApiController : ControllerBase
    {
        private readonly MinecraftServerManager _minecraftServerManager;
        public ServerApiController(MinecraftServerManager manager)
        {
            _minecraftServerManager = manager;
        }

        [HttpPost("start")]
        public IActionResult StartServer(string Id)
        {
            if (HttpContext.User.Identity.Name == null || MinecraftServerManager.GetServerSpecs(Id).OwnerName != HttpContext.User.Identity.Name)
                if (!SharedAdministrationManager.HasSharedThisServer(Id, HttpContext.User.Identity.Name) 
                    || !SharedAdministrationManager.GetUserSharedRights(HttpContext.User.Identity.Name, Id).StartStopServer)
                    return Unauthorized();
            try
            {
                _minecraftServerManager.LaunchServer(Id);
                return Ok("Сервер запускается.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка запуска сервера: {ex.Message}");
            }
        }

        [HttpPost("stop")]
        public async Task<IActionResult> StopServer(string Id)
        {
            if (HttpContext.User.Identity.Name == null || MinecraftServerManager.GetServerSpecs(Id).OwnerName != HttpContext.User.Identity.Name)
                if (!SharedAdministrationManager.HasSharedThisServer(Id, HttpContext.User.Identity.Name) 
                    || !SharedAdministrationManager.GetUserSharedRights(HttpContext.User.Identity.Name, Id).StartStopServer)
                    return Unauthorized();
            try
            {
                await MinecraftServerManager.serversOnline.First(x => x.Id == Id).StopServer();
                return Ok("Выключение.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка выключения сервера: {ex.Message}");
            }
        }

        [HttpPost("command")]
        public async Task<IActionResult> SendCommand([FromBody] string command, string Id)
        {
            if (HttpContext.User.Identity.Name == null || MinecraftServerManager.GetServerSpecs(Id).OwnerName != HttpContext.User.Identity.Name)
                if (!SharedAdministrationManager.HasSharedThisServer(Id, HttpContext.User.Identity.Name) 
                    || !SharedAdministrationManager.GetUserSharedRights(HttpContext.User.Identity.Name, Id).SendCommands)
                    return Unauthorized();
            try
            {
                string res = await MinecraftServerManager.serversOnline.First(x => x.Id == Id).SendCommandAsync(command);
                //string res = "Ok";//await MinecraftServerManager.GetInstance().SendCommand(command); // Ваш метод для отправки команды
                return Ok(res);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка отправки команды: {ex.Message}");
            }
        }

        [HttpGet("remaining-time")]
        public IActionResult GetRemainingTime(string Id)
        {
            if (!MinecraftServerManager.serversOnline.Any(x => x.Id == Id)) return NotFound("Сервер не найден");
            return Ok(new { remainingTime = MinecraftServerManager.serversOnline.First(x => x.Id == Id).RemainingTime });
        }
    }
}
