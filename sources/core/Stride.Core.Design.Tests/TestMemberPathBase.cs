// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;

using Stride.Core.Reflection;

namespace Stride.Core.Design.Tests
{
    public class TestMemberPathBase
    {
        protected IStrideMemberDescriptor MemberValue;
        protected IStrideMemberDescriptor MemberSub;
        protected IStrideMemberDescriptor MemberStruct;
        protected IStrideMemberDescriptor MemberSubs;
        protected IStrideMemberDescriptor MemberMaps;
        protected IStrideMemberDescriptor MemberX;
        protected IStrideMemberDescriptor MemberClass;

        protected CollectionDescriptor ListClassDesc;
        protected DictionaryDescriptor MapClassDesc;

        protected TypeDescriptorFactory TypeFactory;

        public struct MyStruct
        {
            public int X { get; set; }

            public MyClass Class { get; set; }
        }

        public class MyClass
        {
            public MyClass()
            {
                Subs = new List<MyClass>();
                Maps = new Dictionary<string, MyClass>();
            }

            public int Value { get; set; }

            public MyClass Sub { get; set; }

            public MyStruct Struct { get; set; }

            public List<MyClass> Subs { get; set; }

            public Dictionary<string, MyClass> Maps { get; set; }
        }

        /// <summary>
        /// Initialize the tests.
        /// </summary>
        public virtual void Initialize()
        {
            TypeFactory = new TypeDescriptorFactory();
            var myClassDesc = TypeFactory.Find(typeof(MyClass));
            var myStructDesc = TypeFactory.Find(typeof(MyStruct));
            ListClassDesc = (CollectionDescriptor)TypeFactory.Find(typeof(List<MyClass>));
            MapClassDesc = (DictionaryDescriptor)TypeFactory.Find(typeof(Dictionary<string, MyClass>));

            MemberValue = (IStrideMemberDescriptor)myClassDesc.Members.FirstOrDefault(member => member.Name == "Value");
            MemberSub = (IStrideMemberDescriptor)myClassDesc.Members.FirstOrDefault(member => member.Name == "Sub");
            MemberStruct = (IStrideMemberDescriptor)myClassDesc.Members.FirstOrDefault(member => member.Name == "Struct");
            MemberSubs = (IStrideMemberDescriptor)myClassDesc.Members.FirstOrDefault(member => member.Name == "Subs");
            MemberMaps = (IStrideMemberDescriptor)myClassDesc.Members.FirstOrDefault(member => member.Name == "Maps");
            MemberX = (IStrideMemberDescriptor)myStructDesc.Members.FirstOrDefault(member => member.Name == "X");
            MemberClass = (IStrideMemberDescriptor)myStructDesc.Members.FirstOrDefault(member => member.Name == "Class");
        }
         
    }
}
