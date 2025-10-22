using System.ComponentModel.DataAnnotations;

namespace CloudCore.Contracts.Requests
{
    public class CreateTeamspaceRequest
    {
        [Required(ErrorMessage = "Teamspace name is required")] //FIXME: Use the error handler
        [StringLength(255, MinimumLength = 1, ErrorMessage = "Teamspace name must be between 1 and 255 characters")]
        public string Name { get; set; } = null!;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        // Note: Storage and member limits are set by the user's subscription plan
        // We don't allow users to set these directly
    }
}