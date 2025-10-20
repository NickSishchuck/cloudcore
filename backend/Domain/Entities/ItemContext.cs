//TODO: Move this thing to it's own service
namespace CloudCore.Domain.Entities
{

    public class ItemContext
    {
        public int? TeamspaceId { get; init; }
        public string? Permission { get; init; }

        public bool IsTeamspaceOperation => TeamspaceId.HasValue;
        public bool IsPersonalOperation => !TeamspaceId.HasValue;

        public static ItemContext Personal() => new ItemContext();

        public static ItemContext Teamspace(int teamspaceId, string permission)
        {
            if (teamspaceId <= 0)
                throw new ArgumentException("Teamspace ID must be greater then 0", nameof(teamspaceId)); //FIXME: Use the error handler

            if (string.IsNullOrWhiteSpace(permission))
                throw new ArgumentException("Permission cannot be null or empty", nameof(permission)); //FIXME: Use the error handler

            return new ItemContext
            {
                TeamspaceId = teamspaceId,
                Permission = permission,
            };
        }

        public bool HasWritePermission()
        {
            if (IsPersonalOperation)
                return true;
            return Permission switch
            {
                "write" => true,
                "admin" => true,
                _ => false
            };
        }

        public bool HasAdminPermission()
        {
            if (IsPersonalOperation)
                return true;

            return Permission == "admin";
        }

        public bool HasReadPermission()
        {
            if (IsPersonalOperation)
                return true;

            return !string.IsNullOrEmpty(Permission);
        }

        public override string ToString()
        {
            if (IsPersonalOperation)
                return "Personal Storage";

            return $"Teamspace {TeamspaceId} (Permission: {Permission})";
        }
    }
}