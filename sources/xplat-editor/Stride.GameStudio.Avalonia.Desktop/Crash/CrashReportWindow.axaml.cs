using Avalonia.Controls;
using Avalonia.Input;

namespace Stride.GameStudio.Avalonia.Desktop.Crash;

public partial class CrashReportWindow : Window
{
    public CrashReportWindow()
    {
        InitializeComponent();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
    }
}
