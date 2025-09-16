namespace CloudCore.Models
{
    public class ValidationResult
    {
        public bool IsValid {  get; set; }
        public string ErrorMessage { get; set; }

        public string ErrorCode { get; set; }

        public object AdditionalData { get; set; }

        public static ValidationResult Success() => new ValidationResult { IsValid = true };

        public static ValidationResult Failure(string message, string code = null, object data = null)
        {
            return new ValidationResult () { 
                IsValid = false,
                ErrorMessage = message,
                ErrorCode = code,
                AdditionalData = data
            };
        }
    }
}
