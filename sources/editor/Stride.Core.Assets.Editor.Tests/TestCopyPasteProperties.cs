// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel.CopyPasteProcessors;
using Stride.Core.Assets.Quantum;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Editor.Tests
{
    public sealed class TestCopyPasteProperties
    {
        // Categories
        private const string PasteDictionaryIntoDictionary = "Paste a dictionary into another dictionary";
        private const string PasteItemIntoDictionary = "Paste an item into a dictionary";
        private const string PasteItemIntoList = "Paste an item into a list";
        private const string PasteListIntoList = "Paste a list into another list";
        private const string ReplaceDictionaryByDictionary = "Replace a dictionary by another dictionary";
        private const string ReplaceItemInDictionary = "Replace an item in a dictionary";
        private const string ReplaceItemInList = "Replace an item in a list";
        private const string ReplaceListByList = "Replace a list by another list";
        private const string SimpleProperty = "Paste simple property";

        private ICopyPasteService service;
        private AssetPropertyGraphContainer propertyGraphContainer;

        public TestCopyPasteProperties()
        {
            propertyGraphContainer = new AssetPropertyGraphContainer(new AssetNodeContainer { NodeBuilder = { NodeFactory = new AssetNodeFactory() } });
            service = new CopyPasteService { PropertyGraphContainer = propertyGraphContainer };
            service.RegisterProcessor(new AssetPropertyPasteProcessor());
        }

        [Fact]
        public void TestCopyPasteSingleProperty()
        {
            var source = new MyClass { Float = 5.0f };
            var target = new MyClass { Float = 2.0f };
            var copiedText = Copy(source, source.Float);
            Paste(target, copiedText, typeof(float), typeof(float), x => x[nameof(MyClass.Float)], NodeIndex.Empty, false);
            Assert.Equal(5.0f, target.Float);
        }

        [Fact]
        public void TestPasteStringAsFloatProperty()
        {
            var target = new MyClass { Float = 2.0f };
            Paste(target, "5", typeof(float), typeof(float), x => x[nameof(MyClass.Float)], NodeIndex.Empty, false);
            Assert.Equal(5.0f, target.Float);
        }

        [Fact]
        public void TestCopyPasteStructMemberProperty()
        {
            var source = new MyClass { Struct = new MyStruct { Integer = 5 } };
            var target = new MyClass { Struct = new MyStruct { Integer = 2 } };
            var copiedText = Copy(source, source.Struct.Integer);
            Paste(target, copiedText, typeof(int), typeof(int), x => x[nameof(MyClass.Struct)].Target[nameof(MyStruct.Integer)], NodeIndex.Empty, false);
            Assert.Equal(5, target.Struct.Integer);
        }

        [Fact]
        public void TestCopyPasteStructProperty()
        {
            var source = new MyClass { Struct = new MyStruct { Integer = 5 } };
            var target = new MyClass { Struct = new MyStruct { Integer = 2 } };
            var copiedText = Copy(source, source.Struct);
            Paste(target, copiedText, typeof(MyStruct), typeof(MyStruct), x => x[nameof(MyClass.Struct)], NodeIndex.Empty, false);
            Assert.Equal(5, target.Struct.Integer);
        }

        [Fact]
        public void TestCopyPasteClassMemberProperty()
        {
            var source = new MyClass { Sub = new MyClass { Float = 5 } };
            var target = new MyClass { Sub = new MyClass { Float = 2 } };
            var copiedText = Copy(source, source.Sub.Float);
            Paste(target, copiedText, typeof(int), typeof(int), x => x[nameof(MyClass.Sub)].Target[nameof(MyClass.Float)], NodeIndex.Empty, false);
            Assert.Equal(5, target.Sub.Float);
        }

        [Fact]
        public void TestCopyPasteClassProperty()
        {
            var source = new MyClass { Sub = new MyClass { Float = 5 } };
            var target = new MyClass { Sub = new MyClass { Float = 2 } };
            var copiedText = Copy(source, source.Sub);
            Paste(target, copiedText, typeof(MyClass), typeof(MyClass), x => x[nameof(MyClass.Sub)], NodeIndex.Empty, false);
            Assert.Equal(5, target.Sub.Float);
        }

        [Fact]
        public void TestCopyPasteListDouble()
        {
            var source = new MyClass { DoubleList = new List<double> { 1, 2, 3 } };
            var target = new MyClass { DoubleList = new List<double> { 4, 5, 6 } };
            var copiedText = Copy(source, source.DoubleList);
            Paste(target, copiedText, typeof(List<double>), typeof(List<double>), x => x[nameof(MyClass.DoubleList)], NodeIndex.Empty, false);
            Assert.Equal(6, target.DoubleList.Count);
            Assert.Equal(4.0, target.DoubleList[0]);
            Assert.Equal(5.0, target.DoubleList[1]);
            Assert.Equal(6.0, target.DoubleList[2]);
            Assert.Equal(1.0, target.DoubleList[3]);
            Assert.Equal(2.0, target.DoubleList[4]);
            Assert.Equal(3.0, target.DoubleList[5]);
        }

        [Fact]
        public void TestCopyReplaceListDouble()
        {
            var source = new MyClass { DoubleList = new List<double> { 1, 2, 3 } };
            var target = new MyClass { DoubleList = new List<double> { 4, 5, 6 } };
            var copiedText = Copy(source, source.DoubleList);
            Paste(target, copiedText, typeof(List<double>), typeof(List<double>), x => x[nameof(MyClass.DoubleList)], NodeIndex.Empty, true);
            Assert.Equal(3, target.DoubleList.Count);
            Assert.Equal(1.0, target.DoubleList[0]);
            Assert.Equal(2.0, target.DoubleList[1]);
            Assert.Equal(3.0, target.DoubleList[2]);
        }

        [Fact]
        public void TestCopyPasteListStruct()
        {
            var source = new MyClass { StructList = new List<MyStruct> { new MyStruct { Integer = 1 }, new MyStruct { Integer = 2 }, new MyStruct { Integer = 3 } } };
            var target = new MyClass { StructList = new List<MyStruct> { new MyStruct { Integer = 4 }, new MyStruct { Integer = 5 }, new MyStruct { Integer = 6 } } };
            var copiedText = Copy(source, source.StructList);
            Paste(target, copiedText, typeof(List<MyStruct>), typeof(List<MyStruct>), x => x[nameof(MyClass.StructList)], NodeIndex.Empty, false);
            Assert.Equal(6, target.StructList.Count);
            Assert.Equal(4.0, target.StructList[0].Integer);
            Assert.Equal(5.0, target.StructList[1].Integer);
            Assert.Equal(6.0, target.StructList[2].Integer);
            Assert.Equal(1.0, target.StructList[3].Integer);
            Assert.Equal(2.0, target.StructList[4].Integer);
            Assert.Equal(3.0, target.StructList[5].Integer);
        }

        [Fact]
        public void TestCopyReplaceListStruct()
        {
            var source = new MyClass { StructList = new List<MyStruct> { new MyStruct { Integer = 1 }, new MyStruct { Integer = 2 }, new MyStruct { Integer = 3 } } };
            var target = new MyClass { StructList = new List<MyStruct> { new MyStruct { Integer = 4 }, new MyStruct { Integer = 5 }, new MyStruct { Integer = 6 } } };
            var copiedText = Copy(source, source.StructList);
            Paste(target, copiedText, typeof(List<MyStruct>), typeof(List<MyStruct>), x => x[nameof(MyClass.StructList)], NodeIndex.Empty, true);
            Assert.Equal(3, target.StructList.Count);
            Assert.Equal(1.0, target.StructList[0].Integer);
            Assert.Equal(2.0, target.StructList[1].Integer);
            Assert.Equal(3.0, target.StructList[2].Integer);
        }

        [Fact]
        public void TestCopyPasteListClass()
        {
            var source = new MyClass { SubList = new List<MyClass> { new MyClass { Float = 1 }, new MyClass { Float = 2 }, new MyClass { Float = 3 } } };
            var target = new MyClass { SubList = new List<MyClass> { new MyClass { Float = 4 }, new MyClass { Float = 5 }, new MyClass { Float = 6 } } };
            var copiedText = Copy(source, source.SubList);
            Paste(target, copiedText, typeof(List<MyClass>), typeof(List<MyClass>), x => x[nameof(MyClass.SubList)], NodeIndex.Empty, false);
            Assert.Equal(6, target.SubList.Count);
            Assert.Equal(4.0, target.SubList[0].Float);
            Assert.Equal(5.0, target.SubList[1].Float);
            Assert.Equal(6.0, target.SubList[2].Float);
            Assert.Equal(1.0, target.SubList[3].Float);
            Assert.Equal(2.0, target.SubList[4].Float);
            Assert.Equal(3.0, target.SubList[5].Float);
        }

        [Fact]
        public void TestCopyReplaceListClass()
        {
            var source = new MyClass { SubList = new List<MyClass> { new MyClass { Float = 1 }, new MyClass { Float = 2 }, new MyClass { Float = 3 } } };
            var target = new MyClass { SubList = new List<MyClass> { new MyClass { Float = 4 }, new MyClass { Float = 5 }, new MyClass { Float = 6 } } };
            var copiedText = Copy(source, source.SubList);
            Paste(target, copiedText, typeof(List<MyClass>), typeof(List<MyClass>), x => x[nameof(MyClass.SubList)], NodeIndex.Empty, true);
            Assert.Equal(3, target.SubList.Count);
            Assert.Equal(1.0, target.SubList[0].Float);
            Assert.Equal(2.0, target.SubList[1].Float);
            Assert.Equal(3.0, target.SubList[2].Float);
        }

        [Fact]
        public void TestCopyPasteListDoubleIntoItem()
        {
            var source = new MyClass { DoubleList = new List<double> { 1, 2, 3 } };
            var target = new MyClass { DoubleList = new List<double> { 4, 5, 6 } };
            var copiedText = Copy(source, source.DoubleList);
            Paste(target, copiedText, typeof(List<double>), typeof(List<double>), x => x[nameof(MyClass.DoubleList)].Target, new NodeIndex(1), false);
            Assert.Equal(6, target.DoubleList.Count);
            Assert.Equal(4.0, target.DoubleList[0]);
            Assert.Equal(1.0, target.DoubleList[1]);
            Assert.Equal(2.0, target.DoubleList[2]);
            Assert.Equal(3.0, target.DoubleList[3]);
            Assert.Equal(5.0, target.DoubleList[4]);
            Assert.Equal(6.0, target.DoubleList[5]);
        }

        [Fact]
        public void TestCopyReplaceListDoubleIntoItem()
        {
            var source = new MyClass { DoubleList = new List<double> { 1, 2, 3 } };
            var target = new MyClass { DoubleList = new List<double> { 4, 5, 6 } };
            var copiedText = Copy(source, source.DoubleList);
            Paste(target, copiedText, typeof(List<double>), typeof(List<double>), x => x[nameof(MyClass.DoubleList)].Target, new NodeIndex(1), true);
            Assert.Equal(5, target.DoubleList.Count);
            Assert.Equal(4.0, target.DoubleList[0]);
            Assert.Equal(1.0, target.DoubleList[1]);
            Assert.Equal(2.0, target.DoubleList[2]);
            Assert.Equal(3.0, target.DoubleList[3]);
            Assert.Equal(6.0, target.DoubleList[4]);
        }

        [Fact]
        public void TestCopyPasteListStructIntoItem()
        {
            var source = new MyClass { StructList = new List<MyStruct> { new MyStruct { Integer = 1 }, new MyStruct { Integer = 2 }, new MyStruct { Integer = 3 } } };
            var target = new MyClass { StructList = new List<MyStruct> { new MyStruct { Integer = 4 }, new MyStruct { Integer = 5 }, new MyStruct { Integer = 6 } } };
            var copiedText = Copy(source, source.StructList);
            Paste(target, copiedText, typeof(List<MyStruct>), typeof(List<MyStruct>), x => x[nameof(MyClass.StructList)].Target, new NodeIndex(1), false);
            Assert.Equal(6, target.StructList.Count);
            Assert.Equal(4.0, target.StructList[0].Integer);
            Assert.Equal(1.0, target.StructList[1].Integer);
            Assert.Equal(2.0, target.StructList[2].Integer);
            Assert.Equal(3.0, target.StructList[3].Integer);
            Assert.Equal(5.0, target.StructList[4].Integer);
            Assert.Equal(6.0, target.StructList[5].Integer);
        }

        [Fact]
        public void TestCopyReplaceListStructIntoItem()
        {
            var source = new MyClass { StructList = new List<MyStruct> { new MyStruct { Integer = 1 }, new MyStruct { Integer = 2 }, new MyStruct { Integer = 3 } } };
            var target = new MyClass { StructList = new List<MyStruct> { new MyStruct { Integer = 4 }, new MyStruct { Integer = 5 }, new MyStruct { Integer = 6 } } };
            var copiedText = Copy(source, source.StructList);
            Paste(target, copiedText, typeof(List<MyStruct>), typeof(List<MyStruct>), x => x[nameof(MyClass.StructList)].Target, new NodeIndex(1), true);
            Assert.Equal(5, target.StructList.Count);
            Assert.Equal(4.0, target.StructList[0].Integer);
            Assert.Equal(1.0, target.StructList[1].Integer);
            Assert.Equal(2.0, target.StructList[2].Integer);
            Assert.Equal(3.0, target.StructList[3].Integer);
            Assert.Equal(6.0, target.StructList[4].Integer);
        }

        [Fact]
        public void TestCopyPasteListClassIntoItem()
        {
            var source = new MyClass { SubList = new List<MyClass> { new MyClass { Float = 1 }, new MyClass { Float = 2 }, new MyClass { Float = 3 } } };
            var target = new MyClass { SubList = new List<MyClass> { new MyClass { Float = 4 }, new MyClass { Float = 5 }, new MyClass { Float = 6 } } };
            var copiedText = Copy(source, source.SubList);
            Paste(target, copiedText, typeof(List<MyClass>), typeof(List<MyClass>), x => x[nameof(MyClass.SubList)].Target, new NodeIndex(1), false);
            Assert.Equal(6, target.SubList.Count);
            Assert.Equal(4.0, target.SubList[0].Float);
            Assert.Equal(1.0, target.SubList[1].Float);
            Assert.Equal(2.0, target.SubList[2].Float);
            Assert.Equal(3.0, target.SubList[3].Float);
            Assert.Equal(5.0, target.SubList[4].Float);
            Assert.Equal(6.0, target.SubList[5].Float);
        }

        [Fact]
        public void TestCopyReplaceListClassIntoItem()
        {
            var source = new MyClass { SubList = new List<MyClass> { new MyClass { Float = 1 }, new MyClass { Float = 2 }, new MyClass { Float = 3 } } };
            var target = new MyClass { SubList = new List<MyClass> { new MyClass { Float = 4 }, new MyClass { Float = 5 }, new MyClass { Float = 6 } } };
            var copiedText = Copy(source, source.SubList);
            Paste(target, copiedText, typeof(List<MyClass>), typeof(List<MyClass>), x => x[nameof(MyClass.SubList)].Target, new NodeIndex(1), true);
            Assert.Equal(5, target.SubList.Count);
            Assert.Equal(4.0, target.SubList[0].Float);
            Assert.Equal(1.0, target.SubList[1].Float);
            Assert.Equal(2.0, target.SubList[2].Float);
            Assert.Equal(3.0, target.SubList[3].Float);
            Assert.Equal(6.0, target.SubList[4].Float);
        }

        [Fact]
        public void TestCopyPasteDoubleIntoList()
        {
            var source = new MyClass { DoubleList = new List<double> { 1, 2, 3 } };
            var target = new MyClass { DoubleList = new List<double> { 4, 5, 6 } };
            var copiedText = Copy(source, source.DoubleList[2]);
            Paste(target, copiedText, typeof(List<double>), typeof(List<double>), x => x[nameof(MyClass.DoubleList)].Target, new NodeIndex(1), false);
            Assert.Equal(4, target.DoubleList.Count);
            Assert.Equal(4.0, target.DoubleList[0]);
            Assert.Equal(3.0, target.DoubleList[1]);
            Assert.Equal(5.0, target.DoubleList[2]);
            Assert.Equal(6.0, target.DoubleList[3]);

            target = new MyClass { DoubleList = new List<double> { 4, 5, 6 } };
            Paste(target, "2", typeof(List<double>), typeof(List<double>), x => x[nameof(MyClass.DoubleList)].Target, new NodeIndex(1), false);
            Assert.Equal(4, target.DoubleList.Count);
            Assert.Equal(4.0, target.DoubleList[0]);
            Assert.Equal(2.0, target.DoubleList[1]);
            Assert.Equal(5.0, target.DoubleList[2]);
            Assert.Equal(6.0, target.DoubleList[3]);
        }

        [Fact]
        public void TestCopyReplaceDoubleIntoList()
        {
            var source = new MyClass { DoubleList = new List<double> { 1, 2, 3 } };
            var target = new MyClass { DoubleList = new List<double> { 4, 5, 6 } };
            var copiedText = Copy(source, source.DoubleList[2]);
            Paste(target, copiedText, typeof(List<double>), typeof(List<double>), x => x[nameof(MyClass.DoubleList)].Target, new NodeIndex(1), true);
            Assert.Equal(3, target.DoubleList.Count);
            Assert.Equal(4.0, target.DoubleList[0]);
            Assert.Equal(3.0, target.DoubleList[1]);
            Assert.Equal(6.0, target.DoubleList[2]);

            target = new MyClass { DoubleList = new List<double> { 4, 5, 6 } };
            Paste(target, "2", typeof(List<double>), typeof(List<double>), x => x[nameof(MyClass.DoubleList)].Target, new NodeIndex(1), true);
            Assert.Equal(3, target.DoubleList.Count);
            Assert.Equal(4.0, target.DoubleList[0]);
            Assert.Equal(2.0, target.DoubleList[1]);
            Assert.Equal(6.0, target.DoubleList[2]);
        }

        [Fact]
        public void TestCopyPasteStructIntoList()
        {
            var source = new MyClass { StructList = new List<MyStruct> { new MyStruct { Integer = 1 }, new MyStruct { Integer = 2 }, new MyStruct { Integer = 3 } } };
            var target = new MyClass { StructList = new List<MyStruct> { new MyStruct { Integer = 4 }, new MyStruct { Integer = 5 }, new MyStruct { Integer = 6 } } };
            var copiedText = Copy(source, source.StructList[2]);
            Paste(target, copiedText, typeof(List<MyStruct>), typeof(List<MyStruct>), x => x[nameof(MyClass.StructList)].Target, new NodeIndex(1), false);
            Assert.Equal(4, target.StructList.Count);
            Assert.Equal(4.0, target.StructList[0].Integer);
            Assert.Equal(3.0, target.StructList[1].Integer);
            Assert.Equal(5.0, target.StructList[2].Integer);
            Assert.Equal(6.0, target.StructList[3].Integer);
        }

        [Fact]
        public void TestCopyReplaceStructIntoList()
        {
            var source = new MyClass { StructList = new List<MyStruct> { new MyStruct { Integer = 1 }, new MyStruct { Integer = 2 }, new MyStruct { Integer = 3 } } };
            var target = new MyClass { StructList = new List<MyStruct> { new MyStruct { Integer = 4 }, new MyStruct { Integer = 5 }, new MyStruct { Integer = 6 } } };
            var copiedText = Copy(source, source.StructList[2]);
            Paste(target, copiedText, typeof(List<MyStruct>), typeof(List<MyStruct>), x => x[nameof(MyClass.StructList)].Target, new NodeIndex(1), true);
            Assert.Equal(3, target.StructList.Count);
            Assert.Equal(4.0, target.StructList[0].Integer);
            Assert.Equal(3.0, target.StructList[1].Integer);
            Assert.Equal(6.0, target.StructList[2].Integer);
        }

        [Fact]
        public void TestCopyPasteClassIntoList()
        {
            var source = new MyClass { SubList = new List<MyClass> { new MyClass { Float = 1 }, new MyClass { Float = 2 }, new MyClass { Float = 3 } } };
            var target = new MyClass { SubList = new List<MyClass> { new MyClass { Float = 4 }, new MyClass { Float = 5 }, new MyClass { Float = 6 } } };
            var copiedText = Copy(source, source.SubList[2]);
            Paste(target, copiedText, typeof(List<MyClass>), typeof(List<MyClass>), x => x[nameof(MyClass.SubList)].Target, new NodeIndex(1), false);
            Assert.Equal(4, target.SubList.Count);
            Assert.Equal(4.0, target.SubList[0].Float);
            Assert.Equal(3.0, target.SubList[1].Float);
            Assert.Equal(5.0, target.SubList[2].Float);
            Assert.Equal(6.0, target.SubList[3].Float);
        }

        [Fact]
        public void TestCopyReplaceClassIntoList()
        {
            var source = new MyClass { SubList = new List<MyClass> { new MyClass { Float = 1 }, new MyClass { Float = 2 }, new MyClass { Float = 3 } } };
            var target = new MyClass { SubList = new List<MyClass> { new MyClass { Float = 4 }, new MyClass { Float = 5 }, new MyClass { Float = 6 } } };
            var copiedText = Copy(source, source.SubList[2]);
            Paste(target, copiedText, typeof(List<MyClass>), typeof(List<MyClass>), x => x[nameof(MyClass.SubList)].Target, new NodeIndex(1), true);
            Assert.Equal(3, target.SubList.Count);
            Assert.Equal(4.0, target.SubList[0].Float);
            Assert.Equal(3.0, target.SubList[1].Float);
            Assert.Equal(6.0, target.SubList[2].Float);
        }

        [Fact]
        public void TestCopyPasteIntoNullList()
        {
            var source = new MyClass { DoubleList = new List<double> { 1, 2, 3 } };
            var target = new MyClass { DoubleList = null };
            var copiedText = Copy(source, source.DoubleList);
            Paste(target, copiedText, typeof(List<double>), typeof(List<double>), x => x[nameof(MyClass.DoubleList)], NodeIndex.Empty, false);
            Assert.Equal(3, target.DoubleList.Count);
            Assert.Equal(1.0, target.DoubleList[0]);
            Assert.Equal(2.0, target.DoubleList[1]);
            Assert.Equal(3.0, target.DoubleList[2]);

            target = new MyClass { DoubleList = null };
            Paste(target, copiedText, typeof(List<double>), typeof(List<double>), x => x[nameof(MyClass.DoubleList)], NodeIndex.Empty, true);
            Assert.Equal(3, target.DoubleList.Count);
            Assert.Equal(1.0, target.DoubleList[0]);
            Assert.Equal(2.0, target.DoubleList[1]);
            Assert.Equal(3.0, target.DoubleList[2]);

            source = new MyClass { DoubleList = new List<double> { 1, 2, 3 } };
            target = new MyClass { DoubleList = null };
            copiedText = Copy(source, source.DoubleList[2]);
            Paste(target, copiedText, typeof(List<double>), typeof(List<double>), x => x[nameof(MyClass.DoubleList)], NodeIndex.Empty, false);
            Assert.Single(target.DoubleList);
            Assert.Equal(3.0, target.DoubleList[0]);

            target = new MyClass { DoubleList = null };
            Paste(target, copiedText, typeof(List<double>), typeof(List<double>), x => x[nameof(MyClass.DoubleList)], NodeIndex.Empty, true);
            Assert.Single(target.DoubleList);
            Assert.Equal(3.0, target.DoubleList[0]);

            target = new MyClass { DoubleList = null };
            Paste(target, "2", typeof(List<double>), typeof(List<double>), x => x[nameof(MyClass.DoubleList)], NodeIndex.Empty, false);
            Assert.Single(target.DoubleList);
            Assert.Equal(2.0, target.DoubleList[0]);

            target = new MyClass { DoubleList = null };
            Paste(target, "2", typeof(List<double>), typeof(List<double>), x => x[nameof(MyClass.DoubleList)], NodeIndex.Empty, true);
            Assert.Single(target.DoubleList);
            Assert.Equal(2.0, target.DoubleList[0]);
        }

        [Fact]
        public void TestCopyPasteDictionaryDouble()
        {
            var source = new MyClass { DoubleDictionary = new Dictionary<string, double> { { "aaa", 1 }, { "bbb", 2 }, { "ccc", 3 } } };
            var target = new MyClass { DoubleDictionary = new Dictionary<string, double> { { "ddd", 4 }, { "eee", 5 }, { "fff", 6 } } };
            var copiedText = Copy(source, source.DoubleDictionary);
            Paste(target, copiedText, typeof(Dictionary<string, double>), typeof(Dictionary<string, double>), x => x[nameof(MyClass.DoubleDictionary)], NodeIndex.Empty, false);
            Assert.Equal(6, target.DoubleDictionary.Count);
            Assert.Equal(1.0, target.DoubleDictionary["aaa"]);
            Assert.Equal(2.0, target.DoubleDictionary["bbb"]);
            Assert.Equal(3.0, target.DoubleDictionary["ccc"]);
            Assert.Equal(4.0, target.DoubleDictionary["ddd"]);
            Assert.Equal(5.0, target.DoubleDictionary["eee"]);
            Assert.Equal(6.0, target.DoubleDictionary["fff"]);
        }

        [Fact]
        public void TestCopyReplaceDictionaryDouble()
        {
            var source = new MyClass { DoubleDictionary = new Dictionary<string, double> { { "aaa", 1 }, { "bbb", 2 }, { "ccc", 3 } } };
            var target = new MyClass { DoubleDictionary = new Dictionary<string, double> { { "ddd", 4 }, { "eee", 5 }, { "fff", 6 } } };
            var copiedText = Copy(source, source.DoubleDictionary);
            Paste(target, copiedText, typeof(Dictionary<string, double>), typeof(Dictionary<string, double>), x => x[nameof(MyClass.DoubleDictionary)], NodeIndex.Empty, true);
            Assert.Equal(3, target.DoubleDictionary.Count);
            Assert.Equal(1.0, target.DoubleDictionary["aaa"]);
            Assert.Equal(2.0, target.DoubleDictionary["bbb"]);
            Assert.Equal(3.0, target.DoubleDictionary["ccc"]);
        }

        [Fact]
        public void TestCopyPasteDictionaryStruct()
        {
            var source = new MyClass { StructDictionary = new Dictionary<string, MyStruct> { { "aaa", new MyStruct { Integer = 1 } }, { "bbb", new MyStruct { Integer = 2 } }, { "ccc", new MyStruct { Integer = 3 } } } };
            var target = new MyClass { StructDictionary = new Dictionary<string, MyStruct> { { "ddd", new MyStruct { Integer = 4 } }, { "eee", new MyStruct { Integer = 5 } }, { "fff", new MyStruct { Integer = 6 } } } };
            var copiedText = Copy(source, source.StructDictionary);
            Paste(target, copiedText, typeof(Dictionary<string, MyStruct>), typeof(Dictionary<string, MyStruct>), x => x[nameof(MyClass.StructDictionary)], NodeIndex.Empty, false);
            Assert.Equal(6, target.StructDictionary.Count);
            Assert.Equal(1.0, target.StructDictionary["aaa"].Integer);
            Assert.Equal(2.0, target.StructDictionary["bbb"].Integer);
            Assert.Equal(3.0, target.StructDictionary["ccc"].Integer);
            Assert.Equal(4.0, target.StructDictionary["ddd"].Integer);
            Assert.Equal(5.0, target.StructDictionary["eee"].Integer);
            Assert.Equal(6.0, target.StructDictionary["fff"].Integer);
        }

        [Fact]
        public void TestCopyReplaceDictionaryStruct()
        {
            var source = new MyClass { StructDictionary = new Dictionary<string, MyStruct> { { "aaa", new MyStruct { Integer = 1 } }, { "bbb", new MyStruct { Integer = 2 } }, { "ccc", new MyStruct { Integer = 3 } } } };
            var target = new MyClass { StructDictionary = new Dictionary<string, MyStruct> { { "ddd", new MyStruct { Integer = 4 } }, { "eee", new MyStruct { Integer = 5 } }, { "fff", new MyStruct { Integer = 6 } } } };
            var copiedText = Copy(source, source.StructDictionary);
            Paste(target, copiedText, typeof(Dictionary<string, MyStruct>), typeof(Dictionary<string, MyStruct>), x => x[nameof(MyClass.StructDictionary)], NodeIndex.Empty, true);
            Assert.Equal(3, target.StructDictionary.Count);
            Assert.Equal(1.0, target.StructDictionary["aaa"].Integer);
            Assert.Equal(2.0, target.StructDictionary["bbb"].Integer);
            Assert.Equal(3.0, target.StructDictionary["ccc"].Integer);
        }

        [Fact]
        public void TestCopyPasteDictionaryClass()
        {
            var source = new MyClass { SubDictionary = new Dictionary<string, MyClass> { { "aaa", new MyClass { Float = 1 } }, { "bbb", new MyClass { Float = 2 } }, { "ccc", new MyClass { Float = 3 } } } };
            var target = new MyClass { SubDictionary = new Dictionary<string, MyClass> { { "ddd", new MyClass { Float = 4 } }, { "eee", new MyClass { Float = 5 } }, { "fff", new MyClass { Float = 6 } } } };
            var copiedText = Copy(source, source.SubDictionary);
            Paste(target, copiedText, typeof(Dictionary<string, MyClass>), typeof(Dictionary<string, MyClass>), x => x[nameof(MyClass.SubDictionary)], NodeIndex.Empty, false);
            Assert.Equal(6, target.SubDictionary.Count);
            Assert.Equal(1.0, target.SubDictionary["aaa"].Float);
            Assert.Equal(2.0, target.SubDictionary["bbb"].Float);
            Assert.Equal(3.0, target.SubDictionary["ccc"].Float);
            Assert.Equal(4.0, target.SubDictionary["ddd"].Float);
            Assert.Equal(5.0, target.SubDictionary["eee"].Float);
            Assert.Equal(6.0, target.SubDictionary["fff"].Float);
        }

        [Fact]
        public void TestCopyReplaceDictionaryClass()
        {
            var source = new MyClass { SubDictionary = new Dictionary<string, MyClass> { { "aaa", new MyClass { Float = 1 } }, { "bbb", new MyClass { Float = 2 } }, { "ccc", new MyClass { Float = 3 } } } };
            var target = new MyClass { SubDictionary = new Dictionary<string, MyClass> { { "ddd", new MyClass { Float = 4 } }, { "eee", new MyClass { Float = 5 } }, { "fff", new MyClass { Float = 6 } } } };
            var copiedText = Copy(source, source.SubDictionary);
            Paste(target, copiedText, typeof(Dictionary<string, MyClass>), typeof(Dictionary<string, MyClass>), x => x[nameof(MyClass.SubDictionary)], NodeIndex.Empty, true);
            Assert.Equal(3, target.SubDictionary.Count);
            Assert.Equal(1.0, target.SubDictionary["aaa"].Float);
            Assert.Equal(2.0, target.SubDictionary["bbb"].Float);
            Assert.Equal(3.0, target.SubDictionary["ccc"].Float);
        }

        [Fact]
        public void TestCopyPasteDictionaryDoubleIntoItem()
        {
            var source = new MyClass { DoubleDictionary = new Dictionary<string, double> { { "aaa", 1 }, { "bbb", 2 }, { "ccc", 3 } } };
            var target = new MyClass { DoubleDictionary = new Dictionary<string, double> { { "ddd", 4 }, { "eee", 5 }, { "fff", 6 } } };
            var copiedText = Copy(source, source.DoubleDictionary);
            Paste(target, copiedText, typeof(Dictionary<string, double>), typeof(Dictionary<string, double>), x => x[nameof(MyClass.DoubleDictionary)].Target, new NodeIndex("eee"), false);
            Assert.Equal(6, target.DoubleDictionary.Count);
            Assert.Equal(1.0, target.DoubleDictionary["aaa"]);
            Assert.Equal(2.0, target.DoubleDictionary["bbb"]);
            Assert.Equal(3.0, target.DoubleDictionary["ccc"]);
            Assert.Equal(4.0, target.DoubleDictionary["ddd"]);
            Assert.Equal(5.0, target.DoubleDictionary["eee"]);
            Assert.Equal(6.0, target.DoubleDictionary["fff"]);
        }

        [Fact]
        public void TestCopyReplaceDictionaryDoubleIntoItem()
        {
            var source = new MyClass { DoubleDictionary = new Dictionary<string, double> { { "aaa", 1 }, { "bbb", 2 }, { "ccc", 3 } } };
            var target = new MyClass { DoubleDictionary = new Dictionary<string, double> { { "ddd", 4 }, { "eee", 5 }, { "fff", 6 } } };
            var copiedText = Copy(source, source.DoubleDictionary);
            Paste(target, copiedText, typeof(Dictionary<string, double>), typeof(Dictionary<string, double>), x => x[nameof(MyClass.DoubleDictionary)].Target, new NodeIndex("eee"), true);
            Assert.Equal(5, target.DoubleDictionary.Count);
            Assert.Equal(4.0, target.DoubleDictionary["ddd"]);
            Assert.Equal(1.0, target.DoubleDictionary["aaa"]);
            Assert.Equal(2.0, target.DoubleDictionary["bbb"]);
            Assert.Equal(3.0, target.DoubleDictionary["ccc"]);
            Assert.Equal(6.0, target.DoubleDictionary["fff"]);
        }

        [Fact]
        public void TestCopyPasteDictionaryStructIntoItem()
        {
            var source = new MyClass { StructDictionary = new Dictionary<string, MyStruct> { { "aaa", new MyStruct { Integer = 1 } }, { "bbb", new MyStruct { Integer = 2 } }, { "ccc", new MyStruct { Integer = 3 } } } };
            var target = new MyClass { StructDictionary = new Dictionary<string, MyStruct> { { "ddd", new MyStruct { Integer = 4 } }, { "eee", new MyStruct { Integer = 5 } }, { "fff", new MyStruct { Integer = 6 } } } };
            var copiedText = Copy(source, source.StructDictionary);
            Paste(target, copiedText, typeof(Dictionary<string, MyStruct>), typeof(Dictionary<string, MyStruct>), x => x[nameof(MyClass.StructDictionary)].Target, new NodeIndex("eee"), false);
            Assert.Equal(6, target.StructDictionary.Count);
            Assert.Equal(1.0, target.StructDictionary["aaa"].Integer);
            Assert.Equal(2.0, target.StructDictionary["bbb"].Integer);
            Assert.Equal(3.0, target.StructDictionary["ccc"].Integer);
            Assert.Equal(4.0, target.StructDictionary["ddd"].Integer);
            Assert.Equal(5.0, target.StructDictionary["eee"].Integer);
            Assert.Equal(6.0, target.StructDictionary["fff"].Integer);
        }

        [Fact]
        public void TestCopyReplaceDictionaryStructIntoItem()
        {
            var source = new MyClass { StructDictionary = new Dictionary<string, MyStruct> { { "aaa", new MyStruct { Integer = 1 } }, { "bbb", new MyStruct { Integer = 2 } }, { "ccc", new MyStruct { Integer = 3 } } } };
            var target = new MyClass { StructDictionary = new Dictionary<string, MyStruct> { { "ddd", new MyStruct { Integer = 4 } }, { "eee", new MyStruct { Integer = 5 } }, { "fff", new MyStruct { Integer = 6 } } } };
            var copiedText = Copy(source, source.StructDictionary);
            Paste(target, copiedText, typeof(Dictionary<string, MyStruct>), typeof(Dictionary<string, MyStruct>), x => x[nameof(MyClass.StructDictionary)].Target, new NodeIndex("eee"), true);
            Assert.Equal(5, target.StructDictionary.Count);
            Assert.Equal(4.0, target.StructDictionary["ddd"].Integer);
            Assert.Equal(1.0, target.StructDictionary["aaa"].Integer);
            Assert.Equal(2.0, target.StructDictionary["bbb"].Integer);
            Assert.Equal(3.0, target.StructDictionary["ccc"].Integer);
            Assert.Equal(6.0, target.StructDictionary["fff"].Integer);
        }

        [Fact]
        public void TestCopyPasteDictionaryClassIntoItem()
        {
            var source = new MyClass { SubDictionary = new Dictionary<string, MyClass> { { "aaa", new MyClass { Float = 1 } }, { "bbb", new MyClass { Float = 2 } }, { "ccc", new MyClass { Float = 3 } } } };
            var target = new MyClass { SubDictionary = new Dictionary<string, MyClass> { { "ddd", new MyClass { Float = 4 } }, { "eee", new MyClass { Float = 5 } }, { "fff", new MyClass { Float = 6 } } } };
            var copiedText = Copy(source, source.SubDictionary);
            Paste(target, copiedText, typeof(Dictionary<string, MyClass>), typeof(Dictionary<string, MyClass>), x => x[nameof(MyClass.SubDictionary)].Target, new NodeIndex("eee"), false);
            Assert.Equal(6, target.SubDictionary.Count);
            Assert.Equal(1.0, target.SubDictionary["aaa"].Float);
            Assert.Equal(2.0, target.SubDictionary["bbb"].Float);
            Assert.Equal(3.0, target.SubDictionary["ccc"].Float);
            Assert.Equal(4.0, target.SubDictionary["ddd"].Float);
            Assert.Equal(5.0, target.SubDictionary["eee"].Float);
            Assert.Equal(6.0, target.SubDictionary["fff"].Float);
        }

        [Fact]
        public void TestCopyReplaceDictionaryClassIntoItem()
        {
            var source = new MyClass { SubDictionary = new Dictionary<string, MyClass> { { "aaa", new MyClass { Float = 1 } }, { "bbb", new MyClass { Float = 2 } }, { "ccc", new MyClass { Float = 3 } } } };
            var target = new MyClass { SubDictionary = new Dictionary<string, MyClass> { { "ddd", new MyClass { Float = 4 } }, { "eee", new MyClass { Float = 5 } }, { "fff", new MyClass { Float = 6 } } } };
            var copiedText = Copy(source, source.SubDictionary);
            Paste(target, copiedText, typeof(Dictionary<string, MyClass>), typeof(Dictionary<string, MyClass>), x => x[nameof(MyClass.SubDictionary)].Target, new NodeIndex("eee"), true);
            Assert.Equal(5, target.SubDictionary.Count);
            Assert.Equal(4.0, target.SubDictionary["ddd"].Float);
            Assert.Equal(1.0, target.SubDictionary["aaa"].Float);
            Assert.Equal(2.0, target.SubDictionary["bbb"].Float);
            Assert.Equal(3.0, target.SubDictionary["ccc"].Float);
            Assert.Equal(6.0, target.SubDictionary["fff"].Float);
        }

        [Fact]
        public void TestCopyPasteDoubleIntoDictionary()
        {
            var source = new MyClass { DoubleDictionary = new Dictionary<string, double> { { "aaa", 1 }, { "bbb", 2 }, { "ccc", 3 } } };
            var target = new MyClass { DoubleDictionary = new Dictionary<string, double> { { "ddd", 4 }, { "eee", 5 }, { "fff", 6 } } };
            var copiedText = Copy(source, source.DoubleDictionary.Single(x => x.Key == "ccc"));
            Paste(target, copiedText, typeof(Dictionary<string, double>), typeof(Dictionary<string, double>), x => x[nameof(MyClass.DoubleDictionary)].Target, new NodeIndex("eee"), false);
            Assert.Equal(4, target.DoubleDictionary.Count);
            Assert.Equal(3.0, target.DoubleDictionary["ccc"]);
            Assert.Equal(4.0, target.DoubleDictionary["ddd"]);
            Assert.Equal(5.0, target.DoubleDictionary["eee"]);
            Assert.Equal(6.0, target.DoubleDictionary["fff"]);
        }

        [Fact]
        public void TestCopyReplaceDoubleIntoDictionary()
        {
            var source = new MyClass { DoubleDictionary = new Dictionary<string, double> { { "aaa", 1 }, { "bbb", 2 }, { "ccc", 3 } } };
            var target = new MyClass { DoubleDictionary = new Dictionary<string, double> { { "ddd", 4 }, { "eee", 5 }, { "fff", 6 } } };
            var copiedText = Copy(source, source.DoubleDictionary.Single(x => x.Key == "ccc"));
            Paste(target, copiedText, typeof(Dictionary<string, double>), typeof(Dictionary<string, double>), x => x[nameof(MyClass.DoubleDictionary)].Target, new NodeIndex("eee"), true);
            Assert.Equal(3, target.DoubleDictionary.Count);
            Assert.Equal(4.0, target.DoubleDictionary["ddd"]);
            Assert.Equal(3.0, target.DoubleDictionary["ccc"]);
            Assert.Equal(6.0, target.DoubleDictionary["fff"]);
        }

        [Fact]
        public void TestCopyPasteStructIntoDictionary()
        {
            var source = new MyClass { StructDictionary = new Dictionary<string, MyStruct> { { "aaa", new MyStruct { Integer = 1 } }, { "bbb", new MyStruct { Integer = 2 } }, { "ccc", new MyStruct { Integer = 3 } } } };
            var target = new MyClass { StructDictionary = new Dictionary<string, MyStruct> { { "ddd", new MyStruct { Integer = 4 } }, { "eee", new MyStruct { Integer = 5 } }, { "fff", new MyStruct { Integer = 6 } } } };
            var copiedText = Copy(source, source.StructDictionary.Single(x => x.Key == "ccc"));
            Paste(target, copiedText, typeof(Dictionary<string, MyStruct>), typeof(Dictionary<string, MyStruct>), x => x[nameof(MyClass.StructDictionary)].Target, new NodeIndex("eee"), false);
            Assert.Equal(4, target.StructDictionary.Count);
            Assert.Equal(3.0, target.StructDictionary["ccc"].Integer);
            Assert.Equal(4.0, target.StructDictionary["ddd"].Integer);
            Assert.Equal(5.0, target.StructDictionary["eee"].Integer);
            Assert.Equal(6.0, target.StructDictionary["fff"].Integer);
        }

        [Fact]
        public void TestCopyReplaceStructIntoDictionary()
        {
            var source = new MyClass { StructDictionary = new Dictionary<string, MyStruct> { { "aaa", new MyStruct { Integer = 1 } }, { "bbb", new MyStruct { Integer = 2 } }, { "ccc", new MyStruct { Integer = 3 } } } };
            var target = new MyClass { StructDictionary = new Dictionary<string, MyStruct> { { "ddd", new MyStruct { Integer = 4 } }, { "eee", new MyStruct { Integer = 5 } }, { "fff", new MyStruct { Integer = 6 } } } };
            var copiedText = Copy(source, source.StructDictionary.Single(x => x.Key == "ccc"));
            Paste(target, copiedText, typeof(Dictionary<string, MyStruct>), typeof(Dictionary<string, MyStruct>), x => x[nameof(MyClass.StructDictionary)].Target, new NodeIndex("eee"), true);
            Assert.Equal(3, target.StructDictionary.Count);
            Assert.Equal(4.0, target.StructDictionary["ddd"].Integer);
            Assert.Equal(3.0, target.StructDictionary["ccc"].Integer);
            Assert.Equal(6.0, target.StructDictionary["fff"].Integer);
        }

        [Fact]
        public void TestCopyPasteClassIntoDictionary()
        {
            var source = new MyClass { SubDictionary = new Dictionary<string, MyClass> { { "aaa", new MyClass { Float = 1 } }, { "bbb", new MyClass { Float = 2 } }, { "ccc", new MyClass { Float = 3 } } } };
            var target = new MyClass { SubDictionary = new Dictionary<string, MyClass> { { "ddd", new MyClass { Float = 4 } }, { "eee", new MyClass { Float = 5 } }, { "fff", new MyClass { Float = 6 } } } };
            var copiedText = Copy(source, source.SubDictionary.Single(x => x.Key == "ccc"));
            Paste(target, copiedText, typeof(Dictionary<string, MyClass>), typeof(Dictionary<string, MyClass>), x => x[nameof(MyClass.SubDictionary)].Target, new NodeIndex("eee"), false);
            Assert.Equal(4, target.SubDictionary.Count);
            Assert.Equal(3.0, target.SubDictionary["ccc"].Float);
            Assert.Equal(4.0, target.SubDictionary["ddd"].Float);
            Assert.Equal(5.0, target.SubDictionary["eee"].Float);
            Assert.Equal(6.0, target.SubDictionary["fff"].Float);
        }

        [Fact]
        public void TestCopyReplaceClassIntoDictionary()
        {
            var source = new MyClass { SubDictionary = new Dictionary<string, MyClass> { { "aaa", new MyClass { Float = 1 } }, { "bbb", new MyClass { Float = 2 } }, { "ccc", new MyClass { Float = 3 } } } };
            var target = new MyClass { SubDictionary = new Dictionary<string, MyClass> { { "ddd", new MyClass { Float = 4 } }, { "eee", new MyClass { Float = 5 } }, { "fff", new MyClass { Float = 6 } } } };
            var copiedText = Copy(source, source.SubDictionary.Single(x => x.Key == "ccc"));
            Paste(target, copiedText, typeof(Dictionary<string, MyClass>), typeof(Dictionary<string, MyClass>), x => x[nameof(MyClass.SubDictionary)].Target, new NodeIndex("eee"), true);
            Assert.Equal(3, target.SubDictionary.Count);
            Assert.Equal(4.0, target.SubDictionary["ddd"].Float);
            Assert.Equal(3.0, target.SubDictionary["ccc"].Float);
            Assert.Equal(6.0, target.SubDictionary["fff"].Float);
        }

        [Fact]
        public void TestCopyPasteDictionaryDoubleWithCollision()
        {
            var source = new MyClass { DoubleDictionary = new Dictionary<string, double> { { "aaa", 1 }, { "bbb", 2 }, { "ccc", 3 } } };
            var target = new MyClass { DoubleDictionary = new Dictionary<string, double> { { "ccc", 4 }, { "eee", 5 }, { "fff", 6 } } };
            var copiedText = Copy(source, source.DoubleDictionary);
            Paste(target, copiedText, typeof(Dictionary<string, double>), typeof(Dictionary<string, double>), x => x[nameof(MyClass.DoubleDictionary)], NodeIndex.Empty, false);
            Assert.Equal(5, target.DoubleDictionary.Count);
            Assert.Equal(1.0, target.DoubleDictionary["aaa"]);
            Assert.Equal(2.0, target.DoubleDictionary["bbb"]);
            Assert.Equal(3.0, target.DoubleDictionary["ccc"]);
            Assert.Equal(5.0, target.DoubleDictionary["eee"]);
            Assert.Equal(6.0, target.DoubleDictionary["fff"]);
        }

        [Fact]
        public void TestCopyPasteDictionaryStructWithCollision()
        {
            var source = new MyClass { StructDictionary = new Dictionary<string, MyStruct> { { "aaa", new MyStruct { Integer = 1 } }, { "bbb", new MyStruct { Integer = 2 } }, { "ccc", new MyStruct { Integer = 3 } } } };
            var target = new MyClass { StructDictionary = new Dictionary<string, MyStruct> { { "ccc", new MyStruct { Integer = 4 } }, { "eee", new MyStruct { Integer = 5 } }, { "fff", new MyStruct { Integer = 6 } } } };
            var copiedText = Copy(source, source.StructDictionary);
            Paste(target, copiedText, typeof(Dictionary<string, MyStruct>), typeof(Dictionary<string, MyStruct>), x => x[nameof(MyClass.StructDictionary)], NodeIndex.Empty, false);
            Assert.Equal(5, target.StructDictionary.Count);
            Assert.Equal(1.0, target.StructDictionary["aaa"].Integer);
            Assert.Equal(2.0, target.StructDictionary["bbb"].Integer);
            Assert.Equal(3.0, target.StructDictionary["ccc"].Integer);
            Assert.Equal(5.0, target.StructDictionary["eee"].Integer);
            Assert.Equal(6.0, target.StructDictionary["fff"].Integer);
        }

        [Fact]
        public void TestCopyPasteDictionaryClassWithCollision()
        {
            var source = new MyClass { SubDictionary = new Dictionary<string, MyClass> { { "aaa", new MyClass { Float = 1 } }, { "bbb", new MyClass { Float = 2 } }, { "ccc", new MyClass { Float = 3 } } } };
            var target = new MyClass { SubDictionary = new Dictionary<string, MyClass> { { "ccc", new MyClass { Float = 4 } }, { "eee", new MyClass { Float = 5 } }, { "fff", new MyClass { Float = 6 } } } };
            var copiedText = Copy(source, source.SubDictionary);
            Paste(target, copiedText, typeof(Dictionary<string, MyClass>), typeof(Dictionary<string, MyClass>), x => x[nameof(MyClass.SubDictionary)], NodeIndex.Empty, false);
            Assert.Equal(5, target.SubDictionary.Count);
            Assert.Equal(1.0, target.SubDictionary["aaa"].Float);
            Assert.Equal(2.0, target.SubDictionary["bbb"].Float);
            Assert.Equal(3.0, target.SubDictionary["ccc"].Float);
            Assert.Equal(5.0, target.SubDictionary["eee"].Float);
            Assert.Equal(6.0, target.SubDictionary["fff"].Float);
        }

        [Fact]
        public void TestCopyPasteDictionaryDoubleIntoItemWithCollision()
        {
            var source = new MyClass { DoubleDictionary = new Dictionary<string, double> { { "aaa", 1 }, { "bbb", 2 }, { "ccc", 3 } } };
            var target = new MyClass { DoubleDictionary = new Dictionary<string, double> { { "ccc", 4 }, { "eee", 5 }, { "fff", 6 } } };
            var copiedText = Copy(source, source.DoubleDictionary);
            Paste(target, copiedText, typeof(Dictionary<string, double>), typeof(Dictionary<string, double>), x => x[nameof(MyClass.DoubleDictionary)].Target, new NodeIndex("eee"), false);
            Assert.Equal(5, target.DoubleDictionary.Count);
            Assert.Equal(1.0, target.DoubleDictionary["aaa"]);
            Assert.Equal(2.0, target.DoubleDictionary["bbb"]);
            Assert.Equal(3.0, target.DoubleDictionary["ccc"]);
            Assert.Equal(5.0, target.DoubleDictionary["eee"]);
            Assert.Equal(6.0, target.DoubleDictionary["fff"]);
        }

        [Fact]
        public void TestCopyPasteDictionaryStructIntoItemWithCollision()
        {
            var source = new MyClass { StructDictionary = new Dictionary<string, MyStruct> { { "aaa", new MyStruct { Integer = 1 } }, { "bbb", new MyStruct { Integer = 2 } }, { "ccc", new MyStruct { Integer = 3 } } } };
            var target = new MyClass { StructDictionary = new Dictionary<string, MyStruct> { { "ccc", new MyStruct { Integer = 4 } }, { "eee", new MyStruct { Integer = 5 } }, { "fff", new MyStruct { Integer = 6 } } } };
            var copiedText = Copy(source, source.StructDictionary);
            Paste(target, copiedText, typeof(Dictionary<string, MyStruct>), typeof(Dictionary<string, MyStruct>), x => x[nameof(MyClass.StructDictionary)].Target, new NodeIndex("eee"), false);
            Assert.Equal(5, target.StructDictionary.Count);
            Assert.Equal(1.0, target.StructDictionary["aaa"].Integer);
            Assert.Equal(2.0, target.StructDictionary["bbb"].Integer);
            Assert.Equal(3.0, target.StructDictionary["ccc"].Integer);
            Assert.Equal(5.0, target.StructDictionary["eee"].Integer);
            Assert.Equal(6.0, target.StructDictionary["fff"].Integer);
        }

        [Fact]
        public void TestCopyPasteDictionaryClassIntoItemWithCollision()
        {
            var source = new MyClass { SubDictionary = new Dictionary<string, MyClass> { { "aaa", new MyClass { Float = 1 } }, { "bbb", new MyClass { Float = 2 } }, { "ccc", new MyClass { Float = 3 } } } };
            var target = new MyClass { SubDictionary = new Dictionary<string, MyClass> { { "ccc", new MyClass { Float = 4 } }, { "eee", new MyClass { Float = 5 } }, { "fff", new MyClass { Float = 6 } } } };
            var copiedText = Copy(source, source.SubDictionary);
            Paste(target, copiedText, typeof(Dictionary<string, MyClass>), typeof(Dictionary<string, MyClass>), x => x[nameof(MyClass.SubDictionary)].Target, new NodeIndex("eee"), false);
            Assert.Equal(5, target.SubDictionary.Count);
            Assert.Equal(1.0, target.SubDictionary["aaa"].Float);
            Assert.Equal(2.0, target.SubDictionary["bbb"].Float);
            Assert.Equal(3.0, target.SubDictionary["ccc"].Float);
            Assert.Equal(5.0, target.SubDictionary["eee"].Float);
            Assert.Equal(6.0, target.SubDictionary["fff"].Float);
        }

        [Fact]
        public void TestCopyPasteDoubleIntoDictionaryWithCollision()
        {
            var source = new MyClass { DoubleDictionary = new Dictionary<string, double> { { "aaa", 1 }, { "bbb", 2 }, { "ccc", 3 } } };
            var target = new MyClass { DoubleDictionary = new Dictionary<string, double> { { "ccc", 4 }, { "eee", 5 }, { "fff", 6 } } };
            var copiedText = Copy(source, source.DoubleDictionary.Single(x => x.Key == "ccc"));
            Paste(target, copiedText, typeof(Dictionary<string, double>), typeof(Dictionary<string, double>), x => x[nameof(MyClass.DoubleDictionary)].Target, new NodeIndex("eee"), false);
            Assert.Equal(3, target.DoubleDictionary.Count);
            Assert.Equal(3.0, target.DoubleDictionary["ccc"]);
            Assert.Equal(5.0, target.DoubleDictionary["eee"]);
            Assert.Equal(6.0, target.DoubleDictionary["fff"]);
        }

        [Fact]
        public void TestCopyPasteStructIntoDictionaryWithCollision()
        {
            var source = new MyClass { StructDictionary = new Dictionary<string, MyStruct> { { "aaa", new MyStruct { Integer = 1 } }, { "bbb", new MyStruct { Integer = 2 } }, { "ccc", new MyStruct { Integer = 3 } } } };
            var target = new MyClass { StructDictionary = new Dictionary<string, MyStruct> { { "ccc", new MyStruct { Integer = 4 } }, { "eee", new MyStruct { Integer = 5 } }, { "fff", new MyStruct { Integer = 6 } } } };
            var copiedText = Copy(source, source.StructDictionary.Single(x => x.Key == "ccc"));
            Paste(target, copiedText, typeof(Dictionary<string, MyStruct>), typeof(Dictionary<string, MyStruct>), x => x[nameof(MyClass.StructDictionary)].Target, new NodeIndex("eee"), false);
            Assert.Equal(3, target.StructDictionary.Count);
            Assert.Equal(3.0, target.StructDictionary["ccc"].Integer);
            Assert.Equal(5.0, target.StructDictionary["eee"].Integer);
            Assert.Equal(6.0, target.StructDictionary["fff"].Integer);
        }

        [Fact]
        public void TestCopyPasteClassIntoDictionaryWithCollision()
        {
            var source = new MyClass { SubDictionary = new Dictionary<string, MyClass> { { "aaa", new MyClass { Float = 1 } }, { "bbb", new MyClass { Float = 2 } }, { "ccc", new MyClass { Float = 3 } } } };
            var target = new MyClass { SubDictionary = new Dictionary<string, MyClass> { { "ccc", new MyClass { Float = 4 } }, { "eee", new MyClass { Float = 5 } }, { "fff", new MyClass { Float = 6 } } } };
            var copiedText = Copy(source, source.SubDictionary.Single(x => x.Key == "ccc"));
            Paste(target, copiedText, typeof(Dictionary<string, MyClass>), typeof(Dictionary<string, MyClass>), x => x[nameof(MyClass.SubDictionary)].Target, new NodeIndex("eee"), false);
            Assert.Equal(3, target.SubDictionary.Count);
            Assert.Equal(3.0, target.SubDictionary["ccc"].Float);
            Assert.Equal(5.0, target.SubDictionary["eee"].Float);
            Assert.Equal(6.0, target.SubDictionary["fff"].Float);
        }

        [Fact]
        public void TestCopyPasteIntoNullDictionary()
        {
            var source = new MyClass { DoubleDictionary = new Dictionary<string, double> { { "aaa", 1 }, { "bbb", 2 }, { "ccc", 3 } } };
            var target = new MyClass { DoubleDictionary = null };
            var copiedText = Copy(source, source.DoubleDictionary);
            Paste(target, copiedText, typeof(Dictionary<string, double>), typeof(Dictionary<string, double>), x => x[nameof(MyClass.DoubleDictionary)], NodeIndex.Empty, false);
            Assert.Equal(3, target.DoubleDictionary.Count);
            Assert.Equal(1.0, target.DoubleDictionary["aaa"]);
            Assert.Equal(2.0, target.DoubleDictionary["bbb"]);
            Assert.Equal(3.0, target.DoubleDictionary["ccc"]);

            target = new MyClass { DoubleDictionary = null };
            Paste(target, copiedText, typeof(Dictionary<string, double>), typeof(Dictionary<string, double>), x => x[nameof(MyClass.DoubleDictionary)], NodeIndex.Empty, true);
            Assert.Equal(3, target.DoubleDictionary.Count);
            Assert.Equal(1.0, target.DoubleDictionary["aaa"]);
            Assert.Equal(2.0, target.DoubleDictionary["bbb"]);
            Assert.Equal(3.0, target.DoubleDictionary["ccc"]);

            source = new MyClass { DoubleDictionary = new Dictionary<string, double> { { "aaa", 1 }, { "bbb", 2 }, { "ccc", 3 } } };
            target = new MyClass { DoubleDictionary = null };
            copiedText = Copy(source, source.DoubleDictionary.Single(x => x.Key == "ccc"));
            Paste(target, copiedText, typeof(Dictionary<string, double>), typeof(Dictionary<string, double>), x => x[nameof(MyClass.DoubleDictionary)], NodeIndex.Empty, false);
            Assert.Single(target.DoubleDictionary);
            Assert.Equal(3.0, target.DoubleDictionary["ccc"]);

            target = new MyClass { DoubleDictionary = null };
            Paste(target, copiedText, typeof(Dictionary<string, double>), typeof(Dictionary<string, double>), x => x[nameof(MyClass.DoubleDictionary)], NodeIndex.Empty, true);
            Assert.Single(target.DoubleDictionary);
            Assert.Equal(3.0, target.DoubleDictionary["ccc"]);
        }

        [DataContract]
        public class MyClass : Asset
        {
            public float Float { get; set; }

            public List<double> DoubleList { get; set; }

            public Dictionary<string, double> DoubleDictionary { get; set; }

            public MyStruct Struct { get; set; }

            public List<MyStruct> StructList { get; set; }

            public Dictionary<string, MyStruct> StructDictionary { get; set; }

            public MyClass Sub { get; set; }

            public List<MyClass> SubList { get; set; }

            public Dictionary<string, MyClass> SubDictionary { get; set; }
        }

        [DataContract]
        public struct MyStruct
        {
            public int Integer { get; set; }

            public MyClass Class { get; set; }
        }

        [NotNull]
        private string Copy([NotNull] Asset asset, object assetValue)
        {
            var propertyGraph = ConstructPropertyGraph(asset);
            var copiedText = service.CopyFromAsset(propertyGraph, propertyGraph.Id, assetValue, false);
            Assert.False(string.IsNullOrEmpty(copiedText));
            return copiedText;
        }

        private void Paste([NotNull] Asset asset, string copiedText, Type deserializedType, [NotNull] Type expectedType, [NotNull] Func<IObjectNode, IGraphNode> targetNodeResolver, NodeIndex index, bool replace)
        {
            var propertyGraph = ConstructPropertyGraph(asset);
            Assert.True(service.CanPaste(copiedText, asset.GetType(), expectedType));

            var result = service.DeserializeCopiedData(copiedText, asset, expectedType);
            Assert.NotNull(result);
            Assert.NotNull(result.Items);
            Assert.Equal(1, result.Items.Count);

            var item = result.Items[0];
            Assert.NotNull(item);
            Assert.NotNull(item.Data);
            Assert.Equal(deserializedType, item.Data.GetType());
            Assert.NotNull(item.Processor);

            var targetNode = targetNodeResolver(propertyGraph.RootNode);
            var nodeAccessor = new NodeAccessor(targetNode, index);
            var propertyContainer = new PropertyContainer { { AssetPropertyPasteProcessor.IsReplaceKey, replace } };
            item.Processor.Paste(item, propertyGraph, ref nodeAccessor, ref propertyContainer);
        }

        private AssetPropertyGraph ConstructPropertyGraph([NotNull] Asset asset)
        {
            var assetItem = new AssetItem("", asset);
            var propertyGraph = AssetQuantumRegistry.ConstructPropertyGraph(propertyGraphContainer, assetItem, null);
            propertyGraphContainer.RegisterGraph(propertyGraph);
            return propertyGraph;
        }
    }
}
