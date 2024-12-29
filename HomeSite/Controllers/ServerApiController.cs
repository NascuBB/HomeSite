using HomeSite.Helpers;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace HomeSite.Controllers
{
    [ApiController]
    [Route("api/server")]
    public class ServerApiController : ControllerBase
    {
        [HttpPost("start")]
        public IActionResult StartServer()
        {
            try
            {
                MinecraftServerManager.GetInstance().LaunchServer();
                return Ok("Сервер запускается.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка запуска сервера: {ex.Message}");
            }
        }

        [HttpPost("stop")]
        public IActionResult StopServer()
        {
            try
            {
                MinecraftServerManager.GetInstance().StopServer();
                return Ok("Выключение.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка выключения сервера: {ex.Message}");
            }
        }

        [HttpPost("command")]
        public IActionResult SendCommand([FromBody] string command)
        {
            try
            {
                MinecraftServerManager.GetInstance().SendCommand(command); // Ваш метод для отправки команды
                return Ok("Команда отправлена.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка отправки команды: {ex.Message}");
            }
        }
    }
}
