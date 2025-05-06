using HomeSite.Entities;
using HomeSite.Helpers;

namespace HomeSite.Managers
{
	public interface ISharedAdministrationManager
	{
		public List<string> GetAllowedUsernames(string Id);

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

		public bool DeleteSharedUser(string Id, string user);

		public bool HasSharedThisServer(string Id, string user);

		public List<SharedRights>? GetAllowedServers(string username);

		public SharedRights? GetUserSharedRights(string username, string Id);

		public void SetUserSharedRights(SharedRights rights);

		public bool DeleteServer(string Id);
	}
}
