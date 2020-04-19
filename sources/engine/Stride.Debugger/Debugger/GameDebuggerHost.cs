// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ServiceModel;
using System.Threading.Tasks;
using Stride.Core.Diagnostics;

namespace Stride.Debugger.Target
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class GameDebuggerHost : IGameDebuggerHost
    {
        private TaskCompletionSource<IGameDebuggerTarget> target = new TaskCompletionSource<IGameDebuggerTarget>();

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

        public void RegisterTarget()
        {
            target.TrySetResult(OperationContext.Current.GetCallbackChannel<IGameDebuggerTarget>());
        }

        public void OnGameExited()
        {
            GameExited?.Invoke();
        }

        public void OnLogMessage(SerializableLogMessage logMessage)
        {
            Log.Log(logMessage);
        }
    }
}
