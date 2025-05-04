using HomeSite.Entities;

namespace HomeSite.Helpers
{
    public class UserHelper
    {
        public static int GetUserId(string username)
        {
            using (var ucon = new UserDBContext())
            {
                return ucon.UserAccounts.First(x => x.username == username).id;
            }
        }

        public static string? GetUsername(int id)
        {
            using (var ucon = new UserDBContext())
            {
                var user = ucon.UserAccounts.Find(id);
                if (user == null)
                    return null;
                else
                    return user.username;
            }
        }
    }
}
