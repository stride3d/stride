// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Input.Tests;

/// <summary>
///   Covers the Input section of the game settings.
/// </summary>
public class TestInputSettings
{
    [Fact]
    public void MaskingIsOnByDefault() => Assert.True(new InputSettings().MaskCapturedInput);

    [Fact]
    public void SettingsTurnMaskingOff()
    {
        using var headless = new HeadlessInput();

        headless.Input.ApplySettings(new InputSettings { MaskCapturedInput = false });

        Assert.False(headless.Input.MaskCapturedInput);
    }

    [Fact]
    public void MissingSettingsKeepMaskingOn()
    {
        using var headless = new HeadlessInput();
        headless.Input.MaskCapturedInput = false;

        headless.Input.ApplySettings(null);

        Assert.True(headless.Input.MaskCapturedInput);
    }
}
