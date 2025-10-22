using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using CloudCore.Common.Errors;
using CloudCore.Contracts.Requests;
using CloudCore.Contracts.Responses;
using CloudCore.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudCore.Controllers
{
    [ApiController]
    [Route("user/{userId}/teamspaces")]
    [Authorize]
    public class TeamspaceController : ControllerBase
    {
        private readonly ITeamspaceService _teamspaceService;
        private readonly IValidationService _validationService;
        private readonly ILogger<TeamspaceController> _logger;

        public TeamspaceController(
            ITeamspaceService teamspaceService,
            IValidationService validationService,
            ILogger<TeamspaceController> logger)
        {
            _teamspaceService = teamspaceService;
            _validationService = validationService;
            _logger = logger;
        }

        #region Teamspace Management

        /// <summary>
        /// Creates a new teamspace for the user
        /// </summary>
        /// <param name="userId">The ID of the user creating the teamspace</param>
        /// <param name="request">Teamspace creation details including name and description</param>
        /// <returns>
        /// 200 OK with teamspace details if successful
        /// 400 Bad Request if teamspace limit reached or invalid data
        /// 403 Forbidden if user is not authorized
        /// 409 Conflict if teamspace name already exists
        /// </returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateTeamspace(
            [Required] int userId,
            [FromBody] CreateTeamspaceRequest request)
        {

            _logger.LogInformation("User {UserId} attempting to create teamspace '{TeamspaceName}'",
                userId, request.Name);

            var result = await _teamspaceService.CreateTeamspaceAsync(userId, request);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to create teamspace '{TeamspaceName}' for User ID: {UserId}. Reason: {ErrorMessage} (Code: {ErrorCode})",
                    request.Name, userId, result.Message, result.ErrorCode);

                return result.ErrorCode switch
                {
                    ErrorCodes.TEAMSPACE_LIMIT_REACHED => BadRequest(ApiResponse.Error(result.Message, result.ErrorCode)),
                    ErrorCodes.TEAMSPACE_NAME_TAKEN => Conflict(ApiResponse.Error(result.Message, result.ErrorCode)),
                    _ => BadRequest(ApiResponse.Error(result.Message, result.ErrorCode))
                };
            }

            _logger.LogInformation("User {UserId} successfully created teamspace '{TeamspaceName}' with ID: {TeamspaceId}",
                userId, request.Name, result.TeamspaceId);

            return Ok(result);
        }

        /// <summary>
        /// Retrieves all teamspaces that the user owns or is a member of
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <returns>List of teamspaces with user's permission level for each</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<TeamspaceResponse>>> GetUserTeamspaces([Required] int userId)
        {

            _logger.LogInformation("Fetching all teamspaces for User ID: {UserId}", userId);

            var teamspaces = await _teamspaceService.GetUserTeamspacesAsync(userId);

            _logger.LogInformation("Successfully fetched {Count} teamspaces for User ID: {UserId}",
                teamspaces.Count(), userId);

            return Ok(teamspaces);
        }

        /// <summary>
        /// Retrieves detailed information about a specific teamspace
        /// </summary>
        /// <param name="userId">The ID of the user requesting the information</param>
        /// <param name="teamspaceId">The ID of the teamspace to retrieve</param>
        /// <returns>
        /// 200 OK with teamspace details if found and user has access
        /// 403 Forbidden if user is not authorized
        /// 404 Not Found if teamspace doesn't exist or user has no access
        /// </returns>
        [HttpGet("{teamspaceId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TeamspaceResponse>> GetTeamspace(
            [Required] int userId,
            [Required] int teamspaceId)
        {

            _logger.LogInformation("User {UserId} fetching teamspace {TeamspaceId}", userId, teamspaceId);

            var teamspace = await _teamspaceService.GetTeamspaceByIdAsync(teamspaceId, userId);

            if (teamspace == null)
            {
                _logger.LogWarning("Teamspace {TeamspaceId} not found or User {UserId} has no access",
                    teamspaceId, userId);
                return NotFound(ApiResponse.Error("Teamspace not found or you don't have access",
                    ErrorCodes.TEAMSPACE_NOT_FOUND));
            }

            _logger.LogInformation("Successfully fetched teamspace {TeamspaceId} for User {UserId}",
                teamspaceId, userId);

            return Ok(teamspace);
        }

        /// <summary>
        /// Updates a teamspace's name and description
        /// </summary>
        /// <param name="userId">The ID of the user (must be admin)</param>
        /// <param name="teamspaceId">The ID of the teamspace to update</param>
        /// <param name="request">Updated teamspace information</param>
        /// <returns>
        /// 200 OK if updated successfully
        /// 400 Bad Request if invalid data
        /// 403 Forbidden if user is not the admin
        /// 404 Not Found if teamspace doesn't exist
        /// 409 Conflict if new name already exists
        /// </returns>
        [HttpPut("{teamspaceId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<TeamspaceResultResponses.UpdateTeamspaceResult>> UpdateTeamspace(
            [Required] int userId,
            [Required] int teamspaceId,
            [FromBody] UpdateTeamspaceRequest request)
        {


            _logger.LogInformation("User {UserId} attempting to update teamspace {TeamspaceId}",
                userId, teamspaceId);

            var result = await _teamspaceService.UpdateTeamspaceAsync(teamspaceId, userId, request);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to update teamspace {TeamspaceId} for User ID: {UserId}. Reason: {ErrorMessage} (Code: {ErrorCode})",
                    teamspaceId, userId, result.Message, result.ErrorCode);

                return result.ErrorCode switch
                {
                    ErrorCodes.TEAMSPACE_NOT_FOUND => NotFound(ApiResponse.Error(result.Message, result.ErrorCode)),
                    ErrorCodes.TEAMSPACE_ACCESS_DENIED => StatusCode(403, ApiResponse.Error(result.Message, result.ErrorCode)),
                    ErrorCodes.TEAMSPACE_NAME_TAKEN => Conflict(ApiResponse.Error(result.Message, result.ErrorCode)),
                    _ => BadRequest(ApiResponse.Error(result.Message, result.ErrorCode))
                };
            }

            _logger.LogInformation("User {UserId} successfully updated teamspace {TeamspaceId}",
                userId, teamspaceId);

            return Ok(result);
        }

        /// <summary>
        /// Deletes a teamspace and all its contents
        /// </summary>
        /// <param name="userId">The ID of the user (must be admin)</param>
        /// <param name="teamspaceId">The ID of the teamspace to delete</param>
        /// <returns>
        /// 200 OK if deleted successfully
        /// 403 Forbidden if user is not the admin
        /// 404 Not Found if teamspace doesn't exist
        /// </returns>
        [HttpDelete("{teamspaceId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TeamspaceResultResponses.DeleteTeamspaceResult>> DeleteTeamspace(
            [Required] int userId,
            [Required] int teamspaceId)
        {

            _logger.LogInformation("User {UserId} attempting to delete teamspace {TeamspaceId}",
                userId, teamspaceId);

            var result = await _teamspaceService.DeleteTeamspaceAsync(teamspaceId, userId);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to delete teamspace {TeamspaceId} for User ID: {UserId}. Reason: {ErrorMessage} (Code: {ErrorCode})",
                    teamspaceId, userId, result.Message, result.ErrorCode);

                return result.ErrorCode switch
                {
                    ErrorCodes.TEAMSPACE_NOT_FOUND => NotFound(ApiResponse.Error(result.Message, result.ErrorCode)),
                    ErrorCodes.TEAMSPACE_ACCESS_DENIED => StatusCode(403, ApiResponse.Error(result.Message, result.ErrorCode)),
                    _ => BadRequest(ApiResponse.Error(result.Message, result.ErrorCode))
                };
            }

            _logger.LogInformation("User {UserId} successfully deleted teamspace {TeamspaceId}",
                userId, teamspaceId);

            return Ok(result);
        }

        #endregion

        #region Member Management

        /// <summary>
        /// Adds a new member to the teamspace
        /// </summary>
        /// <param name="userId">The ID of the user adding the member (must have admin permission)</param>
        /// <param name="teamspaceId">The ID of the teamspace</param>
        /// <param name="request">Member details including email and permission level</param>
        /// <returns>
        /// 200 OK if member added successfully
        /// 400 Bad Request if member limit reached or user not found
        /// 403 Forbidden if requester doesn't have admin permission
        /// 404 Not Found if teamspace doesn't exist
        /// 409 Conflict if member already exists
        /// </returns>
        [HttpPost("{teamspaceId}/members")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<TeamspaceResultResponses.AddMemberResult>> AddMember(
            [Required] int userId,
            [Required] int teamspaceId,
            [FromBody] AddTeamspaceMemberRequest request)
        {

            _logger.LogInformation("User {UserId} attempting to add member '{Email}' to teamspace {TeamspaceId}",
                userId, request.Email, teamspaceId);

            var result = await _teamspaceService.AddMemberAsync(teamspaceId, userId, request);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to add member '{Email}' to teamspace {TeamspaceId}. Reason: {ErrorMessage} (Code: {ErrorCode})",
                    request.Email, teamspaceId, result.Message, result.ErrorCode);

                return result.ErrorCode switch
                {
                    ErrorCodes.TEAMSPACE_NOT_FOUND => NotFound(ApiResponse.Error(result.Message, result.ErrorCode)),
                    ErrorCodes.INSUFFICIENT_PERMISSION => StatusCode(403, ApiResponse.Error(result.Message, result.ErrorCode)),
                    ErrorCodes.USER_NOT_FOUND => BadRequest(ApiResponse.Error(result.Message, result.ErrorCode)),
                    ErrorCodes.MEMBER_ALREADY_EXISTS => Conflict(ApiResponse.Error(result.Message, result.ErrorCode)),
                    ErrorCodes.MEMBER_LIMIT_REACHED => BadRequest(ApiResponse.Error(result.Message, result.ErrorCode)),
                    _ => BadRequest(ApiResponse.Error(result.Message, result.ErrorCode))
                };
            }

            _logger.LogInformation("User {UserId} successfully added member '{Username}' to teamspace {TeamspaceId} with permission '{Permission}'",
                userId, result.Username, teamspaceId, result.PermissionLevel);

            return Ok(result);
        }

        /// <summary>
        /// Retrieves all members of a teamspace
        /// </summary>
        /// <param name="userId">The ID of the user requesting the list (must have at least read permission)</param>
        /// <param name="teamspaceId">The ID of the teamspace</param>
        /// <returns>
        /// 200 OK with list of members
        /// 403 Forbidden if user doesn't have access to the teamspace
        /// 404 Not Found if teamspace doesn't exist
        /// </returns>
        [HttpGet("{teamspaceId}/members")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<TeamspaceMemberResponse>>> GetMembers(
            [Required] int userId,
            [Required] int teamspaceId)
        {

            _logger.LogInformation("User {UserId} fetching members for teamspace {TeamspaceId}",
                userId, teamspaceId);

            var members = await _teamspaceService.GetTeamspaceMembersAsync(teamspaceId, userId);

            if (!members.Any())
            {
                _logger.LogWarning("No members found for teamspace {TeamspaceId} or User {UserId} has no access",
                    teamspaceId, userId);
                return NotFound(ApiResponse.Error("Teamspace not found or you don't have access",
                    ErrorCodes.TEAMSPACE_NOT_FOUND));
            }

            _logger.LogInformation("Successfully fetched {Count} members for teamspace {TeamspaceId}",
                members.Count(), teamspaceId);

            return Ok(members);
        }

        /// <summary>
        /// Updates a member's permission level in the teamspace
        /// </summary>
        /// <param name="userId">The ID of the user updating the permission (must be admin)</param>
        /// <param name="teamspaceId">The ID of the teamspace</param>
        /// <param name="memberUserId">The ID of the member whose permission is being updated</param>
        /// <param name="request">New permission level (read, write, or admin)</param>
        /// <returns>
        /// 200 OK if permission updated successfully
        /// 400 Bad Request if invalid permission level
        /// 403 Forbidden if requester doesn't have admin permission or trying to change admin's permission
        /// 404 Not Found if teamspace or member doesn't exist
        /// </returns>
        [HttpPut("{teamspaceId}/members/{memberUserId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TeamspaceResultResponses.UpdateMemberPermissionResult>> UpdateMemberPermission(
            [Required] int userId,
            [Required] int teamspaceId,
            [Required] int memberUserId,
            [FromBody] UpdateMemberPermissionRequest request)
        {

            _logger.LogInformation("User {UserId} attempting to update member {MemberUserId} permission to '{Permission}' in teamspace {TeamspaceId}",
                userId, memberUserId, request.PermissionLevel, teamspaceId);

            var result = await _teamspaceService.UpdateMemberPermissionAsync(
                teamspaceId, userId, memberUserId, request.PermissionLevel);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to update member {MemberUserId} permission in teamspace {TeamspaceId}. Reason: {ErrorMessage} (Code: {ErrorCode})",
                    memberUserId, teamspaceId, result.Message, result.ErrorCode);

                return result.ErrorCode switch
                {
                    ErrorCodes.TEAMSPACE_NOT_FOUND => NotFound(ApiResponse.Error(result.Message, result.ErrorCode)),
                    ErrorCodes.INSUFFICIENT_PERMISSION => StatusCode(403, ApiResponse.Error(result.Message, result.ErrorCode)),
                    ErrorCodes.CANNOT_REMOVE_ADMIN => StatusCode(403, ApiResponse.Error(result.Message, result.ErrorCode)),
                    ErrorCodes.MEMBER_NOT_FOUND => NotFound(ApiResponse.Error(result.Message, result.ErrorCode)),
                    ErrorCodes.INVALID_PERMISSION => BadRequest(ApiResponse.Error(result.Message, result.ErrorCode)),
                    _ => BadRequest(ApiResponse.Error(result.Message, result.ErrorCode))
                };
            }

            _logger.LogInformation("User {UserId} successfully updated member {MemberUserId} permission to '{Permission}' in teamspace {TeamspaceId}",
                userId, memberUserId, result.NewPermission, teamspaceId);

            return Ok(result);
        }

        /// <summary>
        /// Removes a member from the teamspace
        /// </summary>
        /// <param name="userId">The ID of the user removing the member (must be admin)</param>
        /// <param name="teamspaceId">The ID of the teamspace</param>
        /// <param name="memberUserId">The ID of the member to remove</param>
        /// <returns>
        /// 200 OK if member removed successfully
        /// 403 Forbidden if requester doesn't have admin permission or trying to remove admin
        /// 404 Not Found if teamspace or member doesn't exist
        /// </returns>
        [HttpDelete("{teamspaceId}/members/{memberUserId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TeamspaceResultResponses.RemoveMemberResult>> RemoveMember(
            [Required] int userId,
            [Required] int teamspaceId,
            [Required] int memberUserId)
        {

            _logger.LogInformation("User {UserId} attempting to remove member {MemberUserId} from teamspace {TeamspaceId}",
                userId, memberUserId, teamspaceId);

            var result = await _teamspaceService.RemoveMemberAsync(teamspaceId, userId, memberUserId);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to remove member {MemberUserId} from teamspace {TeamspaceId}. Reason: {ErrorMessage} (Code: {ErrorCode})",
                    memberUserId, teamspaceId, result.Message, result.ErrorCode);

                return result.ErrorCode switch
                {
                    ErrorCodes.TEAMSPACE_NOT_FOUND => NotFound(ApiResponse.Error(result.Message, result.ErrorCode)),
                    ErrorCodes.INSUFFICIENT_PERMISSION => StatusCode(403, ApiResponse.Error(result.Message, result.ErrorCode)),
                    ErrorCodes.CANNOT_REMOVE_ADMIN => StatusCode(403, ApiResponse.Error(result.Message, result.ErrorCode)),
                    ErrorCodes.MEMBER_NOT_FOUND => NotFound(ApiResponse.Error(result.Message, result.ErrorCode)),
                    _ => BadRequest(ApiResponse.Error(result.Message, result.ErrorCode))
                };
            }

            _logger.LogInformation("User {UserId} successfully removed member {MemberUserId} from teamspace {TeamspaceId}",
                userId, memberUserId, teamspaceId);

            return Ok(result);
        }

        /// <summary>
        /// Allows a member to leave a teamspace (admin cannot leave)
        /// </summary>
        /// <param name="userId">The ID of the user leaving the teamspace</param>
        /// <param name="teamspaceId">The ID of the teamspace to leave</param>
        /// <returns>
        /// 200 OK if successfully left the teamspace
        /// 403 Forbidden if user is the admin (must transfer ownership or delete teamspace)
        /// 404 Not Found if teamspace doesn't exist or user is not a member
        /// </returns>
        [HttpPost("{teamspaceId}/leave")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TeamspaceResultResponses.LeaveTeamspaceResult>> LeaveTeamspace(
            [Required] int userId,
            [Required] int teamspaceId)
        {

            _logger.LogInformation("User {UserId} attempting to leave teamspace {TeamspaceId}",
                userId, teamspaceId);

            var result = await _teamspaceService.LeaveTeamspaceAsync(teamspaceId, userId);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed for User {UserId} to leave teamspace {TeamspaceId}. Reason: {ErrorMessage} (Code: {ErrorCode})",
                    userId, teamspaceId, result.Message, result.ErrorCode);

                return result.ErrorCode switch
                {
                    ErrorCodes.TEAMSPACE_NOT_FOUND => NotFound(ApiResponse.Error(result.Message, result.ErrorCode)),
                    ErrorCodes.CANNOT_LEAVE_AS_ADMIN => StatusCode(403, ApiResponse.Error(result.Message, result.ErrorCode)),
                    ErrorCodes.MEMBER_NOT_FOUND => NotFound(ApiResponse.Error(result.Message, result.ErrorCode)),
                    _ => BadRequest(ApiResponse.Error(result.Message, result.ErrorCode))
                };
            }

            _logger.LogInformation("User {UserId} successfully left teamspace {TeamspaceId}",
                userId, teamspaceId);

            return Ok(result);
        }

        #endregion
    }
}