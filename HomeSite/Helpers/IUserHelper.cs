namespace HomeSite.Helpers
{
	public interface IUserHelper
	{
		public int GetUserId(string username);
		public string? GetUsername(int id);
	}
}
