// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Audio;
using Xenko.Core;
using Xenko.Engine.Design;

namespace Xenko.Engine
{
    /// <summary>
    /// Component representing an audio listener.
    /// </summary>
    /// <remarks>
    /// <para>Associate this component to an <see cref="Entity"/> to simulate a physical listener listening to the <see cref="AudioEmitterComponent"/>s of the scene,
    /// placed at the entity's center and oriented along the entity's Oz (forward) and Oy (up) vectors.</para>
    /// <para>Use the AudioSytem's <see cref="AudioSystem.AddListener"/> and <see cref="AudioSystem.RemoveListener"/> functions 
    /// to activate/deactivate the listeners that are actually listening at a given time.</para>
    /// <para>The entity needs to be added to the Entity System so that the associated AudioListenerComponent can be processed.</para></remarks>
    [Display("Audio listener", Expand = ExpandRule.Once)]
    [DataContract("AudioListenerComponent")]
    [DefaultEntityComponentProcessor(typeof(AudioListenerProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentOrder(6000)]
    [ComponentCategory("Audio")]
    public sealed class AudioListenerComponent : ActivableEntityComponent
    {
        [DataMemberIgnore]
        internal AudioListener Listener;
    }
}
