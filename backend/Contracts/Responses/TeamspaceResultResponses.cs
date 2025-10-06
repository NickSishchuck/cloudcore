namespace CloudCore.Contracts.Responses
{
    public class TeamspaceResultResponses
    {
        public abstract class ServiceResult
        {
            public bool IsSuccess { get; set; }
            public string Message { get; set; } = string.Empty;
            public string ErrorCode { get; set; } = string.Empty;
        }
        public class CreateTeamspaceResult : ServiceResult
        {
            public int TeamspaceId { get; set; }
            public string TeamspaceName { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
        }

        public class UpdateTeamspaceResult : ServiceResult
        {
            public int TeamspaceId { get; set; }
            public DateTime Timestamp { get; set; }
        }
        public class RemoveMemberResult : ServiceResult
        {
            public int MemberId { get; set; }
            public DateTime Timestamp { get; set; }
        }
        public class DeleteTeamspaceResult : ServiceResult
        {
            public int TeamspaceId { get; set; }
            public DateTime Timestamp { get; set; }
        }

        public class AddMemberResult : ServiceResult
        {
            public int MemberId { get; set; }
            public string Username { get; set; } = string.Empty;
            public string PermissionLevel { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
        }
        public class UpdateMemberResult : ServiceResult
        {
            public int MemberId { get; set; }
            public string NewPermission { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
        }
        public class UpdateMemberPermissionResult : ServiceResult
        {
            public int MemberId { get; set; }
            public string NewPermission { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
        }
        public class LeaveTeamspaceResult : ServiceResult
        {
            public int TeamspaceId { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }
}
