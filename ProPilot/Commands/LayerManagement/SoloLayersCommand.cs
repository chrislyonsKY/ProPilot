using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace ProPilot.Commands.LayerManagement;

public class SoloLayersCommand : IMapCommand
{
    public string CommandType => "solo_layers";
    public string DisplayName => "Solo Layers";
    public bool IsDestructive => false;

    public ValidationResult Validate(ProPilotCommand command, MapContext context)
    {
        var names = command.Parameters?.LayerNames;
        if (names == null || names.Count == 0)
        {
            if (string.IsNullOrEmpty(command.TargetLayer))
                return ValidationResult.Fail("No layers specified to solo.");
        }
        return ValidationResult.Success();
    }

    public CommandPreview BuildPreview(ProPilotCommand command, MapContext context)
    {
        var names = command.Parameters?.LayerNames ?? new List<string>();
        if (names.Count == 0 && !string.IsNullOrEmpty(command.TargetLayer))
            names = new List<string> { command.TargetLayer };

        return new CommandPreview
        {
            Icon = "??",
            Title = DisplayName,
            Parameters = new()
            {
                ["Solo"] = string.Join(", ", names),
                ["Hidden"] = $"All other layers ({context.Layers.Count - names.Count})"
            },
            Confidence = command.Confidence
        };
    }

    public async Task<CommandResult> ExecuteAsync(ProPilotCommand command, MapContext context)
    {
        var names = command.Parameters?.LayerNames ?? new List<string>();
        if (names.Count == 0 && !string.IsNullOrEmpty(command.TargetLayer))
            names = new List<string> { command.TargetLayer };

        return await QueuedTask.Run(() =>
        {
            var layers = MapView.Active?.Map?.GetLayersAsFlattenedList();
            if (layers == null) return CommandResult.Fail("No active map.");

            foreach (var layer in layers)
            {
                var shouldShow = names.Any(n =>
                    layer.Name.Equals(n, StringComparison.OrdinalIgnoreCase));
                layer.SetVisibility(shouldShow);
            }
            return CommandResult.Ok($"Soloed: {string.Join(", ", names)}. All other layers hidden.");
        });
    }
}
