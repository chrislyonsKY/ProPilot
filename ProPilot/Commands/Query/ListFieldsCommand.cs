using System;
using System.Linq;
using System.Threading.Tasks;

namespace ProPilot.Commands.Query;

public class ListFieldsCommand : IMapCommand
{
    public string CommandType => "list_fields";
    public string DisplayName => "List Fields";
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
                ["Field Count"] = info.FieldNames.Count.ToString()
            },
            Confidence = command.Confidence
        };
    }

    public Task<CommandResult> ExecuteAsync(ProPilotCommand command, MapContext context)
    {
        var info = context.Layers.First(l =>
            l.Name.Equals(command.TargetLayer!, StringComparison.OrdinalIgnoreCase));

        var fieldList = string.Join(", ", info.FieldNames);
        return Task.FromResult(CommandResult.Ok(
            $"{info.Name} ({info.FieldNames.Count} fields): {fieldList}"));
    }
}
