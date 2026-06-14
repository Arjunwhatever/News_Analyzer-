using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vector.Server.Services;
using System.Security.Claims;
using Vector.Server.Services;
using Vector.Server.Models;

namespace Vector.Server.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeedController : ControllerBase
    {
        private readonly ILiveNewsService _newsService;
        private readonly IAuthService _authService;

        public FeedController(ILiveNewsService newsService, IAuthService authService)
        {
            _newsService = newsService;
            _authService = authService;
        }

        [HttpGet("live")]
        [Authorize]
        public async Task<ActionResult<List<LiveNewsArticle>>> GetLiveNews()
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                string? topics = null;
                
                if (!string.IsNullOrEmpty(userIdStr) && Guid.TryParse(userIdStr, out var userId))
                {
                    topics = await _authService.GetPreferencesAsync(userId);
                }

                var articles = await _newsService.GetTopHeadlinesAsync(topics);
                return Ok(articles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to fetch live news: {ex.Message}");
            }
        }
    }
}
