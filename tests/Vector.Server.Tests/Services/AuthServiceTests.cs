using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Vector.Server.Data;
using Vector.Server.Models;
using Vector.Server.Services;
using Xunit;

namespace Vector.Server.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly IConfiguration _configuration;

        public AuthServiceTests()
        {
            // Set up an in-memory configuration builder instead of mocking IConfiguration,
            // which guarantees that GetValue<string>() behaves identically to production.
            var inMemorySettings = new Dictionary<string, string> {
                {"AppSettings:Token", "SuperSecretTestingKeyThatIsAtLeast64CharactersLongSoItSatisfiesHMACSHA512!"},
                {"AppSettings:Issuer", "TestIssuer"},
                {"AppSettings:Audience", "TestAudience"}
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings!)
                .Build();
        }

        private UserDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new UserDbContext(options);
        }

        [Fact]
        public async Task RegisterAsync_Should_Create_New_User_Successfully()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var authService = new AuthService(context, _configuration);
            var request = new UserDto { Username = "newuser", Password = "password123" };

            // Act
            var result = await authService.RegisterAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("newuser", result.Username);
            
            // Verify it was saved to DB
            var dbUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "newuser");
            Assert.NotNull(dbUser);
            Assert.NotNull(dbUser.PasswordHash);
        }

        [Fact]
        public async Task RegisterAsync_Should_Fail_If_Username_Exists()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var authService = new AuthService(context, _configuration);
            var request = new UserDto { Username = "existinguser", Password = "password123" };
            
            // Pre-seed the DB
            await authService.RegisterAsync(request);

            // Act
            var duplicateRequest = new UserDto { Username = "existinguser", Password = "newpassword" };
            var result = await authService.RegisterAsync(duplicateRequest);

            // Assert
            Assert.Null(result); // Service should return null when username exists
        }

        [Fact]
        public async Task LoginAsync_Should_Return_Token_For_Valid_Credentials()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var authService = new AuthService(context, _configuration);
            var request = new UserDto { Username = "loginuser", Password = "correctpassword" };
            
            // Register a user first
            await authService.RegisterAsync(request);

            // Act
            var token = await authService.LoginAsync(request);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);
        }

        [Fact]
        public async Task LoginAsync_Should_Fail_For_Invalid_Password()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var authService = new AuthService(context, _configuration);
            var request = new UserDto { Username = "loginuser", Password = "correctpassword" };
            
            // Register a user first
            await authService.RegisterAsync(request);

            // Act
            var badRequest = new UserDto { Username = "loginuser", Password = "wrongpassword" };
            var token = await authService.LoginAsync(badRequest);

            // Assert
            Assert.Null(token);
        }

        [Fact]
        public async Task LoginAsync_Should_Fail_For_NonExistent_User()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var authService = new AuthService(context, _configuration);

            // Act
            var request = new UserDto { Username = "nobody", Password = "nopassword" };
            var token = await authService.LoginAsync(request);

            // Assert
            Assert.Null(token);
        }
    }
}
