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
        set { if (SetValue(ref field, value)) OnPropertyChanged(nameof(HasResult)); }
    }

    public bool Failed
    {
        get;
        set { if (SetValue(ref field, value)) OnPropertyChanged(nameof(HasResult)); }
    }

    public bool Succeeded
    {
        get;
        set { if (SetValue(ref field, value)) OnPropertyChanged(nameof(HasResult)); }
    }

    public bool Skipped
    {
        get;
        set { if (SetValue(ref field, value)) OnPropertyChanged(nameof(HasResult)); }
    }

    /// <summary>True after the test was queued for a run but before it actually starts;
    /// drives the "…" icon so multi-test runs show what's still waiting.</summary>
    public bool Pending
    {
        get;
        set { if (SetValue(ref field, value)) OnPropertyChanged(nameof(HasResult)); }
    }

    /// <summary>True once the test has any kind of result (running, queued, or finished).
    /// The detail view shows a big "Run" call-to-action when this is false.</summary>
    public bool HasResult => Running || Failed || Succeeded || Skipped || Pending || ElapsedSeconds.HasValue;

    /// <summary>Last execution time in seconds. <see langword="null"/> if not yet run.</summary>
    public decimal? ElapsedSeconds
    {
        get;
        set
        {
            SetValue(ref field, value);
            OnPropertyChanged(nameof(ElapsedDisplay));
            OnPropertyChanged(nameof(HasElapsed));
            OnPropertyChanged(nameof(HasResult));
        }
    }

    public bool HasElapsed => ElapsedSeconds.HasValue;

    public string ElapsedDisplay
    {
        get
        {
            if (!ElapsedSeconds.HasValue) return string.Empty;
            var ms = (double)ElapsedSeconds.Value * 1000.0;
            return ms < 1000.0 ? $"{ms:F0} ms" : $"{ms / 1000.0:F2} s";
        }
    }

    /// <summary>Whether this node passes the current filter. Group nodes are visible if any descendant is visible.</summary>
    public bool IsVisible
    {
        get;
        set => SetValue(ref field, value);
    } = true;

    /// <summary>True for the focused row in a multi-selection — the one shown in the inspect
    /// pane. Drives a small left accent stripe so the primary stands out from the rest of
    /// the selection.</summary>
    public bool IsPrimarySelection
    {
        get;
        set => SetValue(ref field, value);
    }

    public abstract string DisplayName { get; }
}
