// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1402 // File may only contain a single type

using System;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Reflection;

namespace Stride.Navigation
{
    /// <summary>
    /// A group that is used to distinguish between different agent types with it's <see cref="Id"/> used at run-time to acquire the navigation mesh for a group
    /// </summary>
    [DataContract]
    [ObjectFactory(typeof(NavigationMeshGroupFactory))]
    [InlineProperty]
    public class NavigationMeshGroup : IIdentifiable
    {
        [DataMember(-10)]
        [Display(Browsable = false)]
        [NonOverridable]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Display name
        /// </summary>
        [DataMember(0)]
        [InlineProperty]
        public string Name { get; set; }

        /// <summary>
        /// Agent settings for this group
        /// </summary>
        [DataMember(5)]
        public NavigationAgentSettings AgentSettings;

        public override string ToString()
        {
            return $"{Name}";
        }
    }

    public class NavigationMeshGroupFactory : IObjectFactory
    {
        public object New(Type type)
        {
            return new NavigationMeshGroup
            {
                Name = "New group",
                AgentSettings = ObjectFactoryRegistry.NewInstance<NavigationAgentSettings>(),
            };
        }
    }
}
