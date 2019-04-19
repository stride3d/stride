using Xenko.Core.MicroThreading;
using Xenko.Engine;
using Xenko.Engine.Events;
using Xenko.Engine.Processors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xenko.Toolkit.Engine
{
    /// <summary>
    /// Extension methods for <see cref="ScriptSystem"/>.
    /// </summary>
    public static class ScriptSystemExtensions
    {
        /// <summary>
        /// Waits for the specified delay <paramref name="delay"/> .
        /// </summary>
        /// <param name="scriptSystem">The <see cref="ScriptSystem"/>.</param>
        /// <param name="delay">The amount of time to wait.</param>
        /// <returns>The <see cref="Task"/> to await.</returns>
        public static async Task WaitFor(this ScriptSystem scriptSystem, TimeSpan delay)
        {
            if (scriptSystem == null)
            {
                throw new ArgumentNullException(nameof(scriptSystem));
            }

            if (delay <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(delay), "Must be greater than zero.");
            }

            while (scriptSystem.Game.IsRunning && delay >= TimeSpan.Zero)
            {
                delay -= scriptSystem.Game.UpdateTime.Elapsed;
                await scriptSystem.NextFrame();
            }

        }

        /// <summary>
        /// Waits for the specified delay <paramref name="delay"/> .
        /// </summary>
        /// <param name="scriptSystem">The <see cref="ScriptSystem"/>.</param>
        /// <param name="delay">The amount of time to wait.</param>
        /// <param name="scriptDelegateWatcher">Allows to stop waiting if <see cref="ScriptComponent"/> is not active.</param>
        /// <returns>The <see cref="Task"/> to await.</returns>
        internal static async Task WaitFor(this ScriptSystem scriptSystem, TimeSpan delay, ScriptDelegateWatcher scriptDelegateWatcher)
        {
            if (scriptSystem == null)
            {
                throw new ArgumentNullException(nameof(scriptSystem));
            }

            if (delay <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(delay), "Must be greater than zero.");
            }

            while (scriptSystem.Game.IsRunning && scriptDelegateWatcher.IsActive && delay >= TimeSpan.Zero)
            {
                delay -= scriptSystem.Game.UpdateTime.Elapsed;
                await scriptSystem.NextFrame();
            }

        }

        /// <summary>
        /// Adds a micro thread function to the <paramref name="scriptSystem"/> that executes when the event is published.
        /// </summary>
        /// <typeparam name="T">The type of the event handler parameter.</typeparam>
        /// <param name="scriptSystem">The <see cref="ScriptSystem"/>.</param>
        /// <param name="eventKey">The event to wait for.</param>
        /// <param name="action">The micro thread function to execute.</param>
        /// <param name="priority">The priority of the micro thread action being added.</param>
        /// <returns>The <see cref="MicroThread"/>.</returns>
        /// <exception cref="ArgumentNullException"> If <paramref name="scriptSystem"/>, <paramref name="eventKey"/> or <paramref name="action"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// If the <paramref name="action"/> is a <see cref="ScriptComponent"/> instance method the micro thread will be automatically stopped if the <see cref="ScriptComponent"/> or <see cref="Entity"/> is removed.
        /// </remarks>
        public static MicroThread AddOnEventAction<T>(
            this ScriptSystem scriptSystem, 
            EventKey<T> eventKey, Action<T> action, 
            long priority = 0L)
        {
            if (scriptSystem == null)
            {
                throw new ArgumentNullException(nameof(scriptSystem));
            }
            
            if (eventKey == null)
            {
                throw new ArgumentNullException(nameof(eventKey));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }            

            return scriptSystem.AddOnEventAction(new EventReceiver<T>(eventKey), action, priority);

        }


        /// <summary>
        /// Adds a micro thread function to the <paramref name="scriptSystem"/> that executes when the event is published.
        /// </summary>
        /// <typeparam name="T">The type of the event handler parameter.</typeparam>
        /// <param name="scriptSystem">The <see cref="ScriptSystem"/>.</param>
        /// <param name="receiver">The event reciever to listen to for.</param>
        /// <param name="action">The micro thread function to execute.</param>
        /// <param name="priority">The priority of the micro thread action being added.</param>
        /// <returns>The <see cref="MicroThread"/>.</returns>
        /// <exception cref="ArgumentNullException"> If <paramref name="scriptSystem"/>, <paramref name="receiver"/> or <paramref name="action"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// If the <paramref name="action"/> is a <see cref="ScriptComponent"/> instance method the micro thread will be automatically stopped if the <see cref="ScriptComponent"/> or <see cref="Entity"/> is removed.
        /// </remarks>
        public static MicroThread AddOnEventAction<T>(
            this ScriptSystem scriptSystem, 
            EventReceiver<T> receiver, 
            Action<T> action, 
            long priority = 0L)
        {
            if (scriptSystem == null)
            {
                throw new ArgumentNullException(nameof(scriptSystem));
            }

            if (receiver == null)
            {
                throw new ArgumentNullException(nameof(receiver));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            return scriptSystem.AddTask(DoEvent, priority);

            //C# 7 Local function could also use a variable Func<Task> DoEvent = async () => { ... };
            async Task DoEvent()
            {
                var scriptDelegateWatcher = new ScriptDelegateWatcher(action);

                while (scriptSystem.Game.IsRunning && scriptDelegateWatcher.IsActive)
                {
                    if(receiver.TryReceive(out var data))
                    {
                        action(data);
                    }

                    await scriptSystem.NextFrame();
                }
            }
        }


        /// <summary>
        /// Adds a micro thread function to the <paramref name="scriptSystem"/> that executes when the event is published.
        /// </summary>
        /// <typeparam name="T">The type of the event handler parameter.</typeparam>
        /// <param name="scriptSystem">The <see cref="ScriptSystem"/>.</param>
        /// <param name="eventKey">The event to wait for.</param>
        /// <param name="action">The micro thread function to execute.</param>
        /// <param name="priority">The priority of the micro thread action being added.</param>
        /// <returns>The <see cref="MicroThread"/>.</returns>
        /// <exception cref="ArgumentNullException"> If <paramref name="scriptSystem"/>, <paramref name="eventKey"/> or <paramref name="action"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// If the <paramref name="action"/> is a <see cref="ScriptComponent"/> instance method the micro thread will be automatically stopped if the <see cref="ScriptComponent"/> or <see cref="Entity"/> is removed.
        /// </remarks>
        public static MicroThread AddOnEventTask<T>(
           this ScriptSystem scriptSystem,
           EventKey<T> eventKey, Func<T,Task> action,
           long priority = 0L)
        {
            if (scriptSystem == null)
            {
                throw new ArgumentNullException(nameof(scriptSystem));
            }

            if (eventKey == null)
            {
                throw new ArgumentNullException(nameof(eventKey));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            return scriptSystem.AddOnEventTask(new EventReceiver<T>(eventKey), action, priority);

        }


        /// <summary>
        /// Adds a micro thread function to the <paramref name="scriptSystem"/> that executes when the event is published.
        /// </summary>
        /// <typeparam name="T">The type of the event handler parameter.</typeparam>
        /// <param name="scriptSystem">The <see cref="ScriptSystem"/>.</param>
        /// <param name="receiver">The event reciever to listen to for.</param>
        /// <param name="action">The micro thread function to execute.</param>
        /// <param name="priority">The priority of the micro thread action being added.</param>
        /// <returns>The <see cref="MicroThread"/>.</returns>
        /// <exception cref="ArgumentNullException"> If <paramref name="scriptSystem"/>, <paramref name="receiver"/> or <paramref name="action"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// If the <paramref name="action"/> is a <see cref="ScriptComponent"/> instance method the micro thread will be automatically stopped if the <see cref="ScriptComponent"/> or <see cref="Entity"/> is removed.
        /// </remarks>
        public static MicroThread AddOnEventTask<T>(
            this ScriptSystem scriptSystem,
            EventReceiver<T> receiver,
            Func<T, Task> action,
            long priority = 0L)
        {
            if (scriptSystem == null)
            {
                throw new ArgumentNullException(nameof(scriptSystem));
            }

            if (receiver == null)
            {
                throw new ArgumentNullException(nameof(receiver));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }


            return scriptSystem.AddTask(DoEvent, priority);

            //C# 7 Local function could also use a variable Func<Task> DoEvent = async () => { ... };
            async Task DoEvent()
            {
                var scriptDelegateWatcher = new ScriptDelegateWatcher(action);

                while (scriptSystem.Game.IsRunning && scriptDelegateWatcher.IsActive)
                {
                    if (receiver.TryReceive(out var data))
                    {
                        await action(data);
                    }

                    await scriptSystem.NextFrame();
                }
            }
        }


        /// <summary>
        /// Adds a micro thread function to the <paramref name="scriptSystem"/> that executes after waiting specified delay.
        /// </summary>
        /// <param name="scriptSystem">The <see cref="ScriptSystem"/>.</param>
        /// <param name="action">The micro thread function to execute.</param>
        /// <param name="delay">The amount of time to wait for.</param>
        /// <param name="priority">The priority of the micro thread action being added.</param>
        /// <returns>The <see cref="MicroThread"/>.</returns>
        /// <exception cref="ArgumentNullException"> If <paramref name="scriptSystem"/> or <paramref name="action"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="delay"/> is less than zero.</exception>
        /// <remarks>
        /// If the <paramref name="action"/> is a <see cref="ScriptComponent"/> instance method the micro thread will be automatically stopped if the <see cref="ScriptComponent"/> or <see cref="Entity"/> is removed.
        /// </remarks>
        public static MicroThread AddTask(
           this ScriptSystem scriptSystem,
           Func<Task> action,
           TimeSpan delay,
           long priority = 0L)
        {
            if (scriptSystem == null)
            {
                throw new ArgumentNullException(nameof(scriptSystem));
            }          

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            return scriptSystem.AddTask(DoTask, priority);

            //C# 7 Local function could also use a variable Func<Task> DoEvent = async () => { ... };
            async Task DoTask()
            {
                var scriptDelegateWatcher = new ScriptDelegateWatcher(action);

                await scriptSystem.WaitFor(delay, scriptDelegateWatcher);

                if (scriptSystem.Game.IsRunning && scriptDelegateWatcher.IsActive)
                {
                    await action(); 
                }
            }
        }


        /// <summary>
        /// Adds a micro thread function to the <paramref name="scriptSystem"/> that executes after waiting specified delay.
        /// </summary>
        /// <param name="scriptSystem">The <see cref="ScriptSystem"/>.</param>
        /// <param name="action">The micro thread function to execute.</param>
        /// <param name="delay">The amount of time to wait for.</param>
        /// <param name="priority">The priority of the micro thread action being added.</param>
        /// <returns>The <see cref="MicroThread"/>.</returns>
        /// <exception cref="ArgumentNullException"> If <paramref name="scriptSystem"/> or <paramref name="action"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="delay"/> is less than zero.</exception>
        /// <remarks>
        /// If the <paramref name="action"/> is a <see cref="ScriptComponent"/> instance method the micro thread will be automatically stopped if the <see cref="ScriptComponent"/> or <see cref="Entity"/> is removed.
        /// </remarks>
        public static MicroThread AddAction(
           this ScriptSystem scriptSystem,
           Action action,
           TimeSpan delay,
           long priority = 0L)
        {
            if (scriptSystem == null)
            {
                throw new ArgumentNullException(nameof(scriptSystem));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if(delay <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(delay), "Must be greater than zero.");
            }

            return scriptSystem.AddTask(DoTask, priority);

            //C# 7 Local function could also use a variable Func<Task> DoEvent = async () => { ... };
            async Task DoTask()
            {
                var scriptDelegateWatcher = new ScriptDelegateWatcher(action);

                await scriptSystem.WaitFor(delay, scriptDelegateWatcher);

                if (scriptSystem.Game.IsRunning && scriptDelegateWatcher.IsActive)
                {
                    action();
                }
            }
        }

        /// <summary>
        /// Adds a micro thread function to the <paramref name="scriptSystem"/> that executes after waiting specified delay and repeats execution.
        /// </summary>
        /// <param name="scriptSystem">The <see cref="ScriptSystem"/>.</param>
        /// <param name="action">The micro thread function to execute.</param>
        /// <param name="delay">The amount of time to wait for.</param>
        /// <param name="repeatEvery">The amount of time to wait for between repetition.</param>
        /// <param name="priority">The priority of the micro thread action being added.</param>
        /// <returns>The <see cref="MicroThread"/>.</returns>
        /// <exception cref="ArgumentNullException"> If <paramref name="scriptSystem"/> or <paramref name="action"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="delay"/> or <paramref name="repeatEvery"/> is less than zero.</exception>
        /// <remarks>
        /// If the <paramref name="action"/> is a <see cref="ScriptComponent"/> instance method the micro thread will be automatically stopped if the <see cref="ScriptComponent"/> or <see cref="Entity"/> is removed.
        /// </remarks>
        public static MicroThread AddTask(
           this ScriptSystem scriptSystem,
           Func<Task> action,
           TimeSpan delay,
           TimeSpan repeatEvery,
           long priority = 0L)
        {
            if (scriptSystem == null)
            {
                throw new ArgumentNullException(nameof(scriptSystem));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (delay <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(delay), "Must be greater than zero.");
            }

            return scriptSystem.AddTask(DoTask, priority);

            //C# 7 Local function could also use a variable Func<Task> DoEvent = async () => { ... };
            async Task DoTask()
            {
                var elapsedTime = new TimeSpan(0);

                var scriptDelegateWatcher = new ScriptDelegateWatcher(action);

                while (scriptSystem.Game.IsRunning && scriptDelegateWatcher.IsActive)
                {
                    elapsedTime += scriptSystem.Game.UpdateTime.Elapsed;

                    if (elapsedTime >= delay)
                    {
                        elapsedTime -= delay;
                        delay = repeatEvery;
                        await action();
                    }
                    await scriptSystem.NextFrame();
                }
            }
        }

        /// <summary>
        /// Adds a micro thread function to the <paramref name="scriptSystem"/> that executes after waiting specified delay and repeats execution.
        /// </summary>
        /// <param name="scriptSystem">The <see cref="ScriptSystem"/>.</param>
        /// <param name="action">The micro thread function to execute.</param>
        /// <param name="delay">The amount of time to wait for.</param>
        /// <param name="repeatEvery">The amount of time to wait for between repetition.</param>
        /// <param name="priority">The priority of the micro thread action being added.</param>
        /// <returns>The <see cref="MicroThread"/>.</returns>
        /// <exception cref="ArgumentNullException"> If <paramref name="scriptSystem"/> or <paramref name="action"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="delay"/> or <paramref name="repeatEvery"/> is less than zero.</exception>
        /// <remarks>
        /// If the <paramref name="action"/> is a <see cref="ScriptComponent"/> instance method the micro thread will be automatically stopped if the <see cref="ScriptComponent"/> or <see cref="Entity"/> is removed.
        /// </remarks>
        public static MicroThread AddAction(
           this ScriptSystem scriptSystem,
           Action action,
           TimeSpan delay,
           TimeSpan repeatEvery,
           long priority = 0L)
        {
            if (scriptSystem == null)
            {
                throw new ArgumentNullException(nameof(scriptSystem));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (delay <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(delay), "Must be greater than zero.");
            }

            if (repeatEvery <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(repeatEvery), "Must be greater than zero.");
            }

            return scriptSystem.AddTask(DoTask, priority);

            //C# 7 Local function could also use a variable Func<Task> DoEvent = async () => { ... };
            async Task DoTask()
            {
                var elapsedTime = new TimeSpan(0);

                var scriptDelegateWatcher = new ScriptDelegateWatcher(action);

                while (scriptSystem.Game.IsRunning && scriptDelegateWatcher.IsActive)
                {
                    elapsedTime += scriptSystem.Game.UpdateTime.Elapsed;

                    if(elapsedTime >= delay)
                    {
                        elapsedTime -= delay;
                        delay = repeatEvery;
                        action();
                    }                    
                    await scriptSystem.NextFrame();
                }
            }
        }

        /// <summary>
        /// Adds a micro thread function to the <paramref name="scriptSystem"/> that executes after waiting specified delay and repeats execution.
        /// </summary>
        /// <param name="scriptSystem">The <see cref="ScriptSystem"/>.</param>
        /// <param name="action">The micro thread function to execute. The parameter is the progress over time from 0.0f to 1.0f.</param>
        /// <param name="duration">The duration of the time to execute the micro thread function for.</param>
        /// <param name="priority">The priority of the micro thread action being added.</param>
        /// <returns>The <see cref="MicroThread"/>.</returns>
        /// <exception cref="ArgumentNullException"> If <paramref name="scriptSystem"/> or <paramref name="action"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="duration"/> is less than zero.</exception>
        /// <remarks>
        /// If the <paramref name="action"/> is a <see cref="ScriptComponent"/> instance method the micro thread will be automatically stopped if the <see cref="ScriptComponent"/> or <see cref="Entity"/> is removed.
        /// </remarks>
        public static MicroThread AddOverTimeAction(
           this ScriptSystem scriptSystem,
           Action<float> action,
           TimeSpan duration,
           long priority = 0L)
        {
            if (scriptSystem == null)
            {
                throw new ArgumentNullException(nameof(scriptSystem));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (duration <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(duration), "Must be greater than zero.");
            }

            return scriptSystem.AddTask(DoTask, priority);

            //C# 7 Local function could also use a variable Func<Task> DoEvent = async () => { ... };
            async Task DoTask()
            {
                var elapsedTime = new TimeSpan(0);

                var scriptDelegateWatcher = new ScriptDelegateWatcher(action);

                while (scriptSystem.Game.IsRunning && scriptDelegateWatcher.IsActive)
                {
                    elapsedTime += scriptSystem.Game.UpdateTime.Elapsed;

                    if (elapsedTime >= duration)
                    {
                        action(1.0f);
                        break;
                    }
                    else
                    {
                        var progress = (float)(elapsedTime.TotalSeconds / duration.TotalSeconds);

                        action(progress);
                    }
                    await scriptSystem.NextFrame();
                }
            }
        }

        /// <summary>
        /// Cancels all <see cref="MicroThread"/> and clears the <paramref name="microThreads"/> collection.
        /// </summary>
        /// <param name="microThreads">A collection of <see cref="MicroThread"/> to cancel.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="microThreads"/> is <see langword="null"/>.</exception>
        public static void CancelAll(this ICollection<MicroThread> microThreads)
        {
            if (microThreads == null)
            {
                throw new ArgumentNullException(nameof(microThreads));
            }

            foreach (var thread in microThreads)
            {
                thread.Cancel();
            }

            microThreads.Clear();
        }
    }
}
