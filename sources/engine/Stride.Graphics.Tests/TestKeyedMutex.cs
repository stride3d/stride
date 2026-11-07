// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xunit;

namespace Stride.Graphics.Tests;

/// <summary>
/// The keyed mutex of a Direct3D 11 Texture shared with another device (a UI compositor): the mutex is handed over with
/// keys, and a Texture without the option has none.
/// </summary>
public class TestKeyedMutex : GraphicTestGameBase
{
    [SkippableFact]
    public void ATextureWithoutTheOptionHasNoKeyedMutex()
    {
        PerformTest(game =>
        {
            using var texture = Texture.New2D(game.GraphicsDevice, 16, 16, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget | TextureFlags.ShaderResource);

            Assert.False(texture.HasKeyedMutex);
            Assert.Throws<NotSupportedException>(() => texture.TryAcquireSync(0, TimeSpan.Zero));
            Assert.Throws<NotSupportedException>(() => texture.ReleaseSync(0));
        });
    }

    [SkippableFact]
    public void ANegativeTimeoutIsRejected()
    {
        PerformTest(game =>
        {
            using var texture = Texture.New2D(game.GraphicsDevice, 16, 16, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget | TextureFlags.ShaderResource);

            Assert.Throws<ArgumentOutOfRangeException>(() => texture.TryAcquireSync(0, TimeSpan.FromMilliseconds(-2)));
        });
    }

#if !STRIDE_GRAPHICS_API_DIRECT3D11
    [SkippableFact]
    public void AnotherBackendIgnoresTheKeyedMutexOptions()
    {
        PerformTest(game =>
        {
            // The options exist on every backend, so that a caller needs no cast; only Direct3D 11 uses them
            using var texture = Texture.New2D(game.GraphicsDevice, 16, 16, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget | TextureFlags.ShaderResource,
                options: TextureOptions.SharedKeyedMutex | TextureOptions.SharedNtHandle);

            Assert.False(texture.HasKeyedMutex);
            Assert.Equal(IntPtr.Zero, texture.SharedHandle);
            Assert.Equal(string.Empty, texture.SharedNtHandleName);
        });
    }
#endif

#if STRIDE_GRAPHICS_API_DIRECT3D11
    [SkippableFact]
    public void TheMutexIsAcquiredWithTheKeyItWasReleasedWith()
    {
        PerformTest(game =>
        {
            using var texture = Texture.New2D(game.GraphicsDevice, 16, 16, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget | TextureFlags.ShaderResource,
                options: TextureOptions.SharedKeyedMutex);

            Assert.True(texture.HasKeyedMutex);
            // A new mutex is free for key 0 (the game's key); the other device would acquire it with key 1
            Assert.True(texture.TryAcquireSync(0, TimeSpan.Zero));
            texture.ReleaseSync(1);
            // Released with key 1: an acquire with key 0 times out without waiting
            Assert.False(texture.TryAcquireSync(0, TimeSpan.Zero));
            Assert.True(texture.TryAcquireSync(1, TimeSpan.Zero));
            texture.ReleaseSync(0);
            Assert.True(texture.TryAcquireSync(0, TimeSpan.FromMilliseconds(100)));
            texture.ReleaseSync(0);
        });
    }

    [SkippableFact]
    public void AKeyedMutexAloneGivesAGlobalHandleAndWithTheNtOptionAnNtHandle()
    {
        PerformTest(game =>
        {
            using var globalShared = Texture.New2D(game.GraphicsDevice, 16, 16, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget | TextureFlags.ShaderResource,
                options: TextureOptions.SharedKeyedMutex);
            Assert.NotEqual(IntPtr.Zero, globalShared.SharedHandle);
            Assert.Equal(string.Empty, globalShared.SharedNtHandleName);

            using var ntShared = Texture.New2D(game.GraphicsDevice, 16, 16, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget | TextureFlags.ShaderResource,
                options: TextureOptions.SharedKeyedMutex | TextureOptions.SharedNtHandle);
            Assert.NotEqual(IntPtr.Zero, ntShared.SharedHandle);
            Assert.NotEqual(string.Empty, ntShared.SharedNtHandleName);
            Assert.True(ntShared.HasKeyedMutex);
            Assert.True(ntShared.TryAcquireSync(0, TimeSpan.Zero));
            ntShared.ReleaseSync(0);

            // The Texture owns its NT handle and closes it when it is destroyed
            ntShared.Dispose();
            Assert.Equal(IntPtr.Zero, ntShared.SharedHandle);
        });
    }
#endif
}
