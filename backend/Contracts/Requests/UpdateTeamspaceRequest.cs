using System.ComponentModel.DataAnnotations;

namespace CloudCore.Contracts.Requests
{
    public class UpdateTeamspaceRequest
    {
        [Required(ErrorMessage = "Teamspace name is required")]
        [StringLength(255, MinimumLength = 1, ErrorMessage = "Teamspace name must be between 1 and 255 characters")]
        public string Name { get; set; } = null!;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }
    }
}
