// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
        ImageComparisons.CollectionChanged += OnImageComparisonsChanged;
    }

    void OnImageComparisonsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => OnPropertyChanged(nameof(HasImageComparison));

    public void RunTest() => tests.RunTests(this, interactive: false);
    public void RunInteractive() => tests.RunTests(this, interactive: true);

    public override IEnumerable<TestCaseViewModel> EnumerateTestCases()
    {
        yield return this;
    }

    public override TestCaseViewModel? LocateTestCase(ITestCase testCase)
    {
        return (testCase == TestCase) ? this : null;
    }

    public override string DisplayName => TestCase.DisplayName;

    // Tree row label: drop the `Namespace.Class.` prefix since the parent group already carries
    // the class name. Inspect-panel title and filter matching still use the full DisplayName.
    public string ShortDisplayName
    {
        get
        {
            var full = TestCase.DisplayName;
            var prefix = TestCase.TestMethod.TestClass.Class.Name + ".";
            return full.StartsWith(prefix, StringComparison.Ordinal) ? full[prefix.Length..] : full;
        }
    }

    /// <summary>Combined exception messages from the most recent run (newline-joined). Empty if the test passed or hasn't run.</summary>
    public string? FailureMessage
    {
        get;
        set => SetValue(ref field, value);
    }

    /// <summary>Combined stack traces from the most recent run. Empty if the test passed or hasn't run.</summary>
    public string? FailureStackTrace
    {
        get;
        set => SetValue(ref field, value);
    }

    /// <summary>Captured stdout/stderr produced during the most recent run.</summary>
    public string? Output
    {
        get;
        set
        {
            SetValue(ref field, value);
            OnPropertyChanged(nameof(HasOutput));
        }
    }

    public bool HasOutput => !string.IsNullOrEmpty(Output);

    // === Image comparison (populated from ImageComparisonCompleted event in GameTestBase) ===

    /// <summary>Every screenshot/gold comparison reported during the test's most recent run,
    /// in order. UI tests typically push many; graphics tests usually one.</summary>
    public ObservableCollection<ImageComparisonViewModel> ImageComparisons { get; } = new();

    public bool HasImageComparison => ImageComparisons.Count > 0;
}
