// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Packages
{
    /// <summary>
    /// Possible level of logging used by <see cref="IPackagesLogger"/>.
    /// </summary>
    public enum MessageLevel
    {
        Debug,
        Verbose,
        Info,
        Minimal,
        Warning,
        Error,
        InfoSummary,
        ErrorSummary
    }
}
