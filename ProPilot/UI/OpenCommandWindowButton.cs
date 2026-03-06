using System.Diagnostics;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;

namespace ProPilot.UI;

/// <summary>
/// Button that opens the ProPilot command window.
/// Registered in Config.daml as ProPilot_OpenCommandWindow.
/// </summary>
internal class OpenCommandWindowButton : Button
{
    private CommandWindow? _commandWindow;

    protected override void OnClick()
    {
        var module = ProPilotModule.Current;

        // First-run check: if no local model, show setup window
        if (module.Settings.LlmProvider == "bundled" && !module.ModelManager.HasLocalModel())
        {
            Debug.WriteLine("[ProPilot] No local model found — opening setup window.");
            var setupWindow = new SetupWindow();
            setupWindow.Owner = FrameworkApplication.Current.MainWindow;
            setupWindow.Show();
            return;
        }

        // Show or bring forward the command window
        if (_commandWindow == null || !_commandWindow.IsVisible)
        {
            _commandWindow = new CommandWindow();
            _commandWindow.Owner = FrameworkApplication.Current.MainWindow;
            _commandWindow.Show();
        }
        else
        {
            _commandWindow.Activate();
        }
    }

    protected override void OnUpdate()
    {
        // Enable the button only when a map view is active
        Enabled = MapView.Active != null;
    }
}
