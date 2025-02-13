using HomeSite.Models;
using Newtonsoft.Json;

namespace HomeSite.Managers
{
	public class SharedAdministrationManager
	{
		private static readonly string folder = Path.Combine(Environment.CurrentDirectory, "servers");
		private static readonly string sharedjs = Path.Combine(folder, "shareds.json");
		
		public static readonly SharedRights defaultRights = new SharedRights 
		{
			StartStopServer = false,
			EditMods = false,
			EditServerPreferences = false,
			SendCommands = false,
			UploadMods = false,
			AddShareds = false,
		};
        public static readonly SharedRights allRights = new SharedRights
        {
            StartStopServer = true,
            EditMods = true,
            EditServerPreferences = true,
            SendCommands = true,
            UploadMods = true,
			AddShareds = true,
        };
        //user -> serverid -> rights
        public static Dictionary<string, Dictionary<string, SharedRights>> serversSharedAdmins { get; private set; } = new();
		
		public static async void Prepare()
		{
            serversSharedAdmins = await GetServersShareds();
		}

		private static async Task<Dictionary<string, Dictionary<string, SharedRights>>> GetServersShareds()
		{
			try
			{
				if (!Path.Exists(folder))
				{
					Directory.CreateDirectory(folder);
				}
				if (!File.Exists(sharedjs))
				{
					File.Create(sharedjs);
					return new Dictionary<string, Dictionary<string, SharedRights>>();
				}
				return JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, SharedRights>>>(await File.ReadAllTextAsync(sharedjs)) ?? new Dictionary<string, Dictionary<string, SharedRights>>();
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
				return new Dictionary<string, Dictionary<string, SharedRights>>();
			}
		}

		private static async Task SaveServersShareds()
		{
			string servers = JsonConvert.SerializeObject(serversSharedAdmins, Formatting.Indented);
			await File.WriteAllTextAsync(sharedjs, servers);
		}

		public static List<string> GetAllowedUsers(string Id)
		{
			List<string> users = new List<string>();
			foreach(var user in serversSharedAdmins)
			{
				if(user.Value.ContainsKey(Id))
				{
					users.Add(user.Key);
				}
			}
			return users;
			//var s = serversSharedAdmins.Values.Where(x => x.Keys.Contains(Id));
		}

		//public static bool CreateSharedUser(string Id, string user, SharedRights? rights = null)
		//{
		//	rights ??= defaultRights;
		//	if(!serversSharedAdmins.ContainsKey(user))
		//	{
		//		serversSharedAdmins.Add(user, new Dictionary<string, SharedRights> { {Id, rights} });
		//		return true;
		//	}
		//	serversSharedAdmins[user].Add(Id, rights);

		//	return true;
		//}

		public static bool DeleteSharedUser(string Id, string user)
		{
			bool res = serversSharedAdmins[user].Remove(Id);
			if (res)
				SaveServersShareds();
            return res;
		}

		public static bool HasSharedThisServer(string Id, string user)
		{
			if(!serversSharedAdmins.ContainsKey(user)) { return false; }
			return serversSharedAdmins[user].ContainsKey(Id);
		}

		public static Dictionary<string, SharedRights>? GetAllowedServers(string username)
		{
			if (!serversSharedAdmins.ContainsKey(username))
			{
				return null;
			}
			return serversSharedAdmins[username];
		}

		public static SharedRights? GetUserSharedRights(string username, string Id)
		{
			if (!serversSharedAdmins.ContainsKey(username) || !serversSharedAdmins[username].ContainsKey(Id))
			{
				return null;
			}

			return serversSharedAdmins[username][Id];
		}

		public static async void SetUserSharedRights(string username, string Id, SharedRights rights)
		{
			if(!serversSharedAdmins.ContainsKey(username))
			{
				//Dictionary<string, SharedRights> newRights = 
				serversSharedAdmins.Add(username, new Dictionary<string, SharedRights>
				{
					{ Id, rights }
				});
			}
			else if (!serversSharedAdmins[username].ContainsKey(Id))
			{
				serversSharedAdmins[username].Add(Id, rights);
			}
			else
			{
				serversSharedAdmins[username][Id] = rights;
			}
			SaveServersShareds();
		}

	}

	public class SharedRights
	{
        public bool EditServerPreferences { get; set; }
        public bool EditMods { get; set; }
        public bool StartStopServer { get; set; }
        public bool UploadMods { get; set; }
        public bool SendCommands { get; set; }
		public bool AddShareds { get; set; }
    }
}
