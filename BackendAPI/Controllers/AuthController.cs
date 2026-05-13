using Microsoft.AspNetCore.Mvc;
using BackendAPI.DTOs;
using BackendAPI.Services;

namespace BackendAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto loginDto)
        {
            if (_authService.ValidateCredentials(loginDto.Username, loginDto.Password))
            {
                var token = _authService.GenerateJwtToken(loginDto.Username);
                return Ok(new { Token = token });
            }

            return Unauthorized("Neplatné přihlašovací údaje.");
        }
    }
}