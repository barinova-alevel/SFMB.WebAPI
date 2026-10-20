using SFMB.BL.Dtos;

namespace SFMB.BL.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto?> LoginAsync(LoginDto loginDto);
        Task<AuthResponseDto?> RegisterAsync(RegisterDto registerDto);

        /// <summary>
        /// Demo sign-in credentials. Returns null when DemoUser:Password is not configured.
        /// Creates the demo account, or updates its password, so the returned credentials can sign in.
        /// </summary>
        Task<DemoCredentialsDto?> GetDemoCredentialsAsync();
    }
}
