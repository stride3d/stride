// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xenko.Core;
using Xenko.Engine;
using Xenko.Engine.Design;
using Xenko.Graphics;
using Xenko.Video.Rendering;
using Xenko.Core.Annotations;
using Xenko.Core.Collections;

namespace Xenko.Video
{
    /// <summary>
    /// Component representing a video.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Associate this component to an entity to render a video into a texture.
    /// </para>
    /// </remarks>
    [Display("Video", Expand = ExpandRule.Once)]
    [DataContract(nameof(VideoComponent))]
    [DefaultEntityComponentProcessor(typeof(VideoProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentOrder(8000)]
    [ComponentCategory("Video")]
    public class VideoComponent : EntityComponent
    {
        [DataMemberIgnore]
        [CanBeNull]
        public VideoInstance Instance { get; private set; }

        /// <summary>
        /// The source video.
        /// </summary>
        /// <userdoc>
        /// The video asset used as a source.
        /// </userdoc>
        [DataMember(10)]
        public Video Source { get; set; }

        /// <summary>
        /// The target texture where frames from the video will be rendered.
        /// </summary>
        /// <userdoc>
        /// A texture used as target from rendering the frames of the video.
        /// </userdoc>
        [DataMember(20)]
        public Texture Target { get; set; }

        /// <summary>
        /// If activated, the video will automatically restart when reaching the end
        /// </summary>
        /// <userdoc>
        /// If activated, the video will automatically restart when reaching the end
        /// </userdoc>
        [DataMember(30)]
        public bool LoopVideo { get; set; }

        /// <summary>
        /// Defines the maximum number of mip maps that will be generated for the video texture.
        /// </summary>
        /// <userdoc>
        /// The maximum number of mip maps to generate for the video texture.
        /// </userdoc>
        [DataMember(40)]
        public int MaxMipMapCount { get; set; }

        /// <summary>
        /// If activated, the video's audio track will be played
        /// </summary>
        /// <userdoc>
        /// If activated, the video's audio track will be played
        /// </userdoc>
        [DataMember(50)]
        public bool PlayAudio { get; set; } = true;

        /// <summary>
        /// The list of audioEmitteur components.
        /// </summary>
        [DataMember(60)]
        public FastCollection<AudioEmitterComponent> AudioEmitters { get; } = new FastCollection<AudioEmitterComponent>();
        
        internal void AttachInstance([NotNull] VideoInstance instance)
        {
            Instance = instance ?? throw new ArgumentNullException(nameof(instance));
        }

        internal void DetachInstance()
        {
            Instance = null;
        }
    }
}
