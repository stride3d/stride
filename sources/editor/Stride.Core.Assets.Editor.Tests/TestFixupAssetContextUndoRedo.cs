// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xunit;
using Stride.Core.Assets.Editor.Quantum;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Quantum.Tests;
using Stride.Core.Assets.Quantum.Tests.Helpers;
using Stride.Core.Presentation.Services;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Editor.Tests
{
    public class TestFixupAssetContextUndoRedo
    {
        [Fact]
        public void TestChangesToBaseAndDerivedUndoOnce()
        {
            // A fixup (an assembly reload) changes a base asset and its derived one each on its own, with no propagation
            // from the base: undone and redone with propagation, the derived asset would get each change twice
            var context = DeriveAssetTest<Types.MyAsset2, Types.MyAssetBasePropertyGraph>.DeriveAsset(new Types.MyAsset2 { MyStrings = { "String1", "String2" } }, deriveTwice: false);
            var graphContainer = context.Base.Container;
            var baseNode = context.BaseGraph.RootNode[nameof(Types.MyAsset2.MyStrings)].Target;
            var derivedNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset2.MyStrings)].Target;
            var undoRedo = new UndoRedoService(100);

            using (undoRedo.CreateTransaction())
            using (new FixupAssetContext(undoRedo, x => graphContainer.PropagateChangesFromBase = !x))
            {
                foreach (var node in new[] { baseNode, derivedNode })
                {
                    var index = new NodeIndex(1);
                    var oldValue = node.Retrieve(index);
                    node.Remove(oldValue, index);
                    undoRedo.PushOperation(new ContentValueChangeOperation(node, ContentChangeType.CollectionRemove, index, oldValue, null, []));
                }
            }
            Assert.True(graphContainer.PropagateChangesFromBase);
            Assert.Equal(["String1"], context.BaseAsset.MyStrings);
            Assert.Equal(["String1"], context.DerivedAsset.MyStrings);

            undoRedo.Undo();
            Assert.True(graphContainer.PropagateChangesFromBase);
            Assert.Equal(["String1", "String2"], context.BaseAsset.MyStrings);
            Assert.Equal(["String1", "String2"], context.DerivedAsset.MyStrings);

            undoRedo.Redo();
            Assert.True(graphContainer.PropagateChangesFromBase);
            Assert.Equal(["String1"], context.BaseAsset.MyStrings);
            Assert.Equal(["String1"], context.DerivedAsset.MyStrings);
        }
    }
}
