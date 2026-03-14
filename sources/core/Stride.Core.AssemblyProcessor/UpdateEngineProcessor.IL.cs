// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Mono.Cecil;
using Mono.Cecil.Cil;
using Stride.Core.AssemblyProcessor.Serializers;

namespace Stride.Core.AssemblyProcessor;

internal partial class UpdateEngineProcessor
{
    private void ProcessStrideEngineAssembly(CecilSerializerContext context)
    {
        var assembly = context.Assembly;

        // Check "#if IL" directly in the source to easily see what is generated
        GenerateUpdateEngineHelperCode(assembly);
        GenerateUpdatableFieldCode(assembly);
        new UpdatablePropertyCodeGenerator(assembly).GenerateUpdatablePropertyCode();
        new UpdatableListCodeGenerator(assembly).GenerateUpdatablePropertyCode();
    }

    private static void GenerateUpdateEngineHelperCode(AssemblyDefinition assembly)
    {
        var updateEngineHelperType = assembly.MainModule.GetType("Stride.Updater.UpdateEngineHelper");

        // UpdateEngineHelper.ObjectToPtr
        var objectToPtr = RewriteBody(updateEngineHelperType.Methods.First(x => x.Name == "ObjectToPtr"), assembly);
        objectToPtr.Emit(OpCodes.Ldarg, objectToPtr.Body.Method.Parameters[0])
                   .Emit(OpCodes.Conv_I)
                   .Emit(OpCodes.Ret);

        // UpdateEngineHelper.PtrToObject
        // Simpler "ldarg.0 + ret" doesn't work with Xamarin: https://bugzilla.xamarin.com/show_bug.cgi?id=40608
        var ptrToObject = RewriteBody(updateEngineHelperType.Methods.First(x => x.Name == "PtrToObject"), assembly);
        var ptrToObjectLocal = ptrToObject.AddLocal(assembly.MainModule.TypeSystem.Object);
        ptrToObject.Emit(OpCodes.Ldloca_S, ptrToObjectLocal)
                   .Emit(OpCodes.Ldarg, ptrToObject.Body.Method.Parameters[0])
                   // Somehow Xamarin forces us to do a roundtrip to an object
                   .Emit(OpCodes.Stind_I)
                   .Emit(OpCodes.Ldloc_0)
                   .Emit(OpCodes.Ret);

        // UpdateEngineHelper.Unbox
        var unbox = RewriteBody(updateEngineHelperType.Methods.First(x => x.Name == "Unbox"), assembly);
        unbox.Emit(OpCodes.Ldarg, unbox.Body.Method.Parameters[0])
             .Emit(OpCodes.Unbox, unbox.Body.Method.GenericParameters[0])
             .Emit(OpCodes.Ret);
    }

    private static void GenerateUpdatableFieldCode(AssemblyDefinition assembly)
    {
        var updatableFieldType = assembly.MainModule.GetType("Stride.Updater.UpdatableField");
        var updatableFieldGenericType = assembly.MainModule.GetType("Stride.Updater.UpdatableField`1");

        // UpdatableField.GetObject
        var getObject = RewriteBody(updatableFieldType.Methods.First(x => x.Name == "GetObject"), assembly);
        getObject.Emit(OpCodes.Ldarg, getObject.Body.Method.Parameters[0])
                 .Emit(OpCodes.Ldind_Ref)
                 .Emit(OpCodes.Ret);

        // UpdatableField.SetObject
        var setObject = RewriteBody(updatableFieldType.Methods.First(x => x.Name == "SetObject"), assembly);
        setObject.Emit(OpCodes.Ldarg, setObject.Body.Method.Parameters[0])
                 .Emit(OpCodes.Ldarg, setObject.Body.Method.Parameters[1])
                 .Emit(OpCodes.Stind_Ref)
                 .Emit(OpCodes.Ret);

        // UpdatableField<T>.SetStruct
        var setStruct = RewriteBody(updatableFieldGenericType.Methods.First(x => x.Name == "SetStruct"), assembly);
        setStruct.Emit(OpCodes.Ldarg, setStruct.Body.Method.Parameters[0])
                 .Emit(OpCodes.Ldarg, setStruct.Body.Method.Parameters[1])
                 .Emit(OpCodes.Unbox, updatableFieldGenericType.GenericParameters[0])
                 .Emit(OpCodes.Cpobj, updatableFieldGenericType.GenericParameters[0])
                 .Emit(OpCodes.Ret);
    }

    private static ILBuilder RewriteBody(MethodDefinition method, AssemblyDefinition assembly)
    {
        method.Body = new MethodBody(method);
        return new ILBuilder(method.Body, assembly.MainModule);
    }

    /// <summary>
    /// Helper class to generate code for UpdatablePropertyBase (since they usually have lot of similar code).
    /// </summary>
    abstract class UpdatablePropertyBaseCodeGenerator
    {
        protected readonly AssemblyDefinition assembly;
        protected TypeDefinition declaringType;

        public UpdatablePropertyBaseCodeGenerator(AssemblyDefinition assembly)
        {
            this.assembly = assembly;
        }

        public abstract void EmitGetCode(ILBuilder il, TypeReference type);

        public virtual void EmitSetCodeBeforeValue(ILBuilder il, TypeReference type)
        {
        }

        public abstract void EmitSetCodeAfterValue(ILBuilder il, TypeReference type);

        public virtual void GenerateUpdatablePropertyCode()
        {
            // UpdatableProperty.GetStructAndUnbox
            var getStructAndUnbox = RewriteBody(declaringType.Methods.First(x => x.Name == "GetStructAndUnbox"), assembly);
            getStructAndUnbox.Emit(OpCodes.Ldarg, getStructAndUnbox.Body.Method.Parameters[1])
                             //getStructAndUnbox.Emit(OpCodes.Call, assembly.MainModule.ImportReference(unbox).MakeGenericMethod(declaringType.GenericParameters[0]));
                             .Emit(OpCodes.Unbox, declaringType.GenericParameters[0])
                             .Emit(OpCodes.Dup)
                             .Emit(OpCodes.Ldarg, getStructAndUnbox.Body.Method.Parameters[0]);
            EmitGetCode(getStructAndUnbox, declaringType.GenericParameters[0]);
            getStructAndUnbox.Emit(OpCodes.Stobj, declaringType.GenericParameters[0])
                             .Emit(OpCodes.Ret);

            // UpdatableProperty.GetBlittable
            var getBlittable = RewriteBody(declaringType.Methods.First(x => x.Name == "GetBlittable"), assembly);
            getBlittable.Emit(OpCodes.Ldarg, getBlittable.Body.Method.Parameters[1])
                        .Emit(OpCodes.Ldarg, getBlittable.Body.Method.Parameters[0]);
            EmitGetCode(getBlittable, declaringType.GenericParameters[0]);
            getBlittable.Emit(OpCodes.Stobj, declaringType.GenericParameters[0])
                        .Emit(OpCodes.Ret);

            // UpdatableProperty.SetStruct
            var setStruct = RewriteBody(declaringType.Methods.First(x => x.Name == "SetStruct"), assembly);
            setStruct.Emit(OpCodes.Ldarg, setStruct.Body.Method.Parameters[0]);
            EmitSetCodeBeforeValue(setStruct, declaringType.GenericParameters[0]);
            setStruct.Emit(OpCodes.Ldarg, setStruct.Body.Method.Parameters[1])
                     .Emit(OpCodes.Unbox_Any, declaringType.GenericParameters[0]);
            EmitSetCodeAfterValue(setStruct, declaringType.GenericParameters[0]);
            setStruct.Emit(OpCodes.Ret);

            // UpdatableProperty.SetBlittable
            var setBlittable = RewriteBody(declaringType.Methods.First(x => x.Name == "SetBlittable"), assembly);
            setBlittable.Emit(OpCodes.Ldarg, setBlittable.Body.Method.Parameters[0]);
            EmitSetCodeBeforeValue(setBlittable, declaringType.GenericParameters[0]);
            setBlittable.Emit(OpCodes.Ldarg, setBlittable.Body.Method.Parameters[1])
                        .Emit(OpCodes.Ldobj, declaringType.GenericParameters[0]);
            EmitSetCodeAfterValue(setBlittable, declaringType.GenericParameters[0]);
            setBlittable.Emit(OpCodes.Ret);
        }
    }

    class UpdatablePropertyCodeGenerator : UpdatablePropertyBaseCodeGenerator
    {
        private readonly FieldDefinition updatablePropertyGetter;
        private readonly FieldDefinition updatablePropertySetter;
        private readonly FieldDefinition updatablePropertyVirtualDispatchGetter;
        private readonly FieldDefinition updatablePropertyVirtualDispatchSetter;
        protected TypeDefinition declaringTypeForObjectMethods;

        public UpdatablePropertyCodeGenerator(AssemblyDefinition assembly) : base(assembly)
        {
            // GetObject/SetObject are on the non-generic implementation
            declaringTypeForObjectMethods = assembly.MainModule.GetType("Stride.Updater.UpdatableProperty");
            declaringType = assembly.MainModule.GetType("Stride.Updater.UpdatableProperty`1");

            updatablePropertyGetter = declaringTypeForObjectMethods.Fields.First(x => x.Name == "Getter");
            updatablePropertySetter = declaringTypeForObjectMethods.Fields.First(x => x.Name == "Setter");
            updatablePropertyVirtualDispatchGetter = declaringTypeForObjectMethods.Fields.First(x => x.Name == "VirtualDispatchGetter");
            updatablePropertyVirtualDispatchSetter = declaringTypeForObjectMethods.Fields.First(x => x.Name == "VirtualDispatchSetter");
        }

        public override void GenerateUpdatablePropertyCode()
        {
            // For UpdatableProperty, GetObject/SetObject are declared on another type

            // UpdatableProperty.GetObject
            var getObject = RewriteBody(declaringTypeForObjectMethods.Methods.First(x => x.Name == "GetObject"), assembly);
            getObject.Emit(OpCodes.Ldarg, getObject.Body.Method.Parameters[0]);
            EmitGetCode(getObject, assembly.MainModule.TypeSystem.Object);
            getObject.Emit(OpCodes.Ret);

            // UpdatableProperty.SetObject
            var setObject = RewriteBody(declaringTypeForObjectMethods.Methods.First(x => x.Name == "SetObject"), assembly);
            setObject.Emit(OpCodes.Ldarg, setObject.Body.Method.Parameters[0]);
            EmitSetCodeBeforeValue(setObject, assembly.MainModule.TypeSystem.Object);
            setObject.Emit(OpCodes.Ldarg, setObject.Body.Method.Parameters[1]);
            EmitSetCodeAfterValue(setObject, assembly.MainModule.TypeSystem.Object);
            setObject.Emit(OpCodes.Ret);

            base.GenerateUpdatablePropertyCode();
        }

        public override void EmitGetCode(ILBuilder il, TypeReference type)
        {
            var calliInstance = Instruction.Create(OpCodes.Calli, new CallSite(type) { HasThis = true });
            // Note: .NET 6 doesn't like IntPtr => object implicit conversion so we pretend the method expect a IntPtr rather than object
            // (another option would be to use "castclass object" after pushing the IntPtr on the stack)
            var calliVirtualDispatch = Instruction.Create(OpCodes.Calli, new CallSite(type) { HasThis = false, Parameters = { new ParameterDefinition(assembly.MainModule.TypeSystem.IntPtr) } });
            var postCalli = ILBuilder.DefineLabel();

            il.Emit(OpCodes.Ldarg_0)
              .Emit(OpCodes.Ldfld, updatablePropertyGetter)
              .Emit(OpCodes.Ldarg_0)
              // For normal calls, we use ldftn and an instance calls
              // For virtual and interface calls, we generate a dispatch function that calls ldvirtftn on the actual object, then call the method on the object
              // this dispatcher method is static, so the calli has a different signature
              // Note: we could later optimize the bool check by having two variant of Get/SetObject
              // and two different implementations of both UpdatableProperty<T> and UpdatablePropertyObject<T>
              // (not sure if worth it)
              .Emit(OpCodes.Ldfld, updatablePropertyVirtualDispatchGetter)
              .Emit(OpCodes.Brfalse, calliInstance)
              .Append(calliVirtualDispatch)
              .Emit(OpCodes.Br, postCalli)
              .Append(calliInstance)
              .MarkLabel(postCalli);
        }

        public override void EmitSetCodeAfterValue(ILBuilder il, TypeReference type)
        {
            var calliInstance = Instruction.Create(OpCodes.Calli, new CallSite(assembly.MainModule.TypeSystem.Void) { HasThis = true, Parameters = { new ParameterDefinition(type) } });
            // Note: .NET 6 doesn't like IntPtr => object implicit conversion so we pretend the method expect a IntPtr rather than object
            // (another option would be to use "castclass object" after pushing the IntPtr on the stack)
            var calliVirtualDispatch = Instruction.Create(OpCodes.Calli, new CallSite(assembly.MainModule.TypeSystem.Void) { HasThis = false, Parameters = { new ParameterDefinition(assembly.MainModule.TypeSystem.IntPtr), new ParameterDefinition(type) } });
            var postCalli = ILBuilder.DefineLabel();

            il.Emit(OpCodes.Ldarg_0)
              .Emit(OpCodes.Ldfld, updatablePropertySetter)
              .Emit(OpCodes.Ldarg_0)
              .Emit(OpCodes.Ldfld, updatablePropertyVirtualDispatchSetter)
              .Emit(OpCodes.Brfalse, calliInstance)
              .Append(calliVirtualDispatch)
              .Emit(OpCodes.Br, postCalli)
              .Append(calliInstance)
              .MarkLabel(postCalli);
        }
    }

    abstract class UpdatableCustomPropertyCodeGenerator : UpdatablePropertyBaseCodeGenerator
    {
        public UpdatableCustomPropertyCodeGenerator(AssemblyDefinition assembly) : base(assembly)
        {
        }

        public override void GenerateUpdatablePropertyCode()
        {
            // For UpdatableCustomAccessor, GetObject/SetObject are declared in the generic type

            // UpdatableProperty.GetObject
            var getObject = RewriteBody(declaringType.Methods.First(x => x.Name == "GetObject"), assembly);
            getObject.Emit(OpCodes.Ldarg, getObject.Body.Method.Parameters[0]);
            EmitGetCode(getObject, declaringType.GenericParameters[0]);
            getObject.Emit(OpCodes.Box, declaringType.GenericParameters[0]) // Required for Windows 10 AOT
                     .Emit(OpCodes.Ret);

            // UpdatableProperty.SetObject
            var setObject = RewriteBody(declaringType.Methods.First(x => x.Name == "SetObject"), assembly);
            setObject.Emit(OpCodes.Ldarg, setObject.Body.Method.Parameters[0]);
            EmitSetCodeBeforeValue(setObject, declaringType.GenericParameters[0]);
            setObject.Emit(OpCodes.Ldarg, setObject.Body.Method.Parameters[1])
                     .Emit(OpCodes.Unbox_Any, declaringType.GenericParameters[0]); // Required for Windows 10 AOT
            EmitSetCodeAfterValue(setObject, declaringType.GenericParameters[0]);
            setObject.Emit(OpCodes.Ret);

            base.GenerateUpdatablePropertyCode();
        }
    }

    class UpdatableListCodeGenerator : UpdatableCustomPropertyCodeGenerator
    {
        private readonly FieldDefinition indexField;
        private readonly MethodReference ilistGetItem;
        private readonly MethodReference ilistSetItem;

        public UpdatableListCodeGenerator(AssemblyDefinition assembly) : base(assembly)
        {
            declaringType = assembly.MainModule.GetType("Stride.Updater.UpdatableListAccessor`1");
            indexField = assembly.MainModule.GetType("Stride.Updater.UpdatableListAccessor").Fields.First(x => x.Name == "Index");

            // TODO: Update to new method to resolve collection assembly
            var mscorlibAssembly = CecilExtensions.FindCorlibAssembly(assembly);
            var ilistType = mscorlibAssembly.MainModule.GetTypeResolved(typeof(IList<>).FullName);

            var ilistItem = ilistType.Properties.First(x => x.Name == "Item");

            ilistGetItem = assembly.MainModule.ImportReference(ilistItem.GetMethod).MakeGeneric(declaringType.GenericParameters[0]);
            ilistSetItem = assembly.MainModule.ImportReference(ilistItem.SetMethod).MakeGeneric(declaringType.GenericParameters[0]);
        }

        public override void EmitGetCode(ILBuilder il, TypeReference type)
        {
            il.Emit(OpCodes.Ldarg_0)
              .Emit(OpCodes.Ldfld, indexField)
              .Emit(OpCodes.Callvirt, ilistGetItem);
        }

        public override void EmitSetCodeBeforeValue(ILBuilder il, TypeReference type)
        {
            il.Emit(OpCodes.Ldarg_0)
              .Emit(OpCodes.Ldfld, indexField);
        }

        public override void EmitSetCodeAfterValue(ILBuilder il, TypeReference type)
        {
            il.Emit(OpCodes.Callvirt, ilistSetItem);
        }
    }
}
