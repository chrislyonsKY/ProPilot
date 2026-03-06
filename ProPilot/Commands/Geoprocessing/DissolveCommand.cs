using System;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Desktop.Core.Geoprocessing;

namespace ProPilot.Commands.Geoprocessing;

public class DissolveCommand : IMapCommand
{
    public string CommandType => "dissolve";
    public string DisplayName => "Dissolve";
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
        return new CommandPreview
        {
            Icon = "🫧",
            Title = DisplayName,
            Parameters = new()
            {
                ["Input"] = $"{info.Name} ({info.FeatureCount:N0} features)",
                ["Dissolve Field"] = command.Parameters?.FieldName ?? "(all)",
                ["Output"] = command.Parameters?.OutputPath ?? $"{info.Name}_Dissolve"
            },
            Confidence = command.Confidence
        };
    }

    public async Task<CommandResult> ExecuteAsync(ProPilotCommand command, MapContext context)
    {
        var output = command.Parameters?.OutputPath
            ?? $"memory\\{command.TargetLayer}_Dissolve";
        var dissolveField = command.Parameters?.FieldName ?? "";
        var args = ArcGIS.Desktop.Core.Geoprocessing.Geoprocessing.MakeValueArray(
            command.TargetLayer, output, dissolveField);
        var gp = await ArcGIS.Desktop.Core.Geoprocessing.Geoprocessing.ExecuteToolAsync(
            "management.Dissolve", args, null, null, GPExecuteToolFlags.None);
        if (gp.IsFailed)
        {
            var err = string.Join("; ", gp.Messages
                .Where(m => m.Type == GPMessageType.Error).Select(m => m.Text));
            return CommandResult.Fail($"Dissolve failed: {err}");
        }
        return CommandResult.Ok($"Dissolve created: {output}");
    }
}
