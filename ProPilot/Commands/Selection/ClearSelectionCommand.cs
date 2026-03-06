using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace ProPilot.Commands.Selection;

public class ClearSelectionCommand : IMapCommand
{
    public string CommandType => "clear_selection";
    public string DisplayName => "Clear Selection";
    public bool IsDestructive => false;

    public ValidationResult Validate(ProPilotCommand command, MapContext context)
    {
        return ValidationResult.Success();
    }

    public CommandPreview BuildPreview(ProPilotCommand command, MapContext context)
    {
        var totalSelected = context.Layers.Sum(l => l.SelectedCount);
        return new CommandPreview
        {
            Icon = "?",
            Title = DisplayName,
            Parameters = new()
            {
                ["Selected Features"] = totalSelected.ToString("N0"),
                ["Scope"] = string.IsNullOrEmpty(command.TargetLayer) ? "All layers" : command.TargetLayer
            },
            Confidence = command.Confidence
        };
    }

    public async Task<CommandResult> ExecuteAsync(ProPilotCommand command, MapContext context)
    {
        return await QueuedTask.Run(() =>
        {
            var map = MapView.Active?.Map;
            if (map == null) return CommandResult.Fail("No active map.");

            if (!string.IsNullOrEmpty(command.TargetLayer))
            {
                var layer = map.GetLayersAsFlattenedList()
                    .OfType<FeatureLayer>()
                    .FirstOrDefault(l => l.Name.Equals(command.TargetLayer,
                        System.StringComparison.OrdinalIgnoreCase));
                if (layer == null) return CommandResult.Fail("Layer not found.");
                layer.ClearSelection();
                return CommandResult.Ok($"Cleared selection on {layer.Name}.");
            }

            map.ClearSelection();
            return CommandResult.Ok("Cleared selection on all layers.");
        });
    }
}
