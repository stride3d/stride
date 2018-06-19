// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Presentation.Services;

namespace Xenko.Assets.Presentation.SceneEditor.Services
{
    public interface IDebuggerPickerDialog : IModalDialog
    {
        IPickedDebugger SelectedDebugger { get; }
    }
}
