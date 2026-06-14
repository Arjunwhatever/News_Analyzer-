using System.Text.Json.Serialization;

namespace Vector.Server.Models
{
    public class AnalysisResult
    {
        /// <summary>
        /// Political bias score from -10 (far left) to +10 (far right).
        /// </summary>
        [JsonPropertyName("bias_score")]
        public double BiasScore { get; set; }

        /// <summary>
        /// Human-readable label for the score.
        /// </summary>
        [JsonPropertyName("bias_label")]
        public string BiasLabel { get; set; } = string.Empty;

        /// <summary>
        /// Confidence level of the analysis (0.0 - 1.0).
        /// </summary>
        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }

        /// <summary>
        /// Overall tone of the article.
        /// </summary>
        [JsonPropertyName("tone")]
        public string Tone { get; set; } = string.Empty;

        /// <summary>
        /// Key phrases or patterns that contributed to the score.
        /// </summary>
        [JsonPropertyName("key_indicators")]
        public List<string> KeyIndicators { get; set; } = new();

        /// <summary>
        /// A brief summary of the article's slant.
        /// </summary>
        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// The model used for analysis.
        /// </summary>
        [JsonPropertyName("model_used")]
        public string ModelUsed { get; set; } = string.Empty;

        /// <summary>
        /// Topics / themes detected in the article.
        /// </summary>
        [JsonPropertyName("topics")]
        public List<string> Topics { get; set; } = new();

        /// <summary>
        /// Relevance score to user preferences (0.0 - 100.0).
        /// </summary>
        [JsonPropertyName("relevance_score")]
        public double? RelevanceScore { get; set; }

        /// <summary>
        /// Returns the canonical label for a given bias score.
        /// </summary>
        public static string GetLabelForScore(double score) => score switch
        {
            <= -8 => "Far Left",
            <= -5 => "Left",
            <= -2 => "Center-Left",
            <= 2  => "Center / Neutral",
            <= 5  => "Center-Right",
            <= 8  => "Right",
            _     => "Far Right"
        };
    }

    public class OpenRouterResponse
    {
        [JsonPropertyName("choices")]
        public List<Choice>? Choices { get; set; }

        [JsonPropertyName("model")]
        public string? Model { get; set; }
    }

    public class Choice
    {
        [JsonPropertyName("message")]
        public Message? Message { get; set; }
    }

    public class Message
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("reasoning")]
        public string? Reasoning { get; set; }
    }

    /// <summary>
    /// Raw structured JSON that the LLM is expected to return.
    /// </summary>
    public class LlmAnalysisPayload
    {
        [JsonPropertyName("biasScore")]
        public double BiasScore { get; set; }

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }

        [JsonPropertyName("tone")]
        public string Tone { get; set; } = string.Empty;

        [JsonPropertyName("keyIndicators")]
        public List<string> KeyIndicators { get; set; } = new();

        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;

        [JsonPropertyName("topics")]
        public List<string> Topics { get; set; } = new();

        [JsonPropertyName("relevanceScore")]
        public double? RelevanceScore { get; set; }
    }
}
