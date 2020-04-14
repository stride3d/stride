// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Xenko.Graphics.Regression
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
