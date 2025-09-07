using System;
using System.Collections.Generic;
using System.Collections;

namespace Stride.Shaders.Spirv.Core;

/// <summary>
/// Wrapper for the operand list to contain a class name
/// </summary>
public readonly struct LogicalOperandArray(string? className, List<LogicalOperand>? operands = null)
{
    public string ClassName { get; init; } = className ?? "Debug";

    List<LogicalOperand> LogicalOperands { get; } = operands ?? [];

    public int Count => LogicalOperands.Count;

    public bool IsReadOnly => false;

    public LogicalOperand this[int index]
    {
        get => LogicalOperands[index];
        set => LogicalOperands[index] = value;
    }

    public bool GetResultIndex(out int index)
    {
        for(int i = 0; i < LogicalOperands.Count; i++)
        {
            var o = LogicalOperands[i];
            if (o.Kind == OperandKind.IdResult)
            {
                index = i;
                return true;
            }
        }
        index = -1;
        return false;
    }
    public bool GetResultTypeIndex(out int index)
    {
        for (int i = 0; i < LogicalOperands.Count; i++)
        {
            var o = LogicalOperands[i];
            if (o.Kind == OperandKind.IdResultType)
            {
                index = i;
                return true;
            }
        }
        index = -1;
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

    public List<LogicalOperand>.Enumerator GetEnumerator() => LogicalOperands.GetEnumerator();
}