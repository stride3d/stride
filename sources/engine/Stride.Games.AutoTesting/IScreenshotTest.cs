// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;

namespace Stride.Games.AutoTesting;

/// <summary>
/// Contract for a screenshot-regression session script. Implementations describe a sequence
/// of frame waits, simulated input, and screenshot captures via <see cref="IScreenshotTestContext"/>.
/// </summary>
public interface IScreenshotTest
{
    Task Run(IScreenshotTestContext ctx);
}
