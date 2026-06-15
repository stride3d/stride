using Stride.Core.Storage;
using Xunit;

namespace Stride.Core.Tests;

public class ObjectIdTests
{
    [Fact]
    public void New_CreatesUniqueObjectId()
    {
        var id1 = ObjectId.New();
        var id2 = ObjectId.New();

        Assert.NotEqual(id1, id2);
        Assert.NotEqual(ObjectId.Empty, id1);
        Assert.NotEqual(ObjectId.Empty, id2);
    }

    [Fact]
    public void Empty_ReturnsAllZeroId()
    {
        var empty = ObjectId.Empty;
        Assert.Equal("00000000000000000000000000000000", empty.ToString());
    }

    [Fact]
    public void ToString_ReturnsHexString()
    {
        var id = ObjectId.New();
        var str = id.ToString();

        Assert.Equal(32, str.Length);
        Assert.All(str, c => Assert.True(char.IsDigit(c) || (c >= 'a' && c <= 'f')));
    }

    [Fact]
    public void TryParse_WithValidString_ReturnsTrue()
    {
        var id = ObjectId.New();
        var str = id.ToString();

        var result = ObjectId.TryParse(str, out var parsed);

        Assert.True(result);
        Assert.Equal(id, parsed);
    }

    [Fact]
    public void TryParse_WithInvalidString_ReturnsFalse()
    {
        var result = ObjectId.TryParse("invalid", out var parsed);

        Assert.False(result);
        Assert.Equal(ObjectId.Empty, parsed);
    }

    [Fact]
    public void TryParse_WithNullString_ReturnsFalse()
    {
        var result = ObjectId.TryParse(null!, out var parsed);

        Assert.False(result);
        Assert.Equal(ObjectId.Empty, parsed);
    }

    [Fact]
    public void Equals_WithSameId_ReturnsTrue()
    {
        var id1 = ObjectId.New();
        var id2 = id1;

        Assert.True(id1.Equals(id2));
        Assert.True(id1 == id2);
        Assert.False(id1 != id2);
    }

    [Fact]
    public void Equals_WithDifferentId_ReturnsFalse()
    {
        var id1 = ObjectId.New();
        var id2 = ObjectId.New();

        Assert.False(id1.Equals(id2));
        Assert.False(id1 == id2);
        Assert.True(id1 != id2);
    }

    [Fact]
    public void GetHashCode_IsConsistent()
    {
        var id = ObjectId.New();
        var hash1 = id.GetHashCode();
        var hash2 = id.GetHashCode();

        Assert.Equal(hash1, hash2);
    }
}
