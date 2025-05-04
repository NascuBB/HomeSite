using HomeSite.Entities;
using HomeSite.Helpers;
using Newtonsoft.Json;

namespace HomeSite.Managers
{
	public class SharedAdministrationManager
	{
		private static readonly string folder = Path.Combine(Environment.CurrentDirectory, "servers");
		private static readonly string sharedjs = Path.Combine(folder, "shareds.json");

        public static SharedRightsDBO defaultRightsDBO = new SharedRightsDBO
        {
            StartStopServer = false,
            EditMods = false,
            EditServerPreferences = false,
            SendCommands = false,
            UploadMods = false,
            AddShareds = false
        };

        public static SharedRightsDBO allRightsDBO = new SharedRightsDBO
        {
            StartStopServer = true,
            EditMods = true,
            EditServerPreferences = true,
            SendCommands = true,
            UploadMods = true,
            AddShareds = true
        };
        //user -> serverid -> rights
        //public static Dictionary<string, Dictionary<string, SharedRights>> serversSharedAdmins { get; private set; } = new();

        //public static async void Prepare()
        //{
        //          serversSharedAdmins = await GetServersShareds();
        //}

        //private static async Task<Dictionary<string, Dictionary<string, SharedRights>>> GetServersShareds()
        //{
        //	try
        //	{
        //		if (!Path.Exists(folder))
        //		{
        //			Directory.CreateDirectory(folder);
        //		}
        //		if (!File.Exists(sharedjs))
        //		{
        //			File.Create(sharedjs);
        //			return new Dictionary<string, Dictionary<string, SharedRights>>();
        //		}
        //		return JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, SharedRights>>>(await File.ReadAllTextAsync(sharedjs)) ?? new Dictionary<string, Dictionary<string, SharedRights>>();
        //	}
        //	catch (Exception e)
        //	{
        //		Console.WriteLine(e.ToString());
        //		return new Dictionary<string, Dictionary<string, SharedRights>>();
        //	}
        //}

        //private static async Task SaveServersShareds()
        //{
        //	string servers = JsonConvert.SerializeObject(serversSharedAdmins, Formatting.Indented);
        //	await File.WriteAllTextAsync(sharedjs, servers);
        //}

        public static SharedRights defaultRights(int userId, string serverId)
        {
            return new SharedRights
            {
                startstopserver = false,
                editmods = false,
                editserverpreferences = false,
                sendcommands = false,
                uploadmods = false,
                addshareds = false,
                userid = userId,
                serverid = serverId
            };
        }
        public static SharedRights allRights(int userId, string serverId)
        {
            return new SharedRights
            {
                startstopserver = true,
                editmods = true,
                editserverpreferences = true,
                sendcommands = true,
                uploadmods = true,
                addshareds = true,
                userid = userId,
                serverid = serverId
            };
        }

        public static List<string> GetAllowedUsernames(string Id)
		{
			using (var context = new SharedRightsDBContext())
			{
				var shares = context.SharedRights.Where(x => x.serverid == Id).ToList();
                List<string> users = new List<string>();
                foreach (var share in shares)
                {
					string? uname = UserHelper.GetUsername(share.userid);
					if (uname != null)
                        users.Add(uname);
                }
                return users;
            }
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
			using (var context = new SharedRightsDBContext())
			{				
				SharedRights? shToDel = context.SharedRights.Find(UserHelper.GetUserId(user), Id);
				if(shToDel is not null)
				{
                    context.SharedRights.Remove(shToDel);
					context.SaveChanges();
					return true;
                }
				return false;
			}
		}

		public static bool HasSharedThisServer(string Id, string user)
		{
			using(var context = new SharedRightsDBContext())
			{
				if (!context.SharedRights.Any(x => x.userid == UserHelper.GetUserId(user))) return false;
                SharedRights? shToDel = context.SharedRights.Find(UserHelper.GetUserId(user), Id);
				return shToDel is not null;
            }
		}

		public static List<SharedRights>? GetAllowedServers(string username)
		{
            using (var context = new SharedRightsDBContext())
            {
                return context.SharedRights.Where(x => x.userid == UserHelper.GetUserId(username)).ToList();
            }
		}

		public static SharedRights? GetUserSharedRights(string username, string Id)
		{
			using ( var context = new SharedRightsDBContext())
			{
				return context.SharedRights.FirstOrDefault(x => x.serverid == Id && x.userid == UserHelper.GetUserId(username));
			}
		}

		public static async void SetUserSharedRights(SharedRights rights)
		{
            using (var context = new SharedRightsDBContext())
            {
                var right = context.SharedRights.FirstOrDefault(x => x.serverid == rights.serverid && x.userid == rights.userid);
				if (right != null)
				{
					right.addshareds = rights.addshareds;
					right.editmods = rights.editmods;
					right.editserverpreferences = rights.editserverpreferences;
					right.sendcommands = rights.sendcommands;
					right.startstopserver = rights.startstopserver;
					right.uploadmods = rights.uploadmods;
				}
                else
                {
                    context.SharedRights.Add(rights);
                }
                context.SaveChanges();
            }
		}

	}
}
