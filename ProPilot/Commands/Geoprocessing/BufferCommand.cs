using System;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Desktop.Core.Geoprocessing;

namespace ProPilot.Commands.Geoprocessing;

public class BufferCommand : IMapCommand
{
    public string CommandType => "buffer";
    public string DisplayName => "Buffer";
    public bool IsDestructive => false;

    public ValidationResult Validate(ProPilotCommand command, MapContext context)
    {
        if (string.IsNullOrEmpty(command.TargetLayer))
            return ValidationResult.Fail("No target layer specified.");
        if (!context.Layers.Any(l =>
            l.Name.Equals(command.TargetLayer, StringComparison.OrdinalIgnoreCase)))
            return ValidationResult.Fail($"Layer '{command.TargetLayer}' not found.");
        if (command.Parameters?.Distance == null || command.Parameters.Distance <= 0)
            return ValidationResult.Fail("No valid buffer distance specified.");
        return ValidationResult.Success();
    }

    public CommandPreview BuildPreview(ProPilotCommand command, MapContext context)
    {
        var info = context.Layers.First(l =>
            l.Name.Equals(command.TargetLayer!, StringComparison.OrdinalIgnoreCase));
        var unit = command.Parameters!.DistanceUnit ?? "Meters";
        return new CommandPreview
        {
            Icon = "⭕", Title = DisplayName,
            Parameters = new()
            {
                ["Input"] = $"{info.Name} ({info.FeatureCount:N0} features)",
                ["Distance"] = $"{command.Parameters.Distance!.Value:N0} {unit}",
                ["Output"] = command.Parameters.OutputPath ?? $"{info.Name}_Buffer"
            },
            Confidence = command.Confidence
        };
    }

    public async Task<CommandResult> ExecuteAsync(ProPilotCommand command, MapContext context)
    {
        var unit = command.Parameters!.DistanceUnit ?? "Meters";
        var dist = $"{command.Parameters.Distance!.Value} {unit}";
        var output = command.Parameters.OutputPath
            ?? $"memory\\{command.TargetLayer}_Buffer";
        var args = ArcGIS.Desktop.Core.Geoprocessing.Geoprocessing.MakeValueArray(command.TargetLayer, output, dist);
        var gp = await ArcGIS.Desktop.Core.Geoprocessing.Geoprocessing.ExecuteToolAsync(
            "analysis.Buffer", args, null, null, GPExecuteToolFlags.None);
        if (gp.IsFailed)
        {
            var err = string.Join("; ", gp.Messages
                .Where(m => m.Type == GPMessageType.Error).Select(m => m.Text));
            return CommandResult.Fail($"Buffer failed: {err}");
        }
        return CommandResult.Ok($"Buffer created: {output}");
    }
}
