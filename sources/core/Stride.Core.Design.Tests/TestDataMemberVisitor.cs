// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Xunit;
using Stride.Core.Reflection;

namespace Stride.Core.Design.Tests
{
    /// <summary>
    /// Testing <see cref="DataVisitorBase"/>
    /// </summary>
    public class TestDataMemberVisitor
    {
        /// <summary>
        /// An primitive grabber just iterate through the whole object hierarchy
        /// and collect all primitives.
        /// </summary>
        public class PrimitiveGrabber : DataVisitorBase
        {
            public List<object> Collected { get; }

            public PrimitiveGrabber()
            {
                Collected = new List<object>();
            }

            public override void Reset()
            {
                Collected.Clear();
                base.Reset();
            }

            public override void VisitNull()
            {
                Collected.Add(null);
            }

            public override void VisitPrimitive(object primitive, PrimitiveDescriptor descriptor)
            {
                Collected.Add(primitive);
            }
        }

        public class SimpleObject
        {
            public SimpleObject()
            {
            }

            public SimpleObject(int firstValue, int secondValue, int thirdValue, int fourthValue)
            {
                FirstValue = firstValue;
                SecondValue = secondValue;
                ThirdValue = thirdValue;
                FourthValue = fourthValue;
                Collection = new List<object>();
                Dictionary = new Dictionary<object, object>();
            }

            [DataMember(0)]
            public int FirstValue { get; set; }

            [DataMember(1)]
            public int SecondValue { get; set; }

            [DataMember(2)]
            public int ThirdValue { get; set; }

            [DataMember(3)]
            public int? FourthValue { get; set; }

            [DataMemberIgnore]
            public int MemberToIgnore { get; set; }

            [DataMember(4)]
            public string Name { get; set; }

            [DataMember(5)]
            public List<object> Collection { get; set; }

            [DataMember(6)]
            public Dictionary<object, object> Dictionary { get; set; }

            public SimpleObject SubObject { get; set; }
        }

        [Fact]
        public void TestVisitPrimitive()
        {
            var simpleObject = new SimpleObject(1, 2, 3, 4) { Name = "Test", MemberToIgnore = int.MaxValue, SubObject = new SimpleObject(5, 6, 7, 8) };
            var primitiveGrabber = new PrimitiveGrabber();
            primitiveGrabber.Visit(simpleObject);
            Assert.Equal(new List<object>()
                {
                    1, 
                    2, 
                    3, 
                    4, 
                    "Test", 
                    5, // simpleObject.SubObject
                    6, 
                    7, 
                    8,
                    null, // simpleObject.SubObject.Name
                    null // simpleObject.SubObject.SubObject
                }, primitiveGrabber.Collected);

            simpleObject.Collection.Add("Item1");
            simpleObject.Collection.Add("Item2");

            simpleObject.Dictionary.Add("Key1", "Value1");
            simpleObject.Dictionary.Add("Key2", "Value2");

            primitiveGrabber.Reset();
            primitiveGrabber.Visit(simpleObject);
            Assert.Equal(new List<object>()
                {
                    1, 
                    2, 
                    3, 
                    4, 
                    "Test", 
                    "Item1", "Item2", // simpleObject.Collection
                    "Key1", "Value1", "Key2", "Value2", // simpleObject.Dictionary
                    5, // simpleObject.SubObject
                    6, 
                    7, 
                    8, 
                    null, // simpleObject.SubObject.Name
                    null // simpleObject.SubObject.SubObject
                }, primitiveGrabber.Collected);
        }

        public class CustomList : List<object>
        {
            [DataMember(0)]
            public int CustomId { get; set; }

            [DataMember(1)]
            public string Name { get; set; }
        }
        
        [Fact]
        public void TestCollection()
        {
            var customList = new CustomList() {1, 2, 3, 4};
            customList.CustomId = 10;
            customList.Name = "Test";
            var primitiveGrabber = new PrimitiveGrabber();
            primitiveGrabber.Visit(customList);
            Assert.Equal(new List<object>()
                {
                    10, // customList.CustomId
                    "Test", // customList.Name
                    1, 
                    2, 
                    3, 
                    4, 
                }, primitiveGrabber.Collected);
        }


        public class CustomDictionary : Dictionary<object, object>
        {
            [DataMember(0)]
            public int CustomId { get; set; }

            [DataMember(1)]
            public string Name { get; set; }
        }

        [Fact]
        public void TestDictionary()
        {
            var customDict = new CustomDictionary() { {"Key1", "Value1"}, {"Key2", "Value2"}};
            customDict.CustomId = 10;
            customDict.Name = "Test";
            var primitiveGrabber = new PrimitiveGrabber();
            primitiveGrabber.Visit(customDict);
            Assert.Equal(new List<object>()
                {
                    10, // customList.CustomId
                    "Test", // customList.Name
                    "Key1", "Value1",
                    "Key2", "Value2",
                }, primitiveGrabber.Collected);
        }

        [Fact]
        public void TestArray()
        {
            var customArray = new[] {1, 2, 3, 4};
            var primitiveGrabber = new PrimitiveGrabber();
            primitiveGrabber.Visit(customArray);
            Assert.Equal(new List<object>
                {
                    1,2,3,4
                }, primitiveGrabber.Collected);
        }

        [DataContract]
        public class SelfRef
        {
            [DataMember]
            public SelfRef Self { get; set; }
        }

        [Fact]
        public void TestSelfReferencingType()
        {
            var typeDescriptor = TypeDescriptorFactory.Default.Find(typeof(SelfRef));
            Assert.Equal(typeDescriptor, typeDescriptor[nameof(SelfRef.Self)].TypeDescriptor);
        }
    }
}
