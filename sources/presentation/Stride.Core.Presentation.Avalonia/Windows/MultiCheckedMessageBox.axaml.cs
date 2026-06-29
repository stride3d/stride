// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.Windows;

namespace Stride.Core.Presentation.Avalonia.Windows;

/// <summary>
/// A message box that shows any number of independent check boxes below the message. Each
/// <see cref="DialogCheckBoxInfo"/> carries its own label and check state; the same instances are read
/// back after the dialog closes to get the user's choices.
/// </summary>
public partial class MultiCheckedMessageBox : MessageBox
{
    /// <summary>
    /// Identifies the <see cref="CheckBoxes"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<IReadOnlyCollection<DialogCheckBoxInfo>?> CheckBoxesProperty =
        AvaloniaProperty.Register<MultiCheckedMessageBox, IReadOnlyCollection<DialogCheckBoxInfo>?>(nameof(CheckBoxes));

    public IReadOnlyCollection<DialogCheckBoxInfo>? CheckBoxes
    {
        get { return GetValue(CheckBoxesProperty); }
        set { SetValue(CheckBoxesProperty, value); }
    }

    /// <summary>
    /// Shows the dialog. The <paramref name="checkBoxes"/> instances are updated in place with the user's
    /// choices; the return value is the result of the button used to close it.
    /// </summary>
    public static async Task<int> ShowAsync(string caption, string message, IReadOnlyCollection<DialogCheckBoxInfo> checkBoxes, IReadOnlyCollection<DialogButtonInfo> buttons, MessageBoxImage image = MessageBoxImage.None, Window? owner = null)
    {
        var messageBox = new MultiCheckedMessageBox
        {
            ButtonsSource = buttons,
            Content = message,
            Title = caption,
            CheckBoxes = checkBoxes,
        };
        SetGeometry(messageBox, image);
        SetKeyBindings(messageBox, buttons);
        if (owner is not null)
        {
            await messageBox.ShowDialog(owner);
        }
        else
        {
            var tcs = new TaskCompletionSource();
            messageBox.Closed += (sender, args) =>
            {
                tcs.SetResult();
            };
            messageBox.Show();
            await tcs.Task;
        }
        return messageBox.ButtonResult;
    }
}
