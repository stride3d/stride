// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Services;

namespace Stride.Core.Assets.Editor.Services;

public interface IEditorDebugService
{    
    IDebugPage CreateLogDebugPage(Logger logger, string title, bool register = true);

    IDebugPage CreateUndoRedoDebugPage(IUndoRedoService service, string title, bool register = true);

    IDebugPage CreateAssetNodesDebugPage(ISessionViewModel session, string title, bool register = true);

    void RegisterDebugPage(IDebugPage page);

    void UnregisterDebugPage(IDebugPage page);
}
