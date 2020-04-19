// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.ComponentModel;
using Stride.Core.Assets;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Collections;

namespace Stride.Assets.Scripts
{
    [DataContract]
    public class Method : IIdentifiable, IAssetPartDesign<Method>
    {
        public Method()
        {
            Id = Guid.NewGuid();
        }

        public Method(string name) : this()
        {
            Name = name;
        }

        [DataMember(-100), Display(Browsable = false)]
        [NonOverridable]
        public Guid Id { get; set; }

        /// <inheritdoc/>
        [DataMember(-90), Display(Browsable = false)]
        [DefaultValue(null)]
        public BasePart Base { get; set; }

        [DataMember(0)]
        [DefaultValue(Accessibility.Public)]
        public Accessibility Accessibility { get; set; } = Accessibility.Public;

        [DataMember(5)]
        [DefaultValue(VirtualModifier.None)]
        public VirtualModifier VirtualModifier { get; set; } = VirtualModifier.None;

        [DataMember(10)]
        [DefaultValue(false)]
        public bool IsStatic { get; set; }

        [DataMember(20)]
        public string Name { get; set; }

        [DataMember(30)]
        [DefaultValue("void")]
        public string ReturnType { get; set; } = "void";

        [DataMember(40)]
        public TrackingCollection<Parameter> Parameters { get; } = new TrackingCollection<Parameter>();

        [DataMember(50)]
        [NonIdentifiableCollectionItems]
        public AssetPartCollection<Block, Block> Blocks { get; } = new AssetPartCollection<Block, Block>();

        [DataMember(60)]
        [NonIdentifiableCollectionItems]
        public AssetPartCollection<Link, Link> Links { get; } = new AssetPartCollection<Link, Link>();

        /// <inheritdoc/>
        IIdentifiable IAssetPartDesign.Part => this;

        Method IAssetPartDesign<Method>.Part => this;
    }
}
