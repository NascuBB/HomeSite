using HomeSite.Entities;

namespace HomeSite.Models
{
    public class ServerSettingsViewModel
    {
        public required Server Server { get; set; }
        public ServerState ServerState { get; set; }
        public required string Domain { get; set; }
    }
}
