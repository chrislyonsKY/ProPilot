using System.Threading.Tasks;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace ProPilot.Commands.Navigation;

public class ZoomToFullExtentCommand : IMapCommand
{
    public string CommandType => "zoom_to_full_extent";
    public string DisplayName => "Zoom To Full Extent";
    public bool IsDestructive => false;

    public ValidationResult Validate(ProPilotCommand command, MapContext context)
    {
        return ValidationResult.Success();
    }

    public CommandPreview BuildPreview(ProPilotCommand command, MapContext context)
    {
        return new CommandPreview
        {
            Icon = "??",
            Title = DisplayName,
            Parameters = new()
            {
                ["Action"] = "Zoom to the full extent of all layers"
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
            var fullExtent = mapView.Map.CalculateFullExtent();
            await mapView.ZoomToAsync(fullExtent);
            return CommandResult.Ok("Zoomed to full extent.");
        });
    }
}
