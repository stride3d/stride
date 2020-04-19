// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Settings;

namespace Stride.Core.Assets.Editor.Components.Properties
{
    internal class PackageSettingsWrapper
    {
        /// <summary>
        /// An helper class to wrap a <see cref="SettingsKey"/> in the context of a given <see cref="SettingsProfile"/> into a simple object.
        /// </summary>
        internal class SettingsKeyWrapper
        {
            protected readonly SettingsProfile Profile;

            /// <summary>
            /// Initializes a new instance of the <see cref="SettingsKeyWrapper"/> class.
            /// </summary>
            /// <param name="key">The <see cref="SettingsKey"/> represented by this instance.</param>
            /// <param name="profile">The <see cref="SettingsProfile"/> in which this settings key is contained.</param>
            protected SettingsKeyWrapper(SettingsKey key, SettingsProfile profile)
            {
                Key = key;
                Profile = profile;
            }

            /// <summary>
            /// Gets the <see cref="SettingsKey"/> associated to this <see cref="SettingsKeyWrapper"/>.
            /// </summary>
            [DataMemberIgnore]
            public SettingsKey Key { get; private set; }

            /// <summary>
            /// Create a new instance of the correct implementation of <see cref="SettingsKeyWrapper"/> that matches the given settings key.
            /// </summary>
            /// <param name="key">The settings key for which to create a instance.</param>
            /// <param name="profile">The <see cref="SettingsProfile"/> in which this settings key is contained.</param>
            /// <param name="package">The package to mark dirty.</param>
            /// <returns>A new instance of the <see cref="SettingsKeyWrapper"/> class.</returns>
            public static SettingsKeyWrapper Create(SettingsKey key, SettingsProfile profile, Package package = null)
            {
                var result = (SettingsKeyWrapper)Activator.CreateInstance(typeof(SettingsKeyWrapper<>).MakeGenericType(key.Type), key, profile, package);
                return result;
            }
        }

        /// <summary>
        /// An helper class to wrap a <see cref="SettingsKey{T}"/> in the context of a given <see cref="SettingsProfile"/> into a simple object.
        /// </summary>
        /// <typeparam name="T">The type of value in the <see cref="SettingsKey{T}"/>.</typeparam>
        internal class SettingsKeyWrapper<T> : SettingsKeyWrapper
        {
            private readonly SettingsKey<T> key;
            private readonly Package package;

            /// <summary>
            /// Initializes a new instance of the <see cref="SettingsKeyWrapper{T}"/> class.
            /// </summary>
            /// <param name="key">The <see cref="SettingsKey{T}"/> represented by this instance.</param>
            /// <param name="profile">The <see cref="SettingsProfile"/> in which this settings key is contained.</param>
            public SettingsKeyWrapper(SettingsKey<T> key, SettingsProfile profile, Package package)
                : base(key, profile)
            {
                this.key = key;
                this.package = package;
            }

            /// <summary>
            /// Gets or sets the current value that can be pushed later to the <see cref="SettingsKey{T}"/> represented by this instance.
            /// </summary>
            [InlineProperty]
            [NotNull]
            public T TypedValue { get { return GetValue(); } set { SetValue(value); if (package != null) package.IsDirty = true; } }
        
            private void SetValue(T value)
            {
                key.SetValue(value, Profile);
            }

            private T GetValue()
            {
                return key.GetValue(Profile, true);
            }
        }

        internal readonly Dictionary<string, SettingsKeyWrapper> NonExecutableUserSettings = new Dictionary<string, SettingsKeyWrapper>();
        internal readonly Dictionary<string, SettingsKeyWrapper> ExecutableUserSettings = new Dictionary<string, SettingsKeyWrapper>();

        internal bool HasExecutables { get; set; }

        [MemberCollection(ReadOnly = true)]
        [Category]
        [NonIdentifiableCollectionItems]
        public Dictionary<string, SettingsKeyWrapper> UserSettings => HasExecutables ? ExecutableUserSettings : NonExecutableUserSettings;
    }
}
