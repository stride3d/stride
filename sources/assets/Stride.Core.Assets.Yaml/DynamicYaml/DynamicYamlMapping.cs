// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Stride.Core.Reflection;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Yaml
{
    /// <summary>
    /// Dynamic version of <see cref="YamlMappingNode"/>.
    /// </summary>
    public class DynamicYamlMapping : DynamicYamlObject, IDynamicYamlNode, IEnumerable
    {
        private readonly YamlMappingNode node;

        /// <summary>
        /// A mapping used between a property name (e.g: MyProperty) and the property name as serialized
        /// in YAML taking into account overrides (e.g: MyProperty! or MyProperty* or MyProperty*!)
        /// </summary>
        /// <remarls>
        /// NOTE that both <see cref="keyMapping"/> and <see cref="overrides"/> and node.Children
        /// are kept synchronized.
        /// </remarls>
        private Dictionary<string, string> keyMapping;

        /// <summary>
        /// A mapping between a property name (e.g: MyProperty) and the current Override type
        /// </summary>
        private Dictionary<string, OverrideType> overrides;

        public YamlMappingNode Node => node;

        YamlNode IDynamicYamlNode.Node => Node;

        public DynamicYamlMapping(YamlMappingNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            this.node = node;
            ParseOverrides();
        }

        public void AddChild(object key, object value)
        {
            var yamlKey = ConvertFromDynamicForKey(key);
            var yamlValue = ConvertFromDynamic(value);

            var keyPosition = node.Children.IndexOf(yamlKey);
            if (keyPosition != -1)
                return;

            node.Children.Add(yamlKey, yamlValue);
        }

        public void MoveChild(object key, int movePosition)
        {
            var yamlKey = ConvertFromDynamicForKey(key);
            var keyPosition = node.Children.IndexOf(yamlKey);

            if (keyPosition == movePosition)
                return;

            // Remove child
            var item = node.Children[keyPosition];
            node.Children.RemoveAt(keyPosition);

            // Adjust insertion position (if we insert in a position after the removal position)
            if (movePosition > keyPosition)
                movePosition--;

            // Insert item at new position
            node.Children.Insert(movePosition, item.Key, item.Value);
        }


        public bool ContainsChild(object key)
        {
            var yamlKey = ConvertFromDynamicForKey(key);
            var keyPosition = node.Children.IndexOf(yamlKey);

            return (keyPosition != -1);
        }

        public void RemoveChild(object key)
        {
            var yamlKey = ConvertFromDynamicForKey(key);
            var keyPosition = node.Children.IndexOf(yamlKey);
            if (keyPosition != -1)
            {
                node.Children.RemoveAt(keyPosition);

                // Removes any override information
                if (keyMapping != null && key is string)
                {
                    keyMapping.Remove((string)key);
                    overrides.Remove((string)key);
                }
            }
        }

        /// <summary>
        /// Renames a property to a new name while keeping all overrides and key mappings
        /// </summary>
        /// <param name="oldKey">Old property name</param>
        /// <param name="newKey">New property name</param>
        public void RenameChild(object oldKey, object newKey)
        {
            var yamlKey = ConvertFromDynamicForKey(oldKey);
            var keyPosition = node.Children.IndexOf(yamlKey);

            if (keyPosition < 0)
                return; // Key not found, nothing to do

            SetOverride(newKey.ToString(), GetOverride(oldKey.ToString()));

            AddChild(newKey, node.Children[keyPosition].Value);

            RemoveChild(oldKey);
        }

        /// <summary>
        /// Transfers ownership of a property to another parent object and removes it from the current one
        /// </summary>
        /// <param name="oldKey">Old property name</param>
        /// <param name="newParent">New owner for the property</param>
        /// <param name="newKey">New property name</param>
        public void TransferChild(object oldKey, object newParent, object newKey)
        {
            var yamlMapping = newParent as DynamicYamlMapping;
            if (yamlMapping == null)
                return;

            var yamlKey = ConvertFromDynamicForKey(oldKey);
            var keyPosition = node.Children.IndexOf(yamlKey);

            if (keyPosition < 0)
                return; // Key not found, nothing to do

            yamlMapping.SetOverride(newKey.ToString(), GetOverride(oldKey.ToString()));

            yamlMapping.AddChild(newKey, node.Children[keyPosition].Value);

            RemoveChild(oldKey);
        }

        public int IndexOf(object key)
        {
            var yamlKey = ConvertFromDynamicForKey(key);

            return node.Children.IndexOf(yamlKey);
        }

        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            if (binder.Type.IsAssignableFrom(node.GetType()))
            {
                result = node;
            }
            else
            {
                throw new InvalidOperationException();
            }
            return true;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            YamlNode tempNode;
            if (node.Children.TryGetValue(new YamlScalarNode(GetRealPropertyName(binder.Name)), out tempNode))
            {
                result = ConvertToDynamic(tempNode);
                return true;
            }
            result = null;
            // Probably not very good, but unfortunately we have some asset upgraders that relies on null check to check existence
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            var key = new YamlScalarNode(GetRealPropertyName(binder.Name));

            if (value is DynamicYamlEmpty)
                node.Children.Remove(key);
            else
                node.Children[key] = ConvertFromDynamic(value);
            return true;
        }

        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            var key = ConvertFromDynamicForKey(indexes[0]);
            if (value is DynamicYamlEmpty)
                node.Children.Remove(key);
            else
                node.Children[key] = ConvertFromDynamic(value);
            return true;
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            var key = ConvertFromDynamicForKey(indexes[0]);
            result = GetValue(key);
            return true;
        }

        /// <summary>
        /// Gets the override for the specified member.
        /// </summary>
        /// <param name="key">The member name to get the override</param>
        /// <returns>The type of override (if no override, return <see cref="OverrideType.Base"/></returns>
        public OverrideType GetOverride(string key)
        {
            if (overrides == null)
            {
                return OverrideType.Base;
            }

            OverrideType type;
            return overrides.TryGetValue(key, out type) ? type : OverrideType.Base;
        }

        /// <summary>
        /// Sets the override type for the specified member.
        /// </summary>
        /// <param name="key">The member name to setup an override</param>
        /// <param name="type">Type of the override</param>
        public void SetOverride(string key, OverrideType type)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            YamlNode previousMemberKey = null;
            YamlNode previousMemberValue = null;

            if (keyMapping == null)
            {
                keyMapping = new Dictionary<string, string>();
            }
            else
            {
                string previousMemberName;
                if (keyMapping.TryGetValue(key, out previousMemberName))
                {
                    previousMemberKey = new YamlScalarNode(previousMemberName);
                    node.Children.TryGetValue(previousMemberKey, out previousMemberValue);
                }
                keyMapping.Remove(key);
            }

            if (overrides == null)
            {
                overrides = new Dictionary<string, OverrideType>();
            }
            else
            {
                overrides.Remove(key);
            }

            // New member name
            var newMemberName = type == OverrideType.Base
                ? key
                : $"{key}{type.ToText()}";
            
            keyMapping[key] = newMemberName;
            overrides[key] = type;

            // Remap the original YAML node with the override type
            if (previousMemberKey != null)
            {
                int index = node.Children.IndexOf(previousMemberKey);
                node.Children.RemoveAt(index);
                node.Children.Insert(index, new YamlScalarNode(newMemberName), previousMemberValue);
            }
        }

        /// <summary>
        /// Removes an override information from the specified member.
        /// </summary>
        /// <param name="key">The member name</param>
        public void RemoveOverride(string key)
        {
            if (overrides == null)
            {
                return;
            }

            // If we have an override we need to remove the override text from the name
            if (overrides.Remove(key))
            {
                var propertyName = GetRealPropertyName(key);

                var previousKey = new YamlScalarNode(GetRealPropertyName(propertyName));
                int index = node.Children.IndexOf(previousKey);
                var previousMemberValue = node.Children[index].Value;
                node.Children.RemoveAt(index);
                node.Children.Insert(index, new YamlScalarNode(key), previousMemberValue);

                keyMapping[key] = key;
            }
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return node.Children.Select(x => new KeyValuePair<dynamic, dynamic>(ConvertToDynamic(x.Key), ConvertToDynamic(x.Value))).ToArray().GetEnumerator();
        }

        /// <summary>
        /// Helper method to gets the real member key for the specified key (taking into account overrides)
        /// </summary>
        /// <param name="key">The member name</param>
        /// <returns>A member YamlNode</returns>
        private YamlNode ConvertFromDynamicForKey(object key)
        {
            if (key is string)
            {
                key = GetRealPropertyName((string)key);
            }
            return ConvertFromDynamic(key);
        }

        private object GetValue(YamlNode key)
        {
            YamlNode result;
            if (node.Children.TryGetValue(key, out result))
            {
                return ConvertToDynamic(result);
            }
            return null;
        }

        private string GetRealPropertyName(string name)
        {
            if (keyMapping == null)
            {
                return name;
            }

            string realPropertyName;
            if (keyMapping.TryGetValue(name, out realPropertyName))
            {
                return realPropertyName;
            }
            return name;
        }

        /// <summary>
        /// This method will extract overrides information and maintain a separate dictionary to ensure mapping between
        /// a full property name without override (MyProperty) and with its override (e.g: MyProperty! for sealed MyProperty)
        /// </summary>
        private void ParseOverrides()
        {
            foreach (var keyValue in node)
            {
                var scalar = keyValue.Key as YamlScalarNode;
                if (scalar?.Value != null)
                {
                    var isPostFixNew = scalar.Value.EndsWith(OverridePostfixes.PostFixNewText);
                    var isPostFixSealed = scalar.Value.EndsWith(OverridePostfixes.PostFixSealedText);
                    if (isPostFixNew || isPostFixSealed)
                    {
                        var name = scalar.Value;
                        var type = isPostFixNew ? OverrideType.New : OverrideType.Sealed;

                        var isPostFixNewSealedAlt = name.EndsWith(OverridePostfixes.PostFixNewSealedAlt);
                        var isPostFixNewSealed = name.EndsWith(OverridePostfixes.PostFixNewSealed);
                        if (isPostFixNewSealed || isPostFixNewSealedAlt)
                        {
                            type = OverrideType.New | OverrideType.Sealed;
                            name = name.Substring(0, name.Length - 2);
                        }
                        else
                        {
                            name = name.Substring(0, name.Length - 1);
                        }
                        if (keyMapping == null)
                        {
                            keyMapping = new Dictionary<string, string>();
                        }

                        keyMapping[name] = scalar.Value;

                        if (overrides == null)
                        {
                            overrides = new Dictionary<string, OverrideType>();
                        }
                        overrides[name] = type;
                    }
                }
            }
        }
    }
}
