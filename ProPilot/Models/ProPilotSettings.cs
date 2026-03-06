namespace ProPilot.Models;

/// <summary>
/// Persisted settings for ProPilot. Stored per-project (custom properties)
/// with fallback to per-user (%APPDATA%\ProPilot\settings.json).
/// </summary>
public class ProPilotSettings
{
    /// <summary>Ollama API endpoint URL.</summary>
    public string OllamaEndpoint { get; set; } = "http://localhost:11434";

    /// <summary>Model name to use for command parsing.</summary>
    public string ModelName { get; set; } = "mistral";

    /// <summary>Request timeout in seconds.</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>Whether to persist command history across sessions.</summary>
    public bool SaveHistoryAcrossSessions { get; set; } = false;

    /// <summary>LLM provider: "bundled" (default), "ollama", or "openai".</summary>
    public string LlmProvider { get; set; } = "bundled";

    /// <summary>OpenAI model name (only used when LlmProvider = "openai").</summary>
    public string OpenAiModelName { get; set; } = "gpt-4o-mini";
}
