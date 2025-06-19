using HomeSite.Entities;
using HomeSite.Generated;
using HomeSite.Helpers;
using HomeSite.Models;
using static NuGet.Packaging.PackagingConstants;

namespace HomeSite.Managers
{
    public interface IMinecraftServerManager
    {
        /// <summary>
        /// creates server folder and returns id of new created server
        /// </summary>
        /// <param name="name">name of server</param>
        /// <param name="ownerName">username of owner</param>
        /// <param name="version">version of server</param>
        /// <param name="description">description to server</param>
        /// <returns></returns>
        public Task<string> CreateServer(string name, string ownerName, ServerCore serverCore, MinecraftVersion version, string? description = null);

        /// <summary>
        /// Deletes minecraft server
        /// </summary>
        /// <param name="Id">Id of server</param>
        /// <returns></returns>
        public Task<bool> DeleteServer(string Id);

        /// <summary>
        /// Sets new description to server
        /// </summary>
        /// <param name="Id">Id of server</param>
        /// <param name="newValue">new description</param>
        /// <returns></returns>
        public Task SetServerDesc(string Id, string newValue);
        /// <summary>
        /// Sets new name to server
        /// </summary>
        /// <param name="Id">Id of server</param>
        /// <param name="newValue">new name</param>
        /// <returns></returns>
        public Task SetServerName(string Id, string newValue);


        /// <summary>
        /// Get server specifications
        /// </summary>
        /// <param name="id">Id of server</param>
        /// <returns><see cref="Server"/> entity of requested minecraft server</returns>
        public Task<Server?> GetServerSpecs(string id);

        /// <summary>
        /// Check if server exists
        /// </summary>
        /// <param name="Id">Id of server</param>
        /// <returns><see cref="bool"/> true if server exists, oterwise false</returns>
        public Task<bool> ServerExists(string Id);

        public void LaunchServer(string Id);

    }
}
