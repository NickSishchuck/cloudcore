using System.ComponentModel.DataAnnotations;

namespace CloudCore.Contracts.Requests
{
    public class FolderCreateRequest
    {
        [Required]
        [StringLength(250)]
        public string Name { get; set; }
        public int? ParentId { get; set; }
    }
}
