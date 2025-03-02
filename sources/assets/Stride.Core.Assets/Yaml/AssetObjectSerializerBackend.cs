// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Yaml;
using Stride.Core.Annotations;
using Stride.Core.Reflection;
using Stride.Core.Yaml.Events;
using Stride.Core.Yaml.Serialization;
using Stride.Core.Yaml.Serialization.Serializers;

namespace Stride.Core.Yaml;

/// <summary>
/// Internal class used when serializing/deserializing an object.
/// </summary>
public class AssetObjectSerializerBackend : DefaultObjectSerializerBackend
{
    private readonly ITypeDescriptorFactory typeDescriptorFactory;
    private static readonly PropertyKey<YamlAssetPath> MemberPathKey = new("MemberPath", typeof(AssetObjectSerializerBackend));
    public static readonly PropertyKey<YamlAssetMetadata<OverrideType>> OverrideDictionaryKey = new("OverrideDictionary", typeof(AssetObjectSerializerBackend));
    public static readonly PropertyKey<YamlAssetMetadata<Guid>> ObjectReferencesKey = new("ObjectReferences", typeof(AssetObjectSerializerBackend));

    public AssetObjectSerializerBackend(ITypeDescriptorFactory typeDescriptorFactory)
    {
        ArgumentNullException.ThrowIfNull(typeDescriptorFactory);
        this.typeDescriptorFactory = typeDescriptorFactory;
    }

    public override object ReadMemberValue(ref ObjectContext objectContext, IMemberDescriptor memberDescriptor, object memberValue, Type memberType)
    {
        var memberObjectContext = new ObjectContext(objectContext.SerializerContext, memberValue, objectContext.SerializerContext.FindTypeDescriptor(memberType), objectContext.Descriptor, memberDescriptor);

        if (memberDescriptor is MemberDescriptorBase member && objectContext.Settings.Attributes.GetAttribute<NonIdentifiableCollectionItemsAttribute>(member.MemberInfo) != null)
        {
            memberObjectContext.Properties.Add(CollectionWithIdsSerializerBase.NonIdentifiableCollectionItemsKey, true);
        }

        var path = GetCurrentPath(ref objectContext, true);
        path.PushMember(memberDescriptor.Name);
        SetCurrentPath(ref memberObjectContext, path);

        var result = ReadYaml(ref memberObjectContext);
        return result;
    }

    public override void WriteMemberValue(ref ObjectContext objectContext, IMemberDescriptor memberDescriptor, object memberValue, Type memberType)
    {
        var memberObjectContext = new ObjectContext(objectContext.SerializerContext, memberValue, objectContext.SerializerContext.FindTypeDescriptor(memberType), objectContext.Descriptor, memberDescriptor)
        {
            ScalarStyle = memberDescriptor.ScalarStyle,
        };

        // We allow compact style only for collection with non-identifiable items
        var allowCompactStyle = objectContext.SerializerContext.Properties.TryGetValue(CollectionWithIdsSerializerBase.NonIdentifiableCollectionItemsKey, out var nonIdentifiableItems) && nonIdentifiableItems;

        if (memberDescriptor is MemberDescriptorBase member && objectContext.Settings.Attributes.GetAttribute<NonIdentifiableCollectionItemsAttribute>(member.MemberInfo) != null)
        {
            memberObjectContext.Properties.Add(CollectionWithIdsSerializerBase.NonIdentifiableCollectionItemsKey, true);
            allowCompactStyle = true;
        }

        if (allowCompactStyle)
        {
            memberObjectContext.Style = memberDescriptor.Style;
        }

        var path = GetCurrentPath(ref objectContext, true);
        path.PushMember(memberDescriptor.Name);
        SetCurrentPath(ref memberObjectContext, path);

        WriteYaml(ref memberObjectContext);
    }

    public override string ReadMemberName(ref ObjectContext objectContext, string memberName, out bool skipMember)
    {
        var objectType = objectContext.Instance.GetType();

        var realMemberName = TrimAndParseOverride(memberName, out var overrideTypes);

        // For member names, we have a single override, so we always take the last one of the array (In case of legacy property serialized with ~Name)
        var overrideType = overrideTypes[^1];
        if (overrideType != OverrideType.Base)
        {
            if (!objectContext.SerializerContext.Properties.TryGetValue(OverrideDictionaryKey, out var overrides))
            {
                overrides = new YamlAssetMetadata<OverrideType>();
                objectContext.SerializerContext.Properties.Add(OverrideDictionaryKey, overrides);
            }

            var path = GetCurrentPath(ref objectContext, true);
            path.PushMember(realMemberName);
            overrides.Set(path, overrideType);
        }

        var resultMemberName = base.ReadMemberName(ref objectContext, realMemberName, out skipMember);
        return resultMemberName;
    }

    public override void WriteMemberName(ref ObjectContext objectContext, IMemberDescriptor member, string memberName)
    {
        // Replace the key with Stride.Core.Reflection IMemberDescriptor
        // Cache previous 
        if (member != null)
        {
            var customDescriptor = (IMemberDescriptor)member.Tag;
            if (customDescriptor == null)
            {
                customDescriptor = typeDescriptorFactory.Find(objectContext.Instance.GetType()).TryGetMember(memberName);
                member.Tag = customDescriptor;
            }

            if (customDescriptor != null)
            {
                if (objectContext.SerializerContext.Properties.TryGetValue(OverrideDictionaryKey, out var overrides))
                {
                    var path = GetCurrentPath(ref objectContext, true);
                    path.PushMember(memberName);

                    var overrideType = overrides.TryGet(path);
                    if ((overrideType & OverrideType.New) != 0)
                    {
                        memberName += OverridePostfixes.PostFixNew;
                    }
                    if ((overrideType & OverrideType.Sealed) != 0)
                    {
                        memberName += OverridePostfixes.PostFixSealed;
                    }
                }
            }
        }

        base.WriteMemberName(ref objectContext, member, memberName);
    }

    public override object ReadCollectionItem(ref ObjectContext objectContext, object value, Type itemType, int index)
    {
        var path = GetCurrentPath(ref objectContext, true);
        path.PushIndex(index);
        var itemObjectContext = new ObjectContext(objectContext.SerializerContext, value, objectContext.SerializerContext.FindTypeDescriptor(itemType));
        SetCurrentPath(ref itemObjectContext, path);
        return ReadYaml(ref itemObjectContext);
    }

    public override void WriteCollectionItem(ref ObjectContext objectContext, object item, Type itemType, int index)
    {
        var path = GetCurrentPath(ref objectContext, true);
        path.PushIndex(index);
        var itemObjectContext = new ObjectContext(objectContext.SerializerContext, item, objectContext.SerializerContext.FindTypeDescriptor(itemType));
        SetCurrentPath(ref itemObjectContext, path);
        WriteYaml(ref itemObjectContext);
    }

    public override object ReadDictionaryKey(ref ObjectContext objectContext, Type keyType)
    {
        var key = objectContext.Reader.Peek<Scalar>();
        var keyName = TrimAndParseOverride(key.Value, out var overrideTypes);
        key.Value = keyName;

        var keyValue = base.ReadDictionaryKey(ref objectContext, keyType);

        if (overrideTypes[0] != OverrideType.Base)
        {
            if (!objectContext.SerializerContext.Properties.TryGetValue(OverrideDictionaryKey, out var overrides))
            {
                overrides = new YamlAssetMetadata<OverrideType>();
                objectContext.SerializerContext.Properties.Add(OverrideDictionaryKey, overrides);
            }

            var path = GetCurrentPath(ref objectContext, true);
            if (YamlAssetPath.IsCollectionWithIdType(objectContext.Descriptor.Type, keyValue, out var id, out _))
            {
                path.PushItemId(id);
            }
            else
            {
                path.PushIndex(key);
            }
            overrides.Set(path, overrideTypes[0]);
        }

        if (overrideTypes.Length > 1 && overrideTypes[1] != OverrideType.Base)
        {
            if (YamlAssetPath.IsCollectionWithIdType(objectContext.Descriptor.Type, keyValue, out _, out var actualKey))
            {
                if (!objectContext.SerializerContext.Properties.TryGetValue(OverrideDictionaryKey, out var overrides))
                {
                    overrides = new YamlAssetMetadata<OverrideType>();
                    objectContext.SerializerContext.Properties.Add(OverrideDictionaryKey, overrides);
                }

                var path = GetCurrentPath(ref objectContext, true);
                path.PushIndex(actualKey);
                overrides.Set(path, overrideTypes[1]);
            }
        }

        return keyValue;
    }

    public override void WriteDictionaryKey(ref ObjectContext objectContext, object key, Type keyType)
    {
        if (objectContext.SerializerContext.Properties.TryGetValue(OverrideDictionaryKey, out var overrides))
        {
            var itemPath = GetCurrentPath(ref objectContext, true);
            YamlAssetPath? keyPath = null;
            if (YamlAssetPath.IsCollectionWithIdType(objectContext.Descriptor.Type, key, out var id, out var actualKey))
            {
                keyPath = itemPath.Clone();
                keyPath.PushIndex(actualKey);
                itemPath.PushItemId(id);
            }
            else
            {
                itemPath.PushIndex(key);
            }
            var overrideType = overrides.TryGet(itemPath);
            if ((overrideType & OverrideType.New) != 0)
            {
                objectContext.SerializerContext.Properties.Set(ItemIdSerializerBase.OverrideInfoKey, OverridePostfixes.PostFixNew.ToString());
            }
            if ((overrideType & OverrideType.Sealed) != 0)
            {
                objectContext.SerializerContext.Properties.Set(ItemIdSerializerBase.OverrideInfoKey, OverridePostfixes.PostFixSealed.ToString());
            }
            if (keyPath != null)
            {
                overrideType = overrides.TryGet(keyPath);
                if ((overrideType & OverrideType.New) != 0)
                {
                    objectContext.SerializerContext.Properties.Set(KeyWithIdSerializer.OverrideKeyInfoKey, OverridePostfixes.PostFixNew.ToString());
                }
                if ((overrideType & OverrideType.Sealed) != 0)
                {
                    objectContext.SerializerContext.Properties.Set(KeyWithIdSerializer.OverrideKeyInfoKey, OverridePostfixes.PostFixSealed.ToString());
                }
            }
        }
        base.WriteDictionaryKey(ref objectContext, key, keyType);
    }

    public override object ReadDictionaryValue(ref ObjectContext objectContext, Type valueType, object key)
    {
        var path = GetCurrentPath(ref objectContext, true);
        if (YamlAssetPath.IsCollectionWithIdType(objectContext.Descriptor.Type, key, out var id))
        {
            path.PushItemId(id);
        }
        else
        {
            path.PushIndex(key);
        }
        var valueObjectContext = new ObjectContext(objectContext.SerializerContext, null, objectContext.SerializerContext.FindTypeDescriptor(valueType));
        SetCurrentPath(ref valueObjectContext, path);
        return ReadYaml(ref valueObjectContext);
    }

    public override void WriteDictionaryValue(ref ObjectContext objectContext, object key, object value, Type valueType)
    {
        var path = GetCurrentPath(ref objectContext, true);
        if (YamlAssetPath.IsCollectionWithIdType(objectContext.Descriptor.Type, key, out var id))
        {
            path.PushItemId(id);
        }
        else
        {
            path.PushIndex(key);
        }
        var itemObjectContext = new ObjectContext(objectContext.SerializerContext, value, objectContext.SerializerContext.FindTypeDescriptor(valueType));
        SetCurrentPath(ref itemObjectContext, path);
        WriteYaml(ref itemObjectContext);
    }

    public override bool ShouldSerialize(IMemberDescriptor member, ref ObjectContext objectContext)
    {
        if (objectContext.SerializerContext.Properties.TryGetValue(OverrideDictionaryKey, out var overrides))
        {
            var path = GetCurrentPath(ref objectContext, true);
            path.PushMember(member.Name);

            var overrideType = overrides.TryGet(path);
            if (overrideType != OverrideType.Base)
                return true;
        }

        return base.ShouldSerialize(member, ref objectContext);
    }

    public static YamlAssetPath GetCurrentPath(ref ObjectContext objectContext, bool clone)
    {
        YamlAssetPath? path;
        path = objectContext.Properties.TryGetValue(MemberPathKey, out path) ? path : new YamlAssetPath();
        if (clone)
        {
            path = path.Clone();
        }
        return path;
    }

    private static void SetCurrentPath(ref ObjectContext objectContext, YamlAssetPath path)
    {
        objectContext.Properties.Set(MemberPathKey, path);
    }

    internal static string TrimAndParseOverride(string name, out OverrideType[] overrideTypes)
    {
        var split = name.Split('~');

        overrideTypes = new OverrideType[split.Length];
        int i = 0;
        var trimmedName = string.Empty;
        foreach (var namePart in split)
        {
            var realName = namePart.Trim(OverridePostfixes.PostFixSealed, OverridePostfixes.PostFixNew);

            var overrideType = OverrideType.Base;
            if (realName.Length != namePart.Length)
            {
                if (namePart.Contains(OverridePostfixes.PostFixNewSealed) || namePart.EndsWith(OverridePostfixes.PostFixNewSealedAlt, StringComparison.Ordinal))
                {
                    overrideType = OverrideType.New | OverrideType.Sealed;
                }
                else if (namePart.EndsWith(OverridePostfixes.PostFixNew))
                {
                    overrideType = OverrideType.New;
                }
                else if (namePart.EndsWith(OverridePostfixes.PostFixSealed))
                {
                    overrideType = OverrideType.Sealed;
                }
            }
            overrideTypes[i] = overrideType;
            if (i > 0)
                trimmedName += '~';
            trimmedName += realName;
            ++i;
        }
        return trimmedName;
    }
}
