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
        set { if (SetProperty(ref running, value)) OnPropertyChanged(nameof(HasResult)); }
    }

    bool failed;
    public bool Failed
    {
        get => failed;
        set { if (SetProperty(ref failed, value)) OnPropertyChanged(nameof(HasResult)); }
    }

    bool succeeded;
    public bool Succeeded
    {
        get => succeeded;
        set { if (SetProperty(ref succeeded, value)) OnPropertyChanged(nameof(HasResult)); }
    }

    bool skipped;
    public bool Skipped
    {
        get => skipped;
        set { if (SetProperty(ref skipped, value)) OnPropertyChanged(nameof(HasResult)); }
    }

    bool pending;
    /// <summary>True after the test was queued for a run but before it actually starts;
    /// drives the "…" icon so multi-test runs show what's still waiting.</summary>
    public bool Pending
    {
        get => pending;
        set { if (SetProperty(ref pending, value)) OnPropertyChanged(nameof(HasResult)); }
    }

    /// <summary>True once the test has any kind of result (running, queued, or finished).
    /// The detail view shows a big "Run" call-to-action when this is false.</summary>
    public bool HasResult => running || failed || succeeded || skipped || pending || elapsedSeconds.HasValue;

    decimal? elapsedSeconds;
    /// <summary>Last execution time in seconds. <see langword="null"/> if not yet run.</summary>
    public decimal? ElapsedSeconds
    {
        get => elapsedSeconds;
        set
        {
            SetProperty(ref elapsedSeconds, value);
            OnPropertyChanged(nameof(ElapsedDisplay));
            OnPropertyChanged(nameof(HasElapsed));
            OnPropertyChanged(nameof(HasResult));
        }
    }

    public bool HasElapsed => elapsedSeconds.HasValue;

    public string ElapsedDisplay
    {
        get
        {
            if (!elapsedSeconds.HasValue) return string.Empty;
            var ms = (double)elapsedSeconds.Value * 1000.0;
            return ms < 1000.0 ? $"{ms:F0} ms" : $"{ms / 1000.0:F2} s";
        }
    }

    bool isVisible = true;
    /// <summary>Whether this node passes the current filter. Group nodes are visible if any descendant is visible.</summary>
    public bool IsVisible
    {
        get => isVisible;
        set => SetProperty(ref isVisible, value);
    }

    bool isPrimarySelection;
    /// <summary>True for the focused row in a multi-selection — the one shown in the inspect
    /// pane. Drives a small left accent stripe so the primary stands out from the rest of
    /// the selection.</summary>
    public bool IsPrimarySelection
    {
        get => isPrimarySelection;
        set => SetProperty(ref isPrimarySelection, value);
    }

    public abstract string DisplayName { get; }
}
