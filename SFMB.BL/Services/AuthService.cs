using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SFMB.BL.Dtos;
using SFMB.BL.Services.Interfaces;
using SFMB.DAL.Entities;

namespace SFMB.BL.Services
{
    public class AuthService : IAuthService
    {
        public const string DefaultDemoEmail = "demo@goldfish.app";

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        public async Task<AuthResponseDto?> LoginAsync(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null)
            {
                return null;
            }

            // lockout policy configuration:
            var signInResult = await _signInManager.CheckPasswordSignInAsync(
                user,
                loginDto.Password,
                lockoutOnFailure: true);

            if (!signInResult.Succeeded)
            {
                return null;
            }

            var roles = await _userManager.GetRolesAsync(user);
            var token = GenerateJwtToken(user, roles);

            return new AuthResponseDto
            {
                Token = token,
                Email = user.Email!,
                UserId = user.Id,
                Role = roles.FirstOrDefault() ?? "User"
            };
        }

        public async Task<AuthResponseDto?> RegisterAsync(RegisterDto registerDto)
        {
            var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
            if (existingUser != null)
            {
                return null;
            }

            var user = new ApplicationUser
            {
                Email = registerDto.Email,
                UserName = registerDto.Email,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
            {
                return null;
            }

            await _userManager.AddToRoleAsync(user, "User");

            var roles = await _userManager.GetRolesAsync(user);
            var token = GenerateJwtToken(user, roles);

            return new AuthResponseDto
            {
                Token = token,
                Email = user.Email,
                UserId = user.Id,
                Role = roles.FirstOrDefault() ?? "User"
            };
        }

        public async Task<DemoCredentialsDto?> GetDemoCredentialsAsync()
        {
            var password = _configuration["DemoUser:Password"];
            if (string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            var email = _configuration["DemoUser:Email"];
            if (string.IsNullOrWhiteSpace(email))
            {
                email = DefaultDemoEmail;
            }

            email = email.Trim();
            password = password.Trim();
            await EnsureDemoUserAsync(email, password);

            return new DemoCredentialsDto
            {
                Email = email,
                Password = password
            };
        }

        private async Task EnsureDemoUserAsync(string email, string password)
        {
            if (!await _roleManager.RoleExistsAsync("User"))
            {
                var roleResult = await _roleManager.CreateAsync(new IdentityRole("User"));
                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException("Could not create the User role for the demo account.");
                }
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    Email = email,
                    UserName = email,
                    FirstName = "Demo",
                    LastName = "User",
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };

                var createResult = await _userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                {
                    throw new InvalidOperationException("Could not create the demo account.");
                }
            }
            else if (!await _userManager.CheckPasswordAsync(user, password))
            {
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, password);
                if (!resetResult.Succeeded)
                {
                    throw new InvalidOperationException("Could not update the demo account password.");
                }
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                await _userManager.ResetAccessFailedCountAsync(user);
            }

            if (!await _userManager.IsInRoleAsync(user, "User"))
            {
                var addRoleResult = await _userManager.AddToRoleAsync(user, "User");
                if (!addRoleResult.Succeeded)
                {
                    throw new InvalidOperationException("Could not assign the User role to the demo account.");
                }
            }
        }

        private string GenerateJwtToken(ApplicationUser user, IList<string> roles)
        {
            var jwtKey = _configuration["Jwt:Key"];
            var jwtIssuer = _configuration["Jwt:Issuer"];
            var jwtAudience = _configuration["Jwt:Audience"];
            var jwtExpiryHours = _configuration["Jwt:ExpiryInHours"];

            if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) ||
                string.IsNullOrEmpty(jwtAudience) || string.IsNullOrEmpty(jwtExpiryHours))
            {
                throw new InvalidOperationException("JWT configuration is incomplete");
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}")
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddHours(Convert.ToDouble(jwtExpiryHours));

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
