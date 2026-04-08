// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics.Regression;

/// <summary>
///   Describes different ways to compute the size of the Back-Buffer for tests.
/// </summary>
public enum BackBufferSizeMode
{
    /// <summary>
    ///   Fit the size of the Back-Buffer to the desired sizes.
    /// </summary>
    FitToDesiredValues,

    /// <summary>
    ///   Fit the size of the Back-Buffer to the size of the window.
    /// </summary>
    FitToWindowSize,

    /// <summary>
    ///   Calculate the size of the Back-Buffer based on the window ratio and desired height / width.
    /// </summary>
    FitToWindowRatio
};
