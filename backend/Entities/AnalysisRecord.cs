using System;
using System.ComponentModel.DataAnnotations;

namespace Vector.Server.Entities
{
    public class AnalysisRecord
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid? UserId { get; set; }

        [Required]
        [MaxLength(255)]
        public string SourceName { get; set; } = string.Empty;

        [Required]
        public string ArticleUrl { get; set; } = string.Empty;

        [Required]
        public string ArticleTitle { get; set; } = string.Empty;

        [Required]
        public double BiasScore { get; set; }

        [Required]
        [MaxLength(100)]
        public string BiasLabel { get; set; } = string.Empty;

        public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    }
}
