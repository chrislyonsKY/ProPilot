using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProPilot.Commands;
using ProPilot.Services;

namespace ProPilot.ViewModels;

/// <summary>
/// ViewModel for the main ProPilot command window (ProWindow).
/// Orchestrates: user input → LLM parse → preview → execute.
/// </summary>
public partial class CommandWindowViewModel : ObservableObject
{
    private readonly ILlmClient _llmClient;
    private readonly IMapContextBuilder _contextBuilder;
    private readonly CommandRegistry _commandRegistry;
    private readonly CommandResolver _commandResolver;

    // ── Observable Properties ──────────────────────────────────────

    [ObservableProperty]
    private string _commandInput = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _modelName = "mistral";

    [ObservableProperty]
    private CommandPreview? _currentPreview;

    [ObservableProperty]
    private ProPilotCommand? _currentParsedCommand;

    [ObservableProperty]
    private IMapCommand? _currentCommand;

    public ObservableCollection<string> CommandHistory { get; } = new();

    public bool HasPreview => CurrentPreview != null;
    public bool HasHistory => CommandHistory.Count > 0;
    public string ConfidenceText => CurrentPreview != null ? $"{CurrentPreview.Confidence:P0}" : string.Empty;
    public IReadOnlyList<KeyValuePair<string, string>> PreviewParameters =>
        CurrentPreview?.Parameters?.ToList() ?? [];
    public SolidColorBrush ConnectionIndicatorColor =>
        IsConnected ? new SolidColorBrush(Colors.LimeGreen) : new SolidColorBrush(Colors.Gray);

    // ── Constructor ────────────────────────────────────────────────

    public CommandWindowViewModel(
        ILlmClient llmClient,
        IMapContextBuilder contextBuilder,
        CommandRegistry commandRegistry)
    {
        _llmClient = llmClient;
        _contextBuilder = contextBuilder;
        _commandRegistry = commandRegistry;
        _commandResolver = new CommandResolver(commandRegistry);
        ModelName = llmClient.ModelName;

        CommandHistory.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasHistory));

        _ = CheckConnectionAsync();
    }

    partial void OnCurrentPreviewChanged(CommandPreview? value)
    {
        OnPropertyChanged(nameof(HasPreview));
        OnPropertyChanged(nameof(ConfidenceText));
        OnPropertyChanged(nameof(PreviewParameters));
    }

    partial void OnIsConnectedChanged(bool value) => OnPropertyChanged(nameof(ConnectionIndicatorColor));

    // ── Commands ───────────────────────────────────────────────────

    [RelayCommand]
    private async Task SubmitCommandAsync()
    {
        if (string.IsNullOrWhiteSpace(CommandInput)) return;
        IsProcessing = true;
        StatusMessage = "Parsing command...";
        ClearPreview();
        try
        {
            var context = await _contextBuilder.BuildContextAsync();
            StatusMessage = "Waiting for LLM response...";
            var parsed = await _llmClient.ParseCommandAsync(CommandInput, context);
            if (parsed == null) { StatusMessage = "Failed to parse command. Try rephrasing."; return; }

            var (command, preview, validation) = _commandResolver.Resolve(parsed, context);
            if (!validation.IsValid) { StatusMessage = $"Validation error: {validation.ErrorMessage}"; return; }

            CurrentParsedCommand = parsed;
            CurrentCommand = command;
            CurrentPreview = preview;
            StatusMessage = "Ready to execute — review the preview below.";
            CommandHistory.Insert(0, CommandInput);
        }
        catch (Exception ex) { Debug.WriteLine($"[ProPilot] Submit failed: {ex.Message}"); StatusMessage = $"Error: {ex.Message}"; }
        finally { IsProcessing = false; }
    }

    [RelayCommand]
    private async Task ExecuteCommandAsync()
    {
        if (CurrentCommand == null || CurrentParsedCommand == null) return;
        IsProcessing = true;
        StatusMessage = "Executing...";
        try
        {
            var context = await _contextBuilder.BuildContextAsync();
            var result = await CurrentCommand.ExecuteAsync(CurrentParsedCommand, context);
            StatusMessage = result.Success ? $"✓ {result.Message}" : $"✗ {result.Message}";
            ClearPreview();
            CommandInput = string.Empty;
        }
        catch (Exception ex) { Debug.WriteLine($"[ProPilot] Execute failed: {ex.Message}"); StatusMessage = $"Execution error: {ex.Message}"; }
        finally { IsProcessing = false; }
    }

    [RelayCommand]
    private void CancelCommand() { ClearPreview(); StatusMessage = "Ready"; }

    [RelayCommand]
    private async Task CheckConnectionAsync()
    {
        try { IsConnected = await _llmClient.IsAvailableAsync(); ModelName = _llmClient.ModelName; StatusMessage = IsConnected ? "Ready" : "LLM not available — check settings."; }
        catch (Exception ex) { Debug.WriteLine($"[ProPilot] Connection check failed: {ex.Message}"); IsConnected = false; }
    }

    [RelayCommand]
    private void OpenSettings()
    {
        var settingsWindow = new UI.SettingsWindow();
        settingsWindow.Owner = ArcGIS.Desktop.Framework.FrameworkApplication.Current.MainWindow;
        settingsWindow.Show();
    }

    private void ClearPreview() { CurrentPreview = null; CurrentParsedCommand = null; CurrentCommand = null; }
}
