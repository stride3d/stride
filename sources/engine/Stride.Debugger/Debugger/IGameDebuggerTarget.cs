// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Engine;

namespace Stride.Debugger.Target
{
    /// <summary>
    /// Controls a game execution host, that can load and unload assemblies, run games and update assets.
    /// </summary>
    public interface IGameDebuggerTarget
    {
        #region Target
        void Exit();
        #endregion

        #region Assembly
        /// <summary>
        /// Loads the assembly.
        /// </summary>
        /// <param name="assemblyPath">The assembly path.</param>
        /// <returns></returns>
        DebugAssembly AssemblyLoad(string assemblyPath);

        /// <summary>
        /// Loads the assembly.
        /// </summary>
        /// <param name="peData">The PE data.</param>
        /// <param name="pdbData">The PDB data.</param>
        /// <returns></returns>
        DebugAssembly AssemblyLoadRaw(byte[] peData, byte[] pdbData);

        /// <summary>
        /// Unregister and register a group of coherent assembly.
        /// </summary>
        /// <param name="assembliesToUnregister">The assemblies to unregister.</param>
        /// <param name="assembliesToRegister">The assemblies to register.</param>
        /// <returns></returns>
        bool AssemblyUpdate(List<DebugAssembly> assembliesToUnregister, List<DebugAssembly> assembliesToRegister);
        #endregion

        #region Game
        /// <summary>
        /// Enumerates the game types available in the currently loaded assemblies.
        /// </summary>
        /// <returns></returns>
        List<string> GameEnumerateTypeNames();

        /// <summary>
        /// Instantiates and launches the specified game, found using its type name.
        /// </summary>
        /// <param name="gameTypeName">Name of the game type.</param>
        void GameLaunch(string gameTypeName);

        /// <summary>
        /// Stops the current game, using <see cref="Game.Exit"/>.
        /// </summary>
        void GameStop();
        #endregion

        #region Assets
        #endregion
    }
}
