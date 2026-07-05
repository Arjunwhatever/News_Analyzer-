using Vector.Server.Models;

namespace Vector.Server.Services
{
    public interface ILiveNewsService
    {
        Task<List<LiveNewsArticle>> GetTopHeadlinesAsync(string? topics = null, int weeks = 1);
        Task<List<LiveNewsArticle>> GetArticlesFromDomainsAsync(string? topics, List<string> domains, int weeks = 1);
    }
}
