// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1402 // File may only contain a single class
using System;

namespace Xenko.Core.Serialization.Contents
{
    internal abstract class ContentReference : ILoadableReference, IEquatable<ContentReference>
    {
        internal const int NullIdentifier = -1;

        /// <summary>
        /// Gets or sets the location of the referenced content.
        /// </summary>
        /// <value>
        /// The location of the referenced content.
        /// </value>
        public abstract string Location { get; set; }

        public abstract object ObjectValue { get; }

        public ContentReferenceState State { get; protected set; }

        public abstract Type Type { get; }

        public bool Equals(ContentReference other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Location, other.Location) &&
                   Type == other.Type;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ContentReference)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Type.GetHashCode();
        }

        public static bool operator ==(ContentReference left, ContentReference right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ContentReference left, ContentReference right)
        {
            return !Equals(left, right);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{{{Location}}}";
        }
    }

    [DataSerializer(typeof(ContentReferenceDataSerializer<>), Mode = DataSerializerGenericMode.GenericArguments)]
    internal sealed class ContentReference<T> : ContentReference where T : class
    {
        // Depending on state, either value or Location is null (they can't be both non-null)
        private T value;
        private string url;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentReference{T}"/> class with the given value.
        /// </summary>
        /// <param name="value">The value of the referenced content.</param>
        /// <remarks>This constructor should be used during serialization.</remarks>
        public ContentReference(T value)
        {
            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentReference{T}"/> class with the given id and location.
        /// </summary>
        /// <param name="location">The location of the referenced content.</param>
        /// <remarks>This constructor should be used during deserialization.</remarks>
        public ContentReference(string location)
        {
            Location = location;
        }

        /// <inheritdoc />
        public override string Location
        {
            get
            {
                // TODO: Should we return value.Location if value is not null, or just reference Location?
                if (ObjectValue == null)
                    return url;

                return AttachedReferenceManager.GetUrl(ObjectValue);
            }
            set
            {
                if (ObjectValue == null)
                {
                    url = value;
                }
                else
                {
                    AttachedReferenceManager.SetUrl(ObjectValue, value);
                }
            }
        }

        /// <inheritdoc />
        public override object ObjectValue => Value;

        /// <inheritdoc />
        public override Type Type => typeof(T);

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public T Value
        {
            get => value;
            set
            {
                State = ContentReferenceState.Modified;
                this.value = value;
                url = null;
            }
        }
    }
}
