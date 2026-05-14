using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BackendAPI.Services;
using BackendAPI.Models;

namespace BackendAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly AppDbContext _context;

        public AuthController(IAuthService authService, AppDbContext context)
        {
            _authService = authService;
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            if (_authService.ValidateCredentials(model.Username, model.Password))
            {
                var token = _authService.GenerateJwtToken(model.Username);
                
                _context.Logs.Add(new Log { Level = "Info", Message = $"Uživatel '{model.Username}' se úspěšně přihlásil." });
                await _context.SaveChangesAsync();
                
                return Ok(new { token });
            }
            
            _context.Logs.Add(new Log { Level = "Warning", Message = $"Neúspěšný pokus o přihlášení: '{model.Username}'." });
            await _context.SaveChangesAsync();
            
            return Unauthorized("Neplatné jméno nebo heslo.");
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LoginDto model)
        {
            var user = string.IsNullOrEmpty(model.Username) ? "Neznámý uživatel" : model.Username;
            _context.Logs.Add(new Log { Level = "Info", Message = $"Uživatel '{user}' se odhlásil." });
            await _context.SaveChangesAsync();
            return Ok();
        }
    }

    public class LoginDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}