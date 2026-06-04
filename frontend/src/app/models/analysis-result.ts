/**
 * Structured result returned by the backend bias analysis endpoint.
 * Property names use snake_case to match the backend JSON serialization
 * (configured via [JsonPropertyName] attributes in AnalysisResult.cs).
 */
export interface AnalysisResult {
  bias_score: number;
  bias_label: string;
  confidence: number;
  tone: string;
  key_indicators: string[];
  summary: string;
  model_used: string;
  topics: string[];
}
