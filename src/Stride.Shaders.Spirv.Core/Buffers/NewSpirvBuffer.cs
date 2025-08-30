#pragma warning disable CS9264 // Non-nullable property must contain a non-null value when exiting constructor. Consider adding the 'required' modifier, or declaring the property as nullable, or safely handling the case where 'field' is null in the 'get' accessor.

using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;
using Stride.Shaders.Spirv.Core.Parsing;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Core.Buffers;


public interface IMemoryInstruction
{
    OpDataIndex? DataIndex { get; set; }
    MemoryOwner<int> InstructionMemory { get; }
    public void UpdateInstructionMemory();
}

public struct OpData : IDisposable, IComparable<OpData>
{
    public MemoryOwner<int> Memory { get; internal set { field?.Dispose(); field = value; } }
    public readonly Op Op => (Op)(Memory.Span[0] & 0xFFFF);

    public OpData()
    {
        Memory = MemoryOwner<int>.Empty;
    }

    public OpData(MemoryOwner<int> memory)
    {
        Memory = memory;
    }

    public readonly void Dispose() => Memory.Dispose();

    public readonly T Get<T>(string name)
        where T : struct, IFromSpirv<T>
    {
        foreach (var o in this)
        {
            if (name == o.Name && (o.Kind.ToString().Contains("Literal") || o.Kind.ToString().Contains("Id")))
                return o.To<T>();
        }
        throw new Exception($"No operand '{name}' in op {Op}");
    }
    public readonly T GetEnum<T>(string name)
        where T : Enum
    {
        foreach (var o in this)
        {
            if (name == o.Name && !o.Kind.ToString().Contains("Literal") && !o.Kind.ToString().Contains("Id"))
                return o.ToEnum<T>();
        }
        throw new Exception($"No enum operand '{name}' in op {Op}");
    }

    public readonly OpDataEnumerator GetEnumerator() => new(Memory.Span);

    public readonly int CompareTo(OpData other)
    {
        var group = InstructionInfo.GetGroupOrder(this);
        var otherGroup = InstructionInfo.GetGroupOrder(other);
        return group.CompareTo(otherGroup);
    }
}


public record struct OpDataIndex(int Index, NewSpirvBuffer Buffer)
{
    public readonly ref OpData Data => ref Buffer[Index];
}

public class NewSpirvBuffer
{
    List<OpData> Memory { get; set; } = [];

    internal ref OpData this[int index] => ref CollectionsMarshal.AsSpan(Memory)[index];
    // internal OpDataIndex this[int index] => new(index, this);

    public void Add(OpData data)
        => Memory.Add(data);

    public void AddRef<T>(ref T instruction) where T : IMemoryInstruction
    {
        if (instruction.DataIndex is OpDataIndex odi)
        {
            if (odi.Buffer == this)
                return;
            else
                Memory.Add(new(instruction.InstructionMemory));
        }
        else Memory.Add(new(instruction.InstructionMemory));
        instruction.DataIndex = new(Memory.Count - 1, this);

    }
    public void Add<T>(in T instruction) where T : IMemoryInstruction
    {
        if (instruction.DataIndex is OpDataIndex odi)
        {
            if (odi.Buffer == this)
                return;
            else
                Memory.Add(new(instruction.InstructionMemory));
        }
        else Memory.Add(new(instruction.InstructionMemory));
    }

    public void Insert(int index, OpData data)
        => Memory.Insert(index, data);

    /// <summary>
    /// Removes an instruction at a certain index. 
    /// <br/>Be careful when using this method, as it will invalidate any references to the removed instruction.
    /// </summary>
    /// <param name="index"></param>
    /// <returns>true if the instruction was successfully removed</returns>
    public bool RemoveAt(int index)
    {
        if (index < 0 || index >= Memory.Count)
            return false;
        Memory[index].Dispose();
        Memory.RemoveAt(index);
        return true;
    }

    public Enumerator GetEnumerator() => new(this);

    public ref struct Enumerator(NewSpirvBuffer buffer)
    {
        readonly NewSpirvBuffer buffer = buffer;
        private readonly List<OpData> list = buffer.Memory;
        private int index = -1;

        public readonly OpDataIndex Current => new(index, buffer);

        public bool MoveNext()
        {
            if (index < list.Count - 1)
            {
                index++;
                return true;
            }
            return false;
        }
    }

    public void Sort()
    {
        Memory.Sort(static (a, b) => a.CompareTo(b));
    }
}