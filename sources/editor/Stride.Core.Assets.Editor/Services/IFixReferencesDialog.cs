// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Presentation.Services;

namespace Stride.Core.Assets.Editor.Services
{
    public interface IFixReferencesDialog : IModalDialog
    {
        /// <summary>
        /// Applies the fixes decided by the user to the actual objects.
        /// </summary>
        void ApplyReferenceFixes();
    }
}
