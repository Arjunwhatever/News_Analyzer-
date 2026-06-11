using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Vector.Server.Entities;
using Vector.Server.Models;
using Vector.Server.Services;

namespace Vector.Server.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IAuthService authService) : ControllerBase
    {
        
        // Creates a brand new account for the user in our database
        [HttpPost("Register")]
        public async Task<ActionResult<User>> Register(UserDto request)
        {
            var user = await authService.RegisterAsync(request);
            if (user == null)
                return BadRequest("Username already exists");
            return Ok(user);
        }

        // Logs the user in by generating a secure JWT token and dropping it into an HttpOnly cookie
        [HttpPost("Login")]
        public async Task<IActionResult> Login(UserDto request)
        {
           var token = await authService.LoginAsync(request);
            if (token == null)
                return BadRequest("Invalid username or password");

            // We drop the token securely into the browser. JavaScript can't touch this!
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true, // Must be true in production, works locally because we use HTTPS
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(1)
            };
            Response.Cookies.Append("jwt", token, cookieOptions);

            var isAuthOptions = new CookieOptions
            {
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(1)
            };
            Response.Cookies.Append("isAuthenticated", "true", isAuthOptions);

            return Ok(new { message = "Logged in successfully" });
        }

        // Clears the user's cookies, effectively logging them out immediately
        [HttpPost("Logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("jwt");
            Response.Cookies.Delete("isAuthenticated");
            return Ok(new { message = "Logged out successfully" });
        }
        [Authorize]
        [HttpGet]
        public IActionResult AuthenticatedEndpoint()
        {
            return Ok("Authenticated!");
        }
    }
}