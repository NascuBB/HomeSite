using HomeSite.Managers;
using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;

namespace HomeSite.Controllers
{
    [Route("ws/logs/{serverId}")]
    public class LogWebSocketController : ControllerBase
    {
        private readonly LogConnectionManager _logConnectionManager;

        public LogWebSocketController(LogConnectionManager logConnectionManager)
        {
            _logConnectionManager = logConnectionManager;
        }

        [HttpGet]
        public async Task Get(string serverId)
        {
            if (!User.Identity?.IsAuthenticated ?? false)
            {
                HttpContext.Response.StatusCode = 401; // Unauthorized
                return;
            }

            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                using WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await _logConnectionManager.HandleWebSocketAsync(serverId, webSocket);
            }
            else
            {
                HttpContext.Response.StatusCode = 400;
            }
        }
    }

}
