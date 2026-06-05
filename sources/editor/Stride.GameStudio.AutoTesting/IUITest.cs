// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.GameStudio.AutoTesting;

/// <summary>
/// Author-side test fixture. The class is instantiated by the GameStudio loader after the main
/// window is up; <see cref="Run"/> is then driven on a background task with a context handle.
/// </summary>
public interface IUITest
{
    Task Run(IUITestContext ctx);
}
