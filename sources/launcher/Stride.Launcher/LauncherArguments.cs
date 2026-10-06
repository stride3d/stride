// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.


namespace Stride.Launcher;

/// <summary>
/// A structure representing the arguments passed to the launcher process.
/// </summary>
internal struct LauncherArguments
{
    /// <summary>
    /// An enum representing the type of action this process should perform.
    /// </summary>
    public enum ActionType
    {
        Run,
        Uninstall,
    }

    /// <summary>
    /// The list of actions this process should perform.
    /// </summary>
    public List<ActionType> Actions;

    /// <summary>
    /// No UI at all (<c>/quiet</c>): the setup passes it to <c>/uninstall</c> in a silent uninstall (<c>msiexec /qn</c>, or <c>/qr</c> as in <c>winget uninstall</c>),
    /// which a dialog would block.
    /// </summary>
    public bool Quiet;

    public string[] Args;
}
