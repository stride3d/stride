// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable

namespace Stride.Core.Assets.Editor;

public static class EditorPath
{
    public static string EditorTitle { get; set; }

    /// <summary>What this editor process runs on, e.g. ".NET 11.0.0-rc.1, Vulkan"; for windows whose title stays short.</summary>
    public static string? EditorEnvironment { get; set; }

    private static string userDataPath;

    /// <summary>
    /// Gets or sets the folder of the settings files of the editor (<see cref="EditorConfigPath"/>, <see cref="InternalConfigPath"/>).
    /// </summary>
    /// <remarks>
    /// The <c>stride</c> folder of the user's application data unless set. Tests set a folder of their own so that they neither
    /// read nor write the user's settings: on Windows, the application data folder does not follow the <c>APPDATA</c> variable.
    /// </remarks>
    public static string UserDataPath
    {
        get => userDataPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.DoNotVerify), ProductNameDirectory);
        set => userDataPath = value;
    }

    /// <summary>
    /// Gets or sets the folder of the recovery copies of the sessions (<see cref="Components.Recovery.SessionRecoveryService"/>).
    /// </summary>
    /// <remarks>
    /// The <c>stride/Recovery</c> folder of the user's local application data unless set, outside of any project; under
    /// <see cref="UserDataPath"/> when the tests have set it.
    /// </remarks>
    public static string RecoveryPath
    {
        get => field ?? (userDataPath is not null
            ? Path.Combine(userDataPath, "Recovery")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify), ProductNameDirectory, "Recovery"));
        set => field = value;
    }

    public static string DefaultTempPath => Path.Combine(
        Path.GetTempPath(),
        ProductNameDirectory);

    public static string EditorConfigPath => Path.Combine(UserDataPath, "GameStudioSettings.conf");

    public static string InternalConfigPath => Path.Combine(UserDataPath, "GameStudioInternal.conf");

    private static string ProductNameDirectory => "stride";
}
