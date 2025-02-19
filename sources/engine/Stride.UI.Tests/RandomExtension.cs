// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Stride.Core.Mathematics;

namespace Stride.UI.Tests
{
    internal static class RandomExtension
    {
        public static float NextFloat(this Random random)
        {
            return (float)random.NextDouble();
        }

        public static Vector2 NextVector2(this Random random)
        {
            return new Vector2(random.NextFloat(), random.NextFloat());
        }
        
        public static Size2F NextSize2F(this Random random)
        {
            return new Size2F(random.NextFloat(), random.NextFloat());
        }

        public static Thickness NextThickness(this Random random, float leftFactor, float topFactor, float backFactor, float rightFactor)
        {
            return new Thickness(
                random.NextFloat() * leftFactor, random.NextFloat() * topFactor,
                random.NextFloat() * rightFactor, random.NextFloat() * backFactor);
        }
    }
}
