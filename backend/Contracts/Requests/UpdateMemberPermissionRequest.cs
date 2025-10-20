using System.ComponentModel.DataAnnotations;

namespace CloudCore.Contracts.Requests
{
    public class UpdateMemberPermissionRequest
    {
        [Required(ErrorMessage = "Permission level is required")] //FIXME: Use the error handler
        [RegularExpression("^(read|write|admin)$", ErrorMessage = "Permission must be 'read', 'write', or 'admin'")]
        public string PermissionLevel { get; set; } = null!;
    }
}