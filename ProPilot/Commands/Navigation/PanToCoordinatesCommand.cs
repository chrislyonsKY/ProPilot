using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace ProPilot.Commands.Navigation;

public class PanToCoordinatesCommand : IMapCommand
{
    public string CommandType => "pan_to_coordinates";
    public string DisplayName => "Pan To Coordinates";
    public bool IsDestructive => false;

    public ValidationResult Validate(ProPilotCommand command, MapContext context)
    {
        if (command.Parameters?.Coordinates == null)
            return ValidationResult.Fail("No coordinates specified.");
        return ValidationResult.Success();
    }

    public CommandPreview BuildPreview(ProPilotCommand command, MapContext context)
    {
        var coords = command.Parameters!.Coordinates!;
        return new CommandPreview
        {
            Icon = "??",
            Title = DisplayName,
            Parameters = new()
            {
                ["X"] = coords.X.ToString("F6"),
                ["Y"] = coords.Y.ToString("F6"),
                ["WKID"] = coords.Wkid.ToString()
            },
            IsDestructive = false,
            Confidence = command.Confidence
        };
    }

    public async Task<CommandResult> ExecuteAsync(ProPilotCommand command, MapContext context)
    {
        var coords = command.Parameters!.Coordinates!;
        return await QueuedTask.Run(async () =>
        {
            var mapView = MapView.Active;
            if (mapView == null) return CommandResult.Fail("No active map view.");

            var sr = SpatialReferenceBuilder.CreateSpatialReference(coords.Wkid);
            var point = MapPointBuilderEx.CreateMapPoint(coords.X, coords.Y, sr);

            // Project to map's spatial reference if different
            var mapSr = mapView.Map.SpatialReference;
            if (sr.Wkid != mapSr.Wkid)
                point = (MapPoint)GeometryEngine.Instance.Project(point, mapSr);

            await mapView.PanToAsync(point);
            return CommandResult.Ok($"Panned to ({coords.X:F6}, {coords.Y:F6}).");
        });
    }
}
