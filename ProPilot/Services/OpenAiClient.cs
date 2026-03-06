using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
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
/// HTTP client for the OpenAI API (GPT-4o-mini, GPT-4o, etc.).
/// Secondary LLM provider — user opts in via Settings window.
/// API key is read from the OPENAI_API_KEY environment variable.
/// </summary>
public class OpenAiClient : ILlmClient
{
    private readonly HttpClient _httpClient;
    private readonly ProPilotSettings _settings;
    private const string BaseUrl = "https://api.openai.com/v1/chat/completions";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string ModelName => _settings.OpenAiModelName;

    public OpenAiClient(ProPilotSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException(
                "OPENAI_API_KEY environment variable is not set. " +
                "Set it via: setx OPENAI_API_KEY \"your-key-here\" " +
                "or use the Ollama provider instead (no API key required).");
        }

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds)
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);
    }

    /// <inheritdoc />
    public async Task<ProPilotCommand?> ParseCommandAsync(
        string userInput,
        MapContext mapContext,
        CancellationToken cancellationToken = default)
    {
        try
        {
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
                model = _settings.OpenAiModelName,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userInput }
                },
                response_format = new
                {
                    type = "json_schema",
                    json_schema = new
                    {
                        name = "ProPilotCommand",
                        strict = true,
                        schema = schemaElement
                    }
                },
                temperature = 0
            };

            var response = await _httpClient.PostAsJsonAsync(
                BaseUrl, request, JsonOptions, cancellationToken);
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
                Debug.WriteLine("[ProPilot] OpenAI returned empty content.");
                return null;
            }

            Debug.WriteLine($"[ProPilot] OpenAI response: {content}");
            return JsonSerializer.Deserialize<ProPilotCommand>(content, JsonOptions);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ProPilot] OpenAI ParseCommand failed: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("https://api.openai.com/v1/models");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ProPilot] OpenAI health check failed: {ex.Message}");
            return false;
        }
    }
}
