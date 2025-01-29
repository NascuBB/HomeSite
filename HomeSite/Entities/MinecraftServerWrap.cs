using HomeSite.Models;

namespace HomeSite.Entities
{
    public class MinecraftServerWrap
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsRunning { get; set; }
    }
}
