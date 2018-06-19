// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Xenko.Core.BuildEngine.Tests
{
    class Program
    {
        static void Main()
        {
            //var testCancellation = new TestCancellation();
            //testCancellation.TestCancellationToken();
            //testCancellation.TestCancelCallback();
            //testCancellation.TestCancelPrerequisites();
            TestIO test = new TestIO();
            test.TestInputFromPreviousOutputWithCache();
        }
    }
}
