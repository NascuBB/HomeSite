using System.Net.WebSockets;
using System.Text;
using System.Collections.Concurrent;

namespace HomeSite.Managers
{
    public class LogConnectionManager
    {
        private readonly Dictionary<string, List<WebSocket>> _connections = new();

        public async Task HandleWebSocketAsync(string serverId, WebSocket socket)
        {
            try
            {


                if (!_connections.ContainsKey(serverId))
                    _connections[serverId] = new List<WebSocket>();

                _connections[serverId].Add(socket);
                //Console.WriteLine($"🔗 Пользователь подключился к логам сервера {serverId}");

                var buffer = new byte[1024 * 4];
                while (socket.State == WebSocketState.Open)
                {
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _connections[serverId].Remove(socket);
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", CancellationToken.None);
                        break;
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public async Task BroadcastLogAsync(string serverId, string message)
        {
            if (_connections.ContainsKey(serverId))
            {
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                foreach (var socket in _connections[serverId])
                {
                    if (socket.State == WebSocketState.Open)
                    {
                        await socket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }
            }
        }
    }

}
