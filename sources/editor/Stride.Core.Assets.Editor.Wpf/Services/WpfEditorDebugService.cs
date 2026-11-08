// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Services
{
    /// <summary>
    /// The <see cref="IEditorDebugService"/> of the WPF editor: it shows the pages in the WPF debug window, through <see cref="EditorDebugTools"/>.
    /// </summary>
    /// <remarks>
    /// The shared session creates its debug pages while it is constructed, before <see cref="SessionViewModel.Instance"/> is set,
    /// so this service uses the service provider it was given instead.
    /// </remarks>
    public sealed class WpfEditorDebugService : IEditorDebugService
    {
        private readonly IViewModelServiceProvider serviceProvider;

        public WpfEditorDebugService(IViewModelServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public IDebugPage CreateLogDebugPage(Logger logger, string title, bool register = true)
        {
            return EditorDebugTools.CreateLogDebugPage(serviceProvider, logger, title, register);
        }

        public IDebugPage CreateUndoRedoDebugPage(IUndoRedoService actionService, string title, bool register = true)
        {
            return EditorDebugTools.CreateUndoRedoDebugPage(serviceProvider, actionService, title, register);
        }

        public IDebugPage CreateAssetNodesDebugPage(SessionViewModel session, string title, bool register = true)
        {
            return EditorDebugTools.CreateAssetNodesDebugPage(session, title, register);
        }

        public void RegisterDebugPage(IDebugPage page)
        {
            if (page != null)
                EditorDebugTools.RegisterDebugPage(page);
        }

        public void UnregisterDebugPage(IDebugPage page)
        {
            if (page != null)
                EditorDebugTools.UnregisterDebugPage(page);
        }
    }
}
