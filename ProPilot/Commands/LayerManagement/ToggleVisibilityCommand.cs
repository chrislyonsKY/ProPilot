using System;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace ProPilot.Commands.LayerManagement;

public class ToggleVisibilityCommand : IMapCommand
{
    public string CommandType => "toggle_visibility";
    public string DisplayName => "Toggle Visibility";
    public bool IsDestructive => false;

    public ValidationResult Validate(ProPilotCommand command, MapContext context)
    {
        if (string.IsNullOrEmpty(command.TargetLayer))
            return ValidationResult.Fail("No target layer specified.");
        if (!context.Layers.Any(l => l.Name.Equals(command.TargetLayer, StringComparison.OrdinalIgnoreCase)))
            return ValidationResult.Fail($"Layer '{command.TargetLayer}' not found.");
        return ValidationResult.Success();
    }

    public CommandPreview BuildPreview(ProPilotCommand command, MapContext context)
    {
        var info = context.Layers.First(l =>
            l.Name.Equals(command.TargetLayer!, StringComparison.OrdinalIgnoreCase));
        var newState = command.Parameters?.Visibility ?? !info.IsVisible;
        return new CommandPreview
        {
            Icon = newState ? "??" : "??",
            Title = DisplayName,
            Parameters = new()
            {
                ["Layer"] = info.Name,
                ["Current"] = info.IsVisible ? "Visible" : "Hidden",
                ["New State"] = newState ? "Visible" : "Hidden"
            },
            Confidence = command.Confidence
        };
    }

    public async Task<CommandResult> ExecuteAsync(ProPilotCommand command, MapContext context)
    {
        return await QueuedTask.Run(() =>
        {
            var layer = MapView.Active?.Map?.GetLayersAsFlattenedList()
                .FirstOrDefault(l => l.Name.Equals(command.TargetLayer, StringComparison.OrdinalIgnoreCase));
            if (layer == null) return CommandResult.Fail("Layer not found.");
            var newState = command.Parameters?.Visibility ?? !layer.IsVisible;
            layer.SetVisibility(newState);
            return CommandResult.Ok($"{layer.Name} is now {(newState ? "visible" : "hidden")}.");
        });
    }
}
