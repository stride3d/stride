// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;

using Stride.Core.Reflection;

namespace Stride.Core.Design.Tests
{
    public class TestMemberPathBase
    {
        protected IMemberDescriptor MemberValue;
        protected IMemberDescriptor MemberSub;
        protected IMemberDescriptor MemberStruct;
        protected IMemberDescriptor MemberSubs;
        protected IMemberDescriptor MemberMaps;
        protected IMemberDescriptor MemberX;
        protected IMemberDescriptor MemberClass;

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

            MemberValue = (IMemberDescriptor)myClassDesc.Members.FirstOrDefault(member => member.Name == "Value");
            MemberSub = (IMemberDescriptor)myClassDesc.Members.FirstOrDefault(member => member.Name == "Sub");
            MemberStruct = (IMemberDescriptor)myClassDesc.Members.FirstOrDefault(member => member.Name == "Struct");
            MemberSubs = (IMemberDescriptor)myClassDesc.Members.FirstOrDefault(member => member.Name == "Subs");
            MemberMaps = (IMemberDescriptor)myClassDesc.Members.FirstOrDefault(member => member.Name == "Maps");
            MemberX = (IMemberDescriptor)myStructDesc.Members.FirstOrDefault(member => member.Name == "X");
            MemberClass = (IMemberDescriptor)myStructDesc.Members.FirstOrDefault(member => member.Name == "Class");
        }
         
    }
}
