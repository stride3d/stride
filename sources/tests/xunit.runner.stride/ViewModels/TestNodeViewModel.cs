// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit.Abstractions;

namespace xunit.runner.stride.ViewModels;

public abstract class TestNodeViewModel : ViewModelBase
{
    public abstract IEnumerable<TestCaseViewModel> EnumerateTestCases();

    public abstract TestCaseViewModel? LocateTestCase(ITestCase testCase);

    public bool Running
    {
        get;
        set => SetValue(ref field, value);
    }
    public bool Failed
    {
        get;
        set => SetValue(ref field, value);
    }
    public bool Succeeded
    {
        get;
        set => SetValue(ref field, value);
    }

    public abstract string DisplayName { get; }

    public abstract void RunTest();
}
