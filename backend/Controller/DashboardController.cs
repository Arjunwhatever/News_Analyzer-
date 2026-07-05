using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Vector.Server.Models;
using Vector.Server.Services;
using Microsoft.EntityFrameworkCore;
using Vector.Server.Data;

namespace Vector.Server.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController(
        UserDbContext dbContext,
        ILiveNewsService liveNewsService,
        IAuthService authService) : ControllerBase
    {
        [HttpGet("recommendations")]
        public async Task<ActionResult> GetRecommendations([FromQuery] string? topics = null)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized();
            }

            var records = await dbContext.AnalysisRecords
                .Where(r => r.UserId == userId)
                .ToListAsync();

            double averageBias = 0;
            if (records.Any())
            {
                averageBias = records.Average(r => r.BiasScore);
            }

            string topicsToQuery;
            if (!string.IsNullOrWhiteSpace(topics))
            {
                topicsToQuery = topics;
            }
            else
            {
                var userTopics = await authService.GetPreferencesAsync(userId);
                topicsToQuery = string.IsNullOrWhiteSpace(userTopics) ? "politics" : userTopics;
            }

            List<string> targetDomains = new List<string>();
            string message = "";

            if (averageBias < -2.0)
            {
                // User leans left, recommend right
                targetDomains = new List<string> { "foxnews.com", "wsj.com", "nypost.com", "nationalreview.com" };
                message = "Your reading history leans Left. Here are top stories from conservative outlets to balance your perspective.";
            }
            else if (averageBias > 2.0)
            {
                // User leans right, recommend left
                targetDomains = new List<string> { "theguardian.com", "msnbc.com", "cnn.com", "washingtonpost.com" };
                message = "Your reading history leans Right. Here are top stories from progressive outlets to balance your perspective.";
            }
            else
            {
                // Neutral or no history
                targetDomains = new List<string> { "apnews.com", "reuters.com", "bbc.co.uk", "npr.org" };
                message = "Your reading history is balanced! Here are top stories from highly factual centrist publishers.";
            }

            var articles = await liveNewsService.GetArticlesFromDomainsAsync(topicsToQuery, targetDomains);

            return Ok(new
            {
                averageBiasScore = averageBias,
                message = message,
                articles = articles
            });
        }
    }
}
