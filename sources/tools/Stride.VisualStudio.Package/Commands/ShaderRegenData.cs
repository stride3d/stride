// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.VisualStudio.Extensibility.UI;

namespace Stride.VisualStudio.Commands;

/// <summary>
/// Data context for <see cref="ShaderRegenControl"/>: a title, the live CLI log streamed while it runs (one entry
/// per line, colour-classified like the Update dialog), and a final result message.
/// </summary>
[DataContract]
internal sealed class ShaderRegenData : NotifyPropertyChangedObject
{
    private bool isRunning = true;
    private string resultMessage = string.Empty;

    public ShaderRegenData(string title) => Title = title;

    [DataMember]
    public string Title { get; }

    /// <summary>The CLI output, one entry per line, streamed while regeneration runs.</summary>
    [DataMember]
    public ObservableList<LogLine> LogLines { get; } = new();

    /// <summary>True while the CLI is running (drives the progress bar).</summary>
    [DataMember]
    public bool IsRunning
    {
        get => isRunning;
        private set => SetProperty(ref isRunning, value);
    }

    /// <summary>Set when regeneration finishes, with the success or failure summary.</summary>
    [DataMember]
    public string ResultMessage
    {
        get => resultMessage;
        private set => SetProperty(ref resultMessage, value);
    }

    public void Append(string line) => LogLines.Add(LogLine.Classify(line));

    public void Complete(string message)
    {
        IsRunning = false;
        ResultMessage = message;
    }
}
