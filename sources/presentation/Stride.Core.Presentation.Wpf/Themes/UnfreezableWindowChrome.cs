// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Windows;
using System.Windows.Shell;

namespace Stride.Core.Presentation.Themes
{
    /// <summary>
    /// A <see cref="WindowChrome"/> that does not try to freeze.
    /// </summary>
    /// <remarks>
    /// <see cref="WindowChrome"/> binds properties to <see cref="SystemParameters"/>, so it can never be frozen, and each attempt logs a warning.
    /// </remarks>
    public class UnfreezableWindowChrome : WindowChrome
    {
        /// <inheritdoc/>
        protected override Freezable CreateInstanceCore() => new UnfreezableWindowChrome();

        /// <inheritdoc/>
        protected override bool FreezeCore(bool isChecking) => false;
    }
}
