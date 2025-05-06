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

		public int GetUserId(string username)
        {
            return _ucon.UserAccounts.First(x => x.username == username).id;
        }

        public string? GetUsername(int id)
        {
            var user = _ucon.UserAccounts.Find(id);
            if (user == null)
                return null;
            else
                return user.username;
        }
    }
}
