using System;
using System.Diagnostics;
using ArcGIS.Desktop.Framework;
using ProPilot.Commands;
using ProPilot.Models;
using ProPilot.Services;

namespace ProPilot;

/// <summary>
/// ProPilot add-in module. Entry point for the add-in lifecycle.
/// Registers commands and initializes services on load.
/// </summary>
internal class ProPilotModule : Module
{
    private static ProPilotModule? _this;

    /// <summary>
    /// Singleton instance of the ProPilot module.
    /// </summary>
    public static ProPilotModule Current => _this ??=
        (ProPilotModule)FrameworkApplication.FindModule("ProPilot_Module");

    public CommandRegistry CommandRegistry { get; private set; } = null!;
    public ILlmClient LlmClient { get; private set; } = null!;
    public IMapContextBuilder ContextBuilder { get; private set; } = null!;
    public IModelManager ModelManager { get; private set; } = null!;
    public ISettingsService SettingsService { get; private set; } = null!;
    public ProPilotSettings Settings { get; private set; } = null!;

    /// <summary>
    /// Reloads settings and recreates the LLM client.
    /// Called after the user saves new settings in SettingsWindow.
    /// </summary>
    public void ReloadSettings()
    {
        try
        {
            if (LlmClient is IDisposable oldClient)
                oldClient.Dispose();

            Settings = SettingsService.LoadSettings();
            LlmClient = Settings.LlmProvider switch
            {
                "ollama" => new OllamaClient(Settings),
                "openai" => new OpenAiClient(Settings),
                _ => new LLamaSharpClient(Settings, ModelManager)
            };

            Debug.WriteLine($"[ProPilot] Settings reloaded — provider: {Settings.LlmProvider}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ProPilot] Failed to reload settings: {ex.Message}");
        }
    }

    protected override bool Initialize()
    {
        try
        {
            SettingsService = new SettingsService();
            Settings = SettingsService.LoadSettings();
            ModelManager = new ModelManager();

            // Select LLM provider based on settings
            LlmClient = Settings.LlmProvider switch
            {
                "ollama" => new OllamaClient(Settings),
                "openai" => new OpenAiClient(Settings),
                _ => new LLamaSharpClient(Settings, ModelManager) // "bundled" is the default
            };

            ContextBuilder = new MapContextBuilder();
            CommandRegistry = new CommandRegistry();
            CommandRegistry.RegisterBuiltInCommands();

            // NOTE: Do NOT load the model here — it's lazy-loaded on first command.
            // The first time the user clicks ProPilot, check ModelManager.HasLocalModel().
            // If false, show the SetupWindow ProWindow for model download.
            // If true, proceed to CommandWindow.

            Debug.WriteLine("[ProPilot] Module initialized successfully.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ProPilot] Module initialization failed: {ex.Message}");

            // Fallback to safe defaults so the module still loads
            Settings ??= new ProPilotSettings();
            ModelManager ??= new ModelManager();
            ContextBuilder ??= new MapContextBuilder();
            CommandRegistry ??= new CommandRegistry();
            LlmClient ??= new OllamaClient(Settings);
        }

        return base.Initialize();
    }

    protected override void Uninitialize()
    {
        if (LlmClient is IDisposable disposableClient)
            disposableClient.Dispose();

        Debug.WriteLine("[ProPilot] Module uninitialized.");
        base.Uninitialize();
    }
}
