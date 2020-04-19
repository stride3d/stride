// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;
using Stride.Assets.Scripts;

namespace Stride.Assets.Presentation.AssetEditors.VisualScriptEditor
{
    public interface IVisualScriptViewModelService
    {
        /// <summary>
        /// When a variable is dropped in the main view, we want to ask user what he wants to do with it (create a getter or setter?) and position it properly.
        /// </summary>
        /// <returns>The created block, or null if cancelled.</returns>
        Task<Block> TransformVariableIntoBlock(Symbol symbol);
    }
}
