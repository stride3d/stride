// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Animations;
using Stride.Core;
using Stride.Engine;

namespace UIElementLink;

[DataContract]
public class PlayAnimation
{
    public AnimationClip Clip;
    public AnimationBlendOperation BlendOperation = AnimationBlendOperation.LinearBlend;
    public double StartTime = 0;
}

/// <summary>
/// Script which starts a few animations on its entity
/// </summary>
public class AnimationStart : StartupScript
{
    /// <summary>
    /// Al list of animations to be loaded when the script starts
    /// </summary>
    public readonly List<PlayAnimation> Animations = [];

    public override void Start()
    {
			var animComponent = Entity.GetOrCreate<AnimationComponent>();
    
        if (animComponent != null)
            PlayAnimations(animComponent);

        // Destroy this script since it's no longer needed
        Entity.Remove(this);
    }

    private void PlayAnimations(AnimationComponent animComponent)
    {
        foreach (var anim in Animations)
        {
            if (anim.Clip != null)
                animComponent.Add(anim.Clip, anim.StartTime, anim.BlendOperation);
        }

        Animations.Clear();
    }
}
