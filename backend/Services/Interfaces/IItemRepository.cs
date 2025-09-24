using System.ComponentModel.DataAnnotations;
using CloudCore.Contracts.Requests;
using Microsoft.AspNetCore.Mvc;
using static CloudCore.Contracts.Responses.ItemResultResponses;

namespace CloudCore.Services.Interfaces
{
    public interface IItemRepository
    {
        
        Task<(Stream archiveStream, string fileName)> DownloadFolderAsync(int userId, int folderId);

        Task<FileDownloadResult> DownloadFileAsync(int userId, int fileId);

        Task<(Stream archiveStream, string fileName)> DownloadMultipleItemsAsZipAsync( int userId,List<int> itemsId);

        Task<RenameResult> RenameItemAsync(int userId, int itemId, string newName);

        Task<FolderSizeResult> GetFolderSizeAsync(int userId, int folderId);

        Task<ActionResult<Dictionary<int, FolderSizeResult>>> GetMultipleFolderSizesAsync(int userId, List<int> folderIds);

        Task<DeleteResult> DeleteItemAsync(int userId, int itemId);

        Task<UploadResult> UploadFileAsync(int userId, IFormFile file, int? parentId = null);

        Task<CreateFolderResult> CreateFolderAsync(int userId, FolderCreateRequest request);

        Task<RestoreResult> RestoreItemAsync(int userId, int itemId);
    }

}
