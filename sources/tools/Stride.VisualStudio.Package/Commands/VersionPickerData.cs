// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.VisualStudio.Extensibility.UI;

namespace Stride.VisualStudio.Commands;

/// <summary>One upgrade-log line and its severity ("Info", "Warning" or "Error"), used to colour it.</summary>
[DataContract]
internal sealed class LogLine
{
    public LogLine(string text, string severity)
    {
        Text = text;
        Severity = severity;
    }

    [DataMember]
    public string Text { get; }

    [DataMember]
    public string Severity { get; }

    /// <summary>Classifies a line by its leading word or an MSBuild-style ": warning"/": error".</summary>
    public static LogLine Classify(string text)
    {
        var firstWord = new string(text.TrimStart().TakeWhile(char.IsLetter).ToArray());
        if (firstWord.Equals("error", StringComparison.OrdinalIgnoreCase) || text.Contains(": error", StringComparison.OrdinalIgnoreCase))
            return new LogLine(text, "Error");
        if (firstWord.Equals("warning", StringComparison.OrdinalIgnoreCase) || text.Contains(": warning", StringComparison.OrdinalIgnoreCase))
            return new LogLine(text, "Warning");
        return new LogLine(text, "Info");
    }
}

/// <summary>A major.minor line (e.g. "4.4") and its versions, newest first.</summary>
[DataContract]
internal sealed class VersionLine
{
    public VersionLine(string line, IReadOnlyList<string> versions)
    {
        Line = line;
        Versions = versions;
    }

    [DataMember]
    public string Line { get; }

    [DataMember]
    public IReadOnlyList<string> Versions { get; }
}

/// <summary>
/// Data context for <see cref="VersionPickerControl"/>. Runs the whole flow in one dialog: a loading state
/// while versions are fetched, a major.minor line + version picker with a pre-release toggle, and then an
/// in-place upgrade that streams its log into the dialog.
/// </summary>
[DataContract]
internal sealed class VersionPickerData : NotifyPropertyChangedObject
{
    private IReadOnlyList<VersionLine> stableLines = [];
    private IReadOnlyList<VersionLine> allLines = [];
    private string selectedLine = string.Empty;
    private string selectedVersion = string.Empty;
    private bool includePrerelease;
    private bool isLoading = true;
    private bool hasVersions;
    private bool hasPrerelease;
    private bool isUpdating;
    private bool showLog;
    private string statusMessage = string.Empty;
    private string resultMessage = string.Empty;
    private Func<string, Func<string, Task>, CancellationToken, Task<bool>>? updateAction;

    public VersionPickerData(string prompt)
    {
        Prompt = prompt;
        UpdateCommand = new AsyncCommand((_, cancellationToken) => RunUpdateAsync(cancellationToken)) { CanExecute = false };
    }

    [DataMember]
    public string Prompt { get; }

    [DataMember]
    public ObservableList<string> Lines { get; } = new();

    [DataMember]
    public ObservableList<string> Versions { get; } = new();

    /// <summary>The upgrade log, one entry per line, streamed while the upgrade runs.</summary>
    [DataMember]
    public ObservableList<LogLine> LogLines { get; } = new();

    /// <summary>Runs the upgrade for the selected version; enabled only once a version is picked.</summary>
    [DataMember]
    public AsyncCommand UpdateCommand { get; }

    /// <summary>True while versions are being fetched (drives the loading progress bar).</summary>
    [DataMember]
    public bool IsLoading
    {
        get => isLoading;
        private set => SetProperty(ref isLoading, value);
    }

    /// <summary>True once versions are available to pick (drives the combo boxes being enabled).</summary>
    [DataMember]
    public bool HasVersions
    {
        get => hasVersions;
        private set => SetProperty(ref hasVersions, value);
    }

    /// <summary>Whether any pre-release versions exist to toggle (drives the checkbox being enabled).</summary>
    [DataMember]
    public bool HasPrerelease
    {
        get => hasPrerelease;
        private set => SetProperty(ref hasPrerelease, value);
    }

    /// <summary>True while the upgrade runs (drives the upgrade progress bar).</summary>
    [DataMember]
    public bool IsUpdating
    {
        get => isUpdating;
        private set => SetProperty(ref isUpdating, value);
    }

    /// <summary>True once an upgrade has started, so the log area is shown.</summary>
    [DataMember]
    public bool ShowLog
    {
        get => showLog;
        private set => SetProperty(ref showLog, value);
    }

    /// <summary>Set when the fetch produced nothing or failed; shown in place of the versions.</summary>
    [DataMember]
    public string StatusMessage
    {
        get => statusMessage;
        private set => SetProperty(ref statusMessage, value);
    }

    /// <summary>Set when the upgrade finishes, with the success or failure summary.</summary>
    [DataMember]
    public string ResultMessage
    {
        get => resultMessage;
        private set => SetProperty(ref resultMessage, value);
    }

    [DataMember]
    public bool IncludePrerelease
    {
        get => includePrerelease;
        set
        {
            if (SetProperty(ref includePrerelease, value))
                RefreshLines();
        }
    }

    [DataMember]
    public string SelectedLine
    {
        get => selectedLine;
        set
        {
            if (SetProperty(ref selectedLine, value))
                RefreshVersions();
        }
    }

    [DataMember]
    public string SelectedVersion
    {
        get => selectedVersion;
        set
        {
            if (SetProperty(ref selectedVersion, value))
                RefreshCanUpdate();
        }
    }

    /// <summary>Supplies the delegate that performs the upgrade (given the version and a log-line sink).</summary>
    public void SetUpdateAction(Func<string, Func<string, Task>, CancellationToken, Task<bool>> action) => updateAction = action;

    /// <summary>Fills the picker with the fetched versions and leaves the loading state.</summary>
    public void Populate(IReadOnlyList<VersionLine> stable, IReadOnlyList<VersionLine> all)
    {
        stableLines = stable;
        allLines = all;
        HasPrerelease = all.Sum(line => line.Versions.Count) > stable.Sum(line => line.Versions.Count);
        // Start on pre-releases when there's no stable version to offer, so the picker isn't empty. Set the
        // property (not the field) so the checkbox reflects it; then rebuild the lines for the false case too.
        IncludePrerelease = stable.Count == 0 && all.Count > 0;
        RefreshLines();
        HasVersions = true;
        IsLoading = false;
        RefreshCanUpdate();
    }

    /// <summary>Leaves the loading state with a message instead of a version list (empty result or error).</summary>
    public void Fail(string message)
    {
        StatusMessage = message;
        IsLoading = false;
    }

    private async Task RunUpdateAsync(CancellationToken cancellationToken)
    {
        if (updateAction is null || isUpdating || string.IsNullOrWhiteSpace(selectedVersion))
            return;

        IsUpdating = true;
        ShowLog = true;
        RefreshCanUpdate();
        LogLines.Add(LogLine.Classify($"Updating to Stride {selectedVersion}…"));

        bool succeeded;
        try
        {
            succeeded = await updateAction(selectedVersion, line => { LogLines.Add(LogLine.Classify(line)); return Task.CompletedTask; }, cancellationToken);
        }
        catch (Exception exception)
        {
            LogLines.Add(LogLine.Classify(exception.Message));
            succeeded = false;
        }

        IsUpdating = false;
        ResultMessage = succeeded
            ? "Update complete. Accept Visual Studio's prompt to reload the changed projects, then reopen in Game Studio to migrate assets and build."
            : "Update failed — see the log above.";
        RefreshCanUpdate();
    }

    // The upgrade button is enabled once a version is picked, and only while no upgrade is running or finished.
    private void RefreshCanUpdate()
        => UpdateCommand.CanExecute = hasVersions && !isUpdating && resultMessage.Length == 0 && !string.IsNullOrWhiteSpace(selectedVersion);

    private IReadOnlyList<VersionLine> CurrentLines => includePrerelease ? allLines : stableLines;

    // Rebuilds the line list for the current pre-release mode, keeping the selected line when it stays present
    // and otherwise falling back to the newest; setting the line cascades to its versions.
    private void RefreshLines()
    {
        var previous = selectedLine;
        Lines.Clear();
        foreach (var line in CurrentLines)
            Lines.Add(line.Line);

        SelectedLine = CurrentLines.Any(line => line.Line == previous) ? previous : CurrentLines.FirstOrDefault()?.Line ?? string.Empty;
    }

    // Rebuilds the version list for the selected line, keeping the selected version when it stays present and
    // otherwise falling back to the newest in the line.
    private void RefreshVersions()
    {
        var line = CurrentLines.FirstOrDefault(candidate => candidate.Line == selectedLine);
        var previous = selectedVersion;
        Versions.Clear();
        foreach (var version in line?.Versions ?? [])
            Versions.Add(version);

        SelectedVersion = line is not null && line.Versions.Contains(previous) ? previous : line?.Versions.FirstOrDefault() ?? string.Empty;
    }
}
