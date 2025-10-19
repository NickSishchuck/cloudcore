namespace CloudCore.Contracts.Responses
{
    public class ApiResponse
    {
        public bool Success { get; set; }
        public required string Message { get; set; }
        public string? ErrorCode { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public static ApiResponse Ok(string message = "Operation completed successfully")
        {
            return new ApiResponse
            {
                Success = true,
                Message = message
            };
        }

        public static ApiResponse Error(string message, string? errorCode = null)
        {
            return new ApiResponse
            {
                Success = false,
                Message = message,
                ErrorCode = errorCode
            };
        }

    }

    public class ApiResponse<T> : ApiResponse
    {
        public T? Data { get; set; }

        public static ApiResponse<T> Ok(T data, string message = "Operation completed successfully")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static new ApiResponse<T> Error(string message, string? errorCode = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                ErrorCode = errorCode
            };
        }
    }
}
