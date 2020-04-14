// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;

using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Diagnostics;

namespace Xenko.GameStudio.Debugging
{
    /// <summary>
    /// An interface that can run a game in debug mode, inspect states and propagate live changes
    /// </summary>
    public interface IDebugService : IDisposable
    {
        Task<bool> StartDebug(EditorViewModel editor, ProjectViewModel currentProject, LoggerResult logger);
    }
}
