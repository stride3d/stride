// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Reflection;
using System.Threading.Tasks;
using Stride.Core.Diagnostics;
using Stride.Core.MicroThreading;
using Stride.Games;
using Xunit;

namespace Stride.Engine.Tests
{
    [Collection("Windowing")]
    public class ScriptSystemTests
    {
        [Fact]
        public void CancelExceptionDuringGameShutdownIsLogged()
        {
            using var game = new Game();
            var script = new ThrowingCancelScript();
            var scene = new Scene();
            scene.Entities.Add(new Entity { script });
            game.SceneSystem.SceneInstance = new SceneInstance(game.Services, scene);
            game.Script.Update(new GameTime());
            Assert.Equal(1, script.StartCount);

            LogMessage loggedException = null;
            void OnMessage(ILogMessage message)
            {
                if (message is LogMessage log && ReferenceEquals(log.Exception, script.CancelException))
                    loggedException = log;
            }

            GlobalLogger.GlobalMessageLogged += OnMessage;
            try
            {
                // Reproduce GameBase's shutdown order without opening a graphics window.
                game.Script.Dispose();
                Assert.Null(game.Script.Scheduler);
                game.SceneSystem.Dispose();
                Assert.Equal(1, script.CancelCount);
                Assert.NotNull(loggedException);
                Assert.Equal("ScriptSystem", loggedException.Module);
                Assert.Equal(LogMessageType.Error, loggedException.Type);
                Assert.Same(script.CancelException, loggedException.Exception);
            }
            finally
            {
                GlobalLogger.GlobalMessageLogged -= OnMessage;
            }
        }

        [Fact]
        public void CancelExceptionAfterDestroyStillCancelsMicroThread()
        {
            using var game = new Game();
            var scriptSystem = game.Script;
            var script = new ThrowingAsyncCancelScript();
            scriptSystem.Add(script);
            scriptSystem.Update(new GameTime());
            Assert.True(script.Started);
            Assert.False(script.CancellationToken.IsCancellationRequested);

            scriptSystem.Dispose();
            scriptSystem.Remove(script);
            Assert.True(script.CancellationToken.IsCancellationRequested);
            scriptSystem.Remove(script);
            Assert.Equal(1, script.CancelCount);
        }

        [Fact]
        public void CancelExceptionDuringRuntimePropagatesOriginalException()
        {
            using var game = new Game();
            var script = new ThrowingCancelScript();
            game.Script.Add(script);
            game.Script.Update(new GameTime());

            var exception = Assert.Throws<InvalidOperationException>(() => game.Script.Remove(script));

            Assert.Same(script.CancelException, exception);
            Assert.Contains(nameof(ThrowingCancelScript.Cancel), exception.StackTrace);
            Assert.Equal(1, script.CancelCount);
        }

        [Fact]
        public void CancelExceptionWithDebuggerAttachedStillUnschedulesSyncScript()
        {
            using var game = new Game();
            var script = new ThrowingSyncCancelScript();
            SetPropagateExceptions(game.Script.Scheduler, false);
            game.Script.Add(script);
            game.Script.Update(new GameTime());
            Assert.Equal(1, script.UpdateCount);

            game.Script.Remove(script);
            game.Script.Update(new GameTime());
            game.Script.Remove(script);

            Assert.Equal(1, script.UpdateCount);
            Assert.Equal(1, script.CancelCount);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void UpdateExceptionPreservesRuntimePropagationPolicy(bool propagateExceptions)
        {
            using var game = new Game();
            var script = new ThrowingUpdateScript();
            SetPropagateExceptions(game.Script.Scheduler, propagateExceptions);
            game.Script.Add(script);

            if (propagateExceptions)
            {
                var exception = Assert.Throws<InvalidOperationException>(() => game.Script.Update(new GameTime()));
                Assert.Same(script.UpdateException, exception);
                Assert.Contains(nameof(ThrowingUpdateScript.Update), exception.StackTrace);
            }
            else
            {
                game.Script.Update(new GameTime());
                game.Script.Update(new GameTime());
                Assert.Equal(1, script.UpdateCount);
            }
        }

        private static void SetPropagateExceptions(Scheduler scheduler, bool value)
        {
            // The live debugger sets this internal policy; do not expose a new public API for tests.
            typeof(Scheduler).GetProperty("PropagateExceptions", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(scheduler, value);
        }

        private sealed class ThrowingCancelScript : StartupScript
        {
            public int StartCount;
            public int CancelCount;
            public readonly Exception CancelException = new InvalidOperationException("Cancel failed");

            public override void Start() => StartCount++;

            public override void Cancel()
            {
                CancelCount++;
                throw CancelException;
            }
        }

        private sealed class ThrowingAsyncCancelScript : AsyncScript
        {
            public bool Started;
            public int CancelCount;
            private readonly TaskCompletionSource<bool> completion = new();

            public override async Task Execute()
            {
                Started = true;
                await completion.Task;
            }

            public override void Cancel()
            {
                CancelCount++;
                throw new InvalidOperationException("Async Cancel failed");
            }
        }

        private sealed class ThrowingSyncCancelScript : SyncScript
        {
            public int UpdateCount;
            public int CancelCount;

            public override void Update() => UpdateCount++;

            public override void Cancel()
            {
                CancelCount++;
                throw new InvalidOperationException("Sync Cancel failed");
            }
        }

        private sealed class ThrowingUpdateScript : SyncScript
        {
            public int UpdateCount;
            public readonly Exception UpdateException = new InvalidOperationException("Update failed");

            public override void Update()
            {
                UpdateCount++;
                throw UpdateException;
            }
        }
    }
}
