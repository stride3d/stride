// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Xenko.Extensions;
using Xenko.Framework.MicroThreading;
using Xenko.DebugTools.ViewModels;

namespace Xenko.DebugTools
{
    /// <summary>
    /// Interaction logic for ScriptListControl.xaml
    /// </summary>
    public partial class ScriptListControl : UserControl
    {
        private EngineContext engineContext;

        public ScriptListControl()
        {
            InitializeComponent();
        }

        internal void Initialize(EngineContext engineContext)
        {
            this.engineContext = engineContext;

            this.DataContext = new RootViewModel(engineContext, processInfoRenderer);
            processInfoRendererScroller.ScrollToRightEnd();
        }
    }
}
