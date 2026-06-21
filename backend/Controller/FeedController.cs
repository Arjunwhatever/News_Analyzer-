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
        public async Task<ActionResult<List<LiveNewsArticle>>> GetLiveNews([FromQuery] int weeks = 1, [FromQuery] string? category = null)
        {
            try
            {
                string? topics = category;
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                string? userPrefs = null;

                if (!string.IsNullOrEmpty(userIdStr) && Guid.TryParse(userIdStr, out var userId))
                {
                    userPrefs = await _authService.GetPreferencesAsync(userId);
                }

                if (category == "News" || string.IsNullOrEmpty(category))
                {
                    topics = userPrefs;
                }
                else if (category == "Discover")
                {
                    var allTopics = new[] { "Technology", "Business", "Politics", "Science", "Sports", "Entertainment", "Health", "AI", "Space", "Art", "History", "Travel", "Food", "Culture" };
                    var prefsList = userPrefs?.Split(',').Select(t => t.Trim().ToLower()).ToList() ?? new List<string>();
                    
                    var available = allTopics.Where(t => !prefsList.Contains(t.ToLower())).ToList();
                    
                    if (available.Count > 0)
                    {
                        var random = new Random();
                        var selected = available.OrderBy(x => random.Next()).Take(3).ToList();
                        
                        var allArticles = new List<LiveNewsArticle>();
                        foreach (var topic in selected)
                        {
                            var topicArticles = await _newsService.GetTopHeadlinesAsync(topic, weeks);
                            foreach (var article in topicArticles)
                            {
                                article.Topic = topic;
                            }
                            allArticles.AddRange(topicArticles);
                        }
                        
                        // Shuffle them
                        var shuffled = allArticles.OrderBy(x => random.Next()).ToList();
                        return Ok(shuffled);
                    }
                    else
                    {
                        topics = "World"; // Fallback if they selected literally everything
                        var fallbackArticles = await _newsService.GetTopHeadlinesAsync(topics, weeks);
                        foreach (var a in fallbackArticles) a.Topic = "World";
                        return Ok(fallbackArticles);
                    }
                }

                var articles = await _newsService.GetTopHeadlinesAsync(topics, weeks);
                return Ok(articles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to fetch live news: {ex.Message}");
            }
        }
    }
}
