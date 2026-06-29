// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text;

namespace xunit.runner.stride.ViewModels;

/// <summary>
///   A <see cref="TextWriter"/> that fans every write to the original underlying stream AND
///   to a per-test buffer selected by <see cref="CurrentTestId"/>. Used to capture
///   <c>Console.Out</c> / <c>Console.Error</c> output produced while a specific xUnit test
///   case is running, so the inspect panel can show it post-mortem.
/// </summary>
internal sealed class TestOutputRedirector : TextWriter
{
    readonly TextWriter inner;
    readonly Dictionary<string, StringBuilder> buffers = new();
    readonly object gate = new();

    public TestOutputRedirector(TextWriter inner) => this.inner = inner;

    public override Encoding Encoding => inner.Encoding;

    /// <summary>The id of the test currently running; writes are buffered against this id when non-null.</summary>
    public volatile string? CurrentTestId;

    public override void Write(char value)
    {
        inner.Write(value);
        AppendCurrent(value.ToString());
    }

    public override void Write(string? value)
    {
        inner.Write(value);
        if (value is not null) AppendCurrent(value);
    }

    public override void WriteLine(string? value)
    {
        inner.WriteLine(value);
        AppendCurrent((value ?? string.Empty) + Environment.NewLine);
    }

    public override void Write(char[] buffer, int index, int count)
    {
        inner.Write(buffer, index, count);
        AppendCurrent(new string(buffer, index, count));
    }

    void AppendCurrent(string text)
    {
        var id = CurrentTestId;
        if (id is null) return;
        lock (gate)
        {
            if (!buffers.TryGetValue(id, out var sb))
            {
                sb = new StringBuilder();
                buffers[id] = sb;
            }
            sb.Append(text);
        }
    }

    /// <summary>Removes and returns the buffered output for the specified test id, if any.</summary>
    public string? TakeOutput(string id)
    {
        lock (gate)
        {
            if (!buffers.Remove(id, out var sb)) return null;
            var s = sb.ToString();
            return s.Length == 0 ? null : s;
        }
    }

    /// <summary>Returns a snapshot of the current buffer for the specified test id without removing it.</summary>
    public string? PeekOutput(string id)
    {
        lock (gate)
        {
            if (!buffers.TryGetValue(id, out var sb) || sb.Length == 0) return null;
            return sb.ToString();
        }
    }
}
