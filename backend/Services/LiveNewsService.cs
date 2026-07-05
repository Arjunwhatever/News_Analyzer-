using System.Text.Json;
using System.Text.Json.Serialization;
using Vector.Server.Models;

namespace Vector.Server.Services
{
    public class LiveNewsService : ILiveNewsService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;

        public LiveNewsService(HttpClient http, IConfiguration configuration)
        {
            _http = http;
            _apiKey = configuration.GetValue<string>("AppSettings:NewsApiKey")
                ?? throw new InvalidOperationException("NewsApiKey is not configured.");
                
            // NewsAPI requires a User-Agent header
            _http.DefaultRequestHeaders.Add("User-Agent", "VectorNewsAnalyzer/1.0");
        }

        public async Task<List<LiveNewsArticle>> GetTopHeadlinesAsync(string? topics = null, int weeks = 1)
        {
            var fromDate = DateTime.UtcNow.AddDays(-7 * weeks).ToString("yyyy-MM-dd");
            var query = string.IsNullOrWhiteSpace(topics) ? "news" : topics;

            // If we have multiple comma-separated topics (like user preferences), format them into an OR query
            if (query.Contains(','))
            {
                var parts = query.Split(',')
                    .Select(p => $"\"{p.Trim()}\"")
                    .Where(p => p != "\"\"");
                query = string.Join(" OR ", parts);
            }

            var url = $"https://newsapi.org/v2/everything?q={Uri.EscapeDataString(query)}&language=en&sortBy=publishedAt&from={fromDate}&apiKey={_apiKey}";
            
            var response = await _http.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var newsResponse = JsonSerializer.Deserialize<NewsApiResponse>(json, options);

            if (newsResponse?.Articles == null)
            {
                return new List<LiveNewsArticle>();
            }

            return newsResponse.Articles
                .Where(a => !string.IsNullOrEmpty(a.Title) && !a.Title.Contains("[Removed]"))
                .Select(a => new LiveNewsArticle
                {
                    Title = a.Title ?? "No Title",
                    Description = a.Description ?? "",
                    Url = a.Url ?? "",
                    ImageUrl = a.UrlToImage ?? "",
                    SourceName = a.Source?.Name ?? "Unknown Source",
                    PublishedAt = a.PublishedAt ?? ""
                })
                .ToList();
        }

        public async Task<List<LiveNewsArticle>> GetArticlesFromDomainsAsync(string? topics, List<string> domains, int weeks = 1)
        {
            if (domains == null || !domains.Any())
            {
                return new List<LiveNewsArticle>();
            }

            var fromDate = DateTime.UtcNow.AddDays(-7 * weeks).ToString("yyyy-MM-dd");
            var query = string.IsNullOrWhiteSpace(topics) ? "news" : topics;

            if (query.Contains(','))
            {
                var parts = query.Split(',')
                    .Select(p => $"\"{p.Trim()}\"")
                    .Where(p => p != "\"\"");
                query = string.Join(" OR ", parts);
            }

            var domainStr = string.Join(",", domains);
            var url = $"https://newsapi.org/v2/everything?q={Uri.EscapeDataString(query)}&domains={Uri.EscapeDataString(domainStr)}&language=en&sortBy=publishedAt&from={fromDate}&apiKey={_apiKey}";
            
            var response = await _http.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var newsResponse = JsonSerializer.Deserialize<NewsApiResponse>(json, options);

            if (newsResponse?.Articles == null)
            {
                return new List<LiveNewsArticle>();
            }

            return newsResponse.Articles
                .Where(a => !string.IsNullOrEmpty(a.Title) && !a.Title.Contains("[Removed]"))
                .Select(a => new LiveNewsArticle
                {
                    Title = a.Title ?? "No Title",
                    Description = a.Description ?? "",
                    Url = a.Url ?? "",
                    ImageUrl = a.UrlToImage ?? "",
                    SourceName = a.Source?.Name ?? "Unknown Source",
                    PublishedAt = a.PublishedAt ?? ""
                })
                .ToList();
        }

        private class NewsApiResponse
        {
            public string Status { get; set; } = string.Empty;
            public List<NewsApiArticle>? Articles { get; set; }
        }

        private class NewsApiArticle
        {
            public NewsApiSource? Source { get; set; }
            public string? Title { get; set; }
            public string? Description { get; set; }
            public string? Url { get; set; }
            public string? UrlToImage { get; set; }
            public string? PublishedAt { get; set; }
        }

        private class NewsApiSource
        {
            public string? Name { get; set; }
        }
    }
}
