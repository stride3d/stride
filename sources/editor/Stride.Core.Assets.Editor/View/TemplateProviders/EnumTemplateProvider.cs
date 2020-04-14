// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;

using Xenko.Core.Assets.Editor.Services;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Presentation.Quantum;
using Xenko.Core.Presentation.Quantum.View;
using Xenko.Core.Presentation.Quantum.ViewModels;

namespace Xenko.Core.Assets.Editor.View.TemplateProviders
{
    public class EnumTemplateProvider : NodeViewModelTemplateProvider
    {
        public override string Name => $"{(ImageEnum ? "Image" : "")}{(FlagEnum ? "Flag" : "")}Enum";

        public bool ImageEnum { get; set; }

        public bool FlagEnum { get; set; }

        public override bool MatchNode(NodeViewModel node)
        {
            var isEnum = TypeIsEnum(node.Type) || (node.NodeValue != null && TypeIsEnum(node.NodeValue.GetType()));
            if (!isEnum)
                return false;

            if (FlagEnum)
            {
                if (!node.Type.GetCustomAttributes(typeof(FlagsAttribute), false).Any())
                    return false;
            }

            if (ImageEnum)
            {
                var pluginService = node.ServiceProvider.TryGet<IAssetsPluginService>();
                if (pluginService == null || !pluginService.HasImagesForEnum(SessionViewModel.Instance, node.Type))
                    return false;
            }

            return true;
        }

        protected static bool TypeIsEnum(Type type)
        {
            var underlyingType = Nullable.GetUnderlyingType(type);
            return type.IsEnum || (underlyingType != null && underlyingType.IsEnum);
        }
    }
}
