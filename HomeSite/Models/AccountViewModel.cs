using HomeSite.Entities;

namespace HomeSite.Models
{
	public class AccountViewModel
	{
        public bool HasServer { get; set; }
        public bool ShortLogs { get; set; }
        public MinecraftServerWrap? OwnServer { get; set; }
    }
}
