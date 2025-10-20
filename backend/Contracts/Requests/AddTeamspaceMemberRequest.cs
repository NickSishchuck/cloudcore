using System.ComponentModel.DataAnnotations;

namespace CloudCore.Contracts.Requests
{
    public class AddTeamspaceMemberRequest
    {
        [Required(ErrorMessage = "User email is required")] //FIXME: Use the error handler
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Permission level is required")]
        [RegularExpression("^(read|write|admin)$", ErrorMessage = "Permission must be 'read', 'write', or 'admin'")]
        public string PermissionLevel { get; set; } = "read";
    }
}