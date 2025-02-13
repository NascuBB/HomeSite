using HomeSite.Entities;
using HomeSite.Managers;

namespace HomeSite.Models
{
	public class ServerIdViewModel
	{
		public MinecraftServerWrap ServerDesc { get; set; }
		public bool IsRunning { get; set; }
		public ServerState ServerState { get; set; }
		public string logs { get; set; }
        public List<string> AllowedUsers { get; set; }
		public SharedRights SharedRights { get; set; }
		public string Version { get; set; }
		public string PublicAddress { get; set; }
    }


	public enum ServerState
	{
		starting,
		started,
		stopped
	}
}
