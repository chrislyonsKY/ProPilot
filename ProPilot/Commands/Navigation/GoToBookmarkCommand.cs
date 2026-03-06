using System;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace ProPilot.Commands.Navigation;

public class GoToBookmarkCommand : IMapCommand
{
    public string CommandType => "go_to_bookmark";
    public string DisplayName => "Go To Bookmark";
    public bool IsDestructive => false;

    public ValidationResult Validate(ProPilotCommand command, MapContext context)
    {
        var name = command.Parameters?.BookmarkName;
        if (string.IsNullOrEmpty(name))
            return ValidationResult.Fail("No bookmark name specified.");

        if (!context.Bookmarks.Any(b => b.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return ValidationResult.Fail($"Bookmark '{name}' not found. Available: {string.Join(", ", context.Bookmarks)}");

        return ValidationResult.Success();
    }

    public CommandPreview BuildPreview(ProPilotCommand command, MapContext context)
    {
        var name = command.Parameters!.BookmarkName!;
        var match = context.Bookmarks.First(b => b.Equals(name, StringComparison.OrdinalIgnoreCase));

        return new CommandPreview
        {
            Icon = "??",
            Title = DisplayName,
            Parameters = new()
            {
                ["Bookmark"] = match
            },
            IsDestructive = false,
            Confidence = command.Confidence
        };
    }

    public async Task<CommandResult> ExecuteAsync(ProPilotCommand command, MapContext context)
    {
        var name = command.Parameters!.BookmarkName!;
        return await QueuedTask.Run(async () =>
        {
            var mapView = MapView.Active;
            if (mapView == null) return CommandResult.Fail("No active map view.");

            var bookmark = mapView.Map.GetBookmarks()
                .FirstOrDefault(b => b.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (bookmark == null) return CommandResult.Fail($"Bookmark '{name}' not found.");

            await mapView.ZoomToAsync(bookmark);
            return CommandResult.Ok($"Navigated to bookmark '{bookmark.Name}'.");
        });
    }
}
