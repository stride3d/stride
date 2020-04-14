// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

using Xenko.Core.Presentation.Commands;
using Xenko.Core.Presentation.Core;

namespace Xenko.Core.Assets.Editor.View
{
    /// <summary>
    /// Interaction logic for NotificationWindow.xaml
    /// </summary>
    public partial class NotificationWindow
    {
        public NotificationWindow(string caption, string message, ICommand command, object commandParameter)
        {
            InitializeComponent();
            Caption = caption;
            Message = message;
            Command = command;
            CommandParameter = commandParameter;
            DataContext = this;
            var dependencyPropertyWatcher = new DependencyPropertyWatcher(this);
            dependencyPropertyWatcher.RegisterValueChangedHandler(ActualHeightProperty, (s, e) => SetPosition());
            Loaded += NotificationWindowLoaded;
        }

        public string Caption { get; private set; }

        public string Message { get; private set; }

        public ICommand Command { get; private set; }

        public object CommandParameter { get; private set; }

        private void NotificationWindowLoaded(object sender, RoutedEventArgs e)
        {
            SetPosition();
            Topmost = true;
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(9) };
            timer.Tick += (s, arg) => Close();
            timer.Start();
        }

        private void SetPosition()
        {
            var workingArea = SystemParameters.WorkArea;
            var corner = new Point(workingArea.Right, workingArea.Bottom);
            Left = corner.X - ActualWidth - 15;
            Top = corner.Y - ActualHeight - 5;
        }
    }
}
