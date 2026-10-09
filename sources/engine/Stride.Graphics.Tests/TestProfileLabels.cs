// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

using Stride.Core.Mathematics;

namespace Stride.Graphics.Tests;

public class TestProfileLabels : GraphicTestGameBase
{
    /// <summary>
    /// A profile scope opens and closes on the command list whatever its name, including an empty one,
    /// while the debug device passes it to the graphics debugger.
    /// </summary>
    [SkippableTheory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("ProfileScopeName")]
    public void ProfileScopeAcceptsAnyName(string name)
    {
        PerformTest(game =>
        {
            Skip.IfNot(game.GraphicsDevice.IsProfilingSupported, "The device does not pass profile scopes to a graphics debugger.");

            var commandList = game.GraphicsContext.CommandList;
            commandList.BeginProfile(Color.Red, name);
            commandList.EndProfile();
        });
    }
}
