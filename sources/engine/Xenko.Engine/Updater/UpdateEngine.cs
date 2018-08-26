// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Xenko.Core;
using Xenko.Core.Reflection;
using Xenko.Core.Serialization;
using Xenko.Core.Threading;

namespace Xenko.Updater
{
    /// <summary>
    /// Efficiently updates values on objects using property paths.
    /// </summary>
    public static unsafe class UpdateEngine
    {
        private static readonly ConcurrentPool<Stack<UpdateStackEntry>> StackPool = new ConcurrentPool<Stack<UpdateStackEntry>>(() => new Stack<UpdateStackEntry>());
        private static readonly Dictionary<UpdateKey, UpdatableMember> UpdateKeys = new Dictionary<UpdateKey, UpdatableMember>();
        private static readonly Dictionary<Type, UpdateMemberResolver> MemberResolvers = new Dictionary<Type, UpdateMemberResolver>();

        /// <summary>
        /// Registers a new member for a given type and name.
        /// </summary>
        /// <param name="owner">The owner type.</param>
        /// <param name="name">The member name.</param>
        /// <param name="updatableMember">The member update class to get and set value.</param>
        public static void RegisterMember(Type owner, string name, UpdatableMember updatableMember)
        {
            UpdateKeys[new UpdateKey(owner, name)] = updatableMember;
        }

        public static void RegisterMemberResolver(UpdateMemberResolver resolver)
        {
            MemberResolvers[resolver.SupportedType] = resolver;
        }

        /// <summary>
        /// An entry on the stack of <see cref="Compile"/>.
        /// </summary>
        private struct AnimationBuilderStackEntry
        {
            public Type Type;

            // Property path substring for current member
            public int StartIndex;
            public int EndIndex;

            // Current offset in containing object
            public int ObjectStartOffset;

            public UpdatableMember Member;

            // What to do when poping this entry from stack
            public UpdateOperationType LeaveOperation;

            // What offset to set when poping this entry from stack
            public int LeaveOffset;

            // Store current operation index to properly compute "skip count" if an error happens
            public int OperationIndex;

            public AnimationBuilderStackEntry(Type type, int startIndex, int endIndex, int operationIndex)
            {
                Type = type;
                StartIndex = startIndex;
                EndIndex = endIndex;

                ObjectStartOffset = 0;

                Member = null;
                LeaveOperation = UpdateOperationType.Invalid;
                LeaveOffset = 0;

                OperationIndex = operationIndex;
            }
        }

        private const char PathDelimiter = '.';
        private const char PathIndexerOpen = '[';
        private const char PathIndexerClose = ']';
        private const char PathCastOpen = '(';
        private const char PathCastClose = ')';
        private static readonly char[] PathGroupDelimiters = new[] { PathDelimiter, PathIndexerOpen };

        /// <summary>
        /// Encode state of <see cref="Compile"/> so that it can be easily passed
        /// to another function (easier to split it in multiple methods).
        /// </summary>
        private struct ComputeUpdateOperationState
        {
            /// <summary>
            /// Current list of update operations being built.
            /// </summary>
            public List<UpdateOperation> UpdateOperations;

            /// <summary>
            /// Stack of the current path.
            /// </summary>
            public List<AnimationBuilderStackEntry> StackPath;

            public int NewOffset;
            public int PreviousOffset;

            /// <summary>
            /// Current path start index in the string.
            /// </summary>
            public int ParseElementStart;

            /// <summary>
            /// Current path end index in the string.
            /// </summary>
            public int ParseElementEnd;

            /// <summary>
            /// Contains the last node that was just left.
            /// Used to call <see cref="UpdatableMember.CreateEnterChecker"/>.
            /// </summary>
            public UpdatableMember LastChildMember;
        }

        /// <summary>
        /// Compiles a list of update operations into a <see cref="CompiledUpdate"/>, for use with <see cref="Run"/>.
        /// </summary>
        /// <param name="rootObjectType">The type of the root object.</param>
        /// <param name="animationPaths">The different paths and source offsets to use when <see cref="Run"/> is applied.</param>
        /// <returns>A <see cref="CompiledUpdate"/> object that can be used for <see cref="Run"/>.</returns>
        public static CompiledUpdate Compile(Type rootObjectType, List<UpdateMemberInfo> animationPaths)
        {
            var currentPath = string.Empty;
            var temporaryObjectsList = new List<object>();

            var state = new ComputeUpdateOperationState();
            state.UpdateOperations = new List<UpdateOperation>();
            state.StackPath = new List<AnimationBuilderStackEntry>
            {
                new AnimationBuilderStackEntry(rootObjectType, 0, 0, -1),
            };

            foreach (var animationPath in animationPaths)
            {
                var commonPathParts = 1;

                // Detect how much of the path is unmodified (note: we start from 1 because root always stay there)
                for (int index = 1; index < state.StackPath.Count; index++)
                {
                    var pathPart = state.StackPath[index];

                    // Check if next path part is the same (first check length then content)
                    if (((animationPath.Name.Length == pathPart.EndIndex) ||
                         (animationPath.Name.Length > pathPart.EndIndex && (animationPath.Name[pathPart.EndIndex] == PathDelimiter || animationPath.Name[pathPart.EndIndex] == PathIndexerOpen)))
                        && string.Compare(animationPath.Name, pathPart.StartIndex, currentPath, pathPart.StartIndex, pathPart.EndIndex - pathPart.StartIndex, StringComparison.Ordinal) == 0)
                    {
                        commonPathParts++;
                        continue;
                    }

                    break;
                }

                PopObjects(ref state, commonPathParts);

                // Parse the new path parts
                state.ParseElementStart = state.StackPath.Last().EndIndex;

                // Compute offset from start of current object
                state.NewOffset = state.StackPath.Last().ObjectStartOffset;

                while (state.ParseElementStart < animationPath.Name.Length)
                {
                    var containerType = state.StackPath.Last().Type;

                    // We have only two cases for now: array or property/field name
                    bool isIndexerAccess = animationPath.Name[state.ParseElementStart] == PathIndexerOpen;
                    if (isIndexerAccess)
                    {
                        // Parse until end of indexer
                        state.ParseElementEnd = animationPath.Name.IndexOf(PathIndexerClose, state.ParseElementStart + 1);
                        if (state.ParseElementEnd == -1)
                            throw new InvalidOperationException("Property path parse error: could not find indexer end ']'");

                        // Include the indexer close
                        state.ParseElementEnd++;

                        // Parse integer
                        // TODO: Avoid substring?
                        var indexerString = animationPath.Name.Substring(state.ParseElementStart + 1, state.ParseElementEnd - state.ParseElementStart - 2);

                        // T[], IList<T>, etc...
                        // Try to resolve using custom resolver
                        UpdatableMember updatableMember = null;
                        var parentType = containerType;
                        while (parentType != null)
                        {
                            UpdateMemberResolver resolver;
                            if (MemberResolvers.TryGetValue(parentType, out resolver))
                            {
                                updatableMember = resolver.ResolveIndexer(indexerString);
                                if (updatableMember != null)
                                    break;
                            }

                            parentType = parentType.GetTypeInfo().BaseType;
                        }

                        // Try interfaces
                        if (updatableMember == null)
                        {
                            foreach (var implementedInterface in containerType.GetTypeInfo().ImplementedInterfaces)
                            {
                                UpdateMemberResolver resolver;
                                if (MemberResolvers.TryGetValue(implementedInterface, out resolver))
                                {
                                    updatableMember = resolver.ResolveIndexer(indexerString);
                                    if (updatableMember != null)
                                        break;
                                }
                            }
                        }

                        if (updatableMember == null)
                        {
                            throw new InvalidOperationException(string.Format("Property path parse error: could not find binding info for index {0} in type {1}", indexerString, containerType));
                        }

                        ProcessMember(ref state, animationPath, updatableMember, temporaryObjectsList);
                    }
                    else
                    {
                        // Note: first character might be a . delimiter, if so, skip it
                        var propertyStart = state.ParseElementStart;
                        if (animationPath.Name[propertyStart] == PathDelimiter)
                            propertyStart++;

                        // Check if it started with a parenthesis (to perform a cast operation)
                        if (animationPath.Name[propertyStart] == PathCastOpen)
                        {
                            // Parse until end of cast operation
                            state.ParseElementEnd = animationPath.Name.IndexOf(PathCastClose, ++propertyStart);
                            if (state.ParseElementEnd == -1)
                                throw new InvalidOperationException("Property path parse error: could not find cast operation ending ')'");

                            var typeName = animationPath.Name.Substring(propertyStart, state.ParseElementEnd - propertyStart);

                            // Include the indexer close
                            state.ParseElementEnd++;

                            // Try to resolve using alias first, then full assembly registry using assembly qualified name
                            var type = DataSerializerFactory.GetTypeFromAlias(typeName) ?? AssemblyRegistry.GetType(typeName);
                            if (type == null)
                            {
                                throw new InvalidOperationException($"Could not resolve type {typeName}");
                            }

                            // Push entry with new type
                            // TODO: Should we actually perform an early castclass and skip if type is incorrect?
                            state.StackPath.Add(new AnimationBuilderStackEntry(type, state.ParseElementStart, state.ParseElementEnd, -1)
                            {
                                ObjectStartOffset = state.NewOffset,
                            });
                        }
                        else
                        {
                            UpdatableMember updatableMember;

                            // Parse until end next group (or end)
                            state.ParseElementEnd = animationPath.Name.IndexOfAny(PathGroupDelimiters, state.ParseElementStart + 1);
                            if (state.ParseElementEnd == -1)
                                state.ParseElementEnd = animationPath.Name.Length;

                            var propertyName = animationPath.Name.Substring(propertyStart, state.ParseElementEnd - propertyStart); // TODO: Avoid substring?

                            // try to find a member updater
                            var parentType = containerType;
                            while (!UpdateKeys.TryGetValue(new UpdateKey(parentType, propertyName), out updatableMember) && parentType != null)
                            {
                                parentType = parentType.GetTypeInfo().BaseType;
                            }

                            // if not found, try to find a custom resolver 
                            parentType = containerType;
                            while (updatableMember == null && parentType != null)
                            {
                                UpdateMemberResolver resolver;
                                if (MemberResolvers.TryGetValue(parentType, out resolver))
                                {
                                    updatableMember = resolver.ResolveProperty(propertyName);
                                    if (updatableMember != null)
                                        break;
                                }

                                parentType = parentType.GetTypeInfo().BaseType;
                            }

                            if (updatableMember == null)
                            {
                                throw new InvalidOperationException(string.Format("Property path parse error: could not find binding info for member {0} in type {1}", propertyName, containerType));
                            }

                            // Process member
                            ProcessMember(ref state, animationPath, updatableMember, temporaryObjectsList);
                        }
                    }

                    state.ParseElementStart = state.ParseElementEnd;
                }

                currentPath = animationPath.Name;
            }

            // Totally pop the stack (we might still have stuff to copy back into properties
            PopObjects(ref state, 0);

            return new CompiledUpdate
            {
                TemporaryObjects = temporaryObjectsList.ToArray(),
                UpdateOperations = state.UpdateOperations.ToArray(),
            };
        }

        private static void PopObjects(ref ComputeUpdateOperationState state, int desiredStackSize)
        {
            // Leave the objects that are not part of the path anymore
            while (state.StackPath.Count > desiredStackSize)
            {
                // Pop entry
                var stackPathPart = state.StackPath.Last();
                state.StackPath.RemoveAt(state.StackPath.Count - 1);

                // Perform any necessary exit action
                if (stackPathPart.LeaveOperation != UpdateOperationType.Invalid)
                {
                    state.UpdateOperations.Add(new UpdateOperation
                    {
                        Type = stackPathPart.LeaveOperation,
                        Member = stackPathPart.Member,
                    });

                    // We execute a leave operation, previous stack will be restored
                    state.PreviousOffset = stackPathPart.LeaveOffset;
                }

                if (stackPathPart.OperationIndex != -1)
                {
                    var updateOperation = state.UpdateOperations[stackPathPart.OperationIndex];

                    // Setup VerifyEnter using last child of current node
                    var verifyEnter = state.LastChildMember?.CreateEnterChecker();
                    if (verifyEnter != null)
                    {
                        updateOperation.EnterChecker = verifyEnter;
                    }

                    // Sets how many operations to skip in case the object we entered was null
                    updateOperation.SkipCountIfNull = state.UpdateOperations.Count - stackPathPart.OperationIndex - 1;
                    state.UpdateOperations[stackPathPart.OperationIndex] = updateOperation;
                }

                // Set last child member to the node we just left
                state.LastChildMember = stackPathPart.Member;
            }
        }

        private static void ProcessMember(ref ComputeUpdateOperationState state, UpdateMemberInfo animationPath, UpdatableMember updatableMember, List<object> temporaryObjectsList)
        {
            int leaveOffset = 0;
            var leaveOperation = UpdateOperationType.Invalid;

            // Note: only matters on leaf/leave nodes
            // It will be processed during following PopObjects
            state.LastChildMember = updatableMember;

            var updatableField = updatableMember as UpdatableField;
            if (updatableField != null)
            {
                // Apply field offset
                state.NewOffset += updatableField.Offset;

                if (state.ParseElementEnd == animationPath.Name.Length)
                {
                    // Leaf node, perform the set operation
                    state.UpdateOperations.Add(new UpdateOperation
                    {
                        Type = updatableField.GetSetOperationType(),
                        Member = updatableField,
                        AdjustOffset = state.NewOffset - state.PreviousOffset,
                        DataOffset = animationPath.DataOffset,
                    });
                    state.PreviousOffset = state.NewOffset;
                }
                else if (!updatableField.MemberType.GetTypeInfo().IsValueType)
                {
                    // Only in case of objects we need to enter into them
                    state.UpdateOperations.Add(new UpdateOperation
                    {
                        Type = UpdateOperationType.EnterObjectField,
                        Member = updatableField,
                        AdjustOffset = state.NewOffset - state.PreviousOffset,
                    });
                    leaveOperation = UpdateOperationType.Leave;
                    leaveOffset = state.NewOffset;
                    state.PreviousOffset = state.NewOffset = 0;
                }
            }
            else
            {
                var updatableProperty = updatableMember as UpdatablePropertyBase;
                if (updatableProperty != null)
                {
                    if (state.ParseElementEnd == animationPath.Name.Length)
                    {
                        // Leaf node, perform the set the value
                        state.UpdateOperations.Add(new UpdateOperation
                        {
                            Type = updatableProperty.GetSetOperationType(),
                            Member = updatableProperty,
                            AdjustOffset = state.NewOffset - state.PreviousOffset,
                            DataOffset = animationPath.DataOffset,
                        });
                        state.PreviousOffset = state.NewOffset;
                    }
                    else
                    {
                        // Otherwise enter into the property
                        bool isStruct = updatableProperty.MemberType.GetTypeInfo().IsValueType;
                        int temporaryObjectIndex = -1;

                        if (isStruct)
                        {
                            // Struct properties need a storage area so that we can later set the updated value back into the property
                            leaveOperation = UpdateOperationType.LeaveAndCopyStructPropertyBase;
                            temporaryObjectIndex = temporaryObjectsList.Count;
                            temporaryObjectsList.Add(Activator.CreateInstance(updatableProperty.MemberType));
                        }
                        else
                        {
                            leaveOperation = UpdateOperationType.Leave;
                        }

                        state.UpdateOperations.Add(new UpdateOperation
                        {
                            Type = updatableProperty.GetEnterOperationType(),
                            Member = updatableProperty,
                            AdjustOffset = state.NewOffset - state.PreviousOffset,
                            DataOffset = temporaryObjectIndex,
                            SkipCountIfNull = -1,
                        });

                        leaveOffset = state.NewOffset;
                        state.PreviousOffset = state.NewOffset = 0;
                    }
                }
            }

            // No need to add the last part of the path, as we rarely set and then enter (and if we do we need to reevaluate updated value anyway)
            if (state.ParseElementEnd < animationPath.Name.Length)
            {
                state.StackPath.Add(new AnimationBuilderStackEntry(updatableMember.MemberType, state.ParseElementStart, state.ParseElementEnd, state.UpdateOperations.Count - 1)
                {
                    Member = updatableMember,
                    LeaveOperation = leaveOperation,
                    LeaveOffset = leaveOffset,
                    ObjectStartOffset = state.NewOffset,
                });
            }
        }

        /// <summary>
        /// Updates the specified <see cref="target"/> object with new data.
        /// </summary>
        /// <param name="target">The object to update.</param>
        /// <param name="compiledUpdate">The precompiled list of update operations, generated by <see cref="Compile"/>.</param>
        /// <param name="updateData">The data source for blittable struct.</param>
        /// <param name="updateObjects">The data source for objects and non-blittable struct</param>
        public static void Run(object target, CompiledUpdate compiledUpdate, IntPtr updateData, UpdateObjectData[] updateObjects)
        {
            var operations = compiledUpdate.UpdateOperations;
            var temporaryObjects = compiledUpdate.TemporaryObjects;

            var stack = StackPool.Acquire();

            // Current object being processed
            object currentObj = target;
            object nextObject;

            // This object needs to be pinned since we will have a pointer to its memory
            // Note that the stack don't need to have each of its object pinned since we store entries as object + offset
            Interop.Pin(currentObj);

            // pinned test (this will need to be on a stack somehow)
            IntPtr currentPtr = UpdateEngineHelper.ObjectToPtr(currentObj);

            var operationCount = operations.Length;
            if (operationCount == 0)
                return;

            var operation = Interop.Pin(ref operations[0]);
            for (int index = 0; index < operationCount; index++)
            {
                // Adjust offset
                currentPtr += operation.AdjustOffset;

                switch (operation.Type)
                {
                    case UpdateOperationType.EnterObjectProperty:
                    {
                        nextObject = ((UpdatableProperty)operation.Member).GetObject(currentPtr);
                        if ((nextObject == null || (!operation.EnterChecker?.CanEnter(nextObject) ?? false)) && operation.SkipCountIfNull != -1)
                        {
                            index += operation.SkipCountIfNull;
                            operation = Interop.AddPinned(operation, operation.SkipCountIfNull);
                            break;
                        }

                        // Compute offset and push to stack
                        stack.Push(new UpdateStackEntry(
                            currentObj,
                            (int)((byte*)currentPtr - (byte*)UpdateEngineHelper.ObjectToPtr(currentObj))));

                        // Get object
                        currentObj = nextObject;
                        currentPtr = UpdateEngineHelper.ObjectToPtr(currentObj);

                        break;
                    }
                    case UpdateOperationType.EnterStructPropertyBase:
                    {
                        // Compute offset and push to stack
                        stack.Push(new UpdateStackEntry(
                            currentObj,
                            (int)((byte*)currentPtr - (byte*)UpdateEngineHelper.ObjectToPtr(currentObj))));

                        currentObj = temporaryObjects[operation.DataOffset];
                        currentPtr = ((UpdatablePropertyBase)operation.Member).GetStructAndUnbox(currentPtr, currentObj);

                        break;
                    }
                    case UpdateOperationType.EnterObjectField:
                    {
                        nextObject = ((UpdatableField)operation.Member).GetObject(currentPtr);
                        if ((nextObject == null || (!operation.EnterChecker?.CanEnter(nextObject) ?? false)) && operation.SkipCountIfNull != -1)
                        {
                            index += operation.SkipCountIfNull;
                            operation = Interop.AddPinned(operation, operation.SkipCountIfNull);
                            break;
                        }

                        // Compute offset and push to stack
                        stack.Push(new UpdateStackEntry(
                            currentObj,
                            (int)((byte*)currentPtr - (byte*)UpdateEngineHelper.ObjectToPtr(currentObj))));

                        // Get object
                        currentObj = nextObject;
                        currentPtr = UpdateEngineHelper.ObjectToPtr(currentObj);
                        break;
                    }
                    case UpdateOperationType.EnterObjectCustom:
                    {
                        nextObject = ((UpdatableCustomAccessor)operation.Member).GetObject(currentPtr);
                        if ((nextObject == null || (!operation.EnterChecker?.CanEnter(nextObject) ?? false)) && operation.SkipCountIfNull != -1)
                        {
                            index += operation.SkipCountIfNull;
                            operation = Interop.AddPinned(operation, operation.SkipCountIfNull);
                            break;
                        }

                        // Compute offset and push to stack
                        stack.Push(new UpdateStackEntry(
                            currentObj,
                            (int)((byte*)currentPtr - (byte*)UpdateEngineHelper.ObjectToPtr(currentObj))));

                        // Get object
                        currentObj = nextObject;
                        currentPtr = UpdateEngineHelper.ObjectToPtr(currentObj);
                        break;
                    }

                    case UpdateOperationType.LeaveAndCopyStructPropertyBase:
                    {
                        // Save back struct pointer
                        var oldPtr = currentPtr;

                        // Restore currentObj and currentPtr from stack
                        var stackEntry = stack.Pop();
                        currentObj = stackEntry.Object;
                        currentPtr = UpdateEngineHelper.ObjectToPtr(currentObj) + stackEntry.Offset;

                        // Use setter to set back struct
                        ((UpdatablePropertyBase)operation.Member).SetBlittable(currentPtr, oldPtr);

                        break;
                    }
                    case UpdateOperationType.Leave:
                    {
                        // Restore currentObj and currentPtr from stack
                        var stackEntry = stack.Pop();
                        currentObj = stackEntry.Object;
                        currentPtr = UpdateEngineHelper.ObjectToPtr(currentObj) + stackEntry.Offset;
                        break;
                    }
                    case UpdateOperationType.ConditionalSetObjectProperty:
                    {
                        var updateObject = updateObjects[operation.DataOffset];
                        if (updateObject.Condition != 0) // 0 is 0.0f in float
                            ((UpdatableProperty)operation.Member).SetObject(currentPtr, updateObject.Value);
                        break;
                    }
                    case UpdateOperationType.ConditionalSetBlittablePropertyBase:
                    {
                        // TODO: This case can happen quite often (i.e. a float property) and require an extra indirection
                        // We could probably avoid it by having common types as non virtual methods (i.e. object, int, float, maybe even Vector3/4?)
                        var data = (int*)((byte*)updateData + operation.DataOffset);
                        if (*data++ != 0) // 0 is 0.0f in float
                            ((UpdatablePropertyBase)operation.Member).SetBlittable(currentPtr, (IntPtr)data);
                        break;
                    }
                    case UpdateOperationType.ConditionalSetStructPropertyBase:
                    {
                        // TODO: This case can happen quite often (i.e. a float property) and require an extra indirection
                        // We could probably avoid it by having common types as non virtual methods (i.e. object, int, float, maybe even Vector3/4?)
                        var updateObject = updateObjects[operation.DataOffset];
                        if (updateObject.Condition != 0) // 0 is 0.0f in float
                            ((UpdatablePropertyBase)operation.Member).SetStruct(currentPtr, updateObject.Value);
                        break;
                    }
                    case UpdateOperationType.ConditionalSetObjectField:
                    {
                        var updateObject = updateObjects[operation.DataOffset];
                        if (updateObject.Condition != 0) // 0 is 0.0f in float
                            ((UpdatableField)operation.Member).SetObject(currentPtr, updateObject.Value);
                        break;
                    }
                    case UpdateOperationType.ConditionalSetBlittableField:
                    {
                        var data = (int*)((byte*)updateData + operation.DataOffset);
                        if (*data++ != 0) // 0 is 0.0f in float
                            ((UpdatableField)operation.Member).SetBlittable(currentPtr, (IntPtr)data);
                        break;
                    }
                    case UpdateOperationType.ConditionalSetBlittableField4:
                    {
                        var data = (int*)((byte*)updateData + operation.DataOffset);
                        if (*data++ != 0) // 0 is 0.0f in float
                        {
                            *(int*)currentPtr = *data;
                        }
                        break;
                    }
                    case UpdateOperationType.ConditionalSetBlittableField8:
                    {
                        var data = (int*)((byte*)updateData + operation.DataOffset);
                        if (*data++ != 0) // 0 is 0.0f in float
                        {
                            *(Blittable8*)currentPtr = *(Blittable8*)data;
                        }
                        break;
                    }
                    case UpdateOperationType.ConditionalSetBlittableField12:
                    {
                        var data = (int*)((byte*)updateData + operation.DataOffset);
                        if (*data++ != 0) // 0 is 0.0f in float
                        {
                            *(Blittable12*)currentPtr = *(Blittable12*)data;
                        }
                        break;
                    }
                    case UpdateOperationType.ConditionalSetBlittableField16:
                    {
                        var data = (int*)((byte*)updateData + operation.DataOffset);
                        if (*data++ != 0) // 0 is 0.0f in float
                        {
                            *(Blittable16*)currentPtr = *(Blittable16*)data;
                        }
                        break;
                    }
                    case UpdateOperationType.ConditionalSetStructField:
                    {
                        // Use setter to set back struct
                        var updateObject = updateObjects[operation.DataOffset];
                        if (updateObject.Condition != 0) // 0 is 0.0f in float
                            ((UpdatableField)operation.Member).SetStruct(currentPtr, updateObject.Value);
                        break;
                    }
                    case UpdateOperationType.ConditionalSetObjectCustom:
                    {
                        var updateObject = updateObjects[operation.DataOffset];
                        if (updateObject.Condition != 0) // 0 is 0.0f in float
                            ((UpdatableCustomAccessor)operation.Member).SetObject(currentPtr, updateObject.Value);
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                operation = Interop.IncrementPinned(operation);
            }

            StackPool.Release(stack);
        }

        // Helper struct to blit small struct
        [StructLayout(LayoutKind.Sequential, Size = 8)]
        private struct Blittable8
        {
        }

        // Helper struct to blit small struct
        [StructLayout(LayoutKind.Sequential, Size = 12)]
        private struct Blittable12
        {
        }

        // Helper struct to blit small struct
        [StructLayout(LayoutKind.Sequential, Size = 16)]
        private struct Blittable16
        {
        }

        /// <summary>
        /// Internally used as key to register members.
        /// </summary>
        private struct UpdateKey
        {
            public readonly Type Owner;
            public readonly string Name;

            public UpdateKey(Type owner, string name)
            {
                Owner = owner;
                Name = name;
            }

            public override string ToString()
            {
                return $"{Owner.Name}.{Name}";
            }
        }

        /// <summary>
        /// Stack entry used in <see cref="Run"/>.
        /// </summary>
        private struct UpdateStackEntry
        {
            public object Object;
            public int Offset;

            public UpdateStackEntry(object o, int offset)
            {
                Object = o;
                Offset = offset;
            }
        }
    }
}
