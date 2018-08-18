// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Xenko.Core.Annotations;
using Xenko.Core.Extensions;
using Xenko.Core.IO;
using Xenko.Core.Yaml;
using Xenko.Core.Yaml.Events;

namespace Xenko.Core.Settings
{
    /// <summary>
    /// This class represents property to store in the settings that is identified by a key.
    /// </summary>
    public abstract class SettingsKey
    {
        /// <summary>
        /// The default value of the settings key.
        /// </summary>
        protected readonly object DefaultObjectValue;

        /// <summary>
        /// The default value of the settings key.
        /// </summary>
        protected readonly Func<object> DefaultObjectValueCallback;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsKey"/> class.
        /// </summary>
        /// <param name="name">The name of this settings key. Must be unique amongst the application.</param>
        /// <param name="container">The <see cref="SettingsContainer"/> containing this <see cref="SettingsKey"/>.</param>
        /// <param name="defaultValue">The default value associated to this settings key.</param>
        protected SettingsKey([NotNull] UFile name, SettingsContainer container, object defaultValue)
        {
            Name = name;
            DisplayName = name;
            DefaultObjectValue = defaultValue;
            Container = container;
            Container.RegisterSettingsKey(name, defaultValue, this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsKey"/> class.
        /// </summary>
        /// <param name="name">The name of this settings key. Must be unique amongst the application.</param>
        /// <param name="container">The <see cref="SettingsContainer"/> containing this <see cref="SettingsKey"/>.</param>
        /// <param name="defaultValueCallback">A function that returns the default value associated to this settings key.</param>
        protected SettingsKey([NotNull] UFile name, SettingsContainer container, [NotNull] Func<object> defaultValueCallback)
        {
            Name = name;
            DisplayName = name;
            DefaultObjectValueCallback = defaultValueCallback;
            Container = container;
            Container.RegisterSettingsKey(name, defaultValueCallback(), this);
        }

        /// <summary>
        /// Gets the name of this <see cref="SettingsKey"/>.
        /// </summary>
        public UFile Name { get; private set; }

        /// <summary>
        /// Gets the type of this <see cref="SettingsKey"/>.
        /// </summary>
        public abstract Type Type { get; }

        /// <summary>
        /// Gets the <see cref="SettingsContainer"/> containing this <see cref="SettingsKey"/>.
        /// </summary>
        public SettingsContainer Container { get; private set; }

        /// <summary>
        /// Gets or sets the display name of the <see cref="SettingsKey"/>.
        /// </summary>
        /// <remarks>The default value is the name parameter given to the constructor of this class.</remarks>
        public UFile DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the description of this <see cref="SettingsKey"/>.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets an enumeration of acceptable values for this <see cref="SettingsKey"/>.
        /// </summary>
        public abstract IEnumerable<object> AcceptableValues { get; }

        /// <summary>
        /// Gets a collection of fallback deserializer methods in case the default deserialization throws an exception.
        /// </summary>
        /// <remarks>Fallback deserializers can be useful for migration of settings keys when the type of settings has changed.</remarks>
        public List<Func<EventReader, object>> FallbackDeserializers { get; } = new List<Func<EventReader, object>>();

        /// <summary>
        /// Raised when the value of the settings key has been modified and the method <see cref="SettingsProfile.ValidateSettingsChanges"/> has been invoked.
        /// </summary>
        public event EventHandler<ChangesValidatedEventArgs> ChangesValidated;

        /// <summary>
        /// Converts a value of a different type to the type associated with this <see cref="SettingsKey"/>. If the conversion is not possible,
        /// this method will return the default value of the SettingsKey.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>The converted value if the conversion is possible, the default value otherwise.</returns>
        internal abstract object ConvertValue(List<ParsingEvent> value);

        /// <summary>
        /// Notifes that the changes have been validated by <see cref="SettingsProfile.ValidateSettingsChanges"/>.
        /// </summary>
        /// <param name="profile">The profile in which the change has been validated.</param>
        internal void NotifyChangesValidated(SettingsProfile profile)
        {
            ChangesValidated?.Invoke(this, new ChangesValidatedEventArgs(profile));
        }

        /// <summary>
        /// Resolves the profile to use, returning the current profile if the given profile is null and checking the consistency of related <see cref="SettingsContainer"/>.
        /// </summary>
        /// <param name="profile">The profile to resolve.</param>
        /// <returns>The resolved profile.</returns>
        [NotNull]
        protected SettingsProfile ResolveProfile(SettingsProfile profile = null)
        {
            profile = profile ?? Container.CurrentProfile;
            if (profile.Container != Container)
                throw new ArgumentException("This settings key has a different container that the given settings profile.");
            return profile;
        }
    }

    /// <summary>
    /// This class represents a <see cref="SettingsKey"/> containing a value of the specified type <see cref="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of value contained in this settings key.</typeparam>
    public class SettingsKey<T> : SettingsKey
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsKey{T}"/> class.
        /// </summary>
        /// <param name="name">The name of the settings key. Must be unique amongst an application.</param>
        /// <param name="container">The <see cref="SettingsContainer"/> containing this <see cref="SettingsKey"/>.</param>
        public SettingsKey([NotNull] UFile name, SettingsContainer container)
            : this(name, container, default(T))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsKey{T}"/> class.
        /// </summary>
        /// <param name="name">The name of the settings key. Must be unique amongst an application.</param>
        /// <param name="container">The <see cref="SettingsContainer"/> containing this <see cref="SettingsKey"/>.</param>
        /// <param name="defaultValue">The default value for this settings key.</param>
        public SettingsKey([NotNull] UFile name, SettingsContainer container, T defaultValue)
            : base(name, container, defaultValue)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsKey{T}"/> class.
        /// </summary>
        /// <param name="name">The name of this settings key. Must be unique amongst the application.</param>
        /// <param name="container">The <see cref="SettingsContainer"/> containing this <see cref="SettingsKey"/>.</param>
        /// <param name="defaultValueCallback">A function that returns the default value associated to this settings key.</param>
        public SettingsKey([NotNull] UFile name, SettingsContainer container, [NotNull] Func<object> defaultValueCallback)
            : base(name, container, defaultValueCallback)
        {
        }

        /// <inheritdoc/>
        [NotNull]
        public override Type Type { get { return typeof(T); } }

        /// <summary>
        /// Gets the default value of this settings key.
        /// </summary>
        public T DefaultValue { get { return DefaultObjectValueCallback != null ? (T)DefaultObjectValueCallback() : (T)DefaultObjectValue; } }

        /// <summary>
        /// Gets or sets a function that returns an enumation of acceptable values for this <see cref="SettingsKey{T}"/>.
        /// </summary>
        public Func<IEnumerable<T>> GetAcceptableValues { get; set; }

        /// <inheritdoc/>
        public override IEnumerable<object> AcceptableValues { get { return GetAcceptableValues != null ? (IEnumerable<object>)GetAcceptableValues() : Enumerable.Empty<object>(); } }

        /// <summary>
        /// Gets the value of this settings key in the given profile.
        /// </summary>
        /// <param name="profile">The profile in which to look for the value. If <c>null</c>, it will look in the <see cref="SettingsContainer.CurrentProfile"/>.</param>
        /// <param name="searchInParentProfile">If true, the settings service will look in the parent profile of the given profile if the settings key is not defined into it.</param>
        /// <returns>The value of this settings key.</returns>
        /// <exception cref="KeyNotFoundException">No value can be found in the given profile matching this settings key.</exception>
        public T GetValue(SettingsProfile profile, bool searchInParentProfile)
        {
            object value;
            profile = ResolveProfile(profile);
            if (profile.GetValue(Name, out value, searchInParentProfile, false))
            {
                try
                {
                    return (T)value;
                }
                catch (Exception e)
                {
                    // Return default value
                    e.Ignore();
                }
            }
            return DefaultValue;
        }

        /// <summary>
        /// Gets the value of this settings key in the current profile.
        /// </summary>
        /// <returns>The value of this settings key.</returns>
        public T GetValue()
        {
            object value;
            var profile = ResolveProfile();
            if (profile.GetValue(Name, out value, true, false))
            {
                try
                {
                    return (T)value;
                }
                catch (Exception)
                {
                    return DefaultValue;
                }
            }
            // This should never happen
            throw new KeyNotFoundException("Settings key not found");
        }

        /// <summary>
        /// Sets the value of this settings key in the given profile.
        /// </summary>
        /// <param name="value">The new value to set.</param>
        /// <param name="profile">The profile in which to set the value. Must be a non-null that uses the same <see cref="SettingsContainer"/> that this <see cref="SettingsKey"/>.</param>
        public void SetValue(T value, SettingsProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            profile = ResolveProfile(profile);
            profile.SetValue(Name, value);
        }

        /// <summary>
        /// Sets the value of this settings key in the current profile.
        /// </summary>
        /// <param name="value">The new value to set.</param>
        public void SetValue(T value)
        {
            var profile = ResolveProfile();
            profile.SetValue(Name, value);
        }

        /// <inheritdoc/>
        internal override object ConvertValue(List<ParsingEvent> parsingEvents)
        {
            // First use default deserializer to deserialize value.
            try
            {
                var eventReader = new EventReader(new MemoryParser(parsingEvents));
                return SettingsYamlSerializer.Default.Deserialize(eventReader, Type);
            }
            catch (Exception e)
            {
                e.Ignore();
            }

            // If this fails, try to use any available fallback deserializer
            foreach (var deserializer in FallbackDeserializers)
            {
                try
                {
                    var eventReader = new EventReader(new MemoryParser(parsingEvents));
                    return deserializer.Invoke(eventReader);
                }
                catch (Exception e)
                {
                    e.Ignore();
                }
            }

            // Can't decode back, use default value
            return DefaultValue;
        }
    }
}
