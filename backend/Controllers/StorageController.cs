using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using CloudCore.Contracts.Responses;
using CloudCore.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudCore.Controllers
{
    /// <summary>
    /// Controller for managing and viewing storage usage
    /// </summary>
    [ApiController]
    [Route("user/{userId}/storage")]
    [Authorize]
    public class StorageController : ControllerBase
    {
        private readonly IStorageTrackingService _storageTrackingService;
        private readonly IValidationService _validationService;
        private readonly ILogger<StorageController> _logger;

        public StorageController(
            IStorageTrackingService storageTrackingService,
            IValidationService validationService,
            ILogger<StorageController> logger)
        {
            _storageTrackingService = storageTrackingService;
            _validationService = validationService;
            _logger = logger;
        }

        /// <summary>
        /// Gets the user's personal storage usage and limit
        /// </summary>
        /// <param name="userId">User ID from route</param>
        /// <returns>Storage information including used space, limit, and percentage</returns>
        [HttpGet("personal")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetPersonalStorageInfo([Required] int userId)
        {

            _logger.LogInformation("Fetching personal storage info for user {UserId}", userId);

            var (usedMb, limitMb) = await _storageTrackingService.GetPersonalStorageInfoAsync(userId);

            var percentageUsed = limitMb > 0 ? (double)usedMb / limitMb * 100 : 0;

            return Ok(new
            {
                usedMb = usedMb,
                limitMb = limitMb,
                availableMb = limitMb - usedMb,
                percentageUsed = Math.Round(percentageUsed, 2),
                formattedUsed = FormatStorageSize(usedMb),
                formattedLimit = FormatStorageSize(limitMb),
                formattedAvailable = FormatStorageSize(limitMb - usedMb)
            });
        }

        /// <summary>
        /// Gets storage information for a specific teamspace
        /// </summary>
        /// <param name="userId">User ID from route (for authorization)</param>
        /// <param name="teamspaceId">Teamspace ID to get storage for</param>
        /// <returns>Teamspace storage information</returns>
        [HttpGet("teamspace/{teamspaceId}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetTeamspaceStorageInfo(
            [Required] int userId,
            [Required] int teamspaceId)
        {

            _logger.LogInformation("Fetching teamspace storage info. UserId={UserId}, TeamspaceId={TeamspaceId}",
                userId, teamspaceId);

            var (usedMb, limitMb) = await _storageTrackingService.GetTeamspaceStorageInfoAsync(teamspaceId);

            var percentageUsed = limitMb > 0 ? (double)usedMb / limitMb * 100 : 0;

            return Ok(new
            {
                teamspaceId = teamspaceId,
                usedMb = usedMb,
                limitMb = limitMb,
                availableMb = limitMb - usedMb,
                percentageUsed = Math.Round(percentageUsed, 2),
                formattedUsed = FormatStorageSize(usedMb),
                formattedLimit = FormatStorageSize(limitMb),
                formattedAvailable = FormatStorageSize(limitMb - usedMb)
            });
        }

        /// <summary>
        /// Recalculates the user's personal storage from actual file data
        /// Useful for fixing inconsistencies
        /// </summary>
        /// <param name="userId">User ID to recalculate for</param>
        /// <returns>Updated storage information</returns>
        [HttpPost("personal/recalculate")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> RecalculatePersonalStorage([Required] int userId)
        {

            _logger.LogInformation("Recalculating personal storage for user {UserId}", userId);

            await _storageTrackingService.RecalculatePersonalStorageAsync(userId);

            var (usedMb, limitMb) = await _storageTrackingService.GetPersonalStorageInfoAsync(userId);

            return Ok(new
            {
                message = "Storage recalculated successfully",
                usedMb = usedMb,
                limitMb = limitMb,
                formattedUsed = FormatStorageSize(usedMb),
                formattedLimit = FormatStorageSize(limitMb)
            });
        }

        /// <summary>
        /// Recalculates teamspace storage from actual file data
        /// Requires admin permission
        /// </summary>
        /// <param name="userId">User ID (for authorization)</param>
        /// <param name="teamspaceId">Teamspace ID to recalculate</param>
        /// <returns>Updated storage information</returns>
        [HttpPost("teamspace/{teamspaceId}/recalculate")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> RecalculateTeamspaceStorage(
            [Required] int userId,
            [Required] int teamspaceId)
        {

            _logger.LogInformation("Recalculating teamspace storage. UserId={UserId}, TeamspaceId={TeamspaceId}",
                userId, teamspaceId);

            await _storageTrackingService.RecalculateTeamspaceStorageAsync(teamspaceId);

            var (usedMb, limitMb) = await _storageTrackingService.GetTeamspaceStorageInfoAsync(teamspaceId);

            return Ok(new
            {
                message = "Teamspace storage recalculated successfully",
                teamspaceId = teamspaceId,
                usedMb = usedMb,
                limitMb = limitMb,
                formattedUsed = FormatStorageSize(usedMb),
                formattedLimit = FormatStorageSize(limitMb)
            });
        }

        private string FormatStorageSize(long sizeMb)
        {
            if (sizeMb >= 1024)
            {
                return $"{(double)sizeMb / 1024:F2} GB";
            }
            return $"{sizeMb} MB";
        }
    }
}