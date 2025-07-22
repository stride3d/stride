// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.Windows;

namespace Stride.Core.Presentation.Avalonia.Windows;

public partial class MessageBox : MessageDialogBase
{
    public static readonly StyledProperty<IImageBrushSource?> ImageProperty =
        AvaloniaProperty.Register<MessageBox, IImageBrushSource?>(nameof(Image));

    public MessageBox()
    {
        InitializeComponent();
    }

    public IImageBrushSource? Image
    {
        get { return GetValue(ImageProperty); }
        set { SetValue(ImageProperty, value); }
    }

    public static async Task<int> ShowAsync(string caption, string message, IReadOnlyCollection<DialogButtonInfo> buttons, MessageBoxImage image = MessageBoxImage.None, Window? owner = null)
    {
        var messageBox = new MessageBox
        {
            ButtonsSource = buttons,
            Content = message,
            Title = caption,
        };
        SetImage(messageBox, image);
        SetKeyBindings(messageBox, buttons);
        if (owner is not null)
        {
            await messageBox.ShowDialog(owner);
        }
        else
        {
            var tcs = new TaskCompletionSource();
            messageBox.Closed += (_, _) =>
            {
                tcs.SetResult();
            };
            messageBox.Show();
            await tcs.Task;
        }
        return messageBox.ButtonResult;
    }

    protected static void SetKeyBindings(MessageBox messageBox, IEnumerable<DialogButtonInfo> buttons)
    {
        foreach (var button in buttons.Where(x => !string.IsNullOrEmpty(x.Key)))
        {
            var binding = new KeyBinding
            {
                Command = messageBox.ButtonCommand,
                CommandParameter = button.Result,
                Gesture = KeyGesture.Parse(button.Key)
            };
            messageBox.KeyBindings.Add(binding);
        }
    }

    protected static void SetImage(MessageBox messageBox, MessageBoxImage image)
    {
        var imageKey = image switch
        {
            MessageBoxImage.None => null,
            MessageBoxImage.Error => "ImageErrorDialog",
            MessageBoxImage.Question => "ImageQuestionDialog",
            MessageBoxImage.Warning => "ImageWarningDialog",
            MessageBoxImage.Information => "ImageInformationDialog",
            _ => throw new ArgumentOutOfRangeException(nameof(image), image, null),
        };
        if (imageKey is not null)
        {
            messageBox.TryGetResource(imageKey, null, out var bitmap);
            messageBox.Image = (bitmap as ImageBrush)?.Source;
        }
    }
}
