namespace CloudCore.Contracts.Responses
{
    public class ItemResultResponses
    {
        public abstract class ServiceResult
        {
            public bool IsSuccess { get; set; }
            public string Message { get; set; } = string.Empty;
            public string ErrorCode { get; set; } = string.Empty;
        }

        public class FileDownloadResult
        {
            public Stream Stream { get; set; }
            public string FileName { get; set; }
            public string MimeType { get; set; }
        }

        public class RenameResult : ServiceResult
        {
            public int ItemId { get; set; }
            public string NewName { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
        }

        public class FolderSizeResult
        {
            public int FolderId { get; set; }
            public long TotalSize { get; set; }
            public int FileCount { get; set; }
            public string FormattedSize { get; set; } = string.Empty;
        }

        public class DeleteResult : ServiceResult
        {
            public int ItemId { get; set; }
            public int TargetId { get; set; }
        }

        public class RestoreResult : ServiceResult
        { 
        }

        public class MoveResult : ServiceResult
        {
            public int ItemId { get; set; }
            public int UpdatedItemsCount { get; set; }
        }

        public class UploadResult : ServiceResult
        {
            public int ItemId { get; set; }
            public string FileName { get; set; } = string.Empty;
        }

        public class CreateFolderResult : ServiceResult
        {
            public int FolderId { get; set; }
            public string FolderName { get; set; } = string.Empty;
        }
    }
}
