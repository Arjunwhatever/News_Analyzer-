using System.ComponentModel.DataAnnotations;

namespace Vector.Server.Entities
{
    public class NewsSource
    {
        [Key]
        public string SourceName { get; set; } = string.Empty;
        
        public double HistoricalBiasScore { get; set; }
        
        public string Description { get; set; } = string.Empty;
        
        public int ArticleCount { get; set; } = 0;
        
        public DateTime LastEvaluatedAt { get; set; } = DateTime.UtcNow;
    }
}
