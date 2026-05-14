using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BackendAPI.Services
{
    public interface IAuthService
    {
        string GenerateJwtToken(string username);
        bool ValidateCredentials(string username, string password);
    }

    public class AuthService : IAuthService
    {
        private readonly string _secretKey;
        private readonly string _adminUsername;
        private readonly string _adminPassword;

        public AuthService(IConfiguration configuration)
        {
            _secretKey = configuration["JwtSettings:SecretKey"] ?? throw new Exception("Chybí JWT klíč v konfiguraci!");
            _adminUsername = configuration["AdminSettings:Username"] ?? throw new Exception("Chybí admin jméno v konfiguraci!");
            _adminPassword = configuration["AdminSettings:Password"] ?? throw new Exception("Chybí admin heslo v konfiguraci!");
        }

        public bool ValidateCredentials(string username, string password)
        {
            return username == _adminUsername && password == _adminPassword;
        }

        public string GenerateJwtToken(string username)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secretKey);
            
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, username) }),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}