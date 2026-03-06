using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ProPilot.Commands;

// ─────────────────────────────────────────────────────────────
// ProPilotCommand — Deserialized from LLM JSON response
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Represents a parsed command returned by the LLM.
/// Deserialized from structured JSON output enforced by Ollama schema.
/// </summary>
public class ProPilotCommand
{
    [JsonPropertyName("command_type")]
    public string CommandType { get; set; } = "unknown";

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("target_layer")]
    public string? TargetLayer { get; set; }

    [JsonPropertyName("parameters")]
    public CommandParameters? Parameters { get; set; }

    [JsonPropertyName("human_description")]
    public string? HumanDescription { get; set; }
}

/// <summary>
/// Command-specific parameters. Not all fields apply to every command;
/// unused fields will be null.
/// </summary>
public class CommandParameters
{
    [JsonPropertyName("expression")]
    public string? Expression { get; set; }

    [JsonPropertyName("field_name")]
    public string? FieldName { get; set; }

    [JsonPropertyName("distance")]
    public double? Distance { get; set; }

    [JsonPropertyName("distance_unit")]
    public string? DistanceUnit { get; set; }

    [JsonPropertyName("color")]
    public string? Color { get; set; }

    [JsonPropertyName("value")]
    public double? Value { get; set; }

    [JsonPropertyName("source_layer")]
    public string? SourceLayer { get; set; }

    [JsonPropertyName("output_path")]
    public string? OutputPath { get; set; }

    [JsonPropertyName("output_format")]
    public string? OutputFormat { get; set; }

    [JsonPropertyName("visibility")]
    public bool? Visibility { get; set; }

    [JsonPropertyName("renderer_type")]
    public string? RendererType { get; set; }

    [JsonPropertyName("scale")]
    public double? Scale { get; set; }

    [JsonPropertyName("bookmark_name")]
    public string? BookmarkName { get; set; }

    [JsonPropertyName("coordinates")]
    public CoordinateParams? Coordinates { get; set; }

    [JsonPropertyName("spatial_relationship")]
    public string? SpatialRelationship { get; set; }

    [JsonPropertyName("position")]
    public string? Position { get; set; }

    [JsonPropertyName("reference_layer")]
    public string? ReferenceLayer { get; set; }

    [JsonPropertyName("layer_names")]
    public List<string>? LayerNames { get; set; }
}

public class CoordinateParams
{
    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("wkid")]
    public int Wkid { get; set; } = 4326;
}

// ─────────────────────────────────────────────────────────────
// CommandPreview — Rendered in the ProWindow preview panel
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Human-readable preview of a parsed command, displayed before execution.
/// </summary>
public class CommandPreview
{
    /// <summary>Emoji or icon identifier for the command category.</summary>
    public string Icon { get; set; } = "▶";

    /// <summary>Display title (e.g., "Zoom To Layer").</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Key-value pairs shown in the preview panel.</summary>
    public Dictionary<string, string> Parameters { get; set; } = new();

    /// <summary>Whether this command modifies data (shows warning).</summary>
    public bool IsDestructive { get; set; }

    /// <summary>LLM confidence score (0-1).</summary>
    public double Confidence { get; set; }
}

// ─────────────────────────────────────────────────────────────
// CommandResult — Returned after execution
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Result of executing a command.
/// </summary>
public class CommandResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    public static CommandResult Ok(string message) => new() { Success = true, Message = message };
    public static CommandResult Fail(string message) => new() { Success = false, Message = message };
}

// ─────────────────────────────────────────────────────────────
// ValidationResult
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Result of validating a parsed command against the current map context.
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }

    public static ValidationResult Success() => new() { IsValid = true };
    public static ValidationResult Fail(string message) => new() { IsValid = false, ErrorMessage = message };
}
