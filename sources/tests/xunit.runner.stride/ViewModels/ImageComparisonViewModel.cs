// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.IO;
using Avalonia.Media.Imaging;

namespace xunit.runner.stride.ViewModels;

/// <summary>
///   One screenshot-vs-gold comparison reported by GameTestBase. A single test may produce
///   many of these (UI tests, in particular, capture per-frame back-buffers), so they live
///   in an ObservableCollection on the owning <see cref="TestCaseViewModel"/>.
/// </summary>
public class ImageComparisonViewModel : ViewModelBase
{
    public ImageComparisonViewModel(ImageCompareResult result)
    {
        CurrentPath = result.CurrentPath;
        ReferencePath = result.ReferencePath;
        Passed = result.Passed;
        StatsSummary = result.StatsSummary;
        // Filename (no extension) is the most informative compact label for stacks of frames.
        Label = Path.GetFileNameWithoutExtension(
            !string.IsNullOrEmpty(result.CurrentPath) ? result.CurrentPath : result.ReferencePath);
    }

    public string CurrentPath { get; }
    public string ReferencePath { get; }
    public bool Passed { get; }
    public string? StatsSummary { get; }
    public string Label { get; }

    public Bitmap? CurrentBitmap
    {
        get;
        set
        {
            if (SetValue(ref field, value))
            {
                OnPropertyChanged(nameof(ShowCurrentImage));
                OnPropertyChanged(nameof(ShowCurrentPlaceholder));
            }
        }
    }

    public Bitmap? ReferenceBitmap
    {
        get;
        set
        {
            if (SetValue(ref field, value))
            {
                OnPropertyChanged(nameof(ShowReferenceImage));
                OnPropertyChanged(nameof(ShowReferencePlaceholder));
            }
        }
    }

    public WriteableBitmap? DiffBitmap
    {
        get;
        set
        {
            if (SetValue(ref field, value))
            {
                OnPropertyChanged(nameof(ShowDiffImage));
                OnPropertyChanged(nameof(ShowDiffPassed));
                OnPropertyChanged(nameof(ShowDiffPlaceholder));
            }
        }
    }

    public bool ShowCurrentImage => CurrentBitmap is not null;
    public bool ShowCurrentPlaceholder => CurrentBitmap is null && Passed;
    public bool ShowReferenceImage => ReferenceBitmap is not null;
    public bool ShowReferencePlaceholder => ReferenceBitmap is null;
    public bool ShowDiffImage => DiffBitmap is not null;
    public bool ShowDiffPassed => DiffBitmap is null && Passed;
    public bool ShowDiffPlaceholder => DiffBitmap is null && !Passed;
}
