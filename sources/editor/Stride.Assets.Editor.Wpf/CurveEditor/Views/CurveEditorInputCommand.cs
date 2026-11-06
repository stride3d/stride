// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Stride.Core.Presentation.Drawing;

namespace Stride.Assets.Presentation.CurveEditor.Views
{
    /// <summary>
    /// Converts a command of the curve editor view model into a command that the WPF mouse behaviors can run: the WPF point or
    /// rectangle they pass becomes the toolkit-free parameter the view model expects.
    /// </summary>
    public sealed class CurveEditorInputCommand : IValueConverter
    {
        /// <summary>
        /// The command takes the position of the mouse as a <see cref="DrawingPoint"/>, or the selection rectangle as a <see cref="DrawingRect"/>.
        /// </summary>
        public static CurveEditorInputCommand Geometry { get; } = new(ConvertGeometry);

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

        private static object ConvertGeometry(object parameter)
        {
            return parameter switch
            {
                Point point => point.ToDrawing(),
                Rect rect => rect.ToDrawing(),
                _ => parameter,
            };
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
