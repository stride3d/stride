// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Stride.Core;

namespace Stride.Engine
{
    /// <summary>
    /// Contains the id of a component
    /// </summary>
    public readonly ref struct OpaqueComponentId
    {
        private readonly int _id;

        public OpaqueComponentId(int id)
        {
            _id = id;
        }

        public OpaqueComponentId(EntityComponent component)
        {
            _id = RuntimeIdHelper.ToRuntimeId(component);
        }

        public bool Match(EntityComponent component)
        {
            return RuntimeIdHelper.ToRuntimeId(component) == _id;
        }

        public bool Match(int otherId)
        {
            return otherId == _id;
        }

        public bool Match(ISet<int> otherIds)
        {
            return otherIds.Contains(_id);
        }

        public bool Match<T>(IDictionary<int, T> otherIds, [MaybeNullWhen(false)] out T value)
        {
            return otherIds.TryGetValue(_id, out value);
        }
    }
}
