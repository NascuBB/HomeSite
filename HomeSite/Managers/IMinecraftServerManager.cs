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
        /// Gets the collection of servers that are currently in the process of being created, keyed by their unique identifiers.
        /// </summary>
        public Dictionary<string, ServerCreation> InCreation { get; }
        /// <summary>
        /// Gets the list of Minecraft servers that are currently online.
        /// </summary>
        public List<MinecraftServer> ServersOnline { get; }

        /// <summary>
        /// creates server folder and returns id of new created server
        /// </summary>
        /// <param name="name">name of server</param>
        /// <param name="ownerName">username of owner</param>
        /// <param name="version">version of server</param>
        /// <param name="description">description to server</param>
        /// <returns></returns>
        public Task<string> CreateServer(string name, string ownerName, string serverCore, string version, string? description = null);
        /// <summary>
        /// Deletes minecraft server
        /// </summary>
        /// <param name="Id">Id of server</param>
        /// <returns></returns>
        public Task<bool> DeleteServer(string Id);
        /// <summary>
        /// Sets the domain name for the server identified by the specified ID.
        /// </summary>
        /// <param name="id">The unique identifier of the server whose domain is to be updated. Cannot be null or empty.</param>
        /// <param name="newDomain">The new domain name to assign to the server. Cannot be null or empty.</param>
        public Task SetServerDomain(string id, string newDomain);
        /// <summary>
        /// Updates the port mappings for the server identified by the specified ID.
        /// </summary>
        /// <param name="id">The unique identifier of the server whose port mappings are to be updated. Cannot be null or empty.</param>
        /// <param name="portMappings">A list of <see cref="PortMapping"/> objects representing the new port mappings to assign to the server.
        /// Cannot be null.</param>
        public Task SetServerMappings(string id, List<PortMapping> portMappings);

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
        /// <summary>
        /// Start minecraft server container
        /// </summary>
        /// <param name="Id">Id of server</param>
        /// <returns></returns>
        public Task LaunchServer(string Id);
        /// <summary>
        /// Finish server creation.
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public Task<bool> FinishServerCreation(string Id);
        /// <summary>
        /// Retrieves the server creation details for the specified server identifier.
        /// </summary>
        /// <param name="Id">The unique identifier of the server whose creation details are to be retrieved. Cannot be null or empty.</param>
        /// <returns>A <see cref="ServerCreation"/> object containing the creation details of the specified server, or
        /// <c>null</c> if no server with the given identifier exists.</returns>
        public ServerCreation GetServerCreation(string Id);

    }
}
