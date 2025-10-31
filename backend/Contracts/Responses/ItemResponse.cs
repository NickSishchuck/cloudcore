using System.Text.Json.Serialization;
using CloudCore.Domain.Entities;

namespace CloudCore.Contracts.Responses
{
    public class ItemResponse
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string Type { get; set; } = null!;

        public int? ParentId { get; set; }

        public long? FileSize { get; set; }

        public string? MimeType { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }


    }
}