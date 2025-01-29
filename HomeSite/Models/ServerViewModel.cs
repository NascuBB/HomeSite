using HomeSite.Entities;

namespace HomeSite.Models
{
    public class ServerViewModel
    {
        public ServerCreation ServerCreation { get; set; }
        public MinecraftServerWrap? OwnServer { get; set; }
        public List<MinecraftServerWrap>? AllowedServers { get; set; }
    }

    public enum ServerCreation
    {
        notCreated,
        AddingMods,
        Created
    }
}
