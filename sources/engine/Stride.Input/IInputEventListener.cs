// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Input
{
    /// <summary>
    /// Does not listen to any event but is used to pass around a type that might potentially listen for input events
    /// </summary>
    public interface IInputEventListener
    {
    }

    /// <summary>
    /// Interface for classes that want to listen to input event of a certain type
    /// </summary>
    /// <typeparam name="TEventType">The type of <see cref="InputEvent"/> that will be sent to this event listener</typeparam>
    public interface IInputEventListener<TEventType> : IInputEventListener where TEventType : InputEvent
    {
        /// <summary>
        /// Processes a new input event
        /// </summary>
        /// <param name="inputEvent">the input event</param>
        void ProcessEvent(TEventType inputEvent);
    }
}