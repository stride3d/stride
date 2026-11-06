// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Windows;

namespace Stride.Editor.Preview
{
    /// <summary>
    /// A <see cref="IPreviewBuilder"/> that hosts the preview game in a WPF element.
    /// </summary>
    public interface IWpfPreviewBuilder : IPreviewBuilder
    {
        /// <summary>
        /// Gets the WPF element the preview game renders in.
        /// </summary>
        /// <returns>The element.</returns>
        FrameworkElement GetStrideView();
    }
}
