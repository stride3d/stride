// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;

using Stride.Core.Shaders.Ast;

namespace Stride.Shaders.Parser.Mixins
{
    internal class CompositionDictionary : IDictionary<Variable, List<ModuleMixin>>
    {
        private Dictionary<Variable, int> Variables;

        private List<List<ModuleMixin>> Compositions;

        public CompositionDictionary()
        {
            Variables = new Dictionary<Variable, int>();
            Compositions = new List<List<ModuleMixin>>();
        }

        public IEnumerator<KeyValuePair<Variable, List<ModuleMixin>>> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<Variable, List<ModuleMixin>> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            Variables.Clear();
            Compositions.Clear();
        }

        public bool Contains(KeyValuePair<Variable, List<ModuleMixin>> item)
        {
            var index = Variables[item.Key];
            return ReferenceEquals(Compositions[index], item.Value);
        }

        public void CopyTo(KeyValuePair<Variable, List<ModuleMixin>>[] array, int arrayIndex)
        {
            for (var i = arrayIndex; i < array.Length; ++i)
                Add(array[i].Key, array[i].Value);
        }

        public bool Remove(KeyValuePair<Variable, List<ModuleMixin>> item)
        {
            // no need to implement that
            throw new NotImplementedException();
        }

        public int Count
        {
            get
            {
                return Variables.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public bool ContainsKey(Variable key)
        {
            return Variables.ContainsKey(key);
        }

        public void Add(Variable key, List<ModuleMixin> value)
        {
            var newIndex = Compositions.Count;
            Compositions.Add(value);
            Variables.Add(key, newIndex);
        }

        public bool Remove(Variable key)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(Variable key, out List<ModuleMixin> value)
        {
            int index;
            if (Variables.TryGetValue(key, out index) && index < Compositions.Count)
            {
                value = Compositions[index];
                return true;
            }
            value = null;
            return false;
        }

        public List<ModuleMixin> this[Variable key]
        {
            get
            {
                var index = Variables[key];
                return Compositions[index];
            }
            set
            {
                int index;
                if (Variables.TryGetValue(key, out index))
                    Compositions[index] = value;
                else
                    Add(key, value);
            }
        }

        public ICollection<Variable> Keys
        {
            get
            {
                return Variables.Keys;
            }
        }

        public ICollection<List<ModuleMixin>> Values
        {
            get
            {
                return Compositions;
            }
        }

        class Enumerator : IEnumerator<KeyValuePair<Variable, List<ModuleMixin>>>
        {
            private CompositionDictionary compositionDictionary;
            private IEnumerator<KeyValuePair<Variable, int>> enumerator;

            public Enumerator(CompositionDictionary dict)
            {
                compositionDictionary = dict;
                enumerator = compositionDictionary.Variables.GetEnumerator();
            }

            public void Dispose()
            {
                enumerator.Dispose();
                compositionDictionary = null;
            }

            public bool MoveNext()
            {
                return enumerator.MoveNext();
            }

            public void Reset()
            {
                enumerator.Reset();
            }

            public KeyValuePair<Variable, List<ModuleMixin>> Current
            {
                get
                {
                    var cur = enumerator.Current;
                    return new KeyValuePair<Variable, List<ModuleMixin>>(cur.Key, compositionDictionary[cur.Key]);
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }
        }
    }
}
