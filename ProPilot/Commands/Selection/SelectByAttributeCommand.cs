using System;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace ProPilot.Commands.Selection;

public class SelectByAttributeCommand : IMapCommand
{
    public string CommandType => "select_by_attribute";
    public string DisplayName => "Select By Attribute";
    public bool IsDestructive => false;

    public ValidationResult Validate(ProPilotCommand command, MapContext context)
    {
        if (string.IsNullOrEmpty(command.TargetLayer))
            return ValidationResult.Fail("No target layer specified.");
        if (!context.Layers.Any(l => l.Name.Equals(command.TargetLayer, StringComparison.OrdinalIgnoreCase)))
            return ValidationResult.Fail($"Layer '{command.TargetLayer}' not found.");
        if (string.IsNullOrEmpty(command.Parameters?.Expression))
            return ValidationResult.Fail("No SQL expression specified.");
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
                ["Layer"] = command.TargetLayer!,
                ["Expression"] = command.Parameters!.Expression!
            },
            Confidence = command.Confidence
        };
    }

    public async Task<CommandResult> ExecuteAsync(ProPilotCommand command, MapContext context)
    {
        return await QueuedTask.Run(() =>
        {
            var layer = MapView.Active?.Map?.GetLayersAsFlattenedList()
                .OfType<FeatureLayer>()
                .FirstOrDefault(l => l.Name.Equals(command.TargetLayer, StringComparison.OrdinalIgnoreCase));
            if (layer == null) return CommandResult.Fail("Layer not found.");

            var qf = new QueryFilter { WhereClause = command.Parameters!.Expression! };
            layer.Select(qf, SelectionCombinationMethod.New);

            var count = layer.GetSelection().GetCount();
            return CommandResult.Ok($"Selected {count:N0} features in {layer.Name}.");
        });
    }
}
