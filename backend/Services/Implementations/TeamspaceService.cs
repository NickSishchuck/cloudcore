using CloudCore.Contracts.Requests;
using CloudCore.Contracts.Responses;
using CloudCore.Domain.Entities;
using CloudCore.Services.Interfaces;
using CloudCore.Data.Context;
using CloudCore.Common.Errors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static CloudCore.Contracts.Responses.TeamspaceResultResponses;

namespace CloudCore.Services.Implementations
{
    public class TeamspaceService : ITeamspaceService
    {
        private readonly IDbContextFactory<CloudCoreDbContext> _dbContextFactory;
        private readonly ILogger<TeamspaceService> _logger;
        private readonly ISubscriptionService _subscriptionService;

        public TeamspaceService(
            IDbContextFactory<CloudCoreDbContext> dbContextFactory,
            ILogger<TeamspaceService> logger,
            ISubscriptionService subscriptionService)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
            _subscriptionService = subscriptionService;
        }

        #region Teamspace Management

        public async Task<CreateTeamspaceResult> CreateTeamspaceAsync(int userId, CreateTeamspaceRequest request)
        {
            _logger.LogInformation("User {UserId} attempting to create teamspace '{Name}'", userId, request.Name);

            // Check if user can create more teamspaces
            var canCreate = await _subscriptionService.CanCreateTeamspaceAsync(userId);
            if (!canCreate)
            {
                _logger.LogWarning("User {UserId} has reached teamspace limit", userId);
                return new CreateTeamspaceResult
                {
                    IsSuccess = false,
                    ErrorCode = ErrorCodes.TEAMSPACE_LIMIT_REACHED,
                    Message = "You have reached the maximum number of teamspaces for your subscription plan"
                };
            }

            using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                // Check if name is already taken by this user
                var nameExists = await context.Teamspaces
                    .AsNoTracking()
                    .AnyAsync(t => t.Name == request.Name && t.AdminUserId == userId);

                if (nameExists)
                {
                    _logger.LogWarning("Teamspace name '{Name}' already exists for user {UserId}", request.Name, userId);
                    return new CreateTeamspaceResult
                    {
                        IsSuccess = false,
                        ErrorCode = ErrorCodes.TEAMSPACE_NAME_TAKEN,
                        Message = "A teamspace with this name already exists"
                    };
                }

                // Get subscription limits
                var limits = await _subscriptionService.GetTeamspaceLimitsAsync(userId);

                // Create teamspace
                var teamspace = new Teamspace
                {
                    Name = request.Name,
                    Description = request.Description,
                    AdminUserId = userId,
                    StorageLimitMb = limits.StorageLimitMb,
                    MemberLimit = limits.MemberLimit,
                    StorageUsedMb = 0,
                    MemberCount = 1 // Admin counts as a member
                };

                context.Teamspaces.Add(teamspace);
                await context.SaveChangesAsync();

                // Update user's teamspace count
                var user = await context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.TeamspacesOwned = (user.TeamspacesOwned ?? 0) + 1;
                    await context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                _logger.LogInformation("Teamspace '{Name}' (ID: {TeamspaceId}) created successfully by user {UserId}",
                    teamspace.Name, teamspace.Id, userId);

                return new CreateTeamspaceResult
                {
                    IsSuccess = true,
                    ErrorCode = ErrorCodes.CREATED_SUCCESSFULLY,
                    Message = "Teamspace created successfully",
                    TeamspaceId = teamspace.Id,
                    TeamspaceName = teamspace.Name,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to create teamspace for user {UserId}", userId);
                return new CreateTeamspaceResult
                {
                    IsSuccess = false,
                    ErrorCode = ErrorCodes.UNEXPECTED_ERROR,
                    Message = "An error occurred while creating the teamspace"
                };
            }
        }

        public async Task<IEnumerable<TeamspaceResponse>> GetUserTeamspacesAsync(int userId)
        {
            _logger.LogInformation("Fetching all teamspaces for UserId: {UserId}", userId);

            using var context = _dbContextFactory.CreateDbContext();

            // Query teamspaces where user is admin
            var ownedTeamspaces = await context.Teamspaces
                .AsNoTracking()
                .Include(t => t.AdminUser)
                .Where(t => t.AdminUserId == userId)
                .Select(t => new TeamspaceResponse
                {
                    Id = t.Id,
                    Name = t.Name,
                    Description = t.Description,
                    AdminUserId = t.AdminUserId,
                    AdminUsername = t.AdminUser.Username,
                    StorageLimitMb = t.StorageLimitMb,
                    StorageUsedMb = t.StorageUsedMb ?? 0,
                    MemberLimit = t.MemberLimit,
                    MemberCount = t.MemberCount ?? 0,
                    UserPermission = "admin",
                    IsAdmin = true
                })
                .ToListAsync();

            // Query teamspaces where user is a member
            var memberTeamspaces = await context.TeamspaceMembers
                .AsNoTracking()
                .Include(tm => tm.Teamspace)
                .ThenInclude(t => t.AdminUser)
                .Where(tm => tm.UserId == userId)
                .Select(tm => new TeamspaceResponse
                {
                    Id = tm.Teamspace.Id,
                    Name = tm.Teamspace.Name,
                    Description = tm.Teamspace.Description,
                    AdminUserId = tm.Teamspace.AdminUserId,
                    AdminUsername = tm.Teamspace.AdminUser.Username,
                    StorageLimitMb = tm.Teamspace.StorageLimitMb,
                    StorageUsedMb = tm.Teamspace.StorageUsedMb ?? 0,
                    MemberLimit = tm.Teamspace.MemberLimit,
                    MemberCount = tm.Teamspace.MemberCount ?? 0,
                    UserPermission = tm.PermissionLevel ?? "read",
                    IsAdmin = false
                })
                .ToListAsync();

            var allTeamspaces = ownedTeamspaces.Concat(memberTeamspaces).ToList();

            _logger.LogInformation(
                "Found {Count} teamspaces for UserId: {UserId} ({Owned} owned, {Member} member)",
                allTeamspaces.Count, userId, ownedTeamspaces.Count, memberTeamspaces.Count);

            return allTeamspaces;
        }

        public async Task<TeamspaceResponse?> GetTeamspaceByIdAsync(int teamspaceId, int userId)
        {
            _logger.LogInformation("Fetching teamspace {TeamspaceId} for user {UserId}", teamspaceId, userId);

            using var context = _dbContextFactory.CreateDbContext();

            // Check if user is admin
            var teamspace = await context.Teamspaces
                .AsNoTracking()
                .Include(t => t.AdminUser)
                .Where(t => t.Id == teamspaceId && t.AdminUserId == userId)
                .Select(t => new TeamspaceResponse
                {
                    Id = t.Id,
                    Name = t.Name,
                    Description = t.Description,
                    AdminUserId = t.AdminUserId,
                    AdminUsername = t.AdminUser.Username,
                    StorageLimitMb = t.StorageLimitMb,
                    StorageUsedMb = t.StorageUsedMb ?? 0,
                    MemberLimit = t.MemberLimit,
                    MemberCount = t.MemberCount ?? 0,
                    UserPermission = "admin",
                    IsAdmin = true
                })
                .FirstOrDefaultAsync();

            if (teamspace != null)
                return teamspace;

            // Check if user is a member
            var memberTeamspace = await context.TeamspaceMembers
                .AsNoTracking()
                .Include(tm => tm.Teamspace)
                .ThenInclude(t => t.AdminUser)
                .Where(tm => tm.TeamspaceId == teamspaceId && tm.UserId == userId)
                .Select(tm => new TeamspaceResponse
                {
                    Id = tm.Teamspace.Id,
                    Name = tm.Teamspace.Name,
                    Description = tm.Teamspace.Description,
                    AdminUserId = tm.Teamspace.AdminUserId,
                    AdminUsername = tm.Teamspace.AdminUser.Username,
                    StorageLimitMb = tm.Teamspace.StorageLimitMb,
                    StorageUsedMb = tm.Teamspace.StorageUsedMb ?? 0,
                    MemberLimit = tm.Teamspace.MemberLimit,
                    MemberCount = tm.Teamspace.MemberCount ?? 0,
                    UserPermission = tm.PermissionLevel ?? "read",
                    IsAdmin = false
                })
                .FirstOrDefaultAsync();

            if (memberTeamspace == null)
            {
                _logger.LogWarning("Teamspace {TeamspaceId} not found or user {UserId} has no access",
                    teamspaceId, userId);
            }

            return memberTeamspace;
        }

        public async Task<UpdateTeamspaceResult> UpdateTeamspaceAsync(int teamspaceId, int userId, UpdateTeamspaceRequest request)
        {
            _logger.LogInformation("User {UserId} attempting to update teamspace {TeamspaceId}", userId, teamspaceId);

            using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                // Get teamspace and verify admin access
                var teamspace = await context.Teamspaces
                    .FirstOrDefaultAsync(t => t.Id == teamspaceId);

                if (teamspace == null)
                {
                    _logger.LogWarning("Teamspace {TeamspaceId} not found", teamspaceId);
                    return new UpdateTeamspaceResult
                    {
                        IsSuccess = false,
                        ErrorCode = ErrorCodes.TEAMSPACE_NOT_FOUND,
                        Message = "Teamspace not found"
                    };
                }

                if (teamspace.AdminUserId != userId)
                {
                    _logger.LogWarning("User {UserId} does not have admin access to teamspace {TeamspaceId}", userId, teamspaceId);
                    return new UpdateTeamspaceResult
                    {
                        IsSuccess = false,
                        ErrorCode = ErrorCodes.TEAMSPACE_ACCESS_DENIED,
                        Message = "Only the teamspace admin can update these settings"
                    };
                }

                // Check if new name conflicts with another teamspace
                if (teamspace.Name != request.Name)
                {
                    var nameExists = await context.Teamspaces
                        .AsNoTracking()
                        .AnyAsync(t => t.Name == request.Name && t.AdminUserId == userId && t.Id != teamspaceId);

                    if (nameExists)
                    {
                        _logger.LogWarning("Teamspace name '{Name}' already exists for user {UserId}", request.Name, userId);
                        return new UpdateTeamspaceResult
                        {
                            IsSuccess = false,
                            ErrorCode = ErrorCodes.TEAMSPACE_NAME_TAKEN,
                            Message = "A teamspace with this name already exists"
                        };
                    }
                }

                // Update teamspace
                teamspace.Name = request.Name;
                teamspace.Description = request.Description;

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Teamspace {TeamspaceId} updated successfully", teamspaceId);

                return new UpdateTeamspaceResult
                {
                    IsSuccess = true,
                    Message = "Teamspace updated successfully",
                    TeamspaceId = teamspaceId,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to update teamspace {TeamspaceId}", teamspaceId);
                return new UpdateTeamspaceResult
                {
                    IsSuccess = false,
                    ErrorCode = ErrorCodes.UNEXPECTED_ERROR,
                    Message = "An error occurred while updating the teamspace"
                };
            }
        }

        public async Task<DeleteTeamspaceResult> DeleteTeamspaceAsync(int teamspaceId, int userId)
        {
            _logger.LogInformation("User {UserId} attempting to delete teamspace {TeamspaceId}", userId, teamspaceId);

            using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                // Get teamspace and verify admin access
                var teamspace = await context.Teamspaces
                    .Include(t => t.Items)
                    .Include(t => t.TeamspaceMembers)
                    .FirstOrDefaultAsync(t => t.Id == teamspaceId);

                if (teamspace == null)
                {
                    _logger.LogWarning("Teamspace {TeamspaceId} not found", teamspaceId);
                    return new DeleteTeamspaceResult
                    {
                        IsSuccess = false,
                        ErrorCode = ErrorCodes.TEAMSPACE_NOT_FOUND,
                        Message = "Teamspace not found"
                    };
                }

                if (teamspace.AdminUserId != userId)
                {
                    _logger.LogWarning("User {UserId} does not have admin access to teamspace {TeamspaceId}", userId, teamspaceId);
                    return new DeleteTeamspaceResult
                    {
                        IsSuccess = false,
                        ErrorCode = ErrorCodes.TEAMSPACE_ACCESS_DENIED,
                        Message = "Only the teamspace admin can delete it"
                    };
                }

                // Note: Items and Members will be cascade deleted by the database
                context.Teamspaces.Remove(teamspace);

                // Update user's teamspace count
                var user = await context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.TeamspacesOwned = Math.Max(0, (user.TeamspacesOwned ?? 0) - 1);
                }

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Teamspace {TeamspaceId} deleted successfully", teamspaceId);

                return new DeleteTeamspaceResult
                {
                    IsSuccess = true,
                    ErrorCode = ErrorCodes.DELETED_SUCCESSFULLY,
                    Message = "Teamspace deleted successfully",
                    TeamspaceId = teamspaceId,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to delete teamspace {TeamspaceId}", teamspaceId);
                return new DeleteTeamspaceResult
                {
                    IsSuccess = false,
                    ErrorCode = ErrorCodes.UNEXPECTED_ERROR,
                    Message = "An error occurred while deleting the teamspace"
                };
            }
        }

        #endregion

        #region Member Management

        public async Task<AddMemberResult> AddMemberAsync(int teamspaceId, int userId, AddTeamspaceMemberRequest request)
        {
            _logger.LogInformation("User {UserId} attempting to add member '{Email}' to teamspace {TeamspaceId}",
                userId, request.Email, teamspaceId);

            using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                // Verify user has admin permission
                var hasPermission = await HasPermissionAsync(userId, teamspaceId, "admin");
                if (!hasPermission)
                {
                    _logger.LogWarning("User {UserId} does not have admin permission for teamspace {TeamspaceId}",
                        userId, teamspaceId);
                    return new AddMemberResult
                    {
                        IsSuccess = false,
                        ErrorCode = ErrorCodes.INSUFFICIENT_PERMISSION,
                        Message = "Only admins can add members"
                    };
                }

                // Get teamspace
                var teamspace = await context.Teamspaces
                    .FirstOrDefaultAsync(t => t.Id == teamspaceId);

                if (teamspace == null)
                {
                    _logger.LogWarning("Teamspace {TeamspaceId} not found", teamspaceId);
                    return new AddMemberResult
                    {
                        IsSuccess = false,
                        ErrorCode = ErrorCodes.TEAMSPACE_NOT_FOUND,
                        Message = "Teamspace not found"
                    };
                }

                // Check member limit
                var currentMemberCount = teamspace.MemberCount ?? 0;
                if (currentMemberCount >= teamspace.MemberLimit)
                {
                    _logger.LogWarning("Teamspace {TeamspaceId} has reached member limit", teamspaceId);
                    return new AddMemberResult
                    {
                        IsSuccess = false,
                        ErrorCode = ErrorCodes.MEMBER_LIMIT_REACHED,
                        Message = "Teamspace has reached its member limit"
                    };
                }

                // Find user by email
                var newMemberUser = await context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (newMemberUser == null)
                {
                    _logger.LogWarning("User with email '{Email}' not found", request.Email);
                    return new AddMemberResult
                    {
                        IsSuccess = false,
                        ErrorCode = ErrorCodes.USER_NOT_FOUND,
                        Message = "User with this email not found"
                    };
                }

                // Check if user is already admin
                if (teamspace.AdminUserId == newMemberUser.Id)
                {
                    _logger.LogWarning("User {UserId} is already the admin of teamspace {TeamspaceId}",
                        newMemberUser.Id, teamspaceId);
                    return new AddMemberResult
                    {
                        IsSuccess = false,
                        ErrorCode = ErrorCodes.MEMBER_ALREADY_EXISTS,
                        Message = "This user is already the admin of this teamspace"
                    };
                }

                // Check if user is already a member
                var existingMember = await context.TeamspaceMembers
                    .AsNoTracking()
                    .AnyAsync(tm => tm.TeamspaceId == teamspaceId && tm.UserId == newMemberUser.Id);

                if (existingMember)
                {
                    _logger.LogWarning("User {UserId} is already a member of teamspace {TeamspaceId}",
                        newMemberUser.Id, teamspaceId);
                    return new AddMemberResult
                    {
                        IsSuccess = false,
                        ErrorCode = ErrorCodes.MEMBER_ALREADY_EXISTS,
                        Message = "This user is already a member"
                    };
                }

                // Add member
                var member = new TeamspaceMember
                {
                    TeamspaceId = teamspaceId,
                    UserId = newMemberUser.Id,
                    PermissionLevel = request.PermissionLevel,
                    InvitedBy = userId
                };

                context.TeamspaceMembers.Add(member);

                // Update member count
                teamspace.MemberCount = currentMemberCount + 1;

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("User {UserId} added to teamspace {TeamspaceId} with permission '{Permission}'",
                    newMemberUser.Id, teamspaceId, request.PermissionLevel);

                return new AddMemberResult
                {
                    IsSuccess = true,
                    Message = "Member added successfully",
                    MemberId = member.Id,
                    Username = newMemberUser.Username,
                    PermissionLevel = request.PermissionLevel,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to add member to teamspace {TeamspaceId}", teamspaceId);
                return new AddMemberResult
                {
                    IsSuccess = false,
                    ErrorCode = ErrorCodes.UNEXPECTED_ERROR,
                    Message = "An error occurred while adding the member"
                };
            }
        }

        public async Task<RemoveMemberResult> RemoveMemberAsync(int teamspaceId, int userId, int memberUserId)
        {
            _logger.LogInformation("User {UserId} attempting to remove member {MemberUserId} from teamspace {TeamspaceId}",
                userId, memberUserId, teamspaceId);

            using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                // Verify user has admin permission
                var hasPermission = await HasPermissionAsync(userId, teamspaceId, "admin");
                if (!hasPermission)
                {
                    _logger.LogWarning("User {UserId} does not have admin permission for teamspace {TeamspaceId}",
                        userId, teamspaceId);
                    return new RemoveMemberResult
                    {
                        IsSuccess = false,
                        ErrorCode = ErrorCodes.INSUFFICIENT_PERMISSION,
                        Message = "Only admins can remove members"
                    };
                }

                // Get teamspace
                var teamspace = await context.Teamspaces
                    .FirstOrDefaultAsync(t => t.Id == teamspaceId);

                if (teamspace == null)
                {
                    return new RemoveMemberResult
                    {
                        IsSuccess = false,
                        ErrorCode = ErrorCodes.TEAMSPACE_NOT_FOUND,
                        Message = "Teamspace not found"
                    };
                }

                // Can't remove the admin
                if (teamspace.AdminUserId == memberUserId)
                {
                    _logger.LogWarning("Attempted to remove admin {MemberUserId} from teamspace {TeamspaceId}",
                        memberUserId, teamspaceId);
                    return new RemoveMemberResult
                    {
                        IsSuccess = false,
                        ErrorCode = ErrorCodes.CANNOT_REMOVE_ADMIN,
                        Message = "Cannot remove the teamspace admin"
                    };
                }

                // Find and remove member
                var member = await context.TeamspaceMembers
                    .FirstOrDefaultAsync(tm => tm.TeamspaceId == teamspaceId && tm.UserId == memberUserId);

                if (member == null)
                {
                    _logger.LogWarning("Member {MemberUserId} not found in teamspace {TeamspaceId}",
                        memberUserId, teamspaceId);
                    return new RemoveMemberResult
                    {
                        IsSuccess = false,
                        ErrorCode = ErrorCodes.MEMBER_NOT_FOUND,
                        Message = "Member not found in this teamspace"
                    };
                }

                context.TeamspaceMembers.Remove(member);

                // Update member count
                teamspace.MemberCount = Math.Max(1, (teamspace.MemberCount ?? 1) - 1);

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("User {MemberUserId} removed from teamspace {TeamspaceId}",
                    memberUserId, teamspaceId);

                return new RemoveMemberResult
                {
                    IsSuccess = true,
                    Message = "Member removed successfully",
                    MemberId = member.Id,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to remove member from teamspace {TeamspaceId}", teamspaceId);
                return new RemoveMemberResult
                {
                    IsSuccess = false,
                    ErrorCode = ErrorCodes.UNEXPECTED_ERROR,
                    Message = "An error occurred while removing the member"
                };
            }
        }

        public async Task<UpdateMemberPermissionResult> UpdateMemberPermissionAsync(
            int teamspaceId, int userId, int memberUserId, string newPermission)
        {
            _logger.LogInformation(
                "User {UserId} attempting to update member {MemberUserId} permission to '{Permission}' in teamspace {TeamspaceId}",
                userId, memberUserId, newPermission, teamspaceId);

            using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                // Verify user has admin permission
                var hasPermission = await HasPermissionAsync(userId, teamspaceId, "admin");
                if (!hasPermission)
                {
                    _logger.LogWarning("User {UserId} does not have admin permission for teamspace {TeamspaceId}",
                        userId, teamspaceId);
                    return new UpdateMemberPermissionResult
                    {
                        IsSuccess = false,
                        ErrorCode = ErrorCodes.INSUFFICIENT_PERMISSION,
                        Message = "Only admins can update member permissions"
                    };
                }

                // Validate permission level
                if (newPermission != "read" && newPermission != "write" && newPermission != "admin")
                {
                    _logger.LogWarning("Invalid permission level '{Permission}'", newPermission);
                    return new UpdateMemberPermissionResult
                    {
                        IsSuccess = false,
                        ErrorCode = ErrorCodes.INVALID_PERMISSION,
                        Message = "Permission must be 'read', 'write', or 'admin'"
                    };
                }

                // Get teamspace
                var teamspace = await context.Teamspaces
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == teamspaceId);

                if (teamspace == null)
                {
                    return new UpdateMemberPermissionResult
                    {
                        IsSuccess = false,
                        ErrorCode = ErrorCodes.TEAMSPACE_NOT_FOUND,
                        Message = "Teamspace not found"
                    };
                }

                // Can't change admin's permission
                if (teamspace.AdminUserId == memberUserId)
                {
                    _logger.LogWarning("Attempted to change admin {MemberUserId} permission in teamspace {TeamspaceId}",
                        memberUserId, teamspaceId);
                    return new UpdateMemberPermissionResult
                    {
                        IsSuccess = false,
                        ErrorCode = ErrorCodes.CANNOT_REMOVE_ADMIN,
                        Message = "Cannot change the admin's permission level"
                    };
                }

                // Find and update member
                var member = await context.TeamspaceMembers
                    .FirstOrDefaultAsync(tm => tm.TeamspaceId == teamspaceId && tm.UserId == memberUserId);

                if (member == null)
                {
                    _logger.LogWarning("Member {MemberUserId} not found in teamspace {TeamspaceId}",
                        memberUserId, teamspaceId);
                    return new UpdateMemberPermissionResult
                    {
                        IsSuccess = false,
                        ErrorCode = ErrorCodes.MEMBER_NOT_FOUND,
                        Message = "Member not found in this teamspace"
                    };
                }

                member.PermissionLevel = newPermission;

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Member {MemberUserId} permission updated to '{Permission}' in teamspace {TeamspaceId}",
                    memberUserId, newPermission, teamspaceId);

                return new UpdateMemberPermissionResult
                {
                    IsSuccess = true,
                    Message = "Member permission updated successfully",
                    MemberId = member.Id,
                    NewPermission = newPermission,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to update member permission in teamspace {TeamspaceId}", teamspaceId);
                return new UpdateMemberPermissionResult
                {
                    IsSuccess = false,
                    ErrorCode = ErrorCodes.UNEXPECTED_ERROR,
                    Message = "An error occurred while updating the permission"
                };
            }
        }

        public async Task<IEnumerable<TeamspaceMemberResponse>> GetTeamspaceMembersAsync(int teamspaceId, int userId)
        {
            _logger.LogInformation("Fetching members for teamspace {TeamspaceId}", teamspaceId);

            // Verify user has access to this teamspace
            var hasAccess = await HasPermissionAsync(userId, teamspaceId, "read");
            if (!hasAccess)
            {
                _logger.LogWarning("User {UserId} does not have access to teamspace {TeamspaceId}", userId, teamspaceId);
                return Enumerable.Empty<TeamspaceMemberResponse>();
            }

            using var context = _dbContextFactory.CreateDbContext();

            // Get the admin as a "member"
            var admin = await context.Teamspaces
                .AsNoTracking()
                .Include(t => t.AdminUser)
                .Where(t => t.Id == teamspaceId)
                .Select(t => new TeamspaceMemberResponse
                {
                    Id = 0, // Special ID for admin
                    UserId = t.AdminUserId,
                    Username = t.AdminUser.Username,
                    Email = t.AdminUser.Email,
                    PermissionLevel = "admin",
                    InvitedBy = null,
                    InvitedByUsername = null,
                    IsAdmin = true
                })
                .FirstOrDefaultAsync();

            if (admin == null)
            {
                _logger.LogWarning("Teamspace {TeamspaceId} not found", teamspaceId);
                return Enumerable.Empty<TeamspaceMemberResponse>();
            }

            // Get regular members
            var members = await context.TeamspaceMembers
                .AsNoTracking()
                .Include(tm => tm.User)
                .Include(tm => tm.InvitedByNavigation)
                .Where(tm => tm.TeamspaceId == teamspaceId)
                .Select(tm => new TeamspaceMemberResponse
                {
                    Id = tm.Id,
                    UserId = tm.UserId,
                    Username = tm.User.Username,
                    Email = tm.User.Email,
                    PermissionLevel = tm.PermissionLevel ?? "read",
                    InvitedBy = tm.InvitedBy,
                    InvitedByUsername = tm.InvitedByNavigation != null ? tm.InvitedByNavigation.Username : null,
                    IsAdmin = false
                })
                .ToListAsync();

            // Combine admin and members
            var allMembers = new List<TeamspaceMemberResponse> { admin };
            allMembers.AddRange(members);

            _logger.LogInformation("Found {Count} members for teamspace {TeamspaceId}", allMembers.Count, teamspaceId);

            return allMembers;
        }

        public async Task<LeaveTeamspaceResult> LeaveTeamspaceAsync(int teamspaceId, int userId)
        {
            _logger.LogInformation("User {UserId} attempting to leave teamspace {TeamspaceId}", userId, teamspaceId);

            using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                // Get teamspace
                var teamspace = await context.Teamspaces
                    .FirstOrDefaultAsync(t => t.Id == teamspaceId);

                if (teamspace == null)
                {
                    return new LeaveTeamspaceResult
                    {
                        IsSuccess = false,
                        ErrorCode = ErrorCodes.TEAMSPACE_NOT_FOUND,
                        Message = "Teamspace not found"
                    };
                }

                // Admin cannot leave (must transfer ownership or delete teamspace)
                if (teamspace.AdminUserId == userId)
                {
                    _logger.LogWarning("Admin {UserId} attempted to leave teamspace {TeamspaceId}", userId, teamspaceId);
                    return new LeaveTeamspaceResult
                    {
                        IsSuccess = false,
                        ErrorCode = ErrorCodes.CANNOT_LEAVE_AS_ADMIN,
                        Message = "Admin cannot leave the teamspace. Transfer ownership or delete the teamspace instead."
                    };
                }

                // Find and remove member
                var member = await context.TeamspaceMembers
                    .FirstOrDefaultAsync(tm => tm.TeamspaceId == teamspaceId && tm.UserId == userId);

                if (member == null)
                {
                    _logger.LogWarning("User {UserId} is not a member of teamspace {TeamspaceId}", userId, teamspaceId);
                    return new LeaveTeamspaceResult
                    {
                        IsSuccess = false,
                        ErrorCode = ErrorCodes.MEMBER_NOT_FOUND,
                        Message = "You are not a member of this teamspace"
                    };
                }

                context.TeamspaceMembers.Remove(member);

                // Update member count
                teamspace.MemberCount = Math.Max(1, (teamspace.MemberCount ?? 1) - 1);

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("User {UserId} left teamspace {TeamspaceId}", userId, teamspaceId);

                return new LeaveTeamspaceResult
                {
                    IsSuccess = true,
                    Message = "Successfully left the teamspace",
                    TeamspaceId = teamspaceId,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to leave teamspace {TeamspaceId}", teamspaceId);
                return new LeaveTeamspaceResult
                {
                    IsSuccess = false,
                    ErrorCode = ErrorCodes.UNEXPECTED_ERROR,
                    Message = "An error occurred while leaving the teamspace"
                };
            }
        }

        #endregion

        #region Validation & Permissions

        public async Task<bool> HasPermissionAsync(int userId, int teamspaceId, string requiredPermission)
        {
            var userPermission = await GetUserPermissionAsync(userId, teamspaceId);

            if (string.IsNullOrEmpty(userPermission))
                return false;

            // Permission hierarchy: admin > write > read
            return requiredPermission.ToLower() switch
            {
                "read" => true, // Any permission level has read access
                "write" => userPermission == "write" || userPermission == "admin",
                "admin" => userPermission == "admin",
                _ => false
            };
        }

        public async Task<string?> GetUserPermissionAsync(int userId, int teamspaceId)
        {
            using var context = _dbContextFactory.CreateDbContext();

            // Check if user is admin
            var isAdmin = await context.Teamspaces
                .AsNoTracking()
                .AnyAsync(t => t.Id == teamspaceId && t.AdminUserId == userId);

            if (isAdmin)
                return "admin";

            // Check member permission
            var member = await context.TeamspaceMembers
                .AsNoTracking()
                .FirstOrDefaultAsync(tm => tm.TeamspaceId == teamspaceId && tm.UserId == userId);

            return member?.PermissionLevel;
        }

        #endregion
    }
}
