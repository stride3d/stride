// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;

using Xenko.Core.Assets.Editor.ViewModel;

namespace Xenko.Core.Assets.Editor.Services
{
    public interface IAssetsPluginService
    {
        IReadOnlyCollection<AssetsPlugin> Plugins { get; }

        bool HasImagesForEnum(SessionViewModel session, Type enumType);

        object GetImageForEnum(SessionViewModel session, object value);

        IEnumerable<Type> GetPrimitiveTypes(SessionViewModel session);

        IEditorView ConstructEditionView(AssetViewModel asset);

        bool HasEditorView(SessionViewModel session, Type assetType);
    }
}
