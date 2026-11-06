// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Stride.Assets.Editor.ViewModels.CurveEditor;
using Stride.Core.Presentation.Drawing;

namespace Stride.Assets.Presentation.CurveEditor.Views
{
    /// <summary>
    /// Converts a command of the curve editor view model into a command that the WPF mouse behaviors can run: the WPF point or
    /// rectangle they pass becomes the toolkit-free parameter the view model expects, with the selection mode of the modifier
    /// keys held.
    /// </summary>
    public sealed class CurveEditorInputCommand : IValueConverter
    {
        /// <summary>
        /// The command takes the position of the mouse as a <see cref="DrawingPoint"/>.
        /// </summary>
        public static CurveEditorInputCommand Position { get; } = new(p => p is Point point ? point.ToDrawing() : p);

        /// <summary>
        /// The command takes a <see cref="CurveEditorClick"/>: the position of the mouse and the selection mode.
        /// </summary>
        public static CurveEditorInputCommand Click { get; } = new(p => p is Point point ? new CurveEditorClick(point.ToDrawing(), GetSelectionMode()) : p);

        /// <summary>
        /// The command takes a <see cref="CurveEditorSelection"/>: the selection rectangle and the selection mode.
        /// </summary>
        public static CurveEditorInputCommand Selection { get; } = new(p => p is Rect rect ? new CurveEditorSelection(rect.ToDrawing(), GetSelectionMode()) : p);

        private readonly Func<object, object> convertParameter;

        private CurveEditorInputCommand(Func<object, object> convertParameter)
        {
            this.convertParameter = convertParameter;
        }

        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is ICommand command ? new ConvertingCommand(command, convertParameter) : null;
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        private static CurveSelectionMode GetSelectionMode()
        {
            var modifiers = Keyboard.Modifiers;
            return CurveSelectionModes.FromModifiers(modifiers.HasFlag(ModifierKeys.Shift), modifiers.HasFlag(ModifierKeys.Control),
                modifiers.HasFlag(ModifierKeys.Alt) || modifiers.HasFlag(ModifierKeys.Windows));
        }

        private sealed class ConvertingCommand : ICommand
        {
            private readonly ICommand command;
            private readonly Func<object, object> convertParameter;

            public ConvertingCommand(ICommand command, Func<object, object> convertParameter)
            {
                this.command = command;
                this.convertParameter = convertParameter;
            }

            public event EventHandler CanExecuteChanged
            {
                add => command.CanExecuteChanged += value;
                remove => command.CanExecuteChanged -= value;
            }

            public bool CanExecute(object parameter) => command.CanExecute(convertParameter(parameter));

            public void Execute(object parameter) => command.Execute(convertParameter(parameter));
        }
    }
}
