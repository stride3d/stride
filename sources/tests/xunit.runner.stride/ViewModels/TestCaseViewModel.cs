// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit.Abstractions;

namespace xunit.runner.stride.ViewModels;

public class TestCaseViewModel : TestNodeViewModel
{
    private readonly TestsViewModel tests;

    public ITestCase TestCase { get; }

    public TestCaseViewModel(TestsViewModel tests, ITestCase testCase)
    {
        this.tests = tests;
        TestCase = testCase;
    }

    public void RunTest()
    {
        tests.RunTests(this);
    }

    public override IEnumerable<TestCaseViewModel> EnumerateTestCases()
    {
        yield return this;
    }

    public override TestCaseViewModel? LocateTestCase(ITestCase testCase)
    {
        return (testCase == TestCase) ? this : null;
    }

    public override string DisplayName => TestCase.DisplayName;
}
