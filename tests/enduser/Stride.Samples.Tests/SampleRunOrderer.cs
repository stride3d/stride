// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.SampleScreenshotRunner;
using Xunit.Sdk;
using Xunit.v3;

namespace Stride.Samples.Tests
{
    /// <summary>
    /// Runs the tests by name, and gives the sample of each test to <see cref="PreparedSamples"/> in that order, so the
    /// samples the run uses are prepared before its first test, once each.
    /// </summary>
    public sealed class SampleRunOrderer : ITestCaseOrderer
    {
        public IReadOnlyCollection<TTestCase> OrderTestCases<TTestCase>(IReadOnlyCollection<TTestCase> testCases) where TTestCase : notnull, ITestCase
        {
            var ordered = testCases.OrderBy(x => x.TestCaseDisplayName, StringComparer.Ordinal).ToList();
            PreparedSamples.Plan(ordered.OfType<IXunitTestCase>().Select(SampleScreenshotTests.SampleOf).OfType<string>());
            return ordered;
        }
    }
}
