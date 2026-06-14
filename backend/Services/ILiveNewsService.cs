using Vector.Server.Models;

namespace Vector.Server.Services
{
    public interface ILiveNewsService
    {
        Task<List<LiveNewsArticle>> GetTopHeadlinesAsync(string? topics = null);
    }
}
