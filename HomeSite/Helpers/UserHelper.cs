using HomeSite.Entities;

namespace HomeSite.Helpers
{
    public class UserHelper : IUserHelper
    {
        private readonly UserDBContext _ucon;
		public UserHelper(UserDBContext ucon)
		{
			_ucon = ucon;
		}

        public bool ChangeUserSizeUsed(long size, string username)
        {
            var user = _ucon.UserAccounts.First(x => x.Username == username);
            if (user == null)
                return false;
            user.SizeUsed += size;
            return _ucon.SaveChanges() > 0;
                
        }

        public bool ChangeUserSizeUsed(long size, int uid)
        {
            var user = _ucon.UserAccounts.Find(uid);
            if (user == null)
                return false;
            user.SizeUsed += size;
            return _ucon.SaveChanges() > 0;
        }

        public int GetUserId(string username)
        {
            return _ucon.UserAccounts.First(x => x.Username == username).Id;
        }

        public string? GetUsername(int id)
        {
            var user = _ucon.UserAccounts.Find(id);
            if (user == null)
                return null;
            else
                return user.Username;
        }

        public long GetUserSizeUsed(int id)
        {
            var user = _ucon.UserAccounts.Find(id);
            if (user == null)
                return 0;
            else
                return user.SizeUsed;
        }

        public long GetUserSizeUsed(string username)
        {
            var user = _ucon.UserAccounts.First(x => x.Username == username);
            if (user == null)
                return 0;
            else
                return user.SizeUsed;
        }
    }
}
