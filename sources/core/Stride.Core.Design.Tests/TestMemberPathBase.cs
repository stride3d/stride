// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Reflection;

namespace Stride.Core.Design.Tests;

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
            Subs = [];
            Maps = [];
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
        var myClassDesc = TypeFactory.Find(typeof(MyClass))!;
        var myStructDesc = TypeFactory.Find(typeof(MyStruct))!;
        ListClassDesc = (CollectionDescriptor)TypeFactory.Find(typeof(List<MyClass>))!;
        MapClassDesc = (DictionaryDescriptor)TypeFactory.Find(typeof(Dictionary<string, MyClass>))!;

        MemberValue = myClassDesc.Members.First(member => member.Name == "Value");
        MemberSub = myClassDesc.Members.First(member => member.Name == "Sub");
        MemberStruct = myClassDesc.Members.First(member => member.Name == "Struct");
        MemberSubs = myClassDesc.Members.First(member => member.Name == "Subs");
        MemberMaps = myClassDesc.Members.First(member => member.Name == "Maps");
        MemberX = myStructDesc.Members.First(member => member.Name == "X");
        MemberClass = myStructDesc.Members.First(member => member.Name == "Class");
    }
}
