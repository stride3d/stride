// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Xenko.Core.Packages
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
