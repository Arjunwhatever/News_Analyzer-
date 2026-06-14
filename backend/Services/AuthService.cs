using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Vector.Server.Data;
using Vector.Server.Entities;
using Vector.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Vector.Server.Services
{
    public class AuthService(UserDbContext context, IConfiguration configuration) : IAuthService
    {
       // Handles the actual registration logic, checking for duplicates and hashing the password
       public async Task<User?> RegisterAsync(UserDto request)
        {   
            if(await context.Users.AnyAsync(u => u.Username == request.Username))
            {
                return null;
            }
            var user = new User();
            
            // We never store plain text passwords! Hash it up securely.
            var hashedPassword = new PasswordHasher<User>().HashPassword(user, request.Password);

            user.Username = request.Username;
            user.PasswordHash = hashedPassword;
            user.PreferredTopics = request.PreferredTopics;

            context.Users.Add(user);
            await context.SaveChangesAsync();
            return user;
        }
        // Helper method that cooks up a JWT token holding the user's identity
        private string CreateToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(
                configuration.GetValue<string>("Appsettings:Token"))!);

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var tokenDescriptor = new JwtSecurityToken(issuer: configuration.GetValue<string>("AppSettings:Issuer"),
                audience: configuration.GetValue<string>("AppSettings:Audience"),
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }
        // Checks the database for the user, verifies their password hash, and returns a token if everything checks out!
        public async Task<string?> LoginAsync(UserDto request)
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (user is null)
            {
                return null;
            }

            if (new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, request.Password)
                == PasswordVerificationResult.Failed)
            {
                return null;
            }
            
            return CreateToken(user);
        }

        public async Task<string?> GetPreferencesAsync(Guid userId)
        {
            var user = await context.Users.FindAsync(userId);
            return user?.PreferredTopics;
        }

        public async Task UpdatePreferencesAsync(Guid userId, string topics)
        {
            var user = await context.Users.FindAsync(userId);
            if (user != null)
            {
                user.PreferredTopics = topics;
                await context.SaveChangesAsync();
            }
        }
    }

}
