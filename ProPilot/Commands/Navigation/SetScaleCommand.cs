using System.Threading.Tasks;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace ProPilot.Commands.Navigation;

public class SetScaleCommand : IMapCommand
{
    public string CommandType => "set_scale";
    public string DisplayName => "Set Scale";
    public bool IsDestructive => false;

    public ValidationResult Validate(ProPilotCommand command, MapContext context)
    {
        if (command.Parameters?.Scale == null || command.Parameters.Scale <= 0)
            return ValidationResult.Fail("No valid scale specified.");
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
                ["New Scale"] = $"1:{command.Parameters!.Scale!.Value:N0}",
                ["Current Scale"] = $"1:{context.Scale:N0}"
            },
            IsDestructive = false,
            Confidence = command.Confidence
        };
    }

    public async Task<CommandResult> ExecuteAsync(ProPilotCommand command, MapContext context)
    {
        var scale = command.Parameters!.Scale!.Value;
        return await QueuedTask.Run(async () =>
        {
            var mapView = MapView.Active;
            if (mapView == null) return CommandResult.Fail("No active map view.");

            var camera = mapView.Camera;
            camera.Scale = scale;
            await mapView.ZoomToAsync(camera);
            return CommandResult.Ok($"Scale set to 1:{scale:N0}.");
        });
    }
}
