using System;
using System.Collections.Generic;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProPilot.Models;
using ProPilot.Services;

namespace ProPilot.ViewModels;

/// <summary>
/// ViewModel for the ProPilot settings ProWindow.
/// </summary>
public partial class SettingsWindowViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty] private string _llmProvider = "bundled";
    [ObservableProperty] private string _ollamaEndpoint = "http://localhost:11434";
    [ObservableProperty] private string _modelName = "mistral";
    [ObservableProperty] private int _timeoutSeconds = 30;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public List<string> AvailableProviders { get; } = ["bundled", "ollama", "openai"];

    public SettingsWindowViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;

        var settings = _settingsService.LoadSettings();
        LlmProvider = settings.LlmProvider;
        OllamaEndpoint = settings.OllamaEndpoint;
        ModelName = settings.ModelName;
        TimeoutSeconds = settings.TimeoutSeconds;
    }

    [RelayCommand]
    private void SaveToProject()
    {
        var settings = BuildSettings();
        _settingsService.SaveSettings(settings, saveToProject: true);
        ProPilotModule.Current.ReloadSettings();
        StatusMessage = "? Settings saved to project. Provider reloaded.";
    }

    [RelayCommand]
    private void SaveToUserDefaults()
    {
        var settings = BuildSettings();
        _settingsService.SaveSettings(settings, saveToProject: false);
        ProPilotModule.Current.ReloadSettings();
        StatusMessage = "? Settings saved as user defaults. Provider reloaded.";
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task TestConnection()
    {
        StatusMessage = "Testing connection...";
        try
        {
            var settings = BuildSettings();
            ILlmClient testClient = settings.LlmProvider switch
            {
                "ollama" => new OllamaClient(settings),
                "openai" => new OpenAiClient(settings),
                _ => throw new InvalidOperationException(
                    "Bundled provider does not require a connection test.")
            };

            var available = await testClient.IsAvailableAsync();
            StatusMessage = available
                ? "? Connection successful!"
                : "? Connection failed — check endpoint and model name.";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ProPilot] Connection test failed: {ex.Message}");
            StatusMessage = $"? {ex.Message}";
        }
    }

    private ProPilotSettings BuildSettings() => new()
    {
        LlmProvider = LlmProvider,
        OllamaEndpoint = OllamaEndpoint,
        ModelName = ModelName,
        TimeoutSeconds = TimeoutSeconds
    };
}
