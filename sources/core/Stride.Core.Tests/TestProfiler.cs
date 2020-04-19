// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;
using Stride.Core.Diagnostics;

namespace Stride.Core.Tests
{
    public class TestProfiler
    {
        public static readonly ProfilingKey TestGroup = new ProfilingKey("TestProfiler");
        public static readonly ProfilingKey TestKey = new ProfilingKey(TestGroup, "Test", ProfilingKeyFlags.Log);
        public static readonly ProfilingKey Test2Key = new ProfilingKey(TestGroup, "Test2", ProfilingKeyFlags.Log);

        [Fact]
        public void TestSimpleNotEnabled()
        {
            Profiler.Reset();
            var watcher = ExpectLog(new List<MatchMessageDelegate>());
            {
                using (var profile = Profiler.Begin(TestKey))
                {
                    Utilities.Sleep(100);
                }
            }
            watcher.Finish();
        }

        [Fact]
        public void TestSimpleEnabled()
        {
            Profiler.Reset();
            const int timeToWait = 20;
            var watcher = ExpectLog(new List<MatchMessageDelegate>()
            {
                MessageStartWith("[Profiler] #0: Begin: TestProfiler.Test"),
                MessageStartWith("[Profiler] #0: End: TestProfiler.Test", timeToWait),
            });
            {

                Profiler.Enable(TestKey);
                using (var profile = Profiler.Begin(TestKey))
                {
                    Utilities.Sleep(timeToWait);
                }
            }
            watcher.Finish();
        }

        [Fact]
        public void TestSimpleNested()
        {
            Profiler.Reset();
            const int timeToWait = 20;
            var watcher = ExpectLog(new List<MatchMessageDelegate>()
            {
                MessageStartWith("[Profiler] #0: Begin: TestProfiler.Test"),
                MessageStartWith("[Profiler] #1: Begin: TestProfiler.Test2"),
                MessageStartWith("[Profiler] #1: End: TestProfiler.Test2", timeToWait),
                MessageStartWith("[Profiler] #0: End: TestProfiler.Test", timeToWait),
            });
            {

                Profiler.EnableAll();
                using (var profile = Profiler.Begin(TestKey))
                {
                    using (var profile2 = Profiler.Begin(Test2Key))
                    {
                        Utilities.Sleep(timeToWait);
                    }
                }
            }
            watcher.Finish();
        }

        [Fact]
        public void TestWithMarkers()
        {
            Profiler.Reset();
            const int timeToWait = 10;

            var watcher = ExpectLog(new List<MatchMessageDelegate>()
            {
                MessageStartWith("[Profiler] #0: Begin: TestProfiler.Test"),
                MessageStartWith("[Profiler] #0: Mark: TestProfiler.Test", timeToWait),
                MessageStartWith("[Profiler] #0: Mark: TestProfiler.Test", timeToWait * 2),
                MessageStartWith("[Profiler] #0: End: TestProfiler.Test", timeToWait * 2),
            });
            {

                Profiler.EnableAll();
                using (var profile = Profiler.Begin(TestKey))
                {
                    Utilities.Sleep(timeToWait);
                    profile.Mark();

                    Utilities.Sleep(timeToWait);
                    profile.Mark();
                }
            }
            watcher.Finish();
        }


        [Fact]
        public void TestWitAttributes()
        {
            Profiler.Reset();
            const int timeToWait = 10;

            var watcher = ExpectLog(new List<MatchMessageDelegate>()
            {
                MessageStartWith("[Profiler] #0: Begin: TestProfiler.Test"),
                MessageStartWith("[Profiler] #0: Mark: TestProfiler.Test", message => message.Contains("MyAttribute")),
                MessageStartWith("[Profiler] #0: End: TestProfiler.Test", timeToWait),
            });
            {

                Profiler.EnableAll();
                using (var profile = Profiler.Begin(TestKey))
                {
                    profile.SetAttribute("MyAttribute", 5);
                    Utilities.Sleep(timeToWait);
                    profile.Mark();
                }
            }
            watcher.Finish();
        }


        private static Regex matchElapsed = new Regex(@"Elapsed = ([\d\.]+)");

        // Maximum time difference accepted between elapsed time
        private const double ElapsedTimeEpsilon = 10;

        public static MatchMessageDelegate MessageStartWith(string text, Func<string, bool> matchFunction = null)
        {
            return (string message, out string expectingMessage, bool getOnlyExpectingMessage) =>
            {
                expectingMessage = text;

                if (!getOnlyExpectingMessage && matchFunction != null)
                {
                    matchFunction(message);
                }

                return message.StartsWith(text);
            };
        }

        public static MatchMessageDelegate MessageStartWith(string text, double expectedElapsed)
        {
            return MessageStartWith(text, message =>
                {
                    var match = matchElapsed.Match(message);
                    if (match.Success)
                    {
                        var elapsedStr = match.Groups[1].Value;
                        double elapsed;
                        Assert.True(double.TryParse(elapsedStr, out elapsed), $"Expecting parsable double for elapsed [{elapsedStr}]");
                        // Note: just checking minimum time (max time depends too much on OS scheduling to be reliable)
                        Assert.True(elapsed >= expectedElapsed - ElapsedTimeEpsilon, $"Elapsed time [{elapsed}] is faster than expected value [{expectedElapsed}]");
                    }
                    return true;
                });
        }

        public delegate bool MatchMessageDelegate(string message, out string expectingMessage, bool getOnlyExpectingMessage);

        private class ProfilerWatcher
        {
            public int CurrentMessage;

            public readonly List<MatchMessageDelegate> ExpectedMessages;

            public Action<ILogMessage> LogAction;

            public ProfilerWatcher(List<MatchMessageDelegate> expectedMessages)
            {
                ExpectedMessages = expectedMessages;
            }

            // THis is not a using/Dispose because otherwise it would swallow any inner exception and emit an unrelated exception about missing profiler events
            public void Finish()
            {
                GlobalLogger.GlobalMessageLogged -= LogAction;
                var missingMessage = new StringBuilder();
                for (int i = CurrentMessage; i < ExpectedMessages.Count; i++)
                {
                    string expectedMessage;
                    ExpectedMessages[i](string.Empty, out expectedMessage, true);
                    missingMessage.Append(expectedMessage);
                    if ((CurrentMessage + 1) < ExpectedMessages.Count)
                    {
                        missingMessage.AppendLine();
                    }
                }

                Assert.True(CurrentMessage == ExpectedMessages.Count, $"Invalid number of profiler events received [{CurrentMessage}] Expecting [{ExpectedMessages.Count}]. Missing messages: [{missingMessage}]");
            }
        }

        private ProfilerWatcher ExpectLog(List<MatchMessageDelegate> expectedMessages)
        {
            var watcher = new ProfilerWatcher(expectedMessages);
            watcher.LogAction = message =>
            {
                var messageToString = message.ToString();
                Console.Out.WriteLine(message.ToString());
                Console.Out.Flush();

                Assert.True(watcher.CurrentMessage < expectedMessages.Count, $"Unexpected message received: [{messageToString}]");
                string expectedMessage;
                var result = expectedMessages[watcher.CurrentMessage](messageToString, out expectedMessage, false);
                Assert.True(result, $"Expecting message [{expectedMessage}]");
                watcher.CurrentMessage++;
            };
            GlobalLogger.GlobalMessageLogged += watcher.LogAction;
            return watcher;
        }
    }
}
