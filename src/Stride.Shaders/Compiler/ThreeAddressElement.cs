using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shaders.Parsing.Backend;


public enum Operators
{
    Mul,
    Div,
    Mod,
    Plus,
    Minus,
    LeftShift,
    RightShift,
    And,
    Or,
    Xor,
    Less,
    Greater,
    LessEqual,
    GreaterEqual,
    Equals,
    NotEquals,
    LogicalAnd,
    LogicalOr
}


public interface Register { }

public struct NamedRegister : Register
{
    public string Name { get; set; }
}
public struct ValueRegister<T> : Register
{
    public T Value { get; set; }
}

public struct ThreeAddressOperation
{
    public string RegisterName { get; set; }
    public Register Left { get; set; }
    public Register Right { get; set; }
    public Operators Op { get; set; }
}



