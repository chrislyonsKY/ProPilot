using System.Threading;
using System.Threading.Tasks;
using ProPilot.Commands;

namespace ProPilot.Services;

/// <summary>
/// Abstraction over the LLM backend. Ollama implementation in v1,
/// designed for future provider extensibility.
/// </summary>
public interface ILlmClient
{
    /// <summary>
    /// Sends a natural language command to the LLM with map context,
    /// receives a structured ProPilotCommand JSON response.
    /// </summary>
    /// <param name="userInput">Raw natural language command from the user.</param>
    /// <param name="mapContext">Current map state snapshot.</param>
    /// <param name="cancellationToken">Cancellation token for timeout/user cancel.</param>
    /// <returns>Parsed command, or null if parsing failed.</returns>
    Task<ProPilotCommand?> ParseCommandAsync(
        string userInput,
        MapContext mapContext,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the LLM service is reachable and the configured model is available.
    /// </summary>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// Gets the currently configured model name.
    /// </summary>
    string ModelName { get; }
}

/// <summary>
/// Builds a snapshot of the current map state for LLM context injection.
/// Must be called on the MCT (QueuedTask.Run).
/// </summary>
public interface IMapContextBuilder
{
    /// <summary>
    /// Captures layers, fields, selections, extent, bookmarks, and symbology
    /// from the active map view.
    /// </summary>
    Task<MapContext> BuildContextAsync();
}

/// <summary>
/// Manages ProPilot settings persistence (project-level + user-level fallback).
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Loads settings from project custom properties, falling back to
    /// user-level settings, then hardcoded defaults.
    /// </summary>
    ProPilot.Models.ProPilotSettings LoadSettings();

    /// <summary>
    /// Saves settings to the specified scope.
    /// </summary>
    /// <param name="settings">Settings to save.</param>
    /// <param name="saveToProject">If true, save to project. If false, save to user defaults.</param>
    void SaveSettings(ProPilot.Models.ProPilotSettings settings, bool saveToProject = true);
}
