// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.Windows;
using Stride.Core.Translation;

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

    /// <summary>
    /// Gets a new instance of <see cref="DialogButtonInfo"/> to serve as 'Cancel' button.
    /// </summary>
    /// <remarks>
    /// <see cref="DialogButtonInfo.IsCancel"/> is set to <see langword="true"/>.
    /// <see cref="DialogButtonInfo.Result"/> is set to <see cref="MessageBoxResult.Cancel"/>.</remarks>
    public static DialogButtonInfo ButtonCancel => new()
    {
        IsCancel = true,
        Result = (int)MessageBoxResult.Cancel,
        Content = "Cancel",
    };

    /// <summary>
    /// Gets a new instance of <see cref="DialogButtonInfo"/> to serve as 'OK' button.
    /// </summary>
    /// <remarks>
    /// <see cref="DialogButtonInfo.IsDefault"/> is set to <see langword="true"/>.
    /// <see cref="DialogButtonInfo.Result"/> is set to <see cref="MessageBoxResult.OK"/>.</remarks>
    public static DialogButtonInfo ButtonOK => new()
    {
        IsDefault = true,
        Result = (int)MessageBoxResult.OK,
        Content = "OK",
    };

    /// <summary>
    /// Gets a new instance of <see cref="DialogButtonInfo"/> to serve as 'No' button.
    /// </summary>
    /// <remarks>
    /// <see cref="DialogButtonInfo.Result"/> is set to <see cref="MessageBoxResult.No"/>.</remarks>
    public static DialogButtonInfo ButtonNo => new()
    {
        Result = (int)MessageBoxResult.No,
        Content = "No",
        Key = Tr._p("KeyGesture", "N"),
    };

    /// <summary>
    /// Gets a new instance of <see cref="DialogButtonInfo"/> to serve as 'Yes' button.
    /// </summary>
    /// <remarks>
    /// <see cref="DialogButtonInfo.IsDefault"/> is set to <see langword="true"/>.
    /// <see cref="DialogButtonInfo.Result"/> is set to <see cref="MessageBoxResult.Yes"/>.</remarks>
    public static DialogButtonInfo ButtonYes => new()
    {
        IsDefault = true,
        Result = (int)MessageBoxResult.Yes,
        Content = "Yes",
        Key = Tr._p("KeyGesture", "Y"),
    };

    public static async Task<MessageBoxResult> ShowAsync(string caption, string message, MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None, Window? owner = null)
    {
        var buttonList = GetButtons(buttons);
        var messageBox = new MessageBox
        {
            ButtonsSource = buttonList,
            Content = message,
            Title = caption,
        };
        SetImage(messageBox, image);
        SetKeyBindings(messageBox, buttonList);
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
        return (MessageBoxResult)messageBox.ButtonResult;
    }

    protected static IReadOnlyCollection<DialogButtonInfo> GetButtons(MessageBoxButton buttons)
    {
        IReadOnlyCollection<DialogButtonInfo> buttonList;
        switch (buttons)
        {
            case MessageBoxButton.OK:
                var buttonOk = ButtonOK;
                buttonOk.IsCancel = true;
                buttonList = new[] { buttonOk };
                break;

            case MessageBoxButton.OKCancel:
                buttonList = new[] { ButtonOK, ButtonCancel };
                break;

            case MessageBoxButton.YesNoCancel:
                buttonList = new[] { ButtonYes, ButtonNo, ButtonCancel };
                break;

            case MessageBoxButton.YesNo:
                var buttonNo = ButtonNo;
                buttonNo.IsCancel = true;
                buttonList = new[] { ButtonYes, buttonNo };
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(buttons), buttons, null);
        }
        return buttonList;
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
