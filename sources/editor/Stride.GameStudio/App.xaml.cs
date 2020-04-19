// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Windows;

namespace Stride.GameStudio
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private DataBindingExceptionRethrower exceptionRethrower;

        protected override void OnStartup(StartupEventArgs e)
        {
            exceptionRethrower = new DataBindingExceptionRethrower();
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            exceptionRethrower?.Dispose();
        }
    }
}
