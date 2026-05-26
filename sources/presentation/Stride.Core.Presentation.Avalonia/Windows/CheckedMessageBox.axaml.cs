// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.Windows;

namespace Stride.Core.Presentation.Avalonia.Windows;

public partial class CheckedMessageBox : MessageBox
{
    /// <summary>
    /// Identifies the <see cref="CheckedMessage"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<string> CheckedMessageProperty =
        AvaloniaProperty.Register<CheckedMessageBox, string>(nameof(CheckedMessage));

    /// <summary>
    /// Identifies the <see cref="IsCheckedProperty"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<bool> IsCheckedProperty =
        AvaloniaProperty.Register<CheckedMessageBox, bool>(nameof(IsChecked));

    public string CheckedMessage
    {
        get { return (string)GetValue(CheckedMessageProperty); }
        set { SetValue(CheckedMessageProperty, value); }
    }

    public bool? IsChecked
    {
        get { return (bool?)GetValue(IsCheckedProperty); }
        set { SetValue(IsCheckedProperty, value); }
    }

    public static async Task<CheckedMessageBoxResult> ShowAsync(string caption, string message, bool? isChecked, string checkedMessage, IReadOnlyCollection<DialogButtonInfo> buttons, MessageBoxImage image = MessageBoxImage.None, Window? owner = null)
    {
        var messageBox = new CheckedMessageBox
        {
            ButtonsSource = buttons,
            Content = message,
            Title = caption,
            CheckedMessage = checkedMessage,
            IsChecked = isChecked,
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
        var result = messageBox.ButtonResult;
        return new CheckedMessageBoxResult(result, messageBox.IsChecked);
    }

}
