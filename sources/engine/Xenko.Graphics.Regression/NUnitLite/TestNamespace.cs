// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_PLATFORM_IOS || XENKO_PLATFORM_ANDROID
using NUnit.Framework.Internal;

namespace Xenko.Graphics.Regression
{
    public class TestNamespace : TestSuite
    {
        public TestNamespace(string @namespace)
            : base(@namespace)
        {
        }

        public override string TestType
        {
            get { return "Namespace"; }
        }
    }
}
#endif
