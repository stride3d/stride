// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Graphics.Regression
{
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public class RegressionTestAttribute : System.Attribute
    {
        private int frameIndex;

        public RegressionTestAttribute(int frame)
        {
            frameIndex = frame;
        }
    }
}
