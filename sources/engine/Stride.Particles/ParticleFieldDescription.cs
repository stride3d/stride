// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Particles
{
    public abstract class ParticleFieldDescription
    {
        private readonly int hashCode;

        protected ParticleFieldDescription(string name)
        {
            Name = name;
            hashCode = name?.GetHashCode() ?? 0;
            FieldSize = 0;
        }

        public override int GetHashCode() => hashCode;

        public int FieldSize { get; protected set; }

        public string Name { get; }
    }

    public class ParticleFieldDescription<T> : ParticleFieldDescription where T : struct
    {
        public ParticleFieldDescription(string name)
            : base(name)
        {
            FieldSize = ParticleUtilities.AlignedSize(Utilities.SizeOf<T>(), 4);
        }

        public ParticleFieldDescription(string name, T defaultValue)
            : this(name)
        {
            DefaultValue = defaultValue;
        }

        public T DefaultValue { get; }
    }
}
