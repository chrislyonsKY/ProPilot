using System.Threading.Tasks;

namespace ProPilot.Commands;

/// <summary>
/// Contract for all executable map commands in ProPilot.
/// Separates preview (read-only) from execution (mutating) to enable
/// the always-preview model.
/// </summary>
public interface IMapCommand
{
    /// <summary>
    /// Unique command type identifier matching the LLM JSON schema enum.
    /// Example: "zoom_to_layer", "select_by_attribute"
    /// </summary>
    string CommandType { get; }

    /// <summary>
    /// Human-readable name for display in the preview panel.
    /// Example: "Zoom To Layer", "Select By Attribute"
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Whether this command modifies data or map structure.
    /// Triggers a warning icon in the preview panel.
    /// </summary>
    bool IsDestructive { get; }

    /// <summary>
    /// Validates that the parsed command has all required parameters
    /// and that referenced layers/fields exist in the current map context.
    /// </summary>
    /// <param name="command">Parsed command from the LLM.</param>
    /// <param name="context">Current map state snapshot.</param>
    /// <returns>Validation result with error messages if invalid.</returns>
    ValidationResult Validate(ProPilotCommand command, MapContext context);

    /// <summary>
    /// Builds a human-readable preview of what this command will do.
    /// This method is pure read-only — it MUST NOT modify the map.
    /// </summary>
    /// <param name="command">Parsed command from the LLM.</param>
    /// <param name="context">Current map state snapshot.</param>
    /// <returns>Preview data for display in the ProWindow.</returns>
    CommandPreview BuildPreview(ProPilotCommand command, MapContext context);

    /// <summary>
    /// Executes the command against the active map.
    /// SDK commands must be called within QueuedTask.Run().
    /// GP commands use Geoprocessing.ExecuteToolAsync().
    /// </summary>
    /// <param name="command">Parsed command from the LLM.</param>
    /// <param name="context">Current map state snapshot.</param>
    /// <returns>Execution result with success/failure and message.</returns>
    Task<CommandResult> ExecuteAsync(ProPilotCommand command, MapContext context);
}
