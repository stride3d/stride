// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Transactions;

namespace Stride.Core.Settings;

/// <summary>
/// An internal object allowing to rollback/rollforward the creation of a <see cref="SettingsEntry"/> in a <see cref="SettingsProfile"/>,
/// the first time a value is set for its key in this profile.
/// </summary>
internal sealed class SettingsEntryAddOperation : Operation
{
    private readonly SettingsProfile profile;
    private readonly SettingsEntry entry;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsEntryAddOperation"/> class.
    /// </summary>
    /// <param name="profile">The profile the entry has been added to.</param>
    /// <param name="entry">The settings entry that has been added.</param>
    public SettingsEntryAddOperation(SettingsProfile profile, SettingsEntry entry)
    {
        this.profile = profile;
        this.entry = entry;
    }

    /// <inheritdoc/>
    protected override void Rollback()
    {
        lock (SettingsContainer.SettingsLock)
        {
            // Removed only if it is still this entry: the profile then falls back to its parent, or to the key's default value
            if (profile.Settings.TryGetValue(entry.Name, out var current) && current == entry)
                profile.Settings.Remove(entry.Name);
        }
    }

    /// <inheritdoc/>
    protected override void Rollforward()
    {
        lock (SettingsContainer.SettingsLock)
        {
            profile.Settings[entry.Name] = entry;
        }
    }
}
