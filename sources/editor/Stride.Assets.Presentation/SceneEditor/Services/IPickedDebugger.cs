// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Diagnostics;
using System.Threading.Tasks;
using Xenko.Core.Assets.Editor.ViewModel;

namespace Xenko.Assets.Presentation.SceneEditor.Services
{
    public interface IPickedDebugger
    {
        string Name { get; }

        Task<Process> Launch(SessionViewModel session);
    }
}
