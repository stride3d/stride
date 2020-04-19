// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Stride.Core.Annotations;
using Stride.Core.IO;
using Stride.Core.Serialization;
using Stride.Core.Transactions;

namespace Stride.Core.Settings
{
    /// <summary>
    /// This class represents a collection of values for all registered <see cref="SettingsKey"/>. It may also contains values for settings keys that
    /// are not currently registered, if they exist in the file from which the profile was loaded.
    /// </summary>
    [DataSerializer(typeof(Serializer))]
    public class SettingsProfile : IDisposable
    {
        internal ITransactionStack TransactionStack = TransactionStackFactory.Create(int.MaxValue);
        internal bool Saving;
        private readonly SortedList<UFile, SettingsEntry> settings = new SortedList<UFile, SettingsEntry>();
        private readonly HashSet<UFile> modifiedSettings = new HashSet<UFile>();
        private readonly SettingsProfile parentProfile;
        private FileSystemWatcher fileWatcher;
        private UFile filePath;
        private bool monitorFileModification;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsProfile"/> class.
        /// </summary>
        /// <param name="container">The <see cref="SettingsContainer"/> containing this profile.</param>
        /// <param name="parentProfile">The parent profile.</param>
        internal SettingsProfile(SettingsContainer container, SettingsProfile parentProfile)
        {
            Container = container;
            this.parentProfile = parentProfile;
        }

        /// <summary>
        /// Gets the <see cref="SettingsContainer"/> containing this profile.
        /// </summary>
        public SettingsContainer Container { get; internal set; }

        /// <summary>
        /// Gets the path of the file in which this profile has been saved.
        /// </summary>
        public UFile FilePath { get { return filePath; } internal set { Utils.SetAndInvokeIfChanged(ref filePath, value, UpdateMonitoring); } }

        /// <summary>
        /// Gets or sets whether to monitor external modification of the file in which this profile is stored. If <c>true</c>, The <see cref="FileModified"/> event might be raised.
        /// </summary>
        public bool MonitorFileModification { get { return monitorFileModification; } set { Utils.SetAndInvokeIfChanged(ref monitorFileModification, value, UpdateMonitoring); } }

        /// <summary>
        /// Raised when the file corresponding to this profile is modified on the disk, and <see cref="MonitorFileModification"/> is <c>true</c>.
        /// </summary>
        public event EventHandler<FileModifiedEventArgs> FileModified;

        /// <summary>
        /// Gets the collection of <see cref="SettingsEntry"/> currently existing in this <see cref="SettingsProfile"/>.
        /// </summary>
        internal IDictionary<UFile, SettingsEntry> Settings => settings;

        internal bool IsDiscarding { get; private set; }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (fileWatcher != null)
            {
                fileWatcher.Changed -= SettingsFileChanged;
                fileWatcher.Dispose();
            }
        }

        /// <summary>
        /// Indicates whether this settings profile directly contains the given settings key, without
        /// looking into its parent profile.
        /// </summary>
        /// <param name="key">The settings key to look for.</param>
        /// <returns><c>True</c> if the profile contains the given settings key, <c>False</c> otherwise.</returns>
        public bool ContainsKey([NotNull] SettingsKey key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return ContainsKey(key.Name);
        }

        /// <summary>
        /// Indicates whether this settings profile directly contains the a settings key with the given name, without
        /// looking into its parent profile.
        /// </summary>
        /// <param name="name">The name of the settings key to look for.</param>
        /// <returns><c>True</c> if the profile contains the given settings key, <c>False</c> otherwise.</returns>
        public bool ContainsKey([NotNull] UFile name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            lock (SettingsContainer.SettingsLock)
            {
                return Settings.ContainsKey(name);
            }
        }

        /// <summary>
        /// Removes the given settings key.
        /// </summary>
        /// <param name="key">The settings key to remove.</param>
        /// <returns><c>True</c> if the settings key was removed, <c>false</c> otherwise.</returns>
        public bool Remove([NotNull] SettingsKey key)
        {
            return Remove(key.Name);
        }

        /// <summary>
        /// Removes the settings key that match the given name.
        /// </summary>
        /// <param name="name">The name of the settings key to remove.</param>
        /// <returns><c>True</c> if the settings key was removed, <c>false</c> otherwise.</returns>
        public bool Remove(UFile name)
        {
            lock (SettingsContainer.SettingsLock)
            {
                return Settings.Remove(name);
            }
        }

        /// <summary>
        /// Copies the values of this profile into another profile.
        /// </summary>
        /// <param name="profile">The profile in which to copy the values.</param>
        /// <param name="overrideValues">If <c>false</c>, the values already present in the targt profile won't be overriden.</param>
        public void CopyTo(SettingsProfile profile, bool overrideValues)
        {
            lock (SettingsContainer.SettingsLock)
            {
                foreach (var setting in Settings)
                {
                    if (!overrideValues && profile.Settings.ContainsKey(setting.Key))
                        continue;

                    profile.SetValue(setting.Key, setting.Value.Value);
                }
            }
        }

        public void ValidateSettingsChanges()
        {
            var keys = Container.GetAllSettingsKeys();
            List<SettingsKey> modified;
            lock (SettingsContainer.SettingsLock)
            {
                modified = keys.Where(x => modifiedSettings.Contains(x.Name)).ToList();
            }
            foreach (var key in modified)
            {
                key.NotifyChangesValidated(this);
            }
            lock (SettingsContainer.SettingsLock)
            {
                TransactionStack.Clear();
                modifiedSettings.Clear();
            }
        }

        public void DiscardSettingsChanges()
        {
            IsDiscarding = true;
            lock (SettingsContainer.SettingsLock)
            {
                while (TransactionStack.CanRollback)
                {
                    TransactionStack.Rollback();
                }
                TransactionStack.Clear();
                modifiedSettings.Clear();
            }
            IsDiscarding = false;
        }

        /// <summary>
        /// Registers an entry that has not been registered before.
        /// </summary>
        /// <param name="entry">The entry to register.</param>
        internal void RegisterEntry([NotNull] SettingsEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            lock (SettingsContainer.SettingsLock)
            {
                Settings.Add(entry.Name, entry);
            }
        }

        /// <summary>
        /// Gets the settings value that matches the given name.
        /// </summary>
        /// <param name="name">The name of the <see cref="SettingsEntry"/> to fetch.</param>
        /// <param name="value">The resulting value if the name is found, <c>null</c> otherwise.</param>
        /// <param name="searchInParent">Indicates whether to search in the parent profile, if the name is not found in this profile.</param>
        /// <param name="createInCurrentProfile">If true, the list will be created in the current profile, from the value of its parent profile.</param>
        /// <returns><c>true</c> if an entry matching the name is found, <c>false</c> otherwise.</returns>
        internal bool GetValue([NotNull] UFile name, out object value, bool searchInParent, bool createInCurrentProfile)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            SettingsEntry entry = GetEntry(name, searchInParent, createInCurrentProfile);
            if (entry != null)
            {
                value = entry.Value;
                return true;
            }
            value = null;
            return false;
        }

        /// <summary>
        /// Set the value of the entry that match the given name.
        /// </summary>
        /// <param name="name">The name to match.</param>
        /// <param name="value">The value to set.</param>
        internal void SetValue([NotNull] UFile name, object value)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            lock (SettingsContainer.SettingsLock)
            {
                SettingsEntry entry;
                if (!Settings.TryGetValue(name, out entry))
                {
                    entry = SettingsEntry.CreateFromValue(this, name, value);
                    Settings[name] = entry;
                }
                else
                {
                    Settings[name].Value = value;
                }
            }
        }

        /// <summary>
        /// Notifies that the entry with the given name has changed.
        /// </summary>
        /// <param name="name">The name of the entry that has changed.</param>
        internal void NotifyEntryChanged(UFile name)
        {
            lock (SettingsContainer.SettingsLock)
            {
                modifiedSettings.Add(name);
            }
        }

        /// <summary>
        /// Gets the <see cref="SettingsEntry"/> that matches the given name.
        /// </summary>
        /// <param name="name">The name of the <see cref="SettingsEntry"/> to fetch.</param>
        /// <param name="searchInParent">Indicates whether to search in the parent profile, if the name is not found in this profile.</param>
        /// <param name="createInCurrentProfile"></param>
        /// <returns>An instance of <see cref="SettingsEntry"/> that matches the name, or <c>null</c>.</returns>
        private SettingsEntry GetEntry([NotNull] UFile name, bool searchInParent, bool createInCurrentProfile)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            lock (SettingsContainer.SettingsLock)
            {
                SettingsEntry entry;
                if (Settings.TryGetValue(name, out entry))
                    return entry;

                if (createInCurrentProfile)
                {
                    entry = parentProfile.GetEntry(name, true, false);
                    entry = SettingsEntry.CreateFromValue(this, name, entry.Value);
                    RegisterEntry(entry);
                    return entry;
                }
            }

            return parentProfile != null && searchInParent ? parentProfile.GetEntry(name, true, false) : null;
        }

        private void UpdateMonitoring()
        {
            if (fileWatcher != null)
            {
                fileWatcher.Changed -= SettingsFileChanged;
                fileWatcher.Dispose();
            }
            if (MonitorFileModification && FilePath != null && File.Exists(FilePath))
            {
                fileWatcher = new FileSystemWatcher(Path.Combine(Environment.CurrentDirectory, FilePath.GetFullDirectory()), FilePath.GetFileName());
                fileWatcher.Changed += SettingsFileChanged;
                fileWatcher.EnableRaisingEvents = true;
            }
        }

        private void SettingsFileChanged(object sender, FileSystemEventArgs e)
        {
            if (Saving)
                return;

            var handler = FileModified;
            if (handler != null)
            {
                var args = new FileModifiedEventArgs(this);
                handler(null, args);
                if (args.ReloadFile)
                {
                    Container.ReloadSettingsProfile(this);
                }
            }
        }

        internal class Serializer : DataSerializer<SettingsProfile>
        {
            public override void Serialize(ref SettingsProfile obj, ArchiveMode mode, SerializationStream stream)
            {
                throw new NotImplementedException();
            }
        }
    }
}
