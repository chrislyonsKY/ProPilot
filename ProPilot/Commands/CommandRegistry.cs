using System;
using System.Collections.Generic;
using System.Linq;

namespace ProPilot.Commands;

/// <summary>
/// Maps command_type strings from the LLM JSON response to concrete
/// IMapCommand implementations. All commands register here at startup.
/// </summary>
public class CommandRegistry
{
    private readonly Dictionary<string, IMapCommand> _commands = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a command implementation.
    /// </summary>
    public void Register(IMapCommand command)
    {
        _commands[command.CommandType] = command;
    }

    /// <summary>
    /// Resolves a command_type string to its IMapCommand implementation.
    /// Returns null if no command is registered for the given type.
    /// </summary>
    public IMapCommand? Resolve(string commandType)
    {
        return _commands.TryGetValue(commandType, out var command) ? command : null;
    }

    /// <summary>
    /// Gets all registered command types (for LLM system prompt injection).
    /// </summary>
    public IReadOnlyList<string> GetRegisteredTypes() => _commands.Keys.ToList();

    /// <summary>
    /// Registers all built-in command implementations.
    /// Called once at add-in startup.
    /// </summary>
    public void RegisterBuiltInCommands()
    {
        // Navigation
        Register(new Navigation.ZoomToLayerCommand());
        Register(new Navigation.ZoomToSelectionCommand());
        Register(new Navigation.ZoomToFullExtentCommand());
        Register(new Navigation.PanToCoordinatesCommand());
        Register(new Navigation.SetScaleCommand());
        Register(new Navigation.GoToBookmarkCommand());

        // Layer Management
        Register(new LayerManagement.ToggleVisibilityCommand());
        Register(new LayerManagement.SoloLayersCommand());
        Register(new LayerManagement.SetTransparencyCommand());
        Register(new LayerManagement.ReorderLayerCommand());
        Register(new LayerManagement.RemoveLayerCommand());
        Register(new LayerManagement.AddLayerCommand());

        // Selection
        Register(new Selection.SelectByAttributeCommand());
        Register(new Selection.SelectByLocationCommand());
        Register(new Selection.ClearSelectionCommand());
        Register(new Selection.SelectAllCommand());
        Register(new Selection.InvertSelectionCommand());

        // Symbology
        Register(new Symbology.ChangeColorCommand());
        Register(new Symbology.SetLineWidthCommand());
        Register(new Symbology.SetPointSizeCommand());
        Register(new Symbology.ChangeRendererCommand());
        Register(new Symbology.ToggleLabelsCommand());

        // Query
        Register(new Query.SetDefinitionQueryCommand());
        Register(new Query.ClearDefinitionQueryCommand());
        Register(new Query.GetFeatureCountCommand());
        Register(new Query.ListFieldsCommand());

        // Geoprocessing
        Register(new Geoprocessing.BufferCommand());
        Register(new Geoprocessing.ClipCommand());
        Register(new Geoprocessing.ExportDataCommand());
        Register(new Geoprocessing.DissolveCommand());
        Register(new Geoprocessing.MergeCommand());
    }
}

/// <summary>
/// Resolves a parsed ProPilotCommand to a concrete IMapCommand,
/// validates it against the current map context, and builds a preview.
/// </summary>
public class CommandResolver
{
    private readonly CommandRegistry _registry;

    public CommandResolver(CommandRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <summary>
    /// Resolves, validates, and builds a preview for a parsed command.
    /// </summary>
    /// <param name="parsed">Parsed command from the LLM.</param>
    /// <param name="context">Current map state.</param>
    /// <returns>Tuple of (command, preview, validation). Command may be null if unresolved.</returns>
    public (IMapCommand? Command, CommandPreview? Preview, ValidationResult Validation) Resolve(
        ProPilotCommand parsed, MapContext context)
    {
        var command = _registry.Resolve(parsed.CommandType);
        if (command == null)
        {
            return (null, null, ValidationResult.Fail(
                $"Unknown command type: '{parsed.CommandType}'"));
        }

        var validation = command.Validate(parsed, context);
        if (!validation.IsValid)
        {
            return (command, null, validation);
        }

        var preview = command.BuildPreview(parsed, context);
        return (command, preview, ValidationResult.Success());
    }
}
