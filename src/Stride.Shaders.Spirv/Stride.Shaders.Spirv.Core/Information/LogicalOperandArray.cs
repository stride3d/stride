using System;
using System.Collections.Generic;
using System.Collections;

namespace Stride.Shaders.Spirv.Core;

public struct LogicalOperandArray : IList<LogicalOperand>
{

    public string ClassName { get; init; }

    List<LogicalOperand> LogicalOperands { get; }
    public bool HasResult => GetHasResult();
    public bool HasResultType => GetHasResultType();

    public int Count => LogicalOperands.Count;

    public bool IsReadOnly => false;

    public LogicalOperand this[int index]
    {
        get => LogicalOperands[index];
        set => LogicalOperands[index] = value;
    }

    public LogicalOperandArray(string? className)
    {
        ClassName = className ?? "Debug";
        LogicalOperands = new();
    }

    bool GetHasResult()
    {
        foreach (var o in LogicalOperands)
        {
            if (o.Kind == OperandKind.IdResult)
                return true;
        }
        return false;
    }
    bool GetHasResultType()
    {
        foreach (var o in LogicalOperands)
        {
            if (o.Kind == OperandKind.IdResultType)
                return true;
        }
        return false;
    }


    public int IndexOf(LogicalOperand item)
    {
        return LogicalOperands.IndexOf(item);
    }

    public void Insert(int index, LogicalOperand item)
    {
        LogicalOperands.Insert(index, item);
    }

    public void RemoveAt(int index)
    {
        LogicalOperands.RemoveAt(index);
    }

    public void Add(LogicalOperand item)
    {
        LogicalOperands.Add(item);
    }

    public void Clear()
    {
        LogicalOperands.Clear();
    }

    public bool Contains(LogicalOperand item)
    {
        return LogicalOperands.Contains(item);
    }

    public void CopyTo(LogicalOperand[] array, int arrayIndex)
    {
        LogicalOperands.CopyTo(array, arrayIndex);
    }

    public bool Remove(LogicalOperand item)
    {
        return LogicalOperands.Remove(item);
    }

    public IEnumerator<LogicalOperand> GetEnumerator()
    {
        return LogicalOperands.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}