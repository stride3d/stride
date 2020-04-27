// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using ServiceWire.NamedPipes;
using Stride.Core.Diagnostics;

namespace Stride.Debugger.Target
{
    public class GameDebuggerHost : IGameDebuggerHost
    {
        private TaskCompletionSource<IGameDebuggerTarget> target = new TaskCompletionSource<IGameDebuggerTarget>();
        private NpClient<IGameDebuggerTarget> callbackChannel;

        public event Action GameExited;

        public LoggerResult Log { get; private set; }

        public GameDebuggerHost(LoggerResult logger)
        {
            Log = logger;
        }

        public Task<IGameDebuggerTarget> Target
        {
            get { return target.Task; }
        }

        public void RegisterTarget(string callbackAddress)
        {
            callbackChannel = new NpClient<IGameDebuggerTarget>(new NpEndPoint(callbackAddress));
            target.TrySetResult(callbackChannel.Proxy);
        }

        public void OnGameExited()
        {
            GameExited?.Invoke();
        }

        public void OnLogMessage(SerializableLogMessage logMessage)
        {
            Log.Log(logMessage);
        }

        public void Dispose()
        {
            this.callbackChannel.Dispose();
        }
    }
}
