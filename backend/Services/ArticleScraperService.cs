using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace Vector.Server.Services
{
    public class ArticleScraperService
    {
        private readonly HttpClient _http;

        public ArticleScraperService(HttpClient http)
        {
            _http = http;
            _http.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            _http.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Fetches and extracts the main article body from <paramref name="url"/>.
        /// </summary>
        /// <param name="url">Fully-qualified URL of the news article.</param>
        /// <returns>Plain text of the article body.</returns>
        public async Task<string> ExtractArticleTextAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentException("URL cannot be empty.", nameof(url));
            }

            try
            {
                // Ensure URL starts with http/https
                if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && 
                    !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    url = "https://" + url;
                }

                // 1. Fetch raw HTML
                var response = await _http.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var html = await response.Content.ReadAsStringAsync();

                // 2. Parse HTML DOM
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // 3. Remove script, style, head, footer, nav, iframe elements
                var nodesToRemove = doc.DocumentNode.SelectNodes("//script | //style | //head | //footer | //nav | //iframe | //aside | //noscript");
                if (nodesToRemove != null)
                {
                    foreach (var node in nodesToRemove)
                    {
                        node.Remove();
                    }
                }

                // 4. Try to find the main content node
                // Check common container nodes: <article>, <main>, or divs with 'article', 'post', 'entry', 'content' classes/ids
                HtmlNode? contentNode = doc.DocumentNode.SelectSingleNode("//article") 
                                       ?? doc.DocumentNode.SelectSingleNode("//main")
                                       ?? doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'article-body')]")
                                       ?? doc.DocumentNode.SelectSingleNode("//div[contains(@id, 'article-body')]")
                                       ?? doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'story-content')]")
                                       ?? doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'post-content')]")
                                       ?? doc.DocumentNode.SelectSingleNode("//div[contains(@id, 'post-content')]");

                // Fall back to document body if no specific article container is found
                var targetNode = contentNode ?? doc.DocumentNode.SelectSingleNode("//body") ?? doc.DocumentNode;

                // 5. Select all paragraphs inside the target container
                var paragraphNodes = targetNode.SelectNodes(".//p");
                if (paragraphNodes == null || paragraphNodes.Count == 0)
                {
                    // If no paragraphs found in target, try all paragraphs in the document
                    paragraphNodes = doc.DocumentNode.SelectNodes("//p");
                }

                if (paragraphNodes != null && paragraphNodes.Count > 0)
                {
                    // Clean paragraphs and filter out short lines/empty ones (advertisements or links)
                    var paragraphs = paragraphNodes
                        .Select(p => HtmlEntity.DeEntitize(p.InnerText).Trim())
                        .Where(text => text.Length > 40 && !text.StartsWith("Share this", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (paragraphs.Count > 0)
                    {
                        return string.Join("\n\n", paragraphs);
                    }
                }

                // Fallback to plain inner text extraction if no <p> tags exist
                var plainText = HtmlEntity.DeEntitize(targetNode.InnerText).Trim();
                // Clean up excessive whitespace
                plainText = Regex.Replace(plainText, @"[ \t]+", " ");
                plainText = Regex.Replace(plainText, @"[\r\n]+", "\n\n");
                
                if (plainText.Length > 200)
                {
                    return plainText;
                }

                throw new InvalidOperationException("Could not extract sufficient text content from the provided URL.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error scraping article from URL: {ex.Message}", ex);
            }
        }
    }
}
