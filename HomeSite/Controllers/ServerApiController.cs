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
        public async Task<IActionResult> StopServer([FromBody] string pass)
        {
            try
            {
                if(pass != "Jonkler1111")
                {
                    return Ok("пароль не верный");
                }
                await MinecraftServerManager.GetInstance().StopServer();
                return Ok("Выключение.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка выключения сервера: {ex.Message}");
            }
        }

        [HttpPost("command")]
        public async Task<IActionResult> SendCommand([FromBody] string command)
        {
            try
            {
                string res = await MinecraftServerManager.GetInstance().SendCommand(command); // Ваш метод для отправки команды
                return Ok(res);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка отправки команды: {ex.Message}");
            }
        }
    }
}
