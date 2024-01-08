// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;

namespace xunit.runner.stride.ViewModels;

public class TestGroupViewModel : TestNodeViewModel
{
    private readonly TestsViewModel tests;
    private readonly string displayName;

    public List<TestNodeViewModel> Children { get; } = [];

    public TestGroupViewModel(TestsViewModel tests, string displayName)
    {
        this.tests = tests;
        this.displayName = displayName;
    }

    public override IEnumerable<TestCaseViewModel> EnumerateTestCases() => Children.SelectMany(x => x.EnumerateTestCases());

    public void RunTest()
    {
        tests.RunTests(this);
    }

    public override TestCaseViewModel LocateTestCase(ITestCase testCase)
    {
        foreach (var child in Children)
        {
            var result = child.LocateTestCase(testCase);
            if (result != null)
                return result;
        }
        return null;
    }

    public override string DisplayName => displayName;
}
