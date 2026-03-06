using System;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace ProPilot.Commands.Navigation;

public class ZoomToSelectionCommand : IMapCommand
{
    public string CommandType => "zoom_to_selection";
    public string DisplayName => "Zoom To Selection";
    public bool IsDestructive => false;

    public ValidationResult Validate(ProPilotCommand command, MapContext context)
    {
        var hasSelection = context.Layers.Any(l => l.SelectedCount > 0);
        if (!hasSelection)
            return ValidationResult.Fail("No features are currently selected.");
        return ValidationResult.Success();
    }

    public CommandPreview BuildPreview(ProPilotCommand command, MapContext context)
    {
        var totalSelected = context.Layers.Sum(l => l.SelectedCount);
        var layersWithSelection = context.Layers.Where(l => l.SelectedCount > 0).ToList();

        return new CommandPreview
        {
            Icon = "??",
            Title = DisplayName,
            Parameters = new()
            {
                ["Selected Features"] = totalSelected.ToString("N0"),
                ["Layers"] = string.Join(", ", layersWithSelection.Select(l => $"{l.Name} ({l.SelectedCount})"))
            },
            IsDestructive = false,
            Confidence = command.Confidence
        };
    }

    public async Task<CommandResult> ExecuteAsync(ProPilotCommand command, MapContext context)
    {
        return await QueuedTask.Run(async () =>
        {
            var mapView = MapView.Active;
            if (mapView == null) return CommandResult.Fail("No active map view.");
            await mapView.ZoomToSelectedAsync();
            return CommandResult.Ok("Zoomed to selected features.");
        });
    }
}
