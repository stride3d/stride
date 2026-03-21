// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Stride.Input;

namespace Stride.Engine.Mcp
{
    /// <summary>
    /// Bridge between MCP tool handlers (Kestrel threads) and the game thread.
    /// Injected as a singleton into MCP tool methods.
    /// </summary>
    /// <remarks>
    /// Properties are marked with <see cref="JsonIgnoreAttribute"/> to prevent
    /// the MCP SDK's JSON schema generator from walking into complex engine types
    /// that contain ref structs or pointers.
    /// </remarks>
    public sealed class GameBridge
    {
        private readonly Game game;
        private readonly GameMcpSystem system;

        [JsonIgnore]
        public KeyboardSimulated Keyboard { get; }

        [JsonIgnore]
        public MouseSimulated Mouse { get; }

        [JsonIgnore]
        public GamePadSimulated GamePad { get; }

        internal GameBridge(Game game, GameMcpSystem system, KeyboardSimulated keyboard, MouseSimulated mouse, GamePadSimulated gamePad)
        {
            this.game = game;
            this.system = system;
            Keyboard = keyboard;
            Mouse = mouse;
            GamePad = gamePad;
        }

        public Task<T> RunOnGameThread<T>(Func<Game, T> action, CancellationToken ct = default)
        {
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            ct.Register(() => tcs.TrySetCanceled(ct));
            system.EnqueueRequest(new GameThreadRequest
            {
                Action = g => action(g),
                Completion = tcs,
            });
            return tcs.Task.ContinueWith(t => (T)t.Result, ct, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        public Task RunOnGameThread(Action<Game> action, CancellationToken ct = default)
        {
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            ct.Register(() => tcs.TrySetCanceled(ct));
            system.EnqueueRequest(new GameThreadRequest
            {
                Action = g =>
                {
                    action(g);
                    return null;
                },
                Completion = tcs,
            });
            return tcs.Task;
        }
    }
}
