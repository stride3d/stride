// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Input;

namespace Stride.CrashReporter;

public partial class CrashReporterWindow : Window
{
    public CrashReporterWindow()
    {
        InitializeComponent();
        // Land keyboard focus on the primary action so the common path (send) is one keystroke away.
        Opened += (_, _) => SendButton.Focus();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
            Close();
        base.OnKeyDown(e);
    }
}
