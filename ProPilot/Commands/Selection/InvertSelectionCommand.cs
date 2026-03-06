using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace ProPilot.Commands.Selection;

public class InvertSelectionCommand : IMapCommand
{
    public string CommandType => "invert_selection";
    public string DisplayName => "Invert Selection";
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
            Icon = "??",
            Title = DisplayName,
            Parameters = new()
            {
                ["Layer"] = info.Name,
                ["Currently Selected"] = info.SelectedCount.ToString("N0"),
                ["Will Select"] = (info.FeatureCount - info.SelectedCount).ToString("N0")
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

            var currentSelection = layer.GetSelection();
            var currentOids = currentSelection.GetObjectIDs();

            // Select all, then switch to subtract the current selection
            var allFilter = new ArcGIS.Core.Data.QueryFilter { WhereClause = "1=1" };
            layer.Select(allFilter, SelectionCombinationMethod.New);

            if (currentOids.Count > 0)
            {
                var oidField = layer.GetTable().GetDefinition().GetObjectIDField();
                var oidList = string.Join(",", currentOids);
                var invertFilter = new ArcGIS.Core.Data.QueryFilter
                {
                    WhereClause = $"{oidField} IN ({oidList})"
                };
                layer.Select(invertFilter, SelectionCombinationMethod.Subtract);
            }

            var newCount = layer.GetSelection().GetCount();
            return CommandResult.Ok($"Inverted selection on {layer.Name}. Now {newCount:N0} features selected.");
        });
    }
}
