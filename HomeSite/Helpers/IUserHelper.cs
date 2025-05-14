namespace HomeSite.Helpers
{
	public interface IUserHelper
	{
		public int GetUserId(string username);
		public string? GetUsername(int id);
		public long GetUserSizeUsed(int id);
        public long GetUserSizeUsed(string username);
        /// <summary>
        /// Changes size used by user
        /// </summary>
        /// <param name="size">Diff file size</param>
        /// <param name="username">user name</param>
        /// <returns></returns>
        public bool ChangeUserSizeUsed(long size, string username);
        /// <summary>
        /// Changes size used by user
        /// </summary>
        /// <param name="size">Diff file size</param>
        /// <param name="uid">user id</param>
        /// <returns></returns>
        public bool ChangeUserSizeUsed(long size, int uid);
    }
}
