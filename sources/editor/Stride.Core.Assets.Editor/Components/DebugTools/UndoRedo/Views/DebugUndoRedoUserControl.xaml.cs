// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Assets.Editor.Services;
using Stride.Core;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModel;

namespace Stride.Core.Assets.Editor.Components.DebugTools.UndoRedo.Views
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
