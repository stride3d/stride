// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;

using Xunit;
using Stride.Core.Diagnostics;

namespace Stride.Core.Tests
{
    public class TestLogger
    {
        [Fact]
        public void TestLocalLogger()
        {
            var log = new LoggerResult();

            log.Info("#0");
            Assert.Single(log.Messages);
            Assert.Equal(LogMessageType.Info, log.Messages[0].Type);
            Assert.Equal("#0", log.Messages[0].Text);

            log.Info("#1");
            Assert.Equal(2, log.Messages.Count);
            Assert.Equal(LogMessageType.Info, log.Messages[1].Type);
            Assert.Equal("#1", log.Messages[1].Text);

            Assert.False(log.HasErrors);

            log.Error("#2");
            Assert.Equal(3, log.Messages.Count);
            Assert.Equal(LogMessageType.Error, log.Messages[2].Type);
            Assert.Equal("#2", log.Messages[2].Text);

            Assert.True(log.HasErrors);

            log.Error("#3");
            Assert.Equal(4, log.Messages.Count);
            Assert.Equal(LogMessageType.Error, log.Messages[3].Type);
            Assert.Equal("#3", log.Messages[3].Text);

            // Activate log from Info to Fatal. Verbose won't be logged.
            log.ActivateLog(LogMessageType.Info);
            log.Verbose("#4");
            Assert.Equal(4, log.Messages.Count);

            // Activate log from Verbose to Fatal. Verbose will be logged
            log.ActivateLog(LogMessageType.Verbose);
            log.Verbose("#4");
            Assert.Equal(5, log.Messages.Count);

            // Activate log from Info to Fatal and only Debug. Verbose won't be logged.
            log.ActivateLog(LogMessageType.Info);
            log.ActivateLog(LogMessageType.Debug, true);
            log.Verbose("#5");
            log.Debug("#6");
            Assert.Equal(6, log.Messages.Count);
            Assert.Equal("#6", log.Messages[5].Text);
        }

        [Fact]
        public void TestGlobalLogger()
        {
            var log = GlobalLogger.GetLogger("Module1");
            var logbis = GlobalLogger.GetLogger("Module1");
            var log1x = GlobalLogger.GetLogger("Module1x");

            // Check that we get the same instance.
            Assert.Equal(logbis, log);

            // This should work but no handler is installed.
            log.Info("TEST");

            // Instal a message handler.
            var messages = new List<ILogMessage>();
            GlobalLogger.GlobalMessageLogged += messages.Add;

            // Log a simple message (disabled by default).
            log.Verbose("#0");
            Assert.Empty(messages);

            // Activate the log for all loggers starting from Info
            GlobalLogger.ActivateLog(".*", LogMessageType.Verbose);

            // Log a simple message
            log.Verbose("#0");
            Assert.Single(messages);
            Assert.Equal("#0", messages[0].Text);

            // Activate the log for Module1x starting from Debug
            GlobalLogger.ActivateLog(".*x", LogMessageType.Debug);
            log1x.Debug("#1");
            Assert.Equal(2, messages.Count);
            Assert.Equal("#1", messages[1].Text);
        }


        [Fact]
        public void TestCallerInfo()
        {
            var log = new LoggerResult();

            // Use the caller information
            log.Info("#0", CallerInfo.Get());
            log.Info("#1", CallerInfo.Get());

            Assert.Equal(2, log.Messages.Count);
            Assert.NotNull(((LogMessage)log.Messages[0]).CallerInfo);
            Assert.Contains("TestLogger", ((LogMessage)log.Messages[0]).CallerInfo.FilePath);
            Assert.True(((LogMessage)log.Messages[0]).CallerInfo.LineNumber > 0);
            Assert.NotNull(((LogMessage)log.Messages[1]).CallerInfo);
            Assert.Equal(((LogMessage)log.Messages[1]).CallerInfo.LineNumber, ((LogMessage)log.Messages[0]).CallerInfo.LineNumber + 1);
        }
    }
}
