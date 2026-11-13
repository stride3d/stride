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
        get => userDataPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ProductNameDirectory);
        set => userDataPath = value;
    }

    public static string DefaultTempPath => Path.Combine(
        Path.GetTempPath(),
        ProductNameDirectory);

    public static string EditorConfigPath => Path.Combine(UserDataPath, "GameStudioSettings.conf");

    public static string InternalConfigPath => Path.Combine(UserDataPath, "GameStudioInternal.conf");

    private static string ProductNameDirectory => "stride";
}
