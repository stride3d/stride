// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit.Abstractions;

namespace xunit.runner.stride.ViewModels;

public abstract class TestNodeViewModel : ViewModelBase
{
    public abstract IEnumerable<TestCaseViewModel> EnumerateTestCases();

    public abstract TestCaseViewModel? LocateTestCase(ITestCase testCase);

    bool running;
    public bool Running
    {
        get => running;
        set => SetProperty(ref running, value);
    }

    bool failed;
    public bool Failed
    {
        get => failed;
        set => SetProperty(ref failed, value);
    }

    bool succeeded;
    public bool Succeeded
    {
        get => succeeded;
        set => SetProperty(ref succeeded, value);
    }

    public abstract string DisplayName { get; }
}
