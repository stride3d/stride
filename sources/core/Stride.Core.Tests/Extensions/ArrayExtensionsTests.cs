// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;
using Stride.Core.Extensions;
using Stride.Core.Collections;

namespace Stride.Core.Tests.Extensions;

public class ArrayExtensionsTests
{
    [Fact]
    public void ArraysEqual_ReturnsTrueForSameLists()
    {
        var list1 = new List<int> { 1, 2, 3 };
        var list2 = new List<int> { 1, 2, 3 };

        Assert.True(ArrayExtensions.ArraysEqual(list1, list2));
    }

    [Fact]
    public void ArraysEqual_ReturnsTrueForSameReference()
    {
        var list1 = new List<int> { 1, 2, 3 };

        Assert.True(ArrayExtensions.ArraysEqual(list1, list1));
    }

    [Fact]
    public void ArraysEqual_ReturnsFalseForDifferentCounts()
    {
        var list1 = new List<int> { 1, 2, 3 };
        var list2 = new List<int> { 1, 2 };

        Assert.False(ArrayExtensions.ArraysEqual(list1, list2));
    }

    [Fact]
    public void ArraysEqual_ReturnsFalseForDifferentElements()
    {
        var list1 = new List<int> { 1, 2, 3 };
        var list2 = new List<int> { 1, 2, 4 };

        Assert.False(ArrayExtensions.ArraysEqual(list1, list2));
    }

    [Fact]
    public void ArraysEqual_ReturnsFalseForNullList()
    {
        var list1 = new List<int> { 1, 2, 3 };

        Assert.False(ArrayExtensions.ArraysEqual(list1, null!));
        Assert.False(ArrayExtensions.ArraysEqual(null!, list1));
    }

    [Fact]
    public void ArraysEqual_ReturnsTrueForBothNull()
    {
        Assert.True(ArrayExtensions.ArraysEqual<int>(null!, null!));
    }

    [Fact]
    public void ArraysEqual_WorksWithCustomComparer()
    {
        var list1 = new List<string> { "a", "b", "c" };
        var list2 = new List<string> { "A", "B", "C" };

        Assert.True(ArrayExtensions.ArraysEqual(list1, list2, StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void ArraysReferenceEqual_ReturnsTrueForSameReferences()
    {
        var obj1 = new object();
        var obj2 = new object();
        var list1 = new List<object> { obj1, obj2 };
        var list2 = new List<object> { obj1, obj2 };

        Assert.True(ArrayExtensions.ArraysReferenceEqual(list1, list2));
    }

    [Fact]
    public void ArraysReferenceEqual_ReturnsFalseForDifferentReferences()
    {
        var list1 = new List<object> { new object(), new object() };
        var list2 = new List<object> { new object(), new object() };

        Assert.False(ArrayExtensions.ArraysReferenceEqual(list1, list2));
    }

    [Fact]
    public void ArraysReferenceEqual_ReturnsTrueForSameListReference()
    {
        var list1 = new List<string> { "a", "b" };

        Assert.True(ArrayExtensions.ArraysReferenceEqual(list1, list1));
    }

    [Fact]
    public void ArraysReferenceEqual_ReturnsFalseForNullList()
    {
        var list1 = new List<string> { "a", "b" };

        Assert.False(ArrayExtensions.ArraysReferenceEqual(list1, null!));
        Assert.False(ArrayExtensions.ArraysReferenceEqual(null!, list1));
    }

    [Fact]
    public void ArraysReferenceEqual_ReturnsFalseForDifferentCounts()
    {
        var obj1 = new object();
        var list1 = new List<object> { obj1 };
        var list2 = new List<object> { obj1, new object() };

        Assert.False(ArrayExtensions.ArraysReferenceEqual(list1, list2));
    }

    [Fact]
    public void ArraysReferenceEqual_WithFastListStruct_ReturnsTrueForSameReferences()
    {
        var obj1 = new object();
        var obj2 = new object();
        var list1 = new FastListStruct<object>(new[] { obj1, obj2 });
        var list2 = new FastListStruct<object>(new[] { obj1, obj2 });

        Assert.True(ArrayExtensions.ArraysReferenceEqual(list1, list2));
    }

    [Fact]
    public void ArraysReferenceEqual_WithFastListStruct_ReturnsFalseForDifferentReferences()
    {
        var list1 = new FastListStruct<object>(new[] { new object(), new object() });
        var list2 = new FastListStruct<object>(new[] { new object(), new object() });

        Assert.False(ArrayExtensions.ArraysReferenceEqual(list1, list2));
    }

    [Fact]
    public void ArraysReferenceEqual_WithFastListStructRef_ReturnsTrueForSameReferences()
    {
        var obj1 = new object();
        var obj2 = new object();
        var list1 = new FastListStruct<object>(new[] { obj1, obj2 });
        var list2 = new FastListStruct<object>(new[] { obj1, obj2 });

        Assert.True(ArrayExtensions.ArraysReferenceEqual(ref list1, ref list2));
    }

    [Fact]
    public void ComputeHash_ForCollection_ReturnsConsistentHash()
    {
        var list = new List<int> { 1, 2, 3 };

        var hash1 = list.ComputeHash();
        var hash2 = list.ComputeHash();

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_ForCollection_ReturnsDifferentHashForDifferentData()
    {
        var list1 = new List<int> { 1, 2, 3 };
        var list2 = new List<int> { 1, 2, 4 };

        var hash1 = list1.ComputeHash();
        var hash2 = list2.ComputeHash();

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_ForCollection_ReturnsZeroForNull()
    {
        List<int>? list = null;

        var hash = list!.ComputeHash();

        Assert.Equal(0, hash);
    }

    [Fact]
    public void ComputeHash_ForCollection_WorksWithCustomComparer()
    {
        var list1 = new List<string> { "a", "b", "c" };
        var list2 = new List<string> { "A", "B", "C" };

        var hash1 = list1.ComputeHash(StringComparer.OrdinalIgnoreCase);
        var hash2 = list2.ComputeHash(StringComparer.OrdinalIgnoreCase);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_ForArray_ReturnsConsistentHash()
    {
        var array = new[] { 1, 2, 3 };

        var hash1 = array.ComputeHash();
        var hash2 = array.ComputeHash();

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_ForArray_ReturnsDifferentHashForDifferentData()
    {
        var array1 = new[] { 1, 2, 3 };
        var array2 = new[] { 1, 2, 4 };

        var hash1 = array1.ComputeHash();
        var hash2 = array2.ComputeHash();

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_ForArray_ReturnsZeroForNull()
    {
        int[]? array = null;

        var hash = array!.ComputeHash();

        Assert.Equal(0, hash);
    }

    [Fact]
    public void SubArray_ExtractsCorrectSubset()
    {
        var array = new[] { 1, 2, 3, 4, 5 };

        var subArray = array.SubArray(1, 3);

        Assert.Equal(3, subArray.Length);
        Assert.Equal(new[] { 2, 3, 4 }, subArray);
    }

    [Fact]
    public void SubArray_ExtractsFromBeginning()
    {
        var array = new[] { 1, 2, 3, 4, 5 };

        var subArray = array.SubArray(0, 2);

        Assert.Equal(new[] { 1, 2 }, subArray);
    }

    [Fact]
    public void SubArray_ExtractsToEnd()
    {
        var array = new[] { 1, 2, 3, 4, 5 };

        var subArray = array.SubArray(3, 2);

        Assert.Equal(new[] { 4, 5 }, subArray);
    }

    [Fact]
    public void SubArray_ThrowsOnNullArray()
    {
        int[]? array = null;

        Assert.Throws<ArgumentNullException>(() => array!.SubArray(0, 1));
    }

    [Fact]
    public void Concat_ConcatenatesTwoArrays()
    {
        var array1 = new[] { 1, 2, 3 };
        var array2 = new[] { 4, 5, 6 };

        var result = array1.Concat(array2);

        Assert.Equal(6, result.Length);
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6 }, result);
    }

    [Fact]
    public void Concat_HandlesEmptyArrays()
    {
        var array1 = new[] { 1, 2, 3 };
        var array2 = Array.Empty<int>();

        var result = array1.Concat(array2);

        Assert.Equal(3, result.Length);
        Assert.Equal(new[] { 1, 2, 3 }, result);
    }

    [Fact]
    public void Concat_HandlesBothEmptyArrays()
    {
        var array1 = Array.Empty<int>();
        var array2 = Array.Empty<int>();

        var result = array1.Concat(array2);

        Assert.Empty(result);
    }

    [Fact]
    public void Concat_ThrowsOnNullFirstArray()
    {
        int[]? array1 = null;
        var array2 = new[] { 1, 2, 3 };

        Assert.Throws<ArgumentNullException>(() => array1!.Concat(array2));
    }

    [Fact]
    public void Concat_ThrowsOnNullSecondArray()
    {
        var array1 = new[] { 1, 2, 3 };
        int[]? array2 = null;

        Assert.Throws<ArgumentNullException>(() => array1.Concat(array2!));
    }

    [Fact]
    public void Concat_WorksWithReferenceTypes()
    {
        var array1 = new[] { "a", "b" };
        var array2 = new[] { "c", "d" };

        var result = array1.Concat(array2);

        Assert.Equal(new[] { "a", "b", "c", "d" }, result);
    }
}
