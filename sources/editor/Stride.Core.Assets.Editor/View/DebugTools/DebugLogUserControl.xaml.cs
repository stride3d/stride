// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using Stride.Core.Assets.Editor.Services;
using Stride.Core.Presentation.ViewModel;

namespace Stride.Core.Assets.Editor.View.DebugTools
{
    /// <summary>
    /// Interaction logic for DebugLogUserControl.xaml
    /// </summary>
    public partial class DebugLogUserControl : IDebugPage
    {
        public DebugLogUserControl(LoggerViewModel loggerViewModel)
        {
            if (loggerViewModel == null) throw new ArgumentNullException("loggerViewModel");
            Logger = loggerViewModel;
            InitializeComponent();
        }

        public string Title { get; set; }

        public LoggerViewModel Logger { get; private set; }

    }
}
