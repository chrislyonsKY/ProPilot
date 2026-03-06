using ArcGIS.Desktop.Framework.Controls;
using ProPilot.ViewModels;

namespace ProPilot.UI;

/// <summary>
/// Code-behind for the settings window. ONLY sets DataContext.
/// </summary>
public partial class SettingsWindow : ProWindow
{
    public SettingsWindow()
    {
        InitializeComponent();

        var module = ProPilotModule.Current;
        DataContext = new SettingsWindowViewModel(module.SettingsService);
    }
}
