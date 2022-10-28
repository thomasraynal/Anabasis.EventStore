using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Anabasis.Api.Jwt
{
    public class SimpleJwtService : ISimpleJwtService
    {
        private const double EXPIRY_DURATION_MINUTES = 7 * 24 * 60;

        private const string JwtKey = "e21f9fd8-fc01-45dc-9a70-120ecc30aaeb";

        public (string token, DateTime expirationUtcDate) BuildToken(ISimpleJwtUser user)
        {
            var claims = new[] {
                new Claim("jti", $"{user.UserId}"),
                new Claim(ClaimTypes.Name, user.UserMail),
                new Claim(ClaimTypes.Role, user.UserRole),
                new Claim(ClaimTypes.NameIdentifier, $"{user.UserId}")
        };

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);
            var expirationUtcDate = DateTime.UtcNow.AddMinutes(EXPIRY_DURATION_MINUTES);
            var tokenDescriptor = new JwtSecurityToken(claims: claims,
                expires: expirationUtcDate, signingCredentials: credentials);

            var token = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);

            return (token, expirationUtcDate);
        }
        public (bool isUserValid, SecurityToken? securityToken) GetToken(string token)
        {
            var mySecret = Encoding.UTF8.GetBytes(JwtKey);
            var mySecurityKey = new SymmetricSecurityKey(mySecret);
            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                tokenHandler.ValidateToken(token,
                new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    IssuerSigningKey = mySecurityKey,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return (true, validatedToken);
            }
            catch
            {
                return (false, null);
            }
        }
    }
}
