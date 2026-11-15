// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Stride.Core.Presentation.Avalonia.Windows;
using Stride.Core.Presentation.Windows;

namespace Stride.Core.Presentation.Avalonia.Tests.Windows;

/// <summary>
/// The message boxes with check boxes show them: each loads its own template over the one of the plain message box.
/// </summary>
public sealed class CheckedMessageBoxTests
{
    private static readonly DialogButtonInfo[] Buttons =
    [
        new() { Content = "Reload", Result = 1, IsDefault = true },
        new() { Content = "Don't reload", Result = 2, IsCancel = true },
    ];

    [AvaloniaFact]
    public void TheCheckedMessageBoxShowsItsCheckBox()
    {
        var messageBox = new CheckedMessageBox
        {
            ButtonsSource = Buttons,
            Content = "Some game code files have been modified.",
            CheckedMessage = "Don't ask again",
            IsChecked = false,
        };
        messageBox.Show();
        try
        {
            Dispatcher.UIThread.RunJobs();

            var checkBox = Assert.Single(messageBox.GetVisualDescendants().OfType<CheckBox>());
            Assert.Equal("Don't ask again", checkBox.Content);
            Assert.True(checkBox.IsEffectivelyVisible);

            // The answer goes back to the caller
            checkBox.IsChecked = true;
            Assert.True(messageBox.IsChecked);
        }
        finally
        {
            messageBox.Close();
        }
    }

    [AvaloniaFact]
    public void TheMultiCheckedMessageBoxShowsEachCheckBox()
    {
        var backup = new DialogCheckBoxInfo { Content = "Back up the files", IsChecked = true };
        var log = new DialogCheckBoxInfo { Content = "Keep a log", IsChecked = false };
        var messageBox = new MultiCheckedMessageBox
        {
            ButtonsSource = Buttons,
            Content = "The packages need to be upgraded.",
            CheckBoxes = [backup, log],
        };
        messageBox.Show();
        try
        {
            Dispatcher.UIThread.RunJobs();

            var checkBoxes = messageBox.GetVisualDescendants().OfType<CheckBox>().ToList();
            Assert.Equal(["Back up the files", "Keep a log"], checkBoxes.Select(x => x.Content));
            Assert.All(checkBoxes, x => Assert.True(x.IsEffectivelyVisible));

            checkBoxes[1].IsChecked = true;
            Assert.True(log.IsChecked);
        }
        finally
        {
            messageBox.Close();
        }
    }
}
