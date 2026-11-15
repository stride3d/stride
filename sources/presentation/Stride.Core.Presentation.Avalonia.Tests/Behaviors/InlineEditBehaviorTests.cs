// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;
using Stride.Core.Presentation.Avalonia.Behaviors;
using Stride.Core.Presentation.Avalonia.Commands;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Presentation.Avalonia.Tests.Behaviors;

/// <summary>
/// Renaming in place: the Rename command starts the edit, the box takes the focus, and Enter, Escape and the focus end it.
/// </summary>
public sealed class InlineEditBehaviorTests
{
    private static (Window Window, TextBox Box, Button Other, NamedItem Item) Show(bool editable = true)
    {
        var item = new NamedItem("Sphere") { IsEditable = editable };
        var box = new TextBox { [!Visual.IsVisibleProperty] = new Binding(nameof(NamedItem.IsEditing)) };
        // In XAML the behavior binds through the box; a binding from code names its source
        var behavior = new InlineEditBehavior
        {
            [!InlineEditBehavior.NameProperty] = new Binding(nameof(NamedItem.Name)) { Source = item },
            [!InlineEditBehavior.IsEditingProperty] = new Binding(nameof(NamedItem.IsEditing)) { Source = item },
        };
        Interaction.GetBehaviors(box).Add(behavior);
        var other = new Button { Content = "Other" };
        var window = new Window { Width = 300, Height = 200, Content = new StackPanel { Children = { box, other }, DataContext = item } };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, box, other, item);
    }

    [AvaloniaFact]
    public void TheRenameCommandShowsTheBoxWithTheNameSelected()
    {
        var (window, box, _, item) = Show();
        try
        {
            Assert.False(box.IsVisible);
            Assert.True(BeginEditCommand.Instance.CanExecute(item));

            BeginEditCommand.Instance.Execute(item);
            Dispatcher.UIThread.RunJobs();

            Assert.True(item.IsEditing);
            Assert.True(box.IsVisible);
            Assert.True(box.IsFocused);
            Assert.Equal("Sphere", box.Text);
            Assert.Equal("Sphere", box.SelectedText);
            // Not twice
            Assert.False(BeginEditCommand.Instance.CanExecute(item));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void AnItemThatIsNotEditableCannotBeRenamed()
    {
        var (window, box, _, item) = Show(editable: false);
        try
        {
            Assert.False(BeginEditCommand.Instance.CanExecute(item));
            BeginEditCommand.Instance.Execute(item);
            Dispatcher.UIThread.RunJobs();
            Assert.False(item.IsEditing);
            Assert.False(box.IsVisible);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void EnterCommitsTheNewName()
    {
        var (window, box, _, item) = Show();
        try
        {
            BeginEditCommand.Instance.Execute(item);
            Dispatcher.UIThread.RunJobs();
            window.KeyTextInput("Cube");
            window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("Cube", item.Name);
            Assert.False(item.IsEditing);
            Assert.False(box.IsVisible);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void EnterWithTheSameOrAnEmptyNameRenamesNothing()
    {
        var (window, _, _, item) = Show();
        try
        {
            BeginEditCommand.Instance.Execute(item);
            Dispatcher.UIThread.RunJobs();
            window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            Assert.Equal(0, item.Renames);

            BeginEditCommand.Instance.Execute(item);
            Dispatcher.UIThread.RunJobs();
            window.KeyTextInput(" ");
            window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            Assert.Equal(0, item.Renames);
            Assert.Equal("Sphere", item.Name);
            Assert.False(item.IsEditing);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void EscapeAndTheFocusLeavingCancelTheEdit()
    {
        var (window, box, other, item) = Show();
        try
        {
            BeginEditCommand.Instance.Execute(item);
            Dispatcher.UIThread.RunJobs();
            window.KeyTextInput("Cube");
            window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
            Assert.Equal("Sphere", item.Name);
            Assert.False(item.IsEditing);

            BeginEditCommand.Instance.Execute(item);
            Dispatcher.UIThread.RunJobs();
            Assert.True(box.IsFocused);
            window.KeyTextInput("Cube");
            Assert.True(other.Focus());
            Assert.Equal("Sphere", item.Name);
            Assert.False(item.IsEditing);
            Assert.Equal(0, item.Renames);
        }
        finally
        {
            window.Close();
        }
    }

    private sealed class NamedItem : IIsEditableViewModel, INotifyPropertyChanged
    {
        private string name;
        private bool isEditing;

        public NamedItem(string name)
        {
            this.name = name;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Name
        {
            get => name;
            set
            {
                Renames++;
                Set(ref name, value);
            }
        }

        public int Renames { get; private set; }

        public bool IsEditable { get; set; } = true;

        public bool IsEditing
        {
            get => isEditing;
            set => Set(ref isEditing, value);
        }

        private void Set<T>(ref T field, T value, [CallerMemberName] string? property = null)
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}
