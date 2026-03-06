using System;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Desktop.Core.Geoprocessing;

namespace ProPilot.Commands.Geoprocessing;

public class ClipCommand : IMapCommand
{
    public string CommandType => "clip";
    public string DisplayName => "Clip";
    public bool IsDestructive => false;

    public ValidationResult Validate(ProPilotCommand command, MapContext context)
    {
        if (string.IsNullOrEmpty(command.TargetLayer))
            return ValidationResult.Fail("No input layer specified.");
        if (!context.Layers.Any(l => l.Name.Equals(command.TargetLayer, StringComparison.OrdinalIgnoreCase)))
            return ValidationResult.Fail($"Layer '{command.TargetLayer}' not found.");
        var clip = command.Parameters?.SourceLayer;
        if (string.IsNullOrEmpty(clip))
            return ValidationResult.Fail("No clip layer specified.");
        if (!context.Layers.Any(l => l.Name.Equals(clip, StringComparison.OrdinalIgnoreCase)))
            return ValidationResult.Fail($"Clip layer '{clip}' not found.");
        return ValidationResult.Success();
    }

    public CommandPreview BuildPreview(ProPilotCommand command, MapContext context)
    {
        return new CommandPreview
        {
            Icon = "✂",
            Title = DisplayName,
            Parameters = new()
            {
                ["Input"] = command.TargetLayer!,
                ["Clip Layer"] = command.Parameters!.SourceLayer!,
                ["Output"] = command.Parameters.OutputPath ?? $"{command.TargetLayer}_Clip"
            },
            Confidence = command.Confidence
        };
    }

    public async Task<CommandResult> ExecuteAsync(ProPilotCommand command, MapContext context)
    {
        var output = command.Parameters!.OutputPath
            ?? $"memory\\{command.TargetLayer}_Clip";
        var args = ArcGIS.Desktop.Core.Geoprocessing.Geoprocessing.MakeValueArray(
            command.TargetLayer, command.Parameters.SourceLayer, output);
        var gp = await ArcGIS.Desktop.Core.Geoprocessing.Geoprocessing.ExecuteToolAsync(
            "analysis.Clip", args, null, null, GPExecuteToolFlags.None);
        if (gp.IsFailed)
        {
            var err = string.Join("; ", gp.Messages
                .Where(m => m.Type == GPMessageType.Error).Select(m => m.Text));
            return CommandResult.Fail($"Clip failed: {err}");
        }
        return CommandResult.Ok($"Clip created: {output}");
    }
}
