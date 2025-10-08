namespace CloudCore.Services.Interfaces
{
    /// <summary>
    /// Service for tracking and managing storage usage for users and teamspaces
    /// </summary>
    public interface IStorageTrackingService
    {
        #region Personal Storage

        /// <summary>
        /// Updates user's personal storage usage when a file is added
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="fileSizeBytes">Size of file being added in bytes</param>
        Task AddToPersonalStorageAsync(int userId, long fileSizeBytes);

        /// <summary>
        /// Updates user's personal storage usage when a file is removed
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="fileSizeBytes">Size of file being removed in bytes</param>
        Task RemoveFromPersonalStorageAsync(int userId, long fileSizeBytes);

        /// <summary>
        /// Checks if adding a file would exceed user's personal storage limit
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="fileSizeBytes">Size of file to add in bytes</param>
        /// <returns>True if within limit, false if would exceed</returns>
        Task<bool> CanAddToPersonalStorageAsync(int userId, long fileSizeBytes);

        /// <summary>
        /// Gets current personal storage usage and limit for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Tuple of (usedMb, limitMb)</returns>
        Task<(long usedMb, long limitMb)> GetPersonalStorageInfoAsync(int userId);

        #endregion

        #region Teamspace Storage

        /// <summary>
        /// Updates teamspace storage usage when a file is added
        /// </summary>
        /// <param name="teamspaceId">Teamspace ID</param>
        /// <param name="fileSizeBytes">Size of file being added in bytes</param>
        Task AddToTeamspaceStorageAsync(int teamspaceId, long fileSizeBytes);

        /// <summary>
        /// Updates teamspace storage usage when a file is removed
        /// </summary>
        /// <param name="teamspaceId">Teamspace ID</param>
        /// <param name="fileSizeBytes">Size of file being removed in bytes</param>
        Task RemoveFromTeamspaceStorageAsync(int teamspaceId, long fileSizeBytes);

        /// <summary>
        /// Checks if adding a file would exceed teamspace storage limit
        /// </summary>
        /// <param name="teamspaceId">Teamspace ID</param>
        /// <param name="fileSizeBytes">Size of file to add in bytes</param>
        /// <returns>True if within limit, false if would exceed</returns>
        Task<bool> CanAddToTeamspaceStorageAsync(int teamspaceId, long fileSizeBytes);

        /// <summary>
        /// Gets current teamspace storage usage and limit
        /// </summary>
        /// <param name="teamspaceId">Teamspace ID</param>
        /// <returns>Tuple of (usedMb, limitMb)</returns>
        Task<(long usedMb, long limitMb)> GetTeamspaceStorageInfoAsync(int teamspaceId);

        #endregion

        #region Batch Operations

        /// <summary>
        /// Updates storage for multiple items (used during folder operations)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="items">Items being added/removed</param>
        /// <param name="isAdding">True if adding, false if removing</param>
        Task UpdateStorageForItemsAsync(int userId, List<Domain.Entities.Item> items, bool isAdding);

        /// <summary>
        /// Recalculates and updates actual storage usage from database
        /// Useful for fixing inconsistencies
        /// </summary>
        /// <param name="userId">User ID to recalculate for</param>
        Task RecalculatePersonalStorageAsync(int userId);

        /// <summary>
        /// Recalculates and updates actual teamspace storage usage from database
        /// </summary>
        /// <param name="teamspaceId">Teamspace ID to recalculate for</param>
        Task RecalculateTeamspaceStorageAsync(int teamspaceId);

        #endregion
    }
}
