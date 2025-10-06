using CloudCore.Contracts.Requests;
using CloudCore.Contracts.Responses;
using CloudCore.Domain.Entities;

namespace CloudCore.Services.Interfaces
{
    public interface ITeamspaceService
    {
        Task<TeamspaceResultResponses.CreateTeamspaceResult> CreateTeamspaceAsync(int userId, CreateTeamspaceRequest request);

        Task<IEnumerable<TeamspaceResponse>> GetUserTeamspacesAsync(int userId);

        Task<TeamspaceResponse?> GetTeamspaceByIdAsync(int teamspaceId, int userId);

        Task<TeamspaceResultResponses.UpdateTeamspaceResult> UpdateTeamspaceAsync(int teamspaceId, int userId, UpdateTeamspaceRequest request);

        Task<TeamspaceResultResponses.AddMemberResult> AddMemberAsync(int userId, int teamspaceId, AddTeamspaceMemberRequest request);

        Task<TeamspaceResultResponses.RemoveMemberResult> RemoveMemberAsync(int userId, int teamspaceId, int memberUserId);

        Task<TeamspaceResultResponses.UpdateMemberPermissionResult> UpdateMemberPermissionAsync(int userId, int teamspaceId, int memberUserId, string newPermission);

        Task<IEnumerable<TeamspaceMemberResponse>> GetTeamspaceMembersAsync(int teamspaceId, int userId);

        Task<TeamspaceResultResponses.LeaveTeamspaceResult> LeaveTeamspaceAsync(int userId, int teamspaceId);


        Task<bool> HasPermissionAsync(int userId, int teamspaceId, string requiredPermission);
        Task<string?> GetUserPermissionAsync(int userId, int teamspaceid);
    }
}
