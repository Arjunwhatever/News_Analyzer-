using System.Text.Json;
using System.Text.Json.Serialization;
using Vector.Server.Models;

namespace Vector.Server.Services
{
    public class LiveNewsService
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

        public async Task<List<LiveNewsArticle>> GetTopHeadlinesAsync(string? topics = null)
        {
            var url = string.IsNullOrWhiteSpace(topics)
                ? $"https://newsapi.org/v2/top-headlines?country=us&apiKey={_apiKey}"
                : $"https://newsapi.org/v2/everything?q={Uri.EscapeDataString(topics)}&language=en&sortBy=publishedAt&apiKey={_apiKey}";
            
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
