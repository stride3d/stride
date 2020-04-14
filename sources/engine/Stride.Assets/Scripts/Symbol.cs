// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.ComponentModel;
using Stride.Core.Assets;
using Stride.Core;
using Stride.Core.Annotations;

namespace Stride.Assets.Scripts
{
    [DataContract]
    public class Symbol : IIdentifiable, IAssetPartDesign<Symbol>
    {
        public Symbol()
        {
            Id = Guid.NewGuid();
        }

        public Symbol(string type, string name)
            : this()
        {
            Name = name;
            Type = type;
        }

        [DataMember(-100), Display(Browsable = false)]
        [NonOverridable]
        public Guid Id { get; set; }

        /// <inheritdoc/>
        [DataMember(-90), Display(Browsable = false)]
        [DefaultValue(null)]
        public BasePart Base { get; set; }

        /// <summary>
        /// Gets or sets the name of that variable.
        /// </summary>
        [DataMember(-50)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the type of that variable.
        /// </summary>
        [DataMember(-40)]
        public string Type { get; set; }

        /// <inheritdoc/>
        IIdentifiable IAssetPartDesign.Part => this;

        Symbol IAssetPartDesign<Symbol>.Part => this;

        public override string ToString()
        {
            return $"{Type} {Name}";
        }
    }
}
