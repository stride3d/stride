// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.SampleScreenshotRunner;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Stride.Samples.Tests
{
    /// <summary>
    /// Runs the tests by name, and gives the sample of each test to <see cref="PreparedSamples"/> in that order, so the
    /// samples the run uses are prepared before its first test, once each.
    /// </summary>
    public sealed class SampleRunOrderer : ITestCaseOrderer
    {
        public const string TypeName = "Stride.Samples.Tests.SampleRunOrderer";
        public const string AssemblyName = "Stride.Samples.Tests";

        public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases) where TTestCase : ITestCase
        {
            var ordered = testCases.OrderBy(x => x.DisplayName, StringComparer.Ordinal).ToList();
            PreparedSamples.Plan(ordered.Select(x => SampleScreenshotTests.SampleOf(x)).OfType<string>());
            return ordered;
        }
    }
}
