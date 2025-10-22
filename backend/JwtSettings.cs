using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace CloudCore
{
    public class JwtSettings
    {
        public string Key { get; set; } = null!;
        public string Issuer { get; set; } = null!;
        public string Audience { get; set; } = null!;
        public int EmailTokenExpirationMinutes { get; set; }
        public int JwtTokenExpirationDays { get; set; }

    }
}
