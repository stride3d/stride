// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Xunit;
using Xenko.Core.Diagnostics;
using Xenko.FixProjectReferences;

namespace Xenko.Code.Tests
{
    /// <summary>
    /// Test class that check if there is some copy-local references between Xenko projects.
    /// </summary>
    public class FixProjectReferenceTests
    {
        [Fact]
        public void TestCopyLocals()
        {
            var log = new LoggerResult();
            log.ActivateLog(LogMessageType.Error);
            Assert.True(FixProjectReference.ProcessCopyLocals(log, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\build\Xenko.sln"), false),
                $"Found some dependencies between Xenko projects that are not set to CopyLocal=false; please run Xenko.FixProjectReferences:\r\n{log.ToText()}");
        }
    }
}
