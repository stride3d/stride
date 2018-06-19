// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;

using NUnit.Framework;
using Xenko.Core.Diagnostics;

namespace Xenko.Core.Tests
{
    [TestFixture]
    [Description("Tests for Logger")]
    public class TestLogger
    {
        [Test]
        public void TestLocalLogger()
        {
            var log = new LoggerResult();

            log.Info("#0");
            Assert.That(log.Messages.Count, Is.EqualTo(1));
            Assert.That(log.Messages[0].Type, Is.EqualTo(LogMessageType.Info));
            Assert.That(log.Messages[0].Text, Is.EqualTo("#0"));

            log.Info("#1");
            Assert.That(log.Messages.Count, Is.EqualTo(2));
            Assert.That(log.Messages[1].Type, Is.EqualTo(LogMessageType.Info));
            Assert.That(log.Messages[1].Text, Is.EqualTo("#1"));

            Assert.That(log.HasErrors, Is.False);

            log.Error("#2");
            Assert.That(log.Messages.Count, Is.EqualTo(3));
            Assert.That(log.Messages[2].Type, Is.EqualTo(LogMessageType.Error));
            Assert.That(log.Messages[2].Text, Is.EqualTo("#2"));

            Assert.That(log.HasErrors, Is.True);

            log.Error("#3");
            Assert.That(log.Messages.Count, Is.EqualTo(4));
            Assert.That(log.Messages[3].Type, Is.EqualTo(LogMessageType.Error));
            Assert.That(log.Messages[3].Text, Is.EqualTo("#3"));

            // Activate log from Info to Fatal. Verbose won't be logged.
            log.ActivateLog(LogMessageType.Info);
            log.Verbose("#4");
            Assert.That(log.Messages.Count, Is.EqualTo(4));

            // Activate log from Verbose to Fatal. Verbose will be logged
            log.ActivateLog(LogMessageType.Verbose);
            log.Verbose("#4");
            Assert.That(log.Messages.Count, Is.EqualTo(5));

            // Activate log from Info to Fatal and only Debug. Verbose won't be logged.
            log.ActivateLog(LogMessageType.Info);
            log.ActivateLog(LogMessageType.Debug, true);
            log.Verbose("#5");
            log.Debug("#6");
            Assert.That(log.Messages.Count, Is.EqualTo(6));
            Assert.That(log.Messages[5].Text, Is.EqualTo("#6"));
        }

        [Test]
        public void TestGlobalLogger()
        {
            var log = GlobalLogger.GetLogger("Module1");
            var logbis = GlobalLogger.GetLogger("Module1");
            var log1x = GlobalLogger.GetLogger("Module1x");

            // Check that we get the same instance.
            Assert.That(log, Is.EqualTo(logbis));

            // This should work but no handler is installed.
            log.Info("TEST");

            // Instal a message handler.
            var messages = new List<ILogMessage>();
            GlobalLogger.GlobalMessageLogged += messages.Add;

            // Log a simple message (disabled by default).
            log.Verbose("#0");
            Assert.That(messages.Count, Is.EqualTo(0));

            // Activate the log for all loggers starting from Info
            GlobalLogger.ActivateLog(".*", LogMessageType.Verbose);

            // Log a simple message
            log.Verbose("#0");
            Assert.That(messages.Count, Is.EqualTo(1));
            Assert.That(messages[0].Text, Is.EqualTo("#0"));

            // Activate the log for Module1x starting from Debug
            GlobalLogger.ActivateLog(".*x", LogMessageType.Debug);
            log1x.Debug("#1");
            Assert.That(messages.Count, Is.EqualTo(2));
            Assert.That(messages[1].Text, Is.EqualTo("#1"));
        }


        [Test]
        public void TestCallerInfo()
        {
            var log = new LoggerResult();

            // Use the caller information
            log.Info("#0", CallerInfo.Get());
            log.Info("#1", CallerInfo.Get());

            Assert.That(log.Messages.Count, Is.EqualTo(2));
            Assert.That(((LogMessage)log.Messages[0]).CallerInfo, Is.Not.Null);
            Assert.That(((LogMessage)log.Messages[0]).CallerInfo.FilePath, Is.StringContaining("TestLogger"));
            Assert.That(((LogMessage)log.Messages[0]).CallerInfo.LineNumber, Is.GreaterThan(0));
            Assert.That(((LogMessage)log.Messages[1]).CallerInfo, Is.Not.Null);
            Assert.That(((LogMessage)log.Messages[1]).CallerInfo.LineNumber, Is.EqualTo(((LogMessage)log.Messages[0]).CallerInfo.LineNumber + 1));
        }
    }
}
