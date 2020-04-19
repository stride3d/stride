// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Quantum;

namespace Stride.Assets.Presentation.AssetEditors.GraphicsCompositorEditor.ViewModels
{
    /// <summary>
    /// Key for both the path to a reference field and its type
    /// </summary>
    public sealed class SharedRendererReferenceKey : IEquatable<SharedRendererReferenceKey>
    {
        public SharedRendererReferenceKey(GraphNodePath path, Type type)
        {
            Path = path;
            Type = type;
        }

        public GraphNodePath Path { get; }

        public Type Type { get; }

        /// <inheritdoc />
        public bool Equals(SharedRendererReferenceKey other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Path, other.Path) && Type == other.Type;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return Equals(obj as SharedRendererReferenceKey);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return ((Path != null ? Path.GetHashCode() : 0) * 397) ^ (Type != null ? Type.GetHashCode() : 0);
            }
        }
    }
}
