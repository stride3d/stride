// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using Xunit;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Extensions;
using Stride.Core.Presentation.ViewModel;

namespace Stride.Core.Presentation.Tests
{
    public class TestCore
    {
        public class Dummy : ViewModelBase, IComparable
        {
            private string name;

            public string Name { get { return name; } set { SetValue(ref name, value); } }

            public Dummy(string name)
            {
                this.name = name;
            }

            public override string ToString()
            {
                return Name;
            }

            public int CompareTo(object obj)
            {
                return obj == null ? 1 : String.Compare(Name, ((Dummy)obj).Name, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void TestSortedObservableCollection()
        {
            var collection = new SortedObservableCollection<int> { 5, 13, 2, 9, 0, 8, 5, 11, 1, 7, 14, 12, 4, 10, 3, 6 };
            collection.Remove(5);

            for (int i = 0; i < collection.Count; ++i)
            {
                Assert.True(collection[i] == i);
                Assert.True(collection.BinarySearch(i) == i);
            }

            Assert.Throws<InvalidOperationException>(() => collection[4] = 10);
            Assert.Throws<InvalidOperationException>(() => collection.Move(4, 5));
        }

        [Fact]
        public void TestAutoUpdatingSortedObservableCollection()
        {
            var collection = new AutoUpdatingSortedObservableCollection<Dummy> { new Dummy("sss"), new Dummy("eee") };

            var dummy = new Dummy("ggg");
            collection.Add(dummy);

            var sorted = new[] { "eee", "ggg", "sss" };

            for (int i = 0; i < collection.Count; ++i)
            {
                Assert.True(collection[i].Name == sorted[i]);
                Assert.True(collection.BinarySearch(sorted[i], (d, s) => String.Compare(d.Name, s, StringComparison.Ordinal)) == i);
            }

            dummy.Name = "aaa";
            sorted = new[] { "aaa", "eee", "sss" };
            for (int i = 0; i < collection.Count; ++i)
            {
                Assert.True(collection[i].Name == sorted[i]);
                Assert.True(collection.BinarySearch(sorted[i], (d, s) => String.Compare(d.Name, s, StringComparison.Ordinal)) == i);
            }

            dummy.Name = "zzz";
            sorted = new[] { "eee", "sss", "zzz" };
            for (int i = 0; i < collection.Count; ++i)
            {
                Assert.True(collection[i].Name == sorted[i]);
                Assert.True(collection.BinarySearch(sorted[i], (d, s) => String.Compare(d.Name, s, StringComparison.Ordinal)) == i);
            }
        }

        [Fact]
        public void TestCamelCaseSplit()
        {
            var inputStrings = new[]
            {
                "ThisIsOneTestString",
                "ThisOneABCContainsAbreviation",
                "ThisOneContainsASingleCharacterWord",
                "ThisOneEndsWithAbbreviationABC",
                "ThisOneEndsWithASingleCharacterWordZ",
                "  This OneContains   SpacesBetweenSome OfThe Words  ",
            };
            var expectedResult = new[]
            {
                new[] { "This", "Is", "One", "Test", "String" },
                new[] { "This", "One", "ABC", "Contains", "Abreviation" },
                new[] { "This", "One", "Contains", "A", "Single", "Character", "Word" },
                new[] { "This", "One", "Ends", "With", "Abbreviation", "ABC" },
                new[] { "This", "One", "Ends", "With", "A", "Single", "Character", "Word", "Z" },
                new[] { "This", "One", "Contains", "Spaces", "Between", "Some", "Of", "The", "Words" },
            };

            foreach (var testCase in inputStrings.Zip(expectedResult))
            {
                var split = testCase.Item1.CamelCaseSplit();
                Assert.Equal(testCase.Item2.Length, split.Count);
                for (var i = 0; i < testCase.Item2.Length; ++i)
                {
                    Assert.Equal(testCase.Item2[i], split[i]);
                }
            }
        }
    }
}
