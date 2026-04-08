// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Stride.Core.AssemblyProcessor;

/// <summary>
/// A thin fluent wrapper over Cecil's IL emission that reduces verbosity.
/// Provides automatic <see cref="ModuleDefinition.ImportReference(TypeReference)"/> and composite patterns
/// like <c>typeof(T)</c> emission.
/// </summary>
internal class ILBuilder
{
    private readonly MethodBody body;
    private readonly ModuleDefinition module;

    public ILBuilder(MethodBody body, ModuleDefinition module)
    {
        this.body = body;
        this.module = module;
    }

    /// <summary>
    /// The underlying method body, for direct access when needed (e.g. adding variables).
    /// </summary>
    public MethodBody Body => body;

    /// <summary>
    /// The module used for importing references.
    /// </summary>
    public ModuleDefinition Module => module;

    // ── Core Emit overloads ──────────────────────────────────────────

    public ILBuilder Emit(OpCode opcode)
    {
        body.Instructions.Add(Instruction.Create(opcode));
        return this;
    }

    public ILBuilder Emit(OpCode opcode, int value)
    {
        body.Instructions.Add(Instruction.Create(opcode, value));
        return this;
    }

    public ILBuilder Emit(OpCode opcode, byte value)
    {
        body.Instructions.Add(Instruction.Create(opcode, value));
        return this;
    }

    public ILBuilder Emit(OpCode opcode, string value)
    {
        body.Instructions.Add(Instruction.Create(opcode, value));
        return this;
    }

    public ILBuilder Emit(OpCode opcode, TypeReference type)
    {
        body.Instructions.Add(Instruction.Create(opcode, type));
        return this;
    }

    public ILBuilder Emit(OpCode opcode, MethodReference method)
    {
        body.Instructions.Add(Instruction.Create(opcode, method));
        return this;
    }

    public ILBuilder Emit(OpCode opcode, FieldReference field)
    {
        body.Instructions.Add(Instruction.Create(opcode, field));
        return this;
    }

    public ILBuilder Emit(OpCode opcode, VariableDefinition variable)
    {
        body.Instructions.Add(Instruction.Create(opcode, variable));
        return this;
    }

    public ILBuilder Emit(OpCode opcode, ParameterDefinition parameter)
    {
        body.Instructions.Add(Instruction.Create(opcode, parameter));
        return this;
    }

    public ILBuilder Emit(OpCode opcode, Instruction target)
    {
        body.Instructions.Add(Instruction.Create(opcode, target));
        return this;
    }

    public ILBuilder Emit(OpCode opcode, CallSite callSite)
    {
        body.Instructions.Add(Instruction.Create(opcode, callSite));
        return this;
    }

    /// <summary>
    /// Appends a pre-created instruction to the method body.
    /// </summary>
    public ILBuilder Append(Instruction instruction)
    {
        body.Instructions.Add(instruction);
        return this;
    }

    // ── Import helpers ───────────────────────────────────────────────

    /// <summary>
    /// Imports a <see cref="TypeReference"/> into the current module.
    /// Shorthand for <c>module.ImportReference(type)</c>.
    /// </summary>
    public TypeReference Import(TypeReference type) => module.ImportReference(type);

    /// <summary>
    /// Imports a <see cref="MethodReference"/> into the current module.
    /// Shorthand for <c>module.ImportReference(method)</c>.
    /// </summary>
    public MethodReference Import(MethodReference method) => module.ImportReference(method);

    /// <summary>
    /// Imports a <see cref="FieldReference"/> into the current module.
    /// Shorthand for <c>module.ImportReference(field)</c>.
    /// </summary>
    public FieldReference Import(FieldReference field) => module.ImportReference(field);

    // ── Composite patterns ───────────────────────────────────────────

    /// <summary>
    /// Emits <c>typeof(T)</c>: <c>ldtoken type; call GetTypeFromHandle</c>.
    /// </summary>
    public ILBuilder EmitTypeof(TypeReference type, MethodReference getTypeFromHandle)
    {
        Emit(OpCodes.Ldtoken, type);
        Emit(OpCodes.Call, getTypeFromHandle);
        return this;
    }

    /// <summary>
    /// Emits <c>typeof(T).GetTypeInfo().Assembly</c>.
    /// </summary>
    public ILBuilder EmitTypeofAssembly(TypeReference type, MethodReference getTypeFromHandle, MethodReference getTypeInfo, MethodReference getAssembly)
    {
        EmitTypeof(type, getTypeFromHandle);
        Emit(OpCodes.Call, getTypeInfo);
        Emit(OpCodes.Callvirt, getAssembly);
        return this;
    }

    /// <summary>
    /// Emits <c>typeof(T).GetTypeInfo().Module</c>.
    /// </summary>
    public ILBuilder EmitTypeofModule(TypeReference type, MethodReference getTypeFromHandle, MethodReference getTypeInfo, MethodReference getModule)
    {
        EmitTypeof(type, getTypeFromHandle);
        Emit(OpCodes.Call, getTypeInfo);
        Emit(OpCodes.Callvirt, getModule);
        return this;
    }

    /// <summary>
    /// Emits <c>typeof(T).TypeHandle</c>: <c>ldtoken type; call GetTypeFromHandle; callvirt get_TypeHandle</c>.
    /// </summary>
    public ILBuilder EmitTypeHandle(TypeReference type, MethodReference getTypeFromHandle, MethodReference getTypeHandle)
    {
        EmitTypeof(type, getTypeFromHandle);
        Emit(OpCodes.Callvirt, getTypeHandle);
        return this;
    }

    // ── Variable helpers ─────────────────────────────────────────────

    /// <summary>
    /// Adds a local variable to the method body and returns it.
    /// Sets <see cref="MethodBody.InitLocals"/> to <see langword="true"/>.
    /// </summary>
    public VariableDefinition AddLocal(TypeReference type)
    {
        var variable = new VariableDefinition(type);
        body.Variables.Add(variable);
        body.InitLocals = true;
        return variable;
    }

    // ── Label support ────────────────────────────────────────────────

    /// <summary>
    /// Creates a <see cref="Instruction.Create(OpCodes.Nop)"/> that can be used as a branch target,
    /// without appending it to the body. Call <see cref="MarkLabel"/> to place it.
    /// </summary>
    public static Instruction DefineLabel() => Instruction.Create(OpCodes.Nop);

    /// <summary>
    /// Appends a previously defined label instruction to the body, marking the current position.
    /// </summary>
    public ILBuilder MarkLabel(Instruction label)
    {
        body.Instructions.Add(label);
        return this;
    }
}
