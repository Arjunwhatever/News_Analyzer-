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
        
        [HttpPost("Register")]
        public async Task<ActionResult<User>> Register(UserDto request)
        {
            var user = await authService.RegisterAsync(request);
            if (user == null)
                return BadRequest("Username already exists");
            return Ok(user);
        }

        [HttpPost("Login")]
        public async Task<ActionResult<string>> Login(UserDto request)
        {
           var token = await authService.LoginAsync(request);
            if (token == null)
                return BadRequest("Invalid username or password");
            return Ok(token);

        }
        [Authorize]
        [HttpGet]
        public IActionResult AuthenticatedEndpoint()
        {
            return Ok("Authenticated!");
        }
    }
}