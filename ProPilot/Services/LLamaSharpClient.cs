using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using LLama;
using LLama.Common;
using ProPilot.Commands;
using ProPilot.Models;
using ProPilot.Prompts;

namespace ProPilot.Services;

/// <summary>
/// DEFAULT LLM provider — runs inference in-process via LLamaSharp.
/// Model is lazy-loaded on first command (never at Module.Initialize).
/// All inference runs on a background thread via Task.Run to avoid blocking MCT/UI.
/// </summary>
public class LLamaSharpClient : ILlmClient, IDisposable
{
    private readonly ProPilotSettings _settings;
    private readonly IModelManager _modelManager;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    private LLamaWeights? _weights;
    private StatelessExecutor? _executor;
    private ModelParams? _modelParams;
    private bool _isLoaded;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string ModelName => Path.GetFileNameWithoutExtension(
        _modelManager.GetActiveModelPath() ?? "no-model");

    public LLamaSharpClient(ProPilotSettings settings, IModelManager modelManager)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _modelManager = modelManager ?? throw new ArgumentNullException(nameof(modelManager));
    }

    /// <summary>
    /// Lazy-loads the GGUF model on first use. Thread-safe via SemaphoreSlim.
    /// Runs on a background thread to avoid blocking the UI or MCT.
    /// </summary>
    private async Task EnsureModelLoadedAsync()
    {
        if (_isLoaded) return;

        await _loadLock.WaitAsync();
        try
        {
            if (_isLoaded) return;

            var modelPath = _modelManager.GetActiveModelPath();
            if (modelPath == null || !File.Exists(modelPath))
                throw new InvalidOperationException(
                    "No model file found. Run first-time setup to download a model.");

            Debug.WriteLine($"[ProPilot] Loading model: {modelPath}");

            // Load on background thread — model loading is CPU-heavy
            await Task.Run(() =>
            {
                _modelParams = new ModelParams(modelPath)
                {
                    ContextSize = 2048,
                    GpuLayerCount = 0  // CPU only
                };

                _weights = LLamaWeights.LoadFromFile(_modelParams);
                _executor = new StatelessExecutor(_weights, _modelParams);
            });

            _isLoaded = true;
            Debug.WriteLine($"[ProPilot] Model loaded successfully: {modelPath}");
        }
        finally
        {
            _loadLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<ProPilotCommand?> ParseCommandAsync(
        string userInput, MapContext mapContext, CancellationToken cancellationToken = default)
    {
        await EnsureModelLoadedAsync();

        // Build the prompt with map context and command vocabulary
        var schemaElement = CommandSchemaProvider.GetSchema();
        var commandTypes = new List<string>();
        foreach (var item in schemaElement.GetProperty("properties")
            .GetProperty("command_type").GetProperty("enum").EnumerateArray())
        {
            commandTypes.Add(item.GetString()!);
        }

        var systemPrompt = SystemPromptBuilder.Build(mapContext, commandTypes);
        var fullPrompt = $"{systemPrompt}\nUser: {userInput}\nAssistant:";

        // Run inference on background thread — NEVER on UI thread or MCT
        var jsonResult = await Task.Run(async () =>
        {
            var inferenceParams = new InferenceParams
            {
                MaxTokens = 512,
                AntiPrompts = new List<string> { "User:" }
            };

            var sb = new StringBuilder();
            await foreach (var token in _executor!.InferAsync(fullPrompt, inferenceParams, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                sb.Append(token);
            }
            return sb.ToString().Trim();
        }, cancellationToken);

        Debug.WriteLine($"[ProPilot] LLamaSharp response: {jsonResult}");

        try
        {
            return JsonSerializer.Deserialize<ProPilotCommand>(jsonResult, JsonOptions);
        }
        catch (JsonException ex)
        {
            Debug.WriteLine($"[ProPilot] Failed to parse LLamaSharp JSON: {ex.Message}");
            Debug.WriteLine($"[ProPilot] Raw output: {jsonResult}");
            return null;
        }
    }

    /// <inheritdoc />
    public Task<bool> IsAvailableAsync()
    {
        try
        {
            var modelPath = _modelManager.GetActiveModelPath();
            return Task.FromResult(modelPath != null && File.Exists(modelPath));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ProPilot] LLamaSharp availability check failed: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    public void Dispose()
    {
        _weights?.Dispose();
        _loadLock.Dispose();
    }
}
