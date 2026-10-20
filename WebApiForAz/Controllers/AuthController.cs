using Microsoft.AspNetCore.Mvc;
using SFMB.BL.Dtos;
using SFMB.BL.Services.Interfaces;

namespace WebApiForAz.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Demo sign-in credentials for one-click login.
        /// The password comes from DemoUser:Password (environment variable DemoUser__Password).
        /// It is not stored in source control. Returns 404 when that value is missing.
        /// </summary>
        [HttpGet("demo")]
        public async Task<ActionResult<DemoCredentialsDto>> Demo()
        {
            var credentials = await _authService.GetDemoCredentialsAsync();
            if (credentials == null)
            {
                return NotFound();
            }

            return Ok(credentials);
        }

        /// <summary>
        /// Login to get JWT token
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto loginDto)
        {
            if (string.IsNullOrEmpty(loginDto.Email) || string.IsNullOrEmpty(loginDto.Password))
            {
                return BadRequest("Email and password are required.");
            }

            var result = await _authService.LoginAsync(loginDto);
            if (result == null)
            {
                return Unauthorized("Invalid email or password.");
            }

            return Ok(result);
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto registerDto)
        {
            if (string.IsNullOrEmpty(registerDto.Email) || string.IsNullOrEmpty(registerDto.Password))
            {
                return BadRequest("Email and password are required.");
            }

            var result = await _authService.RegisterAsync(registerDto);
            if (result == null)
            {
                return BadRequest("User registration failed. Email may already be in use.");
            }

            return Ok(result);
        }
    }
}
