// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using Xenko.Core.Assets.Editor.Services;
using Xenko.Core.Presentation.ViewModel;

namespace Xenko.Core.Assets.Editor.View.DebugTools
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
