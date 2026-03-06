using System;
using System.Threading.Tasks;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace ProPilot.Commands.LayerManagement;

public class AddLayerCommand : IMapCommand
{
    public string CommandType => "add_layer";
    public string DisplayName => "Add Layer";
    public bool IsDestructive => false;

    public ValidationResult Validate(ProPilotCommand command, MapContext context)
    {
        if (string.IsNullOrEmpty(command.Parameters?.OutputPath)
            && string.IsNullOrEmpty(command.TargetLayer))
            return ValidationResult.Fail("No data source path specified.");
        return ValidationResult.Success();
    }

    public CommandPreview BuildPreview(ProPilotCommand command, MapContext context)
    {
        var source = command.Parameters?.OutputPath ?? command.TargetLayer ?? "unknown";
        return new CommandPreview
        {
            Icon = "?", Title = DisplayName,
            Parameters = new() { ["Source"] = source },
            Confidence = command.Confidence
        };
    }

    public async Task<CommandResult> ExecuteAsync(ProPilotCommand command, MapContext context)
    {
        var source = command.Parameters?.OutputPath ?? command.TargetLayer;
        if (string.IsNullOrEmpty(source))
            return CommandResult.Fail("No data source path.");

        return await QueuedTask.Run(() =>
        {
            var map = MapView.Active?.Map;
            if (map == null) return CommandResult.Fail("No active map.");
            var uri = new Uri(source, UriKind.RelativeOrAbsolute);
            var layer = LayerFactory.Instance.CreateLayer(uri, map);
            if (layer == null)
                return CommandResult.Fail($"Failed to add layer from '{source}'.");
            return CommandResult.Ok($"Added layer '{layer.Name}'.");
        });
    }
}
