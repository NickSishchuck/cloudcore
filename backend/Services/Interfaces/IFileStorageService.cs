namespace CloudCore.Services.Interfaces
{
    public interface IFileStorageService
    {
        string GetUserStoragePath(int userId);
        string GetFileFullPath(int userId, string relativePath);
    }
}
