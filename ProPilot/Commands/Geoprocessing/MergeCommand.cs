using System;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Desktop.Core.Geoprocessing;

namespace ProPilot.Commands.Geoprocessing;

public class MergeCommand : IMapCommand
{
    public string CommandType => "merge";
    public string DisplayName => "Merge";
    public bool IsDestructive => false;

    public ValidationResult Validate(ProPilotCommand command, MapContext context)
    {
        var names = command.Parameters?.LayerNames;
        if (names == null || names.Count < 2)
            return ValidationResult.Fail("At least two layers must be specified to merge.");
        foreach (var n in names)
            if (!context.Layers.Any(l => l.Name.Equals(n, StringComparison.OrdinalIgnoreCase)))
                return ValidationResult.Fail($"Layer '{n}' not found.");
        return ValidationResult.Success();
    }

    public CommandPreview BuildPreview(ProPilotCommand command, MapContext context)
    {
        return new CommandPreview
        {
            Icon = "🔗", Title = DisplayName,
            Parameters = new()
            {
                ["Inputs"] = string.Join(", ", command.Parameters!.LayerNames!),
                ["Output"] = command.Parameters.OutputPath ?? "Merged_Output"
            },
            Confidence = command.Confidence
        };
    }

    public async Task<CommandResult> ExecuteAsync(ProPilotCommand command, MapContext context)
    {
        var inputs = string.Join(";", command.Parameters!.LayerNames!);
        var output = command.Parameters.OutputPath ?? "memory\\Merged_Output";
        var args = ArcGIS.Desktop.Core.Geoprocessing.Geoprocessing.MakeValueArray(inputs, output);
        var gp = await ArcGIS.Desktop.Core.Geoprocessing.Geoprocessing.ExecuteToolAsync(
            "management.Merge", args, null, null, GPExecuteToolFlags.None);
        if (gp.IsFailed)
        {
            var err = string.Join("; ", gp.Messages
                .Where(m => m.Type == GPMessageType.Error).Select(m => m.Text));
            return CommandResult.Fail($"Merge failed: {err}");
        }
        return CommandResult.Ok($"Merge created: {output}");
    }
}
