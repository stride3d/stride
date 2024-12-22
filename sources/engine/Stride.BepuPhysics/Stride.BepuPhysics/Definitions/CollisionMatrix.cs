// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.BepuPhysics.Definitions;

/// <summary>
/// Defines how layers collide between each other.
/// </summary>
/// <remarks>
/// This structure is VERY large, prefer referencing it directly instead of passing it around.
/// </remarks>
public unsafe struct CollisionMatrix : IEquatable<CollisionMatrix>
{
    // Here's an example 4x4 truth table, notice how the table values are mirrored on the top left to bottom right diagonal;
    //       |1234|
    //      1|** *|
    //      2|**  |
    //      3|  * |
    //      4|*  *|
    // We can reduce this struct size by removing all redundant bits, improving cache retention
    // For a 32 * 32 bit matrix, the amount of non-redundant bits can be found by summing numbers from 1 to 32 - doing a gauss sum;
    public const int DataBits = 32 * (32 + 1) / 2;
    public const int DataLength = DataBits / 32 + (DataBits % 32 != 0 ? 1 : 0);

    public static CollisionMatrix All;



    /// <summary>
    /// The last 16 bits of the last integer are not entirely mapped, and as such are undefined
    /// </summary>
    private fixed int _values[DataLength];

    /// <summary>
    /// Returns whether two layers would collide with each other
    /// </summary>
    public readonly bool Get(CollisionLayer layer1, CollisionLayer layer2)
    {
        int index = LayersToIndex(layer1, layer2);
        return (_values[index >> 5] & (1 << index)) != 0;
    }

    /// <summary>
    /// Set whether two layers should collide with each other
    /// </summary>
    public void Set(CollisionLayer layer1, CollisionLayer layer2, bool shouldCollide)
    {
        int index = LayersToIndex(layer1, layer2);
        int bitMask = 1 << index;
        ref int segment = ref _values[index >> 5];

        if (shouldCollide)
            segment |= bitMask;
        else
            segment &= ~bitMask;
    }

    /// <summary>
    /// Return all layers colliding with a given layer
    /// </summary>
    /// <remarks>Prefer <see cref="Get(CollisionLayer, CollisionLayer)"/> over this one when checking a single layer</remarks>
    public readonly CollisionMask Get(CollisionLayer layer)
    {
        // Getting all colliding layers for a given layer optimally is a bit more complex given our non-square matrix;
        // We can only linearly get a certain amount of layers, others have to be read in a somewhat random order;

        CollisionMask mask = 0;
        for (int otherLayer = 0, head = LayersToIndex(0, layer);
             otherLayer <= (int)CollisionLayer.Layer31;
             head += (otherLayer >= (int)layer ? 1 : 31 - otherLayer),/* Preceding layers are laid out in non-sequential bits */
             otherLayer++)
        {
            if ((_values[head >> 5] & (1 << head)) != 0)
                mask |= (CollisionMask)(1 << otherLayer);
        }

        return mask;
    }

    /// <summary>
    /// Set all layers that should collide with a given layer
    /// </summary>
    /// <remarks>Prefer <see cref="Set(CollisionLayer, CollisionLayer, bool)"/> over this one when setting one or two layers</remarks>
    public void Set(CollisionLayer layer, CollisionMask mask)
    {
        // Getting all colliding layers for a given layer optimally is a bit more complex given our non-square matrix;
        // We can only linearly get a certain amount of layers, others have to be read in a somewhat random order;

        for (int otherLayer = 0, head = LayersToIndex(0, layer);
             otherLayer <= (int)CollisionLayer.Layer31;
             head += (otherLayer >= (int)layer ? 1 : 31 - otherLayer),/* Preceding layers are laid out in non-sequential bits */
             otherLayer++)
        {
            ref int segment = ref _values[head >> 5];

            int bitMask = 1 << head;
            if (((int)mask & (1 << otherLayer)) != 0)
                segment |= bitMask;
            else
                segment &= ~bitMask;
        }
    }

    public readonly bool Equals(CollisionMatrix other)
    {
        for (int i = 0; i < DataLength; i++)
        {
            if (_values[i] != other._values[i])
                return false;
        }

        return true;
    }

    public override bool Equals(object? obj) => obj is CollisionMatrix other && Equals(other);
    public static bool operator ==(CollisionMatrix left, CollisionMatrix right) => left.Equals(right);
    public static bool operator !=(CollisionMatrix left, CollisionMatrix right) => !(left == right);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        fixed (void* ptr = _values)
        {
            hash.AddBytes(new ReadOnlySpan<byte>(ptr, DataLength * sizeof(int)));
        }

        return hash.ToHashCode();
    }

    public static int LayersToIndex(CollisionLayer layer1, CollisionLayer layer2)
    {
        if (layer1 > layer2) // The logic below expects layer1 to be the smallest
            (layer1, layer2) = (layer2, layer1);

        int x = (int)layer1;
        int y = (int)layer2;

        // Our matrix is not square, each layer has a decreasing amount of entries based on the amount of preceding layers
        // layer0 = 32 bits, from 0 to 32
        // layer1 = 31 bits, from 32 to 63, one less as collision between layer1 and layer0 has been covered by the preceding layer
        // layer2 = 30 bits, from 63 to 93, two less as collision between layer2 and layer0, layer2 and layer1 has been covered by the preceding layer
        // layer3 = 29 bits, from 93 to 122, three less ...

        // Will not work for non-32 bit matrices
        return y - (x - 63) * x / 2;
        // Longform of the above:
        //return
        // (32 * (32 + 1) / 2) // Total amount of bits, gauss sum
        // - ((32 - x) * ((32 - x) + 1) / 2) // Amount of bits preceding 'x', same gauss sum - 1, effectively getting the start of the range for layer x
        // + (y - x); // where in the bits for 'x' 'y' is located
    }

    static CollisionMatrix()
    {
        for (int i = 0; i < DataBits; i++)
            All._values[i >> 5] |= 1 << i;
    }
}
