using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Vector.Server.Models;
using Vector.Server.Services;

namespace Vector.Server.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalysisController(
        BiasAnalyzerService biasAnalyzerService,
        ArticleScraperService articleScraperService) : ControllerBase
    {
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

                var result = await biasAnalyzerService.AnalyzeAsync(articleText);
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
    }

    public class AnalysisRequest
    {
        public string? Url { get; set; }
        public string? Text { get; set; }
    }
}
