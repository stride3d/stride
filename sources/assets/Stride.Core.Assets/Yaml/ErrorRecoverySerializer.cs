// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Diagnostics;
using Stride.Core.Yaml.Events;
using Stride.Core.Yaml.Serialization;
using Stride.Core.Yaml.Serialization.Serializers;

namespace Stride.Core.Yaml
{
    /// <summary>
    /// Objects that can't be loaded as valid Yaml will be converted to a proxy object implementing <see cref="IUnloadable"/>.
    /// </summary>
    class ErrorRecoverySerializer : ChainedSerializer
    {
        public override object ReadYaml(ref ObjectContext objectContext)
        {
            // Check if we already have a memory parser
            var previousReader = objectContext.SerializerContext.Reader;
            EventReader reader;
            if (!(previousReader.Parser is MemoryParser))
            {
                // Switch to a memory parser so that we can easily rollback in case of error
                // Read all events from this node
                var parsingEvents = new List<ParsingEvent>();
                previousReader.ReadCurrent(parsingEvents);

                objectContext.SerializerContext.Reader = reader = new EventReader(new MemoryParser(parsingEvents));
            }
            else
            {
                reader = previousReader;
                previousReader = null;
            }

            // Get start position in case we need to recover
            var memoryParser = (MemoryParser)reader.Parser;
            var startDepth = reader.CurrentDepth;
            var startPosition = memoryParser.Position;

            // Allow errors to happen
            var previousAllowErrors = objectContext.SerializerContext.AllowErrors;
            objectContext.SerializerContext.AllowErrors = true;

            try
            {
                // Deserialize normally
                return base.ReadYaml(ref objectContext);
            }
            catch (Exception ex) when (ex is YamlException || ex is DefaultObjectFactory.InstanceCreationException) // TODO: Filter only TagTypeSerializer TypeFromTag decoding errors? or more?
            {
                // Find the parsing range for this object
                // Skipping is also important to make sure the depth is properly updated
                reader.Skip(startDepth, startPosition == memoryParser.Position);
                var endPosition = memoryParser.Position;

                // TODO: Only type from user assemblies (and plugins?)
                var type = objectContext.Descriptor?.Type;
                if (type != null)
                {
                    // Dump the range of Yaml for this object
                    var parsingEvents = new List<ParsingEvent>();
                    for (int i = startPosition; i < endPosition; ++i)
                        parsingEvents.Add(memoryParser.ParsingEvents[i]);

                    // Get typename (if available) and temporarily erase it from the parsing events for partial deserialization of base type
                    string tag = "Unknown";
                    var firstNode = memoryParser.ParsingEvents[startPosition] as NodeEvent;
                    if (firstNode != null)
                    {
                        if (firstNode.Tag != null)
                            tag = firstNode.Tag;

                        // Temporarily recreate the node without its tag, so that we can try deserializing as many members as possible still
                        // TODO: Replace this with switch pattern matching (C# 7.0)
                        var mappingStart = firstNode as MappingStart;
                        var sequenceStart = firstNode as SequenceStart;
                        var scalar = firstNode as Scalar;
                        if (mappingStart != null)
                        {
                            memoryParser.ParsingEvents[startPosition] = new MappingStart(mappingStart.Anchor, null, mappingStart.IsImplicit, mappingStart.Style, mappingStart.Start, mappingStart.End);
                        }
                        else if (sequenceStart != null)
                        {
                            memoryParser.ParsingEvents[startPosition] = new SequenceStart(sequenceStart.Anchor, null, sequenceStart.IsImplicit, sequenceStart.Style, sequenceStart.Start, sequenceStart.End);
                        }
                        else if (scalar != null)
                        {
                            memoryParser.ParsingEvents[startPosition] = new Scalar(scalar.Anchor, null, scalar.Value, scalar.Style, scalar.IsPlainImplicit, scalar.IsQuotedImplicit, scalar.Start, scalar.End);
                        }
                        else
                        {
                            throw new NotImplementedException("Unknown node type");
                        }
                    }

                    string typeName = null;
                    string assemblyName = null;
                    if (tag != null)
                    {
                        var tagAsType = tag.StartsWith("!") ? tag.Substring(1) : tag;
                        objectContext.SerializerContext.ParseType(tagAsType, out typeName, out assemblyName);
                    }

                    var log = objectContext.SerializerContext.Logger;
                    log?.Warning($"Could not deserialize object of type '{typeName ?? tag}'; replacing it with an object implementing {nameof(IUnloadable)}", ex);

                    var unloadableObject = UnloadableObjectInstantiator.CreateUnloadableObject(type, typeName, assemblyName, ex.Message, parsingEvents);
                    objectContext.Instance = unloadableObject;
                    objectContext.Descriptor = objectContext.SerializerContext.FindTypeDescriptor(unloadableObject.GetType());

                    // Restore parser position at beginning of object
                    memoryParser.Position = startPosition;
                    reader.RefreshParserState();

                    try
                    {
                        // Here, we try again to deserialize the object in the proxy
                        // Since we erase the tag, it shouldn't try to resolve the unknown type anymore (it will deserialize properties that exist in the base type)
                        unloadableObject = (IUnloadable)base.ReadYaml(ref objectContext);
                    }
                    catch (YamlException)
                    {
                        // Mute exceptions when trying to deserialize the proxy
                        // (in most case, we can do fine with incomplete objects)
                    }
                    finally
                    {
                        // Read until end of object (in case it failed again)
                        // Skipping is also important to make sure the depth is properly updated
                        reader.Skip(startDepth, startPosition == memoryParser.Position);
                        if (firstNode != null)
                            memoryParser.ParsingEvents[startPosition] = firstNode;
                    }

                    return unloadableObject;
                }
                throw;
            }
            finally
            {
                // Restore reader
                if (previousReader != null)
                    objectContext.SerializerContext.Reader = previousReader;

                // Restore states
                objectContext.SerializerContext.AllowErrors = previousAllowErrors;
            }
        }

        public override void WriteYaml(ref ObjectContext objectContext)
        {
            // If it's a IUnloadable, serialize the yaml events as is
            var proxy = objectContext.Instance as IUnloadable;
            if (proxy != null)
            {
                // TODO: Do we want to save values on the base type that might have changed?
                foreach (var parsingEvent in proxy.ParsingEvents)
                    objectContext.Writer.Emit(parsingEvent);
                return;
            }

            base.WriteYaml(ref objectContext);
        }
    }
}
