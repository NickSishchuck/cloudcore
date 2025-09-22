using CloudCore.Contracts.Responses;
using CloudCore.Domain.Entities;

namespace CloudCore.Mappers
{
    public static class ItemMapper
    {
        public static ItemResponse ToResponseDto(this Item item)
        {
            return new ItemResponse
            {
                Id = item.Id,
                Name = item.Name,
                Type = item.Type,
                ParentId = item.ParentId,
                FileSize = item.FileSize,
                MimeType = item.MimeType,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt
            };
        }
    }
}
