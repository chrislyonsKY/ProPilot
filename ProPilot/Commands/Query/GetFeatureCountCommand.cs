using System;
using System.Linq;
using System.Threading.Tasks;

namespace ProPilot.Commands.Query;

public class GetFeatureCountCommand : IMapCommand
{
    public string CommandType => "get_feature_count";
    public string DisplayName => "Get Feature Count";
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
            Icon = "#??",
            Title = DisplayName,
            Parameters = new()
            {
                ["Layer"] = info.Name,
                ["Feature Count"] = info.FeatureCount.ToString("N0"),
                ["Selected"] = info.SelectedCount.ToString("N0")
            },
            Confidence = command.Confidence
        };
    }

    public Task<CommandResult> ExecuteAsync(ProPilotCommand command, MapContext context)
    {
        // Read-only command — data already available in context
        var info = context.Layers.First(l =>
            l.Name.Equals(command.TargetLayer!, StringComparison.OrdinalIgnoreCase));

        var msg = $"{info.Name}: {info.FeatureCount:N0} features";
        if (info.SelectedCount > 0)
            msg += $", {info.SelectedCount:N0} selected";
        if (!string.IsNullOrEmpty(info.DefinitionQuery))
            msg += $" (filtered: {info.DefinitionQuery})";

        return Task.FromResult(CommandResult.Ok(msg));
    }
}
