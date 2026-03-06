using ArcGIS.Desktop.Framework.Controls;
using ProPilot.ViewModels;

namespace ProPilot.UI;

/// <summary>
/// Code-behind for the ProPilot command window. ONLY sets DataContext.
/// </summary>
public partial class CommandWindow : ProWindow
{
    public CommandWindow()
    {
        InitializeComponent();

        var module = ProPilotModule.Current;
        DataContext = new CommandWindowViewModel(
            module.LlmClient,
            module.ContextBuilder,
            module.CommandRegistry);
    }
}
