using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace ProPilot.Prompts;

/// <summary>
/// Assembles the LLM system prompt from the command vocabulary,
/// current map context, and parsing rules. Rebuilt fresh per request.
/// </summary>
public static class SystemPromptBuilder
{
    /// <summary>
    /// Builds the complete system prompt including command definitions
    /// and current map context.
    /// </summary>
    public static string Build(Commands.MapContext context, IReadOnlyList<string> registeredCommands)
    {
        var sb = new StringBuilder();

        // Core identity
        sb.AppendLine("You are ProPilot, a command parser for ArcGIS Pro.");
        sb.AppendLine("You receive natural language commands from the user and output a single JSON object matching the ProPilotCommand schema.");
        sb.AppendLine();

        // Rules
        sb.AppendLine("RULES:");
        sb.AppendLine("- Output ONLY valid JSON. No explanation, no markdown, no conversation.");
        sb.AppendLine("- Resolve layer names fuzzily (case-insensitive, partial match). Pick the closest match from the available layers.");
        sb.AppendLine("- NEVER invent layer names or field names that are not in the map context below.");
        sb.AppendLine("- Set confidence between 0.0 and 1.0 based on how well the input matches a command.");
        sb.AppendLine("- If the command is ambiguous or unclear, set confidence below 0.5 and use command_type \"unknown\".");
        sb.AppendLine("- Always include human_description summarizing what the command will do.");
        sb.AppendLine();

        // Available commands
        sb.AppendLine("AVAILABLE COMMANDS:");
        foreach (var cmd in registeredCommands)
        {
            sb.AppendLine($"  - {cmd}");
        }
        sb.AppendLine();

        // Map context
        sb.AppendLine("CURRENT MAP CONTEXT:");
        sb.AppendLine($"  Map: {context.MapName}");
        sb.AppendLine($"  Scale: 1:{context.Scale:N0}");
        sb.AppendLine($"  Spatial Reference WKID: {context.SpatialReferenceWkid}");

        if (context.Extent != null)
        {
            sb.AppendLine($"  Extent: ({context.Extent.XMin:F4}, {context.Extent.YMin:F4}) - ({context.Extent.XMax:F4}, {context.Extent.YMax:F4})");
        }

        sb.AppendLine();

        // Layers
        if (context.Layers.Count > 0)
        {
            sb.AppendLine("LAYERS:");
            foreach (var layer in context.Layers)
            {
                var vis = layer.IsVisible ? "visible" : "hidden";
                var geom = string.IsNullOrEmpty(layer.GeometryType) ? "" : $" ({layer.GeometryType})";
                var sel = layer.SelectedCount > 0 ? $", {layer.SelectedCount} selected" : "";
                var defQ = !string.IsNullOrEmpty(layer.DefinitionQuery) ? $", query: {layer.DefinitionQuery}" : "";

                sb.AppendLine($"  - {layer.Name}{geom}: {layer.FeatureCount:N0} features, {vis}{sel}{defQ}");

                if (layer.FieldNames.Count > 0)
                {
                    sb.AppendLine($"    Fields: {string.Join(", ", layer.FieldNames.Take(20))}");
                    if (layer.FieldNames.Count > 20)
                        sb.AppendLine($"    ... and {layer.FieldNames.Count - 20} more fields");
                }
            }
            sb.AppendLine();
        }

        // Bookmarks
        if (context.Bookmarks.Count > 0)
        {
            sb.AppendLine("BOOKMARKS:");
            foreach (var bookmark in context.Bookmarks)
            {
                sb.AppendLine($"  - {bookmark}");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }
}

/// <summary>
/// Provides the JSON schema for the ProPilotCommand structure.
/// Passed to Ollama's format parameter to constrain LLM output.
/// </summary>
public static class CommandSchemaProvider
{
    private static readonly string SchemaJson = """
    {
      "type": "object",
      "required": ["command_type", "confidence"],
      "properties": {
        "command_type": {
          "type": "string",
          "enum": [
            "zoom_to_layer", "zoom_to_selection", "zoom_to_full_extent",
            "pan_to_coordinates", "set_scale", "go_to_bookmark",
            "toggle_visibility", "solo_layers", "set_transparency",
            "reorder_layer", "remove_layer", "add_layer",
            "select_by_attribute", "select_by_location", "clear_selection",
            "select_all", "invert_selection",
            "change_color", "set_line_width", "set_point_size",
            "change_renderer", "toggle_labels",
            "set_definition_query", "clear_definition_query",
            "get_feature_count", "list_fields",
            "buffer", "clip", "export_data", "dissolve", "merge",
            "unknown"
          ]
        },
        "confidence": {
          "type": "number",
          "minimum": 0,
          "maximum": 1
        },
        "target_layer": {
          "type": ["string", "null"]
        },
        "parameters": {
          "type": ["object", "null"],
          "properties": {
            "expression": { "type": ["string", "null"] },
            "field_name": { "type": ["string", "null"] },
            "distance": { "type": ["number", "null"] },
            "distance_unit": { "type": ["string", "null"] },
            "color": { "type": ["string", "null"] },
            "value": { "type": ["number", "null"] },
            "source_layer": { "type": ["string", "null"] },
            "output_path": { "type": ["string", "null"] },
            "output_format": { "type": ["string", "null"] },
            "visibility": { "type": ["boolean", "null"] },
            "renderer_type": { "type": ["string", "null"] },
            "scale": { "type": ["number", "null"] },
            "bookmark_name": { "type": ["string", "null"] },
            "coordinates": {
              "type": ["object", "null"],
              "properties": {
                "x": { "type": "number" },
                "y": { "type": "number" },
                "wkid": { "type": "integer" }
              }
            },
            "spatial_relationship": { "type": ["string", "null"] },
            "position": { "type": ["string", "null"] },
            "reference_layer": { "type": ["string", "null"] },
            "layer_names": {
              "type": ["array", "null"],
              "items": { "type": "string" }
            }
          }
        },
        "human_description": {
          "type": ["string", "null"]
        }
      },
      "additionalProperties": false
    }
    """;

    /// <summary>
    /// Returns the JSON schema object for the ProPilotCommand,
    /// suitable for passing as the "format" parameter to Ollama.
    /// </summary>
    public static JsonElement GetSchema()
    {
        return JsonSerializer.Deserialize<JsonElement>(SchemaJson);
    }

    /// <summary>
    /// Returns the raw JSON schema string.
    /// </summary>
    public static string GetSchemaString() => SchemaJson;
}
