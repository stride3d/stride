// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;

using Xunit;

using Stride.Core.Reflection;

namespace Stride.Core.Design.Tests
{
    /// <summary>
    /// Tests for the <see cref="MemberPath"/> class.
    /// </summary>
    public class TestMemberPath : TestMemberPathBase
    {
        /// <summary>
        /// Initialize the tests.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            ShadowObject.Enable = true;
        }

        [Fact]
        public void TestMyClass()
        {
            Initialize();

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
            Assert.True(memberPath.Apply(testClass, MemberPathAction.ValueSet, 1));
            Assert.True(memberPath.TryGetValue(testClass, out value));
            Assert.Equal(1, value);
            Assert.Equal(1, testClass.Value);
            Assert.True(memberPath.Match(memberPath.Clone()));

            // 2) MyClass.Sub.Value = 1
            memberPath.Clear();
            memberPath.Push(MemberSub);
            memberPath.Push(MemberValue);

            Assert.True(memberPath.Apply(testClass, MemberPathAction.ValueSet, 1));
            Assert.True(memberPath.TryGetValue(testClass, out value));
            Assert.Equal(1, value);
            Assert.Equal(1, testClass.Sub.Value);
            Assert.True(memberPath.Match(memberPath.Clone()));

            // 3) MyClass.Struct.X = 1
            memberPath.Clear();
            memberPath.Push(MemberStruct);
            memberPath.Push(MemberX);

            Assert.True(memberPath.Apply(testClass, MemberPathAction.ValueSet, 1));
            Assert.True(memberPath.TryGetValue(testClass, out value));
            Assert.Equal(1, value);
            Assert.Equal(1, testClass.Struct.X);
            Assert.True(memberPath.Match(memberPath.Clone()));

            // 3) MyClass.Maps["XXX"].Value = 1
            memberPath.Clear();
            memberPath.Push(MemberMaps);
            memberPath.Push(MapClassDesc, "XXX");
            memberPath.Push(MemberValue);

            Assert.True(memberPath.Apply(testClass, MemberPathAction.ValueSet, 1));
            Assert.True(memberPath.TryGetValue(testClass, out value));
            Assert.Equal(1, value);
            Assert.Equal(1, testClass.Maps["XXX"].Value);
            Assert.True(memberPath.Match(memberPath.Clone()));

            // 4) MyClass.Subs[0].Value = 1
            memberPath.Clear();
            memberPath.Push(MemberSubs);
            memberPath.Push(ListClassDesc, 0);
            memberPath.Push(MemberValue);

            Assert.True(memberPath.Apply(testClass, MemberPathAction.ValueSet, 1));
            Assert.True(memberPath.TryGetValue(testClass, out value));
            Assert.Equal(1, value);
            Assert.Equal(1, testClass.Subs[0].Value);
            Assert.True(memberPath.Match(memberPath.Clone()));

            // 5) MyClass.Subs[0].X (invalid)
            memberPath.Clear();
            memberPath.Push(MemberSubs);
            memberPath.Push(ListClassDesc, 0);
            memberPath.Push(MemberX);

            Assert.False(memberPath.TryGetValue(testClass, out value));
            Assert.False(memberPath.Apply(testClass, MemberPathAction.ValueSet, 1));
            Assert.True(memberPath.Match(memberPath.Clone()));

            // 6) Remove key MyClass.Maps.Remove("XXX")
            memberPath.Clear();
            memberPath.Push(MemberMaps);
            memberPath.Push(MapClassDesc, "XXX");
            Assert.True(memberPath.Apply(testClass, MemberPathAction.DictionaryRemove, null));
            Assert.False(testClass.Maps.ContainsKey("XXX"));
            Assert.True(memberPath.Match(memberPath.Clone()));

            // 7) Re-add a value to the dictionary
            Assert.True(memberPath.Apply(testClass, MemberPathAction.ValueSet, new MyClass()));
            Assert.True(testClass.Maps.ContainsKey("XXX"));
            Assert.True(memberPath.Match(memberPath.Clone()));

            // 8) Remove key MyClass.Subs.Remove(0)
            memberPath.Clear();
            memberPath.Push(MemberSubs);
            memberPath.Push(ListClassDesc, 0);
            Assert.True(memberPath.Apply(testClass, MemberPathAction.CollectionRemove, null));
            Assert.Empty(testClass.Subs);
            Assert.True(memberPath.Match(memberPath.Clone()));

            // 9) Add a key MyClass.Subs.Add(new MyClass())
            memberPath.Clear();
            memberPath.Push(MemberSubs);
            memberPath.Push(ListClassDesc, 0);
            Assert.True(memberPath.Apply(testClass, MemberPathAction.CollectionAdd, new MyClass()));
            Assert.Single(testClass.Subs);
            Assert.True(memberPath.Match(memberPath.Clone()));
        }
    }
}
