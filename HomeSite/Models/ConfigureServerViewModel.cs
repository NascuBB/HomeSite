using HomeSite.Managers;

namespace HomeSite.Models
{
    public class ConfigureServerViewModel
    {
        public int MaxPlayers { get; set; }
        public GameMode GameMode { get; set; }
        public Difficulty Difficulty { get; set; }
        public bool Whitelist { get; set; }
        public bool OnlineMode { get; set; }
        public bool Pvp { get; set; }
        public bool CommandBlock { get; set; }
        public bool Flight { get; set; }
        public bool SpawnMonsters { get; set; }
        public bool Nether { get; set; }
        public bool ForceGM { get; set; }
        public int SpawnProtection { get; set; }
        public bool IsConfigured { get; set; }
        public bool ModsInstalled { get; set; }
    }
}
