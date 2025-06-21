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
            AddShareds = false,
            SeeServerFiles = false,
        };

        public static SharedRightsDBO allRightsDBO = new SharedRightsDBO
        {
            StartStopServer = true,
            EditMods = true,
            EditServerPreferences = true,
            SendCommands = true,
            UploadMods = true,
            AddShareds = true,
            SeeServerFiles = true
        };

        public static SharedRights defaultRights(int userId, string serverId)
        {
            return new SharedRights
            {
                StartStopServer = false,
                EditMods = false,
                EditServerPreferences = false,
                SendCommands = false,
                UploadServer = false,
                AddShareds = false,
                SeeServerFiles = false,
                UserId = userId,
                ServerId = serverId
            };
        }
        public static SharedRights allRights(int userId, string serverId)
        {
            return new SharedRights
            {
                StartStopServer = true,
                EditMods = true,
                EditServerPreferences = true,
                SendCommands = true,
                UploadServer = true,
                AddShareds = true,
                SeeServerFiles = true,
                UserId = userId,
                ServerId = serverId
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
			var shares = _context.SharedRights.Where(x => x.ServerId == Id).ToList();
            List<string> users = new List<string>();
            foreach (var share in shares)
            {
				string? uname = _userHelper.GetUsername(share.UserId);
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
	            .Where(us => us.ServerId == Id)
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
			if (!_context.SharedRights.Any(x => x.UserId == _userHelper.GetUserId(user))) return false;
            SharedRights? thisShared = _context.SharedRights.Find(_userHelper.GetUserId(user), Id);
			return thisShared is not null;
		}

		public List<SharedRights>? GetAllowedServers(string username)
		{
            return _context.SharedRights.Where(x => x.UserId == _userHelper.GetUserId(username)).ToList();
		}

		public SharedRights? GetUserSharedRights(string username, string Id)
		{
			return _context.SharedRights.FirstOrDefault(x => x.ServerId == Id && x.UserId == _userHelper.GetUserId(username));
		}

		public async void SetUserSharedRights(SharedRights rights)
		{
            var right = _context.SharedRights.FirstOrDefault(x => x.ServerId == rights.ServerId && x.UserId == rights.UserId);
			if (right != null)
			{
				right.AddShareds = rights.AddShareds;
				right.EditMods = rights.EditMods;
				right.EditServerPreferences = rights.EditServerPreferences;
				right.SendCommands = rights.SendCommands;
				right.StartStopServer = rights.StartStopServer;
				right.UploadServer = rights.UploadServer;
                right.SeeServerFiles = rights.SeeServerFiles;
			}
            else
            {
                _context.SharedRights.Add(rights);
            }
            _context.SaveChanges();
            
		}

	}
}
