// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;
using Stride.Graphics;
using Stride.Graphics.Regression;
using Xunit;

namespace Stride.Audio.Tests
{
    /// <summary>
    /// The base class for all the audio games.
    /// </summary>
    public class AudioTestGame : GameTestBase
    {
        public AudioTestGame()
        {
            // VideoSmokeTest uses VideoTexture, which loads SpriteEffectExtTextureRegular —
            // that shader calls SampleLevel in the pixel shader and needs Shader Model 4.0 /
            // FL10.0+. The asset GameSettings is also at Level_10_0 to match (so asset compile
            // doesn't degrade the shader silently). Other Audio.Tests tests don't need FL10
            // but inherit it harmlessly.
            GraphicsDeviceManager.PreferredGraphicsProfile = [GraphicsProfile.Level_10_0];
        }
    }
}
