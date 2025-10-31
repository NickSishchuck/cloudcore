using System.ComponentModel.DataAnnotations;

namespace CloudCore.Common.QueryParameters
{
    public class QueryParameters
    {
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 30;

        [StringLength(50)]
        public string SortBy { get; set; } = "name";

        [RegularExpression("^(asc|desc)$", ErrorMessage = "SortDir must be 'asc' or 'desc'.")]
        public string SortDir { get; set; } = "asc";

        public string? SearchQuery { get; set; } = null;
    }
}