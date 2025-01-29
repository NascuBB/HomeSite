using HomeSite.Entities;

namespace HomeSite.Models
{
	public class ServerIdViewModel
	{
		public MinecraftServerWrap ServerDesc { get; set; }
		public bool IsRunning { get; set; }
		public ServerState ServerState { get; set; }
		public string logs { get; set; }
	}


	public enum ServerState
	{
		starting,
		started
	}
}
