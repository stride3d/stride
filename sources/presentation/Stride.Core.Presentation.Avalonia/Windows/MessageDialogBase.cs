// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Stride.Core.Presentation.Avalonia.Services;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Presentation.Windows;

namespace Stride.Core.Presentation.Avalonia.Windows;

public abstract class MessageDialogBase : Window
{
    private readonly ICommandBase buttonCommand;
    private IReadOnlyCollection<DialogButtonInfo> buttonsSource = null!;

    public static readonly DirectProperty<MessageDialogBase, ICommandBase> ButtonCommandProperty =
        AvaloniaProperty.RegisterDirect<MessageDialogBase, ICommandBase>(nameof(ButtonCommand), o => o.ButtonCommand);

    public static readonly DirectProperty<MessageDialogBase, IReadOnlyCollection<DialogButtonInfo>> ButtonsSourceProperty =
        AvaloniaProperty.RegisterDirect<MessageDialogBase, IReadOnlyCollection<DialogButtonInfo>>(nameof(ButtonsSource), o => o.ButtonsSource);

    protected MessageDialogBase()
    {
        var serviceProvider = new ViewModelServiceProvider(new[] { DispatcherService.Create() });
        buttonCommand = new AnonymousCommand<int>(serviceProvider, ButtonClick);
    }

    public ICommandBase ButtonCommand
    {
        get => buttonCommand;
    }

    public required IReadOnlyCollection<DialogButtonInfo> ButtonsSource
    {
        get => buttonsSource;
        init => SetAndRaise(ButtonsSourceProperty, ref buttonsSource, value);
    }

    public int ButtonResult { get; private set; }

    private void ButtonClick(int parameter)
    {
        ButtonResult = parameter;
        Close();
    }
}
