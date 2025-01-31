namespace HomeSite.Models
{
    public class MinecraftServerSpecifications
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string OwnerName { get; set; }
        public string Version { get; set; }
        public int PublicPort { get; set; }
        public int RCONPort { get; set; }
    }
}
