using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ProPilot.Commands;
using ProPilot.Models;
using ProPilot.Prompts;

namespace ProPilot.Services;

/// <summary>
/// HTTP client for the Ollama local LLM API.
/// Sends natural language commands with map context and receives
/// structured JSON responses constrained by the ProPilotCommand schema.
/// </summary>
public class OllamaClient : ILlmClient
{
    private readonly HttpClient _httpClient;
    private readonly ProPilotSettings _settings;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string ModelName => _settings.ModelName;

    public OllamaClient(ProPilotSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_settings.OllamaEndpoint),
            Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds)
        };
    }

    /// <inheritdoc />
    public async Task<ProPilotCommand?> ParseCommandAsync(
        string userInput,
        MapContext mapContext,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Build system prompt — use command types from the schema enum
            var schemaElement = CommandSchemaProvider.GetSchema();
            var commandTypes = schemaElement
                .GetProperty("properties")
                .GetProperty("command_type")
                .GetProperty("enum")
                .EnumerateArray()
                .Select(e => e.GetString()!)
                .ToList();

            var systemPrompt = SystemPromptBuilder.Build(mapContext, commandTypes);

            var request = new
            {
                model = _settings.ModelName,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userInput }
                },
                format = schemaElement,
                temperature = 0,
                stream = false
            };

            var response = await _httpClient.PostAsJsonAsync(
                "/v1/chat/completions", request, JsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadFromJsonAsync<JsonElement>(
                cancellationToken: cancellationToken);

            var content = responseJson
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(content))
            {
                Debug.WriteLine("[ProPilot] Ollama returned empty content.");
                return null;
            }

            Debug.WriteLine($"[ProPilot] Ollama response: {content}");
            return JsonSerializer.Deserialize<ProPilotCommand>(content, JsonOptions);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ProPilot] Ollama ParseCommand failed: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/tags");
            if (!response.IsSuccessStatusCode) return false;

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            if (json.TryGetProperty("models", out var models))
            {
                foreach (var model in models.EnumerateArray())
                {
                    if (model.TryGetProperty("name", out var name) &&
                        name.GetString()?.Contains(_settings.ModelName, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        return true;
                    }
                }
            }

            Debug.WriteLine($"[ProPilot] Ollama is running but model '{_settings.ModelName}' not found.");
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ProPilot] Ollama health check failed: {ex.Message}");
            return false;
        }
    }
}
