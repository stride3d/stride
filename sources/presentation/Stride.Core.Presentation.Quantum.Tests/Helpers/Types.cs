// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Quantum;

namespace Stride.Core.Presentation.Quantum.Tests.Helpers;

public static class Types
{
    public class TestPropertyProvider : IPropertyProviderViewModel
    {
        private readonly IObjectNode rootNode;

        public TestPropertyProvider(IObjectNode rootNode)
        {
            this.rootNode = rootNode;
        }
        public bool CanProvidePropertiesViewModel => true;

        public IObjectNode GetRootNode()
        {
            return rootNode;
        }

        bool IPropertyProviderViewModel.ShouldConstructMember(IMemberNode member) => true;

        bool IPropertyProviderViewModel.ShouldConstructItem(IObjectNode collection, NodeIndex index) => true;
    }

    public class SimpleObject
    {
        public required string Name { get; set; }

        public string? Nam { get; set; } // To test scenario when Name.StartsWith(Nam) is true
    }

    public class DependentPropertyContainer
    {
        public required string Title { get; set; }

        public required SimpleObject Instance { get; set; }
    }

    public class SimpleType
    {
        public required string String { get; set; }
    }

    public class ClassWithRef
    {
        [Display(1)]
        public required string String { get; set; }
        [Display(2)]
        public ClassWithRef? Ref { get; set; }
    }

    public class ClassWithCollection
    {
        [Display(1)]
        public required string String { get; set; }
        [Display(2)]
        public List<string> List { get; set; } = [];
    }

    public class ClassWithRefCollection
    {
        [Display(1)]
        public required string String { get; set; }
        [Display(2)]
        public List<SimpleType> List { get; set; } = [];
    }
}
