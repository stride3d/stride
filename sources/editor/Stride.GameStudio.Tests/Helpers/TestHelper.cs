// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Assets.Editor.Services;
using Xenko.Core.Assets.Editor.ViewModel.CopyPasteProcessors;
using Xenko.Core.Assets.Quantum;
using Xenko.Assets.Presentation.ViewModel.CopyPasteProcessors;

namespace Xenko.GameStudio.Tests.Helpers
{
    public static class TestHelper
    {
        public static ICopyPasteService CreateCopyPasteService()
        {
            var propertyGraphContainer = new AssetPropertyGraphContainer(new AssetNodeContainer { NodeBuilder = { NodeFactory = new AssetNodeFactory() } });
            return CreateCopyPasteService(propertyGraphContainer);
        }

        public static ICopyPasteService CreateCopyPasteService(AssetPropertyGraphContainer propertyGraphContainer)
        {
            // CopyPasteService is internal
            var serviceType = typeof(Xenko.Core.Assets.Editor.EditorPath).Assembly.GetType("Xenko.Core.Assets.Editor.Services.CopyPasteService");
            var service = (ICopyPasteService)Activator.CreateInstance(serviceType, propertyGraphContainer);
            service.RegisterProcessor(new AssetPropertyPasteProcessor());
            service.RegisterProcessor(new EntityComponentPasteProcessor());
            service.RegisterProcessor(new EntityHierarchyPasteProcessor());
            return service;
        }
    }
}
