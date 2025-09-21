namespace CloudCore.Models
{
    public class PaginatedResponse<T>
    {
        public IEnumerable<T> Data { get; set; }
        public PaginationMetadata Pagination { get; set; }
    }

    public class PaginationMetadata
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public bool HasNext { get; set; }
        public bool HasPrevious { get; set; }
    }
}
