// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;

using NUnit.Framework;

using Xenko.Core.Reflection;

namespace Xenko.Core.Design.Tests
{
    /// <summary>
    /// Tests for the <see cref="MemberPath"/> class.
    /// </summary>
    [TestFixture]
    public class TestMemberPath : TestMemberPathBase
    {
        /// <summary>
        /// Initialize the tests.
        /// </summary>
        [OneTimeSetUp]
        public override void Initialize()
        {
            base.Initialize();
            ShadowObject.Enable = true;
        }

        [Test]
        public void TestMyClass()
        {
            var testClass = new MyClass
            {
                Sub = new MyClass(),
                Maps = { ["XXX"] = new MyClass() }
            };
            testClass.Subs.Add(new MyClass());

            // 1) MyClass.Value = 1
            var memberPath = new MemberPath();
            memberPath.Push(MemberValue);

            object value;
            Assert.IsTrue(memberPath.Apply(testClass, MemberPathAction.ValueSet, 1));
            Assert.IsTrue(memberPath.TryGetValue(testClass, out value));
            Assert.AreEqual(1, value);
            Assert.AreEqual(1, testClass.Value);
            Assert.IsTrue(memberPath.Match(memberPath.Clone()));

            // 2) MyClass.Sub.Value = 1
            memberPath.Clear();
            memberPath.Push(MemberSub);
            memberPath.Push(MemberValue);

            Assert.IsTrue(memberPath.Apply(testClass, MemberPathAction.ValueSet, 1));
            Assert.IsTrue(memberPath.TryGetValue(testClass, out value));
            Assert.AreEqual(1, value);
            Assert.AreEqual(1, testClass.Sub.Value);
            Assert.IsTrue(memberPath.Match(memberPath.Clone()));

            // 3) MyClass.Struct.X = 1
            memberPath.Clear();
            memberPath.Push(MemberStruct);
            memberPath.Push(MemberX);

            Assert.IsTrue(memberPath.Apply(testClass, MemberPathAction.ValueSet, 1));
            Assert.IsTrue(memberPath.TryGetValue(testClass, out value));
            Assert.AreEqual(1, value);
            Assert.AreEqual(1, testClass.Struct.X);
            Assert.IsTrue(memberPath.Match(memberPath.Clone()));

            // 3) MyClass.Maps["XXX"].Value = 1
            memberPath.Clear();
            memberPath.Push(MemberMaps);
            memberPath.Push(MapClassDesc, "XXX");
            memberPath.Push(MemberValue);

            Assert.IsTrue(memberPath.Apply(testClass, MemberPathAction.ValueSet, 1));
            Assert.IsTrue(memberPath.TryGetValue(testClass, out value));
            Assert.AreEqual(1, value);
            Assert.AreEqual(1, testClass.Maps["XXX"].Value);
            Assert.IsTrue(memberPath.Match(memberPath.Clone()));

            // 4) MyClass.Subs[0].Value = 1
            memberPath.Clear();
            memberPath.Push(MemberSubs);
            memberPath.Push(ListClassDesc, 0);
            memberPath.Push(MemberValue);

            Assert.IsTrue(memberPath.Apply(testClass, MemberPathAction.ValueSet, 1));
            Assert.IsTrue(memberPath.TryGetValue(testClass, out value));
            Assert.AreEqual(1, value);
            Assert.AreEqual(1, testClass.Subs[0].Value);
            Assert.IsTrue(memberPath.Match(memberPath.Clone()));

            // 5) MyClass.Subs[0].X (invalid)
            memberPath.Clear();
            memberPath.Push(MemberSubs);
            memberPath.Push(ListClassDesc, 0);
            memberPath.Push(MemberX);

            Assert.IsFalse(memberPath.TryGetValue(testClass, out value));
            Assert.IsFalse(memberPath.Apply(testClass, MemberPathAction.ValueSet, 1));
            Assert.IsTrue(memberPath.Match(memberPath.Clone()));

            // 6) Remove key MyClass.Maps.Remove("XXX")
            memberPath.Clear();
            memberPath.Push(MemberMaps);
            memberPath.Push(MapClassDesc, "XXX");
            Assert.IsTrue(memberPath.Apply(testClass, MemberPathAction.DictionaryRemove, null));
            Assert.IsFalse(testClass.Maps.ContainsKey("XXX"));
            Assert.IsTrue(memberPath.Match(memberPath.Clone()));

            // 7) Re-add a value to the dictionary
            Assert.IsTrue(memberPath.Apply(testClass, MemberPathAction.ValueSet, new MyClass()));
            Assert.IsTrue(testClass.Maps.ContainsKey("XXX"));
            Assert.IsTrue(memberPath.Match(memberPath.Clone()));

            // 8) Remove key MyClass.Subs.Remove(0)
            memberPath.Clear();
            memberPath.Push(MemberSubs);
            memberPath.Push(ListClassDesc, 0);
            Assert.IsTrue(memberPath.Apply(testClass, MemberPathAction.CollectionRemove, null));
            Assert.AreEqual(0, testClass.Subs.Count);
            Assert.IsTrue(memberPath.Match(memberPath.Clone()));

            // 9) Add a key MyClass.Subs.Add(new MyClass())
            memberPath.Clear();
            memberPath.Push(MemberSubs);
            memberPath.Push(ListClassDesc, 0);
            Assert.IsTrue(memberPath.Apply(testClass, MemberPathAction.CollectionAdd, new MyClass()));
            Assert.AreEqual(1, testClass.Subs.Count);
            Assert.IsTrue(memberPath.Match(memberPath.Clone()));
        }
    }
}
