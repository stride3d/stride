// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using NUnit.Framework;
using Xenko.Core.Assets.Editor.Quantum;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Assets.Quantum;
using Xenko.Core.Assets.Quantum.Tests;
using Xenko.Core.Presentation.Dirtiables;
using Xenko.Core.Presentation.Services;
using Xenko.Core.Quantum;

namespace Xenko.Core.Assets.Editor.Tests
{
    /// <summary>
    /// This class repeats the tests done by <see cref="TestArchetypesBasic"/> and verifies that undo/redo works for them.
    /// </summary>
    [TestFixture]
    public class TestArchetypesBasicUndoRedo
    {
        // TODO: this is a copy of AssetViewModel.AssetPropertyChanged - we'd like to move this out of the view model so we can test the actual normal workflow.
        public class AssetSlimContainer
        {
            private readonly IUndoRedoService undoRedoService;
            private readonly AssetPropertyGraph propertyGraph;

            public AssetSlimContainer(IUndoRedoService undoRedoService, AssetPropertyGraph propertyGraph)
            {
                this.undoRedoService = undoRedoService;
                this.propertyGraph = propertyGraph;
            }

            public void AssetPropertyChanged(object sender, IAssetNodeChangeEventArgs e)
            {
                if (!undoRedoService.UndoRedoInProgress)
                {
                    // Don't create action items if the change comes from the Base
                    if (!propertyGraph.UpdatingPropertyFromBase)
                    {
                        var index = (e as ItemChangeEventArgs)?.Index ?? Index.Empty;
                        var overrideChange = new AssetContentValueChangeOperation((IAssetNode)e.Node, e.ChangeType, index, e.OldValue, e.NewValue, e.PreviousOverride, e.NewOverride, e.ItemId, Enumerable.Empty<IDirtiable>());
                        undoRedoService.PushOperation(overrideChange);
                    }
                }
            }
        }

        private static void RunTest(TestArchetypesRun run)
        {
            var undoRedo = new UndoRedoService(100);
            // We don't have easy access to the context and the graphes, let's use dynamic for simplicity
            dynamic dynamicRun = run;
            var context = dynamicRun.Context;
            ((AssetPropertyGraph)context.BaseGraph).Changed += new AssetSlimContainer(undoRedo, context.BaseGraph).AssetPropertyChanged;
            ((AssetPropertyGraph)context.DerivedGraph).Changed += new AssetSlimContainer(undoRedo, context.DerivedGraph).AssetPropertyChanged;
            ((AssetPropertyGraph)context.BaseGraph).ItemChanged += new AssetSlimContainer(undoRedo, context.BaseGraph).AssetPropertyChanged;
            ((AssetPropertyGraph)context.DerivedGraph).ItemChanged += new AssetSlimContainer(undoRedo, context.DerivedGraph).AssetPropertyChanged;
            using (undoRedo.CreateTransaction())
            {
                run.FirstChange();
            }
            using (undoRedo.CreateTransaction())
            {
                run.SecondChange();
            }
            run.SecondChangeCheck();
            undoRedo.Undo();
            run.FirstChangeCheck();
            undoRedo.Undo();
            run.InitialCheck();
            undoRedo.Redo();
            run.FirstChangeCheck();
            undoRedo.Redo();
            run.SecondChangeCheck();
        }

        [Test]
        public void TestSimplePropertyChange()
        {
            RunTest(TestArchetypesBasic.PrepareSimplePropertyChange());
        }

        [Test]
        public void TestAbstractPropertyChange()
        {
            RunTest(TestArchetypesBasic.PrepareAbstractPropertyChange());
        }

        [Test]
        public void TestSimpleCollectionUpdate()
        {
            RunTest(TestArchetypesBasic.PrepareSimpleCollectionUpdate());
        }

        [Test]
        public void TestSimpleCollectionAdd()
        {
            RunTest(TestArchetypesBasic.PrepareSimpleCollectionAdd());
        }

        [Test]
        public void TestSimpleCollectionRemove()
        {
            RunTest(TestArchetypesBasic.PrepareSimpleCollectionRemove());
        }

        [Test]
        public void TestCollectionInStructUpdate()
        {
            RunTest(TestArchetypesBasic.PrepareCollectionInStructUpdate());
        }

        [Test]
        public void TestSimpleDictionaryUpdate()
        {
            RunTest(TestArchetypesBasic.PrepareSimpleDictionaryUpdate());
        }

        [Test]
        public void TestSimpleDictionaryAdd()
        {
            RunTest(TestArchetypesBasic.PrepareSimpleDictionaryAdd());
        }

        [Test]
        public void TestSimpleDictionaryRemove()
        {
            RunTest(TestArchetypesBasic.PrepareSimpleDictionaryRemove());
        }

        [Test]
        public void TestObjectCollectionUpdate()
        {
            RunTest(TestArchetypesBasic.PrepareObjectCollectionUpdate());
        }

        [Test]
        public void TestObjectCollectionAdd()
        {
            RunTest(TestArchetypesBasic.PrepareObjectCollectionAdd());
        }

        [Test]
        public void TestAbstractCollectionUpdate()
        {
            RunTest(TestArchetypesBasic.PrepareAbstractCollectionUpdate());
        }

        [Test]
        public void TestAbstractCollectionAdd()
        {
            RunTest(TestArchetypesBasic.PrepareAbstractCollectionAdd());
        }

        [Test]
        public void TestAbstractDictionaryUpdate()
        {
            RunTest(TestArchetypesBasic.PrepareAbstractDictionaryUpdate());
        }

        [Test]
        public void TestAbstractDictionaryAdd()
        {
            RunTest(TestArchetypesBasic.PrepareAbstractDictionaryAdd());
        }
    }
}

