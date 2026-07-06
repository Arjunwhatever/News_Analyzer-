using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vector.Server.Models;

namespace Vector.Server.Services
{
    public class BiasAnalyzerService
    {
        // Use Gemini 3.5 Flash natively via Google's own API
        private const string Model = "gemini-3.5-flash";
        private const string ApiBase = "https://generativelanguage.googleapis.com/v1beta/models";

        private readonly HttpClient _http;
        private readonly ILogger<BiasAnalyzerService> _logger;
        private readonly string _apiKey;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public BiasAnalyzerService(HttpClient http, IConfiguration configuration, ILogger<BiasAnalyzerService> logger)
        {
            _http = http;
            _logger = logger;
            _apiKey = configuration.GetValue<string>("AppSettings:GoogleApiKey") 
                ?? throw new InvalidOperationException("Google API key is not configured in AppSettings:GoogleApiKey.");

            _http.Timeout = TimeSpan.FromSeconds(90);
        }

        // The core method that takes raw text, packages it up nicely, and sends it to the AI model
        public async Task<AnalysisResult> AnalyzeAsync(string articleText, string? userTopics = null)
        {
            var prompt = BuildPrompt(articleText);

            var requestBody = new
            {
                systemInstruction = new 
                {
                    parts = new[] { new { text = SystemPrompt(userTopics) } }
                },
                contents = new[]
                {
                    new 
                    {
                        parts = new[] { new { text = prompt } }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.3,
                    maxOutputTokens = 4000,
                    responseMimeType = "application/json"
                }
            };
            var json    = JsonSerializer.Serialize(requestBody, _jsonOptions);
            // Attempt to analyze the article using a list of fallback models.
            // If a higher-tier model (e.g. gemini-3.5-flash) is experiencing high demand (503 Service Unavailable),
            // the system will automatically fall back to the next model (e.g. gemini-1.5-flash).
            var modelsToTry = new[] { "gemini-3.5-flash", "gemini-1.5-flash" };

            // We allow up to 2 retries per model to handle transient network issues or temporary server-side spikes.
            int maxRetries = 2; // per model

            foreach (var currentModel in modelsToTry)
            {
                var url = $"{ApiBase}/{currentModel}:generateContent?key={_apiKey}";
                
                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    try
                    {
                        var response = await _http.PostAsync(url, content);

                        if (response.IsSuccessStatusCode)
                        {
                            var tempJson = await response.Content.ReadAsStringAsync();
                            try
                            {
                                var result = ParseResponse(tempJson, currentModel);
                                _logger.LogInformation("Successfully parsed LLM Response from {Model}", currentModel);
                                return result;
                            }
                            catch (InvalidOperationException ex)
                            {
                                _logger.LogWarning("Model {Model} returned invalid JSON: {Error}", currentModel, ex.Message);
                                if (attempt == maxRetries)
                                    break; // give up on this model if JSON is consistently invalid
                                
                                await Task.Delay(1000 * attempt);
                                continue;
                            }
                        }

                        var errorBody = await response.Content.ReadAsStringAsync();
                        _logger.LogError("Model {Model} failed with {StatusCode} on attempt {Attempt}: {ErrorBody}", currentModel, response.StatusCode, attempt, errorBody);
                        
                        // 400 Bad Request indicates a permanent error with the payload format. 
                        // Retrying won't fix this, so we break out of the retry loop for this model.
                        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                        {
                            break; // Bad request won't fix itself on retry, maybe next model works
                        }
                        
                        // If we've exhausted all retries for the current model, break and try the next fallback model.
                        if (attempt == maxRetries)
                        {
                            break; // Move to next model
                        }
                        
                        await Task.Delay(1000 * attempt);
                    }
                    catch (TaskCanceledException)
                    {
                        _logger.LogWarning("Model {Model} timed out on attempt {Attempt}.", currentModel, attempt);
                        if (attempt == maxRetries)
                        {
                            break; // Move to next model
                        }
                        await Task.Delay(1000 * attempt);
                    }
                }
            }

            throw new HttpRequestException("Gemini failed to process the request after trying all fallback models and retries.");
        }

        public async Task<(double biasScore, string description)> AnalyzeSourceHistoricalBiasAsync(string sourceName)
        {
            var prompt = $"Based on your general knowledge, what is the established historical political bias of the news publisher '{sourceName}'? Provide a very brief 1-sentence description and a bias score from -10 to 10 (where -10 is far-left, 0 is neutral, and 10 is far-right). Respond ONLY in valid JSON with this exact schema:\n{{\n  \"biasScore\": <number>,\n  \"description\": \"<1 sentence description>\"\n}}";


            var requestBody = new
            {
                contents = new[]
                {
                    new 
                    {
                        parts = new[] { new { text = prompt } }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.3,
                    maxOutputTokens = 4000,
                    responseMimeType = "application/json"
                }
            };

            var json = JsonSerializer.Serialize(requestBody, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = $"{ApiBase}/{Model}:generateContent?key={_apiKey}";

            try
            {
                var response = await _http.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var tempJson = await response.Content.ReadAsStringAsync();
                    try
                    {
                        var geminiResp = JsonSerializer.Deserialize<JsonElement>(tempJson, _jsonOptions);
                        var rawContent = geminiResp
                            .GetProperty("candidates")[0]
                            .GetProperty("content")
                            .GetProperty("parts")[0]
                            .GetProperty("text")
                            .GetString() ?? "{}";

                        var cleaned = StripMarkdownFences(rawContent.Trim());
                        var parsed = JsonSerializer.Deserialize<JsonElement>(cleaned, _jsonOptions);
                        var score = parsed.TryGetProperty("biasScore", out var scoreProp) ? scoreProp.GetDouble() : 0.0;
                        var desc = parsed.TryGetProperty("description", out var descProp) ? descProp.GetString() : "Established bias";
                        return (Math.Clamp(Math.Round(score, 1), -10.0, 10.0), desc ?? "Established bias");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Model {Model} returned invalid JSON for source history: {Error}", Model, ex.Message);
                    }
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("Model {Model} timed out for source history.", Model);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning("Model {Model} threw HTTP exception for source history: {Error}", Model, ex.Message);
            }

            _logger.LogWarning("Failed to evaluate source {SourceName}. Returning 0.0", sourceName);
            return (0.0, "Established bias");
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

            CRITICAL TOPIC INSTRUCTION:
            For the "topics" array, NEVER use broad categories like 'Politics', 'Art', 'History', or 'News'.
            You MUST use highly specific proper nouns, names of people, specific locations (e.g., 'Gaza'), or precise event names.

            Scoring guidelines (biasScore):
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
              "topics": ["<Proper noun 1 (e.g. 'Gaza')>", "<Proper noun 2 (e.g. 'Supreme Court')>"], // NEVER use broad categories like 'Politics' or 'History'
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
        private AnalysisResult ParseResponse(string responseJson, string modelUsed)
        {
            var geminiResp = JsonSerializer.Deserialize<JsonElement>(responseJson, _jsonOptions);
            string rawContent;
            
            try 
            {
                rawContent = geminiResp
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? "{}";
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to extract text from Gemini response. Raw JSON: {responseJson[..Math.Min(500, responseJson.Length)]}. Error: {ex.Message}");
            }

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
                ModelUsed     = modelUsed
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
