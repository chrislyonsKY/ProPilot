using ArcGIS.Desktop.Framework.Controls;
using ProPilot.ViewModels;

namespace ProPilot.UI;

/// <summary>
/// Code-behind for the first-time setup window. ONLY sets DataContext.
/// </summary>
public partial class SetupWindow : ProWindow
{
    public SetupWindow()
    {
        InitializeComponent();

        var module = ProPilotModule.Current;
        DataContext = new SetupWindowViewModel(module.ModelManager);
    }
}
