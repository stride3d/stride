// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;

namespace xunit.runner.stride;

/// <summary>
///   Emits a Visual Studio TRX test result file from xunit message events. Parsable by
///   <c>dotnet test --logger trx</c> consumers and Azure DevOps.
/// </summary>
public sealed class TrxWriter : TestMessageSink
{
    private static readonly XNamespace Ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
    private static readonly Guid TestType = new("13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b"); // xunit "UnitTest"
    private static readonly Guid TestListAll = new("19431567-8539-422a-85d7-44ee4e166bda");
    private static readonly Guid TestListNotInList = new("8c84fa94-04c1-424b-9868-57a2d4851a1d");

    private readonly Dictionary<string, ResultEntry> resultsByCaseId = new();
    private readonly DateTime startTime = DateTime.UtcNow;
    private DateTime endTime = DateTime.UtcNow;
    private string? assemblyName;
    private decimal totalExecutionSeconds;
    private int testsTotal;
    private int testsFailed;
    private int testsSkipped;

    public ManualResetEvent Finished { get; } = new(initialState: false);

    public TrxWriter()
    {
        Execution.TestPassedEvent += a => RecordOutcome(a.Message, Outcome.Passed, error: null);
        Execution.TestFailedEvent += a =>
        {
            var error = new ErrorInfo
            {
                Message = a.Message.Messages is { Length: > 0 } m ? string.Join("\n", m) : null,
                StackTrace = a.Message.StackTraces is { Length: > 0 } s ? string.Join("\n", s) : null,
            };
            RecordOutcome(a.Message, Outcome.Failed, error);
        };
        Execution.TestSkippedEvent += a => RecordOutcome(a.Message, Outcome.NotExecuted,
            new ErrorInfo { Message = a.Message.Reason, StackTrace = null });
        Execution.TestAssemblyStartingEvent += a => assemblyName ??= a.Message.TestAssembly.Assembly.Name;
        Execution.TestAssemblyFinishedEvent += a =>
        {
            totalExecutionSeconds = a.Message.ExecutionTime;
            testsTotal = a.Message.TestsRun;
            testsFailed = a.Message.TestsFailed;
            testsSkipped = a.Message.TestsSkipped;
            endTime = DateTime.UtcNow;
            Finished.Set();
        };
    }

    private void RecordOutcome(ITestResultMessage message, Outcome outcome, ErrorInfo? error)
    {
        var test = message.Test;
        var testCase = test.TestCase;
        var key = testCase.UniqueID;
        // xunit can report multiple results for one test case (theories), keyed per result;
        // fold them under a single TRX entry, taking the worst outcome.
        if (!resultsByCaseId.TryGetValue(key, out var entry))
        {
            entry = new ResultEntry
            {
                TestId = DeterministicGuid(testCase.UniqueID),
                ExecutionId = Guid.NewGuid(),
                DisplayName = test.DisplayName,
                ClassName = testCase.TestMethod?.TestClass?.Class?.Name ?? string.Empty,
                MethodName = testCase.TestMethod?.Method?.Name ?? test.DisplayName,
            };
            resultsByCaseId[key] = entry;
        }
        // Worse outcome wins (Failed > NotExecuted > Passed).
        if (outcome == Outcome.Failed || (outcome == Outcome.NotExecuted && entry.Outcome == Outcome.Passed))
        {
            entry.Outcome = outcome;
            entry.Error = error;
        }
        entry.DurationSeconds += (double)message.ExecutionTime;
    }

    /// <summary>Writes the accumulated results to a TRX file at <paramref name="path"/>.</summary>
    public void WriteTo(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var runId = Guid.NewGuid();
        var runUser = Environment.UserName;
        var computer = Environment.MachineName;

        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(Ns + "TestRun",
                new XAttribute("id", runId.ToString()),
                new XAttribute("name", $"{runUser}@{computer} {startTime:yyyy-MM-dd HH:mm:ss}"),
                new XAttribute("runUser", runUser),
                new XElement(Ns + "Times",
                    new XAttribute("creation", startTime.ToString("o", CultureInfo.InvariantCulture)),
                    new XAttribute("queuing", startTime.ToString("o", CultureInfo.InvariantCulture)),
                    new XAttribute("start", startTime.ToString("o", CultureInfo.InvariantCulture)),
                    new XAttribute("finish", endTime.ToString("o", CultureInfo.InvariantCulture))),
                new XElement(Ns + "TestSettings",
                    new XAttribute("name", "default"),
                    new XAttribute("id", Guid.NewGuid().ToString())),
                new XElement(Ns + "Results", BuildResults()),
                new XElement(Ns + "TestDefinitions", BuildDefinitions()),
                new XElement(Ns + "TestEntries", BuildEntries()),
                new XElement(Ns + "TestLists",
                    new XElement(Ns + "TestList",
                        new XAttribute("name", "Results Not in a List"),
                        new XAttribute("id", TestListNotInList.ToString())),
                    new XElement(Ns + "TestList",
                        new XAttribute("name", "All Loaded Results"),
                        new XAttribute("id", TestListAll.ToString()))),
                new XElement(Ns + "ResultSummary",
                    new XAttribute("outcome", testsFailed > 0 ? "Failed" : "Completed"),
                    new XElement(Ns + "Counters",
                        new XAttribute("total", testsTotal),
                        new XAttribute("executed", testsTotal - testsSkipped),
                        new XAttribute("passed", testsTotal - testsFailed - testsSkipped),
                        new XAttribute("failed", testsFailed),
                        new XAttribute("error", 0),
                        new XAttribute("notExecuted", testsSkipped)))));

        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var xw = XmlWriter.Create(fs, new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) });
        doc.Save(xw);
    }

    private IEnumerable<XElement> BuildResults()
    {
        foreach (var entry in resultsByCaseId.Values)
        {
            var resultEl = new XElement(Ns + "UnitTestResult",
                new XAttribute("executionId", entry.ExecutionId.ToString()),
                new XAttribute("testId", entry.TestId.ToString()),
                new XAttribute("testName", entry.DisplayName),
                new XAttribute("computerName", Environment.MachineName),
                new XAttribute("duration", TimeSpan.FromSeconds(entry.DurationSeconds).ToString("c", CultureInfo.InvariantCulture)),
                new XAttribute("startTime", startTime.ToString("o", CultureInfo.InvariantCulture)),
                new XAttribute("endTime", endTime.ToString("o", CultureInfo.InvariantCulture)),
                new XAttribute("testType", TestType.ToString()),
                new XAttribute("outcome", entry.Outcome.ToString()),
                new XAttribute("testListId", TestListNotInList.ToString()),
                new XAttribute("relativeResultsDirectory", entry.ExecutionId.ToString()));
            if (entry.Error is { } error)
            {
                resultEl.Add(new XElement(Ns + "Output",
                    new XElement(Ns + "ErrorInfo",
                        error.Message is null ? null : new XElement(Ns + "Message", error.Message),
                        error.StackTrace is null ? null : new XElement(Ns + "StackTrace", error.StackTrace))));
            }
            yield return resultEl;
        }
    }

    private IEnumerable<XElement> BuildDefinitions()
    {
        foreach (var entry in resultsByCaseId.Values)
        {
            yield return new XElement(Ns + "UnitTest",
                new XAttribute("name", entry.DisplayName),
                new XAttribute("storage", assemblyName ?? string.Empty),
                new XAttribute("id", entry.TestId.ToString()),
                new XElement(Ns + "Execution", new XAttribute("id", entry.ExecutionId.ToString())),
                new XElement(Ns + "TestMethod",
                    new XAttribute("codeBase", assemblyName ?? string.Empty),
                    new XAttribute("adapterTypeName", "executor://xunit/VsTestRunner2/netcoreapp"),
                    new XAttribute("className", entry.ClassName),
                    new XAttribute("name", entry.MethodName)));
        }
    }

    private IEnumerable<XElement> BuildEntries()
    {
        foreach (var entry in resultsByCaseId.Values)
        {
            yield return new XElement(Ns + "TestEntry",
                new XAttribute("testId", entry.TestId.ToString()),
                new XAttribute("executionId", entry.ExecutionId.ToString()),
                new XAttribute("testListId", TestListNotInList.ToString()));
        }
    }

    // Stable ID per test case so reruns produce identical TRX testIds (helpful for diffing).
    private static Guid DeterministicGuid(string input)
    {
        var bytes = System.Security.Cryptography.SHA1.HashData(Encoding.UTF8.GetBytes(input));
        var guidBytes = new byte[16];
        Array.Copy(bytes, guidBytes, 16);
        return new Guid(guidBytes);
    }

    private enum Outcome { Passed, Failed, NotExecuted }

    private sealed class ResultEntry
    {
        public Guid TestId;
        public Guid ExecutionId;
        public string DisplayName = string.Empty;
        public string ClassName = string.Empty;
        public string MethodName = string.Empty;
        public double DurationSeconds;
        public Outcome Outcome = Outcome.Passed;
        public ErrorInfo? Error;
    }

    private sealed class ErrorInfo
    {
        public string? Message;
        public string? StackTrace;
    }
}
