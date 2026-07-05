using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Vector.Server.Models;
using Vector.Server.Services;

namespace Vector.Server.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalysisController(
        BiasAnalyzerService biasAnalyzerService,
        ArticleScraperService articleScraperService,
        IAuthService authService,
        Vector.Server.Data.UserDbContext dbContext) : ControllerBase
    {
        // The main brain of the API! This endpoint accepts an article URL or text payload, extracts the content, and passes it to the AI for bias scoring.
        [HttpPost("analyze")]
        public async Task<ActionResult<AnalysisResult>> Analyze(AnalysisRequest request)
        {
            if (request == null)
            {
                return BadRequest("Invalid request payload.");
            }

            try
            {
                string articleText = string.Empty;

                if (!string.IsNullOrWhiteSpace(request.Url))
                {
                    // URL Scraping mode
                    articleText = await articleScraperService.ExtractArticleTextAsync(request.Url);
                }
                else if (!string.IsNullOrWhiteSpace(request.Text))
                {
                    // Direct text mode
                    articleText = request.Text;
                }
                else
                {
                    return BadRequest("Either a URL or article text must be provided.");
                }

                if (string.IsNullOrWhiteSpace(articleText) || articleText.Length < 10)
                {
                    return BadRequest("Provided text is too short to perform a reliable bias analysis.");
                }

                string? userTopics = null;
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userIdStr) && Guid.TryParse(userIdStr, out var userId))
                {
                    userTopics = await authService.GetPreferencesAsync(userId);
                }

                // Once we have a decent chunk of text, we hand it off to the Bias Analyzer Service to do the heavy lifting!
                var result = await biasAnalyzerService.AnalyzeAsync(articleText, userTopics);

                // Save to historical records
                var sourceName = "Unknown";
                if (!string.IsNullOrWhiteSpace(request.Url))
                {
                    try
                    {
                        var uri = new Uri(request.Url);
                        sourceName = uri.Host.Replace("www.", "");
                    }
                    catch { }
                }
                else
                {
                    sourceName = "Direct Text";
                }

                if (sourceName != "Direct Text" && sourceName != "Unknown")
                {
                    // Check if we already have this source evaluated
                    var existingSource = dbContext.NewsSources.FirstOrDefault(s => s.SourceName == sourceName);
                    if (existingSource == null)
                    {
                        var (sourceBiasScore, sourceDesc) = await biasAnalyzerService.AnalyzeSourceHistoricalBiasAsync(sourceName);
                        existingSource = new Vector.Server.Entities.NewsSource
                        {
                            SourceName = sourceName,
                            HistoricalBiasScore = sourceBiasScore,
                            Description = sourceDesc,
                            ArticleCount = 1,
                            LastEvaluatedAt = DateTime.UtcNow
                        };
                        dbContext.NewsSources.Add(existingSource);
                    }
                    else
                    {
                        existingSource.ArticleCount++;
                    }
                }

                var record = new Vector.Server.Entities.AnalysisRecord
                {
                    SourceName = sourceName,
                    ArticleUrl = request.Url ?? "N/A",
                    ArticleTitle = "Unknown Title", // Future enhancement: extract title during scraping
                    BiasScore = result.BiasScore,
                    BiasLabel = result.BiasLabel,
                    AnalyzedAt = DateTime.UtcNow
                };

                dbContext.AnalysisRecords.Add(record);
                await dbContext.SaveChangesAsync();

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred during analysis: {ex.Message}");
            }
        }

        [HttpGet("sources")]
        public ActionResult GetSourcesBiasStats()
        {
            var stats = dbContext.NewsSources
                .Select(s => new
                {
                    SourceName = s.SourceName,
                    AverageBias = s.HistoricalBiasScore,
                    ArticleCount = s.ArticleCount,
                    Description = s.Description
                })
                .OrderByDescending(s => s.ArticleCount)
                .ToList();

            return Ok(stats);
        }
    }

    public class AnalysisRequest
    {
        public string? Url { get; set; }
        public string? Text { get; set; }
    }
}
