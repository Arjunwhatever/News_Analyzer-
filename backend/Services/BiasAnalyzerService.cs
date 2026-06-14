using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vector.Server.Models;

namespace Vector.Server.Services
{
    public class BiasAnalyzerService
    {
        // Google Gemma 4 31B — Free model with decent output 
        private const string Model = "google/gemma-4-31b-it:free";
        private const string ApiBase = "https://openrouter.ai/api/v1/chat/completions";

        private readonly HttpClient _http;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public BiasAnalyzerService(HttpClient http, IConfiguration configuration)
        {
            _http = http;
            var apiKey = configuration.GetValue<string>("AppSettings:OpenRouterApiKey") 
                ?? throw new InvalidOperationException("OpenRouter API key is not configured in AppSettings:OpenRouterApiKey.");

            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            _http.DefaultRequestHeaders.Add("HTTP-Referer", "https://github.com/political-bias-analyzer");
            _http.DefaultRequestHeaders.Add("X-Title", "Political Bias Analyzer");
            _http.Timeout = TimeSpan.FromSeconds(90);
        }

        // The core method that takes raw text, packages it up nicely, and sends it to the AI model
        public async Task<AnalysisResult> AnalyzeAsync(string articleText, string? userTopics = null)
        {
            var prompt = BuildPrompt(articleText);

            var requestBody = new
            {
                model = Model,
                messages = new[]
                {
                    new { role = "system", content = SystemPrompt(userTopics) },
                    new { role = "user",   content = prompt }
                },
                temperature = 0.2,   // Low temperature for consistent scoring
                max_tokens  = 800
            };

            var json    = JsonSerializer.Serialize(requestBody, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync(ApiBase, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"OpenRouter returned {(int)response.StatusCode} {response.ReasonPhrase}. " +
                    $"Details: {errorBody}");
            }

            // Grab the JSON string the AI spat back and parse it into our clean C# object
            var responseJson = await response.Content.ReadAsStringAsync();
            return ParseResponse(responseJson);
        }

       
        // Private helpers
       

        private static string SystemPrompt(string? userTopics)
        {
            var relevanceInstruction = string.IsNullOrWhiteSpace(userTopics)
                ? ""
                : $"\n            The user is interested in these topics: {userTopics}. Evaluate how relevant this article is to their interests and assign a relevanceScore from 0 to 100.";

            return $$"""
            You are an expert political media analyst. Your job is to evaluate the political bias
            of news articles and produce structured analysis in JSON format only.

            Scoring guidelines (bias_score):
              -10 to -8  : Far-left / socialist / progressive extremism
              -7  to -5  : Strong left-wing / liberal framing
              -4  to -2  : Moderate left / center-left lean
              -1  to +1  : Neutral / balanced / factual
              +2  to +4  : Moderate right / center-right lean
              +5  to +7  : Strong right-wing / conservative framing
              +8  to +10 : Far-right / nationalist / reactionary

            Signals to look for:
              • Language choices (e.g. "undocumented" vs "illegal")
              • Which voices are quoted and how many from each side
              • Framing of policy debates
              • Emotional vs factual tone
              • Omissions that favour one side
              • Loaded adjectives and adverbs

            IMPORTANT: Respond ONLY with valid JSON. Do not include markdown fences, preamble, or
            any text outside the JSON object. Use exactly this schema:
            {
              "biasScore": <number -10 to 10, one decimal place>,
              "confidence": <number 0.0 to 1.0>,
              "tone": "<Analytical | Emotional | Sensational | Neutral | Partisan>",
              "keyIndicators": ["<indicator 1>", "<indicator 2>", ...],
              "summary": "<2-3 sentence plain-English explanation of the bias>",
              "topics": ["<topic 1>", "<topic 2>", ...],
              "relevanceScore": <number 0 to 100>
            }{{relevanceInstruction}}
            """;
        }

        private static string BuildPrompt(string articleText)
        {
            // Truncate very long articles to keep token usage reasonable
            const int maxChars = 8000;
            var truncated = articleText.Length > maxChars
                ? articleText[..maxChars] + "\n\n[Article truncated for analysis]"
                : articleText;

            return $"Analyze the political bias of the following news article:\n\n{truncated}";
        }

        // Deserializes the raw LLM response string into our structured AnalysisResult object
        private AnalysisResult ParseResponse(string responseJson)
        {
            var openRouterResp = JsonSerializer.Deserialize<OpenRouterResponse>(responseJson, _jsonOptions)
                ?? throw new InvalidOperationException("Empty response from API.");

            var message = openRouterResp.Choices?.FirstOrDefault()?.Message;

            // Primary: read from content. Fallback: read from reasoning
            // (some free models are reasoning-only and put output in the reasoning field)
            var rawContent = message?.Content
                ?? message?.Reasoning
                ?? throw new InvalidOperationException(
                    $"No message content in API response. Raw JSON: {responseJson[..Math.Min(500, responseJson.Length)]}");

            // Strip potential markdown fences the model might include despite instructions
            var cleaned = StripMarkdownFences(rawContent.Trim());

            LlmAnalysisPayload payload;
            try
            {
                payload = JsonSerializer.Deserialize<LlmAnalysisPayload>(cleaned, _jsonOptions)
                    ?? throw new InvalidOperationException("Null payload after deserialization.");
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    $"Model returned non-JSON content. Raw response:\n{rawContent}\n\nParse error: {ex.Message}");
            }

            // Clamp score to valid range
            var score = Math.Clamp(Math.Round(payload.BiasScore, 1), -10.0, 10.0);

            return new AnalysisResult
            {
                BiasScore     = score,
                BiasLabel     = AnalysisResult.GetLabelForScore(score),
                Confidence    = Math.Clamp(payload.Confidence, 0.0, 1.0),
                Tone          = payload.Tone,
                KeyIndicators = payload.KeyIndicators,
                Summary       = payload.Summary,
                Topics        = payload.Topics,
                RelevanceScore = payload.RelevanceScore,
                ModelUsed     = Model
            };
        }

        private static string StripMarkdownFences(string text)
        {
            if (text.StartsWith("```"))
            {
                var firstNewline = text.IndexOf('\n');
                if (firstNewline >= 0) text = text[(firstNewline + 1)..];
            }
            if (text.EndsWith("```"))
                text = text[..^3].TrimEnd();
            return text.Trim();
        }
    }
}
