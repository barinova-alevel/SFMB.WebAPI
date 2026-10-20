using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SFMB.BL.Dtos;
using SFMB.BL.Services;
using SFMB.DAL.Entities;
using Xunit;

namespace SFMB.Tests
{
    public class AuthServiceTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
        private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            // Setup UserManager mock
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object,
                null!, null!, null!, null!, null!, null!, null!, null!);

            // Setup SignInManager mock (Identity 2.2 ctor used via SFMB.BL package reference)
            _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
                _userManagerMock.Object,
                Mock.Of<IHttpContextAccessor>(),
                Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(),
                Mock.Of<IOptions<IdentityOptions>>(),
                Mock.Of<ILogger<SignInManager<ApplicationUser>>>(),
                Mock.Of<IAuthenticationSchemeProvider>());

            var roleStoreMock = new Mock<IRoleStore<IdentityRole>>();
            _roleManagerMock = new Mock<RoleManager<IdentityRole>>(
                roleStoreMock.Object,
                null!, null!, null!, null!);

            // Setup Configuration mock
            _configurationMock = new Mock<IConfiguration>();
            _configurationMock.Setup(c => c["Jwt:Key"]).Returns("YourSuperSecretKeyForJWTTokenGenerationThatIsAtLeast32CharactersLong");
            _configurationMock.Setup(c => c["Jwt:Issuer"]).Returns("SFMBApi");
            _configurationMock.Setup(c => c["Jwt:Audience"]).Returns("SFMBApi");
            _configurationMock.Setup(c => c["Jwt:ExpiryInHours"]).Returns("24");

            _authService = new AuthService(
                _userManagerMock.Object,
                _signInManagerMock.Object,
                _roleManagerMock.Object,
                _configurationMock.Object);
        }

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ReturnsAuthResponse()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "test@example.com",
                Password = "Password123!"
            };

            var user = new ApplicationUser
            {
                Id = "user-id",
                Email = loginDto.Email,
                UserName = loginDto.Email,
                FirstName = "Test",
                LastName = "User"
            };

            _userManagerMock.Setup(um => um.FindByEmailAsync(loginDto.Email))
                .ReturnsAsync(user);
            _signInManagerMock.Setup(sm => sm.CheckPasswordSignInAsync(user, loginDto.Password, true))
                .ReturnsAsync(SignInResult.Success);
            _userManagerMock.Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "User" });

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(loginDto.Email, result.Email);
            Assert.Equal("user-id", result.UserId);
            Assert.Equal("User", result.Role);
            Assert.NotEmpty(result.Token);
        }

        [Fact]
        public async Task LoginAsync_WithInvalidEmail_ReturnsNull()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "nonexistent@example.com",
                Password = "Password123!"
            };

            _userManagerMock.Setup(um => um.FindByEmailAsync(loginDto.Email))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task LoginAsync_WithInvalidPassword_ReturnsNull()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "test@example.com",
                Password = "WrongPassword"
            };

            var user = new ApplicationUser
            {
                Id = "user-id",
                Email = loginDto.Email,
                UserName = loginDto.Email
            };

            _userManagerMock.Setup(um => um.FindByEmailAsync(loginDto.Email))
                .ReturnsAsync(user);
            _signInManagerMock.Setup(sm => sm.CheckPasswordSignInAsync(user, loginDto.Password, true))
                .ReturnsAsync(SignInResult.Failed);

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task RegisterAsync_WithNewUser_ReturnsAuthResponse()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "newuser@example.com",
                Password = "Password123!",
                FirstName = "New",
                LastName = "User"
            };

            _userManagerMock.Setup(um => um.FindByEmailAsync(registerDto.Email))
                .ReturnsAsync((ApplicationUser?)null);
            _userManagerMock.Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>(), registerDto.Password))
                .ReturnsAsync(IdentityResult.Success)
                .Callback<ApplicationUser, string>((user, pass) => user.Id = "new-user-id");
            _userManagerMock.Setup(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(um => um.GetRolesAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(new List<string> { "User" });

            // Act
            var result = await _authService.RegisterAsync(registerDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(registerDto.Email, result.Email);
            Assert.Equal("User", result.Role);
            Assert.NotEmpty(result.Token);
            _userManagerMock.Verify(um => um.CreateAsync(It.IsAny<ApplicationUser>(), registerDto.Password), Times.Once);
            _userManagerMock.Verify(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_WithExistingEmail_ReturnsNull()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "existing@example.com",
                Password = "Password123!",
                FirstName = "Existing",
                LastName = "User"
            };

            var existingUser = new ApplicationUser
            {
                Id = "existing-user-id",
                Email = registerDto.Email,
                UserName = registerDto.Email
            };

            _userManagerMock.Setup(um => um.FindByEmailAsync(registerDto.Email))
                .ReturnsAsync(existingUser);

            // Act
            var result = await _authService.RegisterAsync(registerDto);

            // Assert
            Assert.Null(result);
            _userManagerMock.Verify(um => um.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_WithFailedCreation_ReturnsNull()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "newuser@example.com",
                Password = "weak",
                FirstName = "New",
                LastName = "User"
            };

            _userManagerMock.Setup(um => um.FindByEmailAsync(registerDto.Email))
                .ReturnsAsync((ApplicationUser?)null);
            _userManagerMock.Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>(), registerDto.Password))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too weak" }));

            // Act
            var result = await _authService.RegisterAsync(registerDto);

            // Assert
            Assert.Null(result);
            _userManagerMock.Verify(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetDemoCredentialsAsync_WhenPasswordMissing_ReturnsNull()
        {
            _configurationMock.Setup(c => c["DemoUser:Password"]).Returns((string?)null);

            var result = await _authService.GetDemoCredentialsAsync();

            Assert.Null(result);
            _userManagerMock.Verify(um => um.FindByEmailAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetDemoCredentialsAsync_WhenUserMissing_CreatesDemoUser()
        {
            _configurationMock.Setup(c => c["DemoUser:Password"]).Returns("DemoPass1");
            _configurationMock.Setup(c => c["DemoUser:Email"]).Returns((string?)null);
            _roleManagerMock.Setup(rm => rm.RoleExistsAsync("User")).ReturnsAsync(true);
            _userManagerMock.Setup(um => um.FindByEmailAsync(AuthService.DefaultDemoEmail))
                .ReturnsAsync((ApplicationUser?)null);
            _userManagerMock.Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>(), "DemoPass1"))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(um => um.IsLockedOutAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(false);
            _userManagerMock.Setup(um => um.IsInRoleAsync(It.IsAny<ApplicationUser>(), "User"))
                .ReturnsAsync(false);
            _userManagerMock.Setup(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"))
                .ReturnsAsync(IdentityResult.Success);

            var result = await _authService.GetDemoCredentialsAsync();

            Assert.NotNull(result);
            Assert.Equal(AuthService.DefaultDemoEmail, result.Email);
            Assert.Equal("DemoPass1", result.Password);
            _userManagerMock.Verify(um => um.CreateAsync(It.IsAny<ApplicationUser>(), "DemoPass1"), Times.Once);
        }

        [Fact]
        public async Task GetDemoCredentialsAsync_WhenPasswordDiffers_ResetsDemoPassword()
        {
            var user = new ApplicationUser
            {
                Id = "demo-id",
                Email = "demo@goldfish.app",
                UserName = "demo@goldfish.app"
            };

            _configurationMock.Setup(c => c["DemoUser:Password"]).Returns("DemoPass1");
            _configurationMock.Setup(c => c["DemoUser:Email"]).Returns("demo@goldfish.app");
            _roleManagerMock.Setup(rm => rm.RoleExistsAsync("User")).ReturnsAsync(true);
            _userManagerMock.Setup(um => um.FindByEmailAsync("demo@goldfish.app")).ReturnsAsync(user);
            _userManagerMock.Setup(um => um.CheckPasswordAsync(user, "DemoPass1")).ReturnsAsync(false);
            _userManagerMock.Setup(um => um.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token");
            _userManagerMock.Setup(um => um.ResetPasswordAsync(user, "reset-token", "DemoPass1"))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(um => um.IsLockedOutAsync(user)).ReturnsAsync(false);
            _userManagerMock.Setup(um => um.IsInRoleAsync(user, "User")).ReturnsAsync(true);

            var result = await _authService.GetDemoCredentialsAsync();

            Assert.NotNull(result);
            Assert.Equal("demo@goldfish.app", result.Email);
            _userManagerMock.Verify(um => um.ResetPasswordAsync(user, "reset-token", "DemoPass1"), Times.Once);
            _userManagerMock.Verify(um => um.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        }
    }
}
