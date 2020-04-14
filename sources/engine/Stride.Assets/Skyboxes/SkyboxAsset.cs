// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Compiler;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Contents;
using Xenko.Core.Yaml;
using Xenko.Graphics;
using Xenko.Rendering.Skyboxes;

namespace Xenko.Assets.Skyboxes
{
    /// <summary>
    /// The skybox asset.
    /// </summary>
    [DataContract("SkyboxAsset")]
    [AssetDescription(FileExtension)]
    [AssetContentType(typeof(Skybox))]
    [AssetFormatVersion(XenkoConfig.PackageName, CurrentVersion, "2.0.0.0")]
    public sealed partial class SkyboxAsset : Asset
    {
        private const string CurrentVersion = "2.0.0.0";

        /// <summary>
        /// The default file extension used by the <see cref="SkyboxAsset"/>.
        /// </summary>
        public const string FileExtension = ".xksky";

        /// <summary>
        /// Initializes a new instance of the <see cref="SkyboxAsset"/> class.
        /// </summary>
        public SkyboxAsset()
        {
            IsSpecularOnly = false;
            DiffuseSHOrder = SkyboxPreFilteringDiffuseOrder.Order3;
            SpecularCubeMapSize = 256;
        }

        /// <summary>
        /// Gets or sets the type of skybox.
        /// </summary>
        /// <value>The type of skybox.</value>
        /// <userdoc>The texture to use as skybox (eg a cubemap or panoramic texture)</userdoc>
        [DataMember(10)]
        [Display("Texture", Expand = ExpandRule.Always)]
        public Texture CubeMap { get; set; }

        /// <summary>
        /// Gets or set if this skybox affects specular only, if <c>false</c> this skybox will affect ambient lighting
        /// </summary>
        /// <userdoc>
        /// Use the skybox only for specular lighting
        /// </userdoc>
        [DataMember(15)]
        [DefaultValue(false)]
        [Display("Specular only")]
        public bool IsSpecularOnly { get; set; }

        /// <summary>
        /// Gets or sets the diffuse sh order.
        /// </summary>
        /// <value>The diffuse sh order.</value>
        /// <userdoc>The level of detail of the compressed skybox, used for diffuse lighting (dull materials). Order5 is more detailed than Order3.</userdoc>
        [DefaultValue(SkyboxPreFilteringDiffuseOrder.Order3)]
        [Display("Diffuse SH order")]
        [DataMember(20)]
        public SkyboxPreFilteringDiffuseOrder DiffuseSHOrder { get; set; }

        /// <summary>
        /// Gets or sets the specular cubemap size
        /// </summary>
        /// <value>The specular cubemap size.</value>
        /// <userdoc>The cubemap size used for specular lighting. Larger cubemap have more detail.</userdoc>
        [DefaultValue(256)]
        [Display("Specular texture size")]
        [DataMember(30)]
        [DataMemberRange(64, 0)]
        public int SpecularCubeMapSize { get; set; }

        public IEnumerable<IReference> GetDependencies()
        {
            if (CubeMap != null)
            {
                var reference = AttachedReferenceManager.GetAttachedReference(CubeMap);
                yield return new AssetReference(reference.Id, reference.Url);
            }
        }
    }
}
