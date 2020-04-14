// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Presentation.Services;

namespace Xenko.Core.Assets.Editor.Services
{
    public interface IFixReferencesDialog : IModalDialog
    {
        /// <summary>
        /// Applies the fixes decided by the user to the actual objects.
        /// </summary>
        void ApplyReferenceFixes();
    }
}
