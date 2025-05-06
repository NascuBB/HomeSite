using HomeSite.Entities;
using HomeSite.Helpers;
using Newtonsoft.Json;

namespace HomeSite.Managers
{
	public class SharedAdministrationManager : ISharedAdministrationManager
	{
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

		private readonly SharedRightsDBContext _context;
        private readonly IUserHelper _userHelper;

		public SharedAdministrationManager(SharedRightsDBContext context, IUserHelper userHelper)
		{
			_context = context;
            _userHelper = userHelper;
		}

		public List<string> GetAllowedUsernames(string Id)
		{
			var shares = _context.SharedRights.Where(x => x.serverid == Id).ToList();
            List<string> users = new List<string>();
            foreach (var share in shares)
            {
				string? uname = _userHelper.GetUsername(share.userid);
				if (uname != null)
                    users.Add(uname);
            }
            return users;
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

		public bool DeleteSharedUser(string Id, string user)
		{
			SharedRights? shToDel = _context.SharedRights.Find(_userHelper.GetUserId(user), Id);
			if(shToDel is not null)
			{
                _context.SharedRights.Remove(shToDel);
				_context.SaveChanges();
				return true;
            }
			return false;
		}

        public bool DeleteServer(string Id)
        {
            try
			{
                var userServerLinks = _context.SharedRights
	            .Where(us => us.serverid == Id)
	            .ToList();

			    _context.SharedRights.RemoveRange(userServerLinks);
                _context.SaveChanges();
                return true; 
            }
            catch (Exception ex)
            {
                return false;
            }
		}

		public bool HasSharedThisServer(string Id, string user)
		{
			if (!_context.SharedRights.Any(x => x.userid == _userHelper.GetUserId(user))) return false;
            SharedRights? thisShared = _context.SharedRights.Find(_userHelper.GetUserId(user), Id);
			return thisShared is not null;
		}

		public List<SharedRights>? GetAllowedServers(string username)
		{
            return _context.SharedRights.Where(x => x.userid == _userHelper.GetUserId(username)).ToList();
		}

		public SharedRights? GetUserSharedRights(string username, string Id)
		{
			return _context.SharedRights.FirstOrDefault(x => x.serverid == Id && x.userid == _userHelper.GetUserId(username));
		}

		public async void SetUserSharedRights(SharedRights rights)
		{
            var right = _context.SharedRights.FirstOrDefault(x => x.serverid == rights.serverid && x.userid == rights.userid);
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
                _context.SharedRights.Add(rights);
            }
            _context.SaveChanges();
            
		}

	}
}
