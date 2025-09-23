namespace CloudCore.Services.Interfaces
{
    public interface ITrashCleanupService
    {
        Task<int> CleanupExpiredItemsAsync();
    }
}
