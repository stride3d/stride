// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Assets.Editor.Services;
using Xenko.Core;
using Xenko.Core.Presentation.Services;
using Xenko.Core.Presentation.ViewModel;

namespace Xenko.Core.Assets.Editor.Components.DebugTools.UndoRedo.Views
{
    /// <summary>
    /// Interaction logic for DebugActionStackUserControl.xaml
    /// </summary>
    public partial class DebugUndoRedoUserControl : IDebugPage, IDestroyable
    {        
        public DebugUndoRedoUserControl(IViewModelServiceProvider serviceProvider, IUndoRedoService undoRedo)
        {
            InitializeComponent();
            DataContext = new DebugUndoRedoViewModel(serviceProvider, undoRedo);
        }

        public string Title { get; set; }

        public void Destroy()
        {
            ((DebugUndoRedoViewModel)DataContext).Destroy();
        }
    }
}
