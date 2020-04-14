// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Engine;
using Stride.Updater;

namespace Stride.Animations
{
    /// <summary>
    /// Applies animation from a <see cref="AnimationClip"/> to a <see cref="Rendering.SkeletonUpdater"/>.
    /// </summary>
    public class AnimationUpdater
    {
        // Keep track of source channel (if changed, we should regenerate updateChannels)
        private List<AnimationBlender.Channel> currentSourceChannels;
        private int currentSourceChannelCount;
        private CompiledUpdate compiledUpdate;

        public unsafe void Update(Entity entity, AnimationClipResult result)
        {
            // Check if we need to regenerate "update channels" (i.e. how to copy data from result to hierarchy)
            if (compiledUpdate.UpdateOperations == null // First time?...
                || currentSourceChannels != result.Channels // ... or changed?
                || currentSourceChannels.Count != currentSourceChannelCount) // .. or modified? (could only append channel)
            {
                RegenerateUpdateChannels(result.Channels);
                currentSourceChannels = result.Channels;
                currentSourceChannelCount = currentSourceChannels.Count;
            }

            // Copy results to node hierarchy
            fixed (byte* structures = result.Data)
            {
                UpdateEngine.Run(entity, compiledUpdate, (IntPtr)structures, result.Objects);
            }
        }

        private void RegenerateUpdateChannels(List<AnimationBlender.Channel> channels)
        {
            var updateMemberInfos = new List<UpdateMemberInfo>();

            foreach (var channel in channels)
            {
                if (channel.IsUserCustomProperty)
                    continue;

                updateMemberInfos.Add(new UpdateMemberInfo { Name = channel.PropertyName, DataOffset = channel.Offset });
            }

            compiledUpdate = UpdateEngine.Compile(typeof(Entity), updateMemberInfos);
        }
    }
}
