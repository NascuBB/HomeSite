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
        public async Task<IActionResult> StopServer([FromBody] string pass, string Id)
        {
            try
            {
                if(pass != "Jonkler1111")
                {
                    return Ok("пароль не верный");
                }
                //await MinecraftServerManager.GetInstance().StopServer();
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
    }
}
