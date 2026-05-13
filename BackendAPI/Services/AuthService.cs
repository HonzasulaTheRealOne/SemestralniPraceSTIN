using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
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
        private readonly string _secretKey = "TajnyKlicProSemestralniPraciSTIN2026_MusiBytDostatecneDlouhy";

        public bool ValidateCredentials(string username, string password)
        {
            return username == "admin" && password == "admin";
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