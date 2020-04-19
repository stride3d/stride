// Copyright (c) 2015 SharpYaml - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// -------------------------------------------------------------------------------
// SharpYaml is a fork of YamlDotNet https://github.com/aaubry/YamlDotNet
// published with the following license:
// -------------------------------------------------------------------------------
// 
// Copyright (c) 2008, 2009, 2010, 2011, 2012 Antoine Aubry
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
// of the Software, and to permit persons to whom the Software is furnished to do
// so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Stride.Core.Yaml
{
    [DebuggerDisplay("Count = {Count}")]
    internal class SortedDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDictionary
    {
        private KeyCollection keys;

        private ValueCollection values;

        private readonly TreeSet<KeyValuePair<TKey, TValue>> _set;

        public SortedDictionary()
            : this((IComparer<TKey>) null)
        {
        }

        public SortedDictionary(IDictionary<TKey, TValue> dictionary)
            : this(dictionary, null)
        {
        }

        public SortedDictionary(IDictionary<TKey, TValue> dictionary, IComparer<TKey> comparer)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException();
            }

            _set = new TreeSet<KeyValuePair<TKey, TValue>>(new KeyValuePairComparer(comparer));

            foreach (KeyValuePair<TKey, TValue> pair in dictionary)
            {
                _set.Add(pair);
            }
        }

        public SortedDictionary(IComparer<TKey> comparer)
        {
            _set = new TreeSet<KeyValuePair<TKey, TValue>>(new KeyValuePairComparer(comparer));
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> keyValuePair)
        {
            _set.Add(keyValuePair);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> keyValuePair)
        {
            TreeSet<KeyValuePair<TKey, TValue>>.Node node = _set.FindNode(keyValuePair);
            if (node == null)
            {
                return false;
            }

            if (keyValuePair.Value == null)
            {
                return node.Item.Value == null;
            }
            else
            {
                return EqualityComparer<TValue>.Default.Equals(node.Item.Value, keyValuePair.Value);
            }
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> keyValuePair)
        {
            TreeSet<KeyValuePair<TKey, TValue>>.Node node = _set.FindNode(keyValuePair);
            if (node == null)
            {
                return false;
            }

            if (EqualityComparer<TValue>.Default.Equals(node.Item.Value, keyValuePair.Value))
            {
                _set.Remove(keyValuePair);
                return true;
            }
            return false;
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly { get { return false; } }

        public TValue this[TKey key]
        {
            get
            {
                if (key == null)
                {
                    throw new ArgumentNullException();
                }

                TreeSet<KeyValuePair<TKey, TValue>>.Node node =
                    _set.FindNode(new KeyValuePair<TKey, TValue>(key, default(TValue)));
                if (node == null)
                {
                    throw new KeyNotFoundException();
                }

                return node.Item.Value;
            }
            set
            {
                if (key == null)
                {
                    throw new ArgumentNullException();
                }

                TreeSet<KeyValuePair<TKey, TValue>>.Node node =
                    _set.FindNode(new KeyValuePair<TKey, TValue>(key, default(TValue)));
                if (node == null)
                {
                    _set.Add(new KeyValuePair<TKey, TValue>(key, value));
                }
                else
                {
                    node.Item = new KeyValuePair<TKey, TValue>(node.Item.Key, value);
                    _set.UpdateVersion();
                }
            }
        }

        public int Count { get { return _set.Count; } }

        public IComparer<TKey> Comparer { get { return ((KeyValuePairComparer) _set.Comparer).keyComparer; } }

        public KeyCollection Keys
        {
            get
            {
                if (keys == null)
                    keys = new KeyCollection(this);
                return keys;
            }
        }

        ICollection<TKey> IDictionary<TKey, TValue>.Keys { get { return Keys; } }

        public ValueCollection Values
        {
            get
            {
                if (values == null)
                    values = new ValueCollection(this);
                return values;
            }
        }

        ICollection<TValue> IDictionary<TKey, TValue>.Values { get { return Values; } }

        public void Add(TKey key, TValue value)
        {
            if (key == null)
            {
                throw new ArgumentNullException();
            }
            _set.Add(new KeyValuePair<TKey, TValue>(key, value));
        }

        public void Clear()
        {
            _set.Clear();
        }

        public bool ContainsKey(TKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException();
            }

            return _set.Contains(new KeyValuePair<TKey, TValue>(key, default(TValue)));
        }

        public bool ContainsValue(TValue value)
        {
            bool found = false;
            if (value == null)
            {
                _set.InOrderTreeWalk(delegate(TreeSet<KeyValuePair<TKey, TValue>>.Node node)
                {
                    if (node.Item.Value == null)
                    {
                        found = true;
                        return false; // stop the walk
                    }
                    return true;
                });
            }
            else
            {
                EqualityComparer<TValue> valueComparer = EqualityComparer<TValue>.Default;
                _set.InOrderTreeWalk(delegate(TreeSet<KeyValuePair<TKey, TValue>>.Node node)
                {
                    if (valueComparer.Equals(node.Item.Value, value))
                    {
                        found = true;
                        return false; // stop the walk
                    }
                    return true;
                });
            }
            return found;
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
        {
            _set.CopyTo(array, index);
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this, Enumerator.KeyValuePair);
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return new Enumerator(this, Enumerator.KeyValuePair);
        }

        public bool Remove(TKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException();
            }

            return _set.Remove(new KeyValuePair<TKey, TValue>(key, default(TValue)));
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (key == null)
            {
                throw new ArgumentNullException();
            }

            TreeSet<KeyValuePair<TKey, TValue>>.Node node =
                _set.FindNode(new KeyValuePair<TKey, TValue>(key, default(TValue)));
            if (node == null)
            {
                value = default(TValue);
                return false;
            }
            value = node.Item.Value;
            return true;
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection) _set).CopyTo(array, index);
        }

        bool IDictionary.IsFixedSize { get { return false; } }

        bool IDictionary.IsReadOnly { get { return false; } }

        ICollection IDictionary.Keys { get { return (ICollection) Keys; } }

        ICollection IDictionary.Values { get { return (ICollection) Values; } }

        object IDictionary.this[object key]
        {
            get
            {
                if (IsCompatibleKey(key))
                {
                    TValue value;
                    if (TryGetValue((TKey) key, out value))
                    {
                        return value;
                    }
                }
                return null;
            }
            set
            {
                VerifyKey(key);
                VerifyValueType(value);
                this[(TKey) key] = (TValue) value;
            }
        }

        void IDictionary.Add(object key, object value)
        {
            VerifyKey(key);
            VerifyValueType(value);
            Add((TKey) key, (TValue) value);
        }

        bool IDictionary.Contains(object key)
        {
            if (IsCompatibleKey(key))
            {
                return ContainsKey((TKey) key);
            }
            return false;
        }

        private static void VerifyKey(object key)
        {
            if (key == null)
            {
                throw new ArgumentNullException();
            }

            if (!(key is TKey))
            {
                throw new ArgumentException();
            }
        }

        private static bool IsCompatibleKey(object key)
        {
            if (key == null)
            {
                throw new ArgumentNullException();
            }

            return (key is TKey);
        }

        private static void VerifyValueType(object value)
        {
            if ((value is TValue) || (value == null && !typeof(TValue).IsValueType))
            {
                return;
            }
            throw new ArgumentNullException();
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return new Enumerator(this, Enumerator.DictEntry);
        }

        void IDictionary.Remove(object key)
        {
            if (IsCompatibleKey(key))
            {
                Remove((TKey) key);
            }
        }

        bool ICollection.IsSynchronized { get { return false; } }

        object ICollection.SyncRoot { get { return ((ICollection) _set).SyncRoot; } }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this, Enumerator.KeyValuePair);
        }

        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IDictionaryEnumerator
        {
            private TreeSet<KeyValuePair<TKey, TValue>>.Enumerator treeEnum;
            private readonly int getEnumeratorRetType; // What should Enumerator.Current return?

            internal const int KeyValuePair = 1;
            internal const int DictEntry = 2;

            internal Enumerator(SortedDictionary<TKey, TValue> dictionary, int getEnumeratorRetType)
            {
                treeEnum = dictionary._set.GetEnumerator();
                this.getEnumeratorRetType = getEnumeratorRetType;
            }

            public bool MoveNext()
            {
                return treeEnum.MoveNext();
            }

            public void Dispose()
            {
                treeEnum.Dispose();
            }

            public KeyValuePair<TKey, TValue> Current { get { return treeEnum.Current; } }

            internal bool NotStartedOrEnded { get { return treeEnum.NotStartedOrEnded; } }

            internal void Reset()
            {
                treeEnum.Reset();
            }


            void IEnumerator.Reset()
            {
                treeEnum.Reset();
            }

            object IEnumerator.Current
            {
                get
                {
                    if (NotStartedOrEnded)
                    {
                        throw new InvalidOperationException();
                    }

                    if (getEnumeratorRetType == DictEntry)
                    {
                        return new DictionaryEntry(Current.Key, Current.Value);
                    }
                    else
                    {
                        return new KeyValuePair<TKey, TValue>(Current.Key, Current.Value);
                    }
                }
            }

            object IDictionaryEnumerator.Key
            {
                get
                {
                    if (NotStartedOrEnded)
                    {
                        throw new InvalidOperationException();
                    }

                    return Current.Key;
                }
            }

            object IDictionaryEnumerator.Value
            {
                get
                {
                    if (NotStartedOrEnded)
                    {
                        throw new InvalidOperationException();
                    }

                    return Current.Value;
                }
            }

            DictionaryEntry IDictionaryEnumerator.Entry
            {
                get
                {
                    if (NotStartedOrEnded)
                    {
                        throw new InvalidOperationException();
                    }

                    return new DictionaryEntry(Current.Key, Current.Value);
                }
            }
        }

        public sealed class KeyCollection : ICollection<TKey>, ICollection
        {
            private readonly SortedDictionary<TKey, TValue> dictionary;

            public KeyCollection(SortedDictionary<TKey, TValue> dictionary)
            {
                if (dictionary == null)
                {
                    throw new ArgumentNullException();
                }
                this.dictionary = dictionary;
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(dictionary);
            }

            IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
            {
                return new Enumerator(dictionary);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return new Enumerator(dictionary);
            }

            public void CopyTo(TKey[] array, int index)
            {
                if (array == null)
                {
                    throw new ArgumentNullException();
                }

                if (index < 0)
                {
                    throw new ArgumentOutOfRangeException();
                }

                if (array.Length - index < Count)
                {
                    throw new ArgumentException();
                }

                dictionary._set.InOrderTreeWalk(delegate(TreeSet<KeyValuePair<TKey, TValue>>.Node node)
                {
                    array[index++] = node.Item.Key;
                    return true;
                });
            }

            void ICollection.CopyTo(Array array, int index)
            {
                if (array == null)
                {
                    throw new ArgumentNullException();
                }

                if (array.Rank != 1)
                {
                    throw new ArgumentException();
                }

                if (array.GetLowerBound(0) != 0)
                {
                    throw new ArgumentException();
                }

                if (index < 0)
                {
                    throw new ArgumentOutOfRangeException();
                }

                if (array.Length - index < dictionary.Count)
                {
                    throw new ArgumentException();
                }

                TKey[] keys = array as TKey[];
                if (keys != null)
                {
                    CopyTo(keys, index);
                }
                else
                {
                    object[] objects = (object[]) array;
                    if (objects == null)
                    {
                        throw new ArgumentException();
                    }

                    try
                    {
                        dictionary._set.InOrderTreeWalk(delegate(TreeSet<KeyValuePair<TKey, TValue>>.Node node)
                        {
                            objects[index++] = node.Item.Key;
                            return true;
                        });
                    }
                    catch (ArrayTypeMismatchException)
                    {
                        throw new ArgumentException();
                    }
                }
            }

            public int Count { get { return dictionary.Count; } }

            bool ICollection<TKey>.IsReadOnly { get { return true; } }

            void ICollection<TKey>.Add(TKey item)
            {
                throw new NotSupportedException();
            }

            void ICollection<TKey>.Clear()
            {
                throw new NotSupportedException();
            }

            bool ICollection<TKey>.Contains(TKey item)
            {
                return dictionary.ContainsKey(item);
            }

            bool ICollection<TKey>.Remove(TKey item)
            {
                throw new NotSupportedException();
            }

            bool ICollection.IsSynchronized { get { return false; } }

            Object ICollection.SyncRoot { get { return ((ICollection) dictionary).SyncRoot; } }

            public struct Enumerator : IEnumerator<TKey>, IEnumerator
            {
                private SortedDictionary<TKey, TValue>.Enumerator dictEnum;

                internal Enumerator(SortedDictionary<TKey, TValue> dictionary)
                {
                    dictEnum = dictionary.GetEnumerator();
                }

                public void Dispose()
                {
                    dictEnum.Dispose();
                }

                public bool MoveNext()
                {
                    return dictEnum.MoveNext();
                }

                public TKey Current { get { return dictEnum.Current.Key; } }

                object IEnumerator.Current
                {
                    get
                    {
                        if (dictEnum.NotStartedOrEnded)
                        {
                            throw new InvalidOperationException();
                        }

                        return Current;
                    }
                }

                void IEnumerator.Reset()
                {
                    dictEnum.Reset();
                }
            }
        }

        public sealed class ValueCollection : ICollection<TValue>, ICollection
        {
            private readonly SortedDictionary<TKey, TValue> dictionary;

            public ValueCollection(SortedDictionary<TKey, TValue> dictionary)
            {
                if (dictionary == null)
                {
                    throw new ArgumentNullException();
                }
                this.dictionary = dictionary;
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(dictionary);
            }

            IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
            {
                return new Enumerator(dictionary);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return new Enumerator(dictionary);
            }

            public void CopyTo(TValue[] array, int index)
            {
                if (array == null)
                {
                    throw new ArgumentNullException();
                }

                if (index < 0)
                {
                    throw new ArgumentOutOfRangeException();
                }

                if (array.Length - index < Count)
                {
                    throw new NotSupportedException();
                }

                dictionary._set.InOrderTreeWalk(delegate(TreeSet<KeyValuePair<TKey, TValue>>.Node node)
                {
                    array[index++] = node.Item.Value;
                    return true;
                });
            }

            void ICollection.CopyTo(Array array, int index)
            {
                if (array == null)
                {
                    throw new ArgumentException();
                }

                if (array.Rank != 1)
                {
                    throw new ArgumentException();
                }

                if (array.GetLowerBound(0) != 0)
                {
                    throw new ArgumentException();
                }

                if (index < 0)
                {
                    throw new ArgumentOutOfRangeException();
                }

                if (array.Length - index < dictionary.Count)
                {
                    throw new ArgumentException();
                }

                TValue[] values = array as TValue[];
                if (values != null)
                {
                    CopyTo(values, index);
                }
                else
                {
                    object[] objects = (object[]) array;
                    if (objects == null)
                    {
                        throw new ArgumentException();
                    }

                    try
                    {
                        dictionary._set.InOrderTreeWalk(delegate(TreeSet<KeyValuePair<TKey, TValue>>.Node node)
                        {
                            objects[index++] = node.Item.Value;
                            return true;
                        });
                    }
                    catch (ArrayTypeMismatchException)
                    {
                        throw new ArgumentException();
                    }
                }
            }

            public int Count { get { return dictionary.Count; } }

            bool ICollection<TValue>.IsReadOnly { get { return true; } }

            void ICollection<TValue>.Add(TValue item)
            {
                throw new NotSupportedException();
            }

            void ICollection<TValue>.Clear()
            {
                throw new NotSupportedException();
            }

            bool ICollection<TValue>.Contains(TValue item)
            {
                return dictionary.ContainsValue(item);
            }

            bool ICollection<TValue>.Remove(TValue item)
            {
                throw new NotSupportedException();
            }

            bool ICollection.IsSynchronized { get { return false; } }

            Object ICollection.SyncRoot { get { return ((ICollection) dictionary).SyncRoot; } }

            public struct Enumerator : IEnumerator<TValue>, IEnumerator
            {
                private SortedDictionary<TKey, TValue>.Enumerator dictEnum;

                internal Enumerator(SortedDictionary<TKey, TValue> dictionary)
                {
                    dictEnum = dictionary.GetEnumerator();
                }

                public void Dispose()
                {
                    dictEnum.Dispose();
                }

                public bool MoveNext()
                {
                    return dictEnum.MoveNext();
                }

                public TValue Current { get { return dictEnum.Current.Value; } }

                object IEnumerator.Current
                {
                    get
                    {
                        if (dictEnum.NotStartedOrEnded)
                        {
                            throw new InvalidOperationException();
                        }

                        return Current;
                    }
                }

                void IEnumerator.Reset()
                {
                    dictEnum.Reset();
                }
            }
        }

        internal class KeyValuePairComparer : Comparer<KeyValuePair<TKey, TValue>>
        {
            internal IComparer<TKey> keyComparer;

            public KeyValuePairComparer(IComparer<TKey> keyComparer)
            {
                if (keyComparer == null)
                {
                    this.keyComparer = Comparer<TKey>.Default;
                }
                else
                {
                    this.keyComparer = keyComparer;
                }
            }

            public override int Compare(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y)
            {
                return keyComparer.Compare(x.Key, y.Key);
            }
        }
    }

    //
    // A binary search tree is a red-black tree if it satifies the following red-black properties:
    // 1. Every node is either red or black
    // 2. Every leaf (nil node) is black
    // 3. If a node is red, the both its children are black
    // 4. Every simple path from a node to a descendant leaf contains the same number of black nodes
    // 
    // The basic idea of red-black tree is to represent 2-3-4 trees as standard BSTs but to add one extra bit of information  
    // per node to encode 3-nodes and 4-nodes. 
    // 4-nodes will be represented as:		  B
    //															  R			R
    // 3 -node will be represented as:		   B			 or		 B	 
    //															  R		  B			   B	   R
    // 
    // For a detailed description of the algorithm, take a look at "Algorithm" by Rebert Sedgewick.
    //

    internal delegate bool TreeWalkAction<T>(TreeSet<T>.Node node);

    internal enum TreeRotation
    {
        LeftRotation = 1,
        RightRotation = 2,
        RightLeftRotation = 3,
        LeftRightRotation = 4,
    }

    internal class TreeSet<T> : ICollection<T>, ICollection
    {
        private Node root;
        private readonly IComparer<T> comparer;
        private int count;
        private int version;
        private Object _syncRoot;

        private const String ComparerName = "Comparer";
        private const String CountName = "Count";
        private const String ItemsName = "Items";
        private const String VersionName = "Version";

        public TreeSet(IComparer<T> comparer)
        {
            this.comparer = comparer ?? Comparer<T>.Default;
        }

        public int Count { get { return count; } }

        public IComparer<T> Comparer { get { return comparer; } }

        bool ICollection<T>.IsReadOnly { get { return false; } }

        bool ICollection.IsSynchronized { get { return false; } }

        object ICollection.SyncRoot
        {
            get
            {
                if (_syncRoot == null)
                {
                    System.Threading.Interlocked.CompareExchange(ref _syncRoot, new Object(), null);
                }
                return _syncRoot;
            }
        }

        public void Add(T item)
        {
            if (root == null)
            {
                // empty tree
                root = new Node(item, false);
                count = 1;
                return;
            }

            //
            // Search for a node at bottom to insert the new node. 
            // If we can guanratee the node we found is not a 4-node, it would be easy to do insertion.
            // We split 4-nodes along the search path.
            // 
            Node current = root;
            Node parent = null;
            Node grandParent = null;
            Node greatGrandParent = null;

            int order = 0;
            while (current != null)
            {
                order = comparer.Compare(item, current.Item);
                if (order == 0)
                {
                    // We could have changed root node to red during the search process.
                    // We need to set it to black before we return.
                    root.IsRed = false;
                    throw new ArgumentException();
                }

                // split a 4-node into two 2-nodes				
                if (Is4Node(current))
                {
                    Split4Node(current);
                    // We could have introduced two consecutive red nodes after split. Fix that by rotation.
                    if (IsRed(parent))
                    {
                        InsertionBalance(current, ref parent, grandParent, greatGrandParent);
                    }
                }
                greatGrandParent = grandParent;
                grandParent = parent;
                parent = current;
                current = (order < 0) ? current.Left : current.Right;
            }

            Debug.Assert(parent != null, "Parent node cannot be null here!");
            // ready to insert the new node
            Node node = new Node(item);
            if (order > 0)
            {
                parent.Right = node;
            }
            else
            {
                parent.Left = node;
            }

            // the new node will be red, so we will need to adjust the colors if parent node is also red
            if (parent.IsRed)
            {
                InsertionBalance(node, ref parent, grandParent, greatGrandParent);
            }

            // Root node is always black
            root.IsRed = false;
            ++count;
            ++version;
        }

        public void Clear()
        {
            root = null;
            count = 0;
            ++version;
        }

        public bool Contains(T item)
        {
            return FindNode(item) != null;
        }

        //
        // Do a in order walk on tree and calls the delegate for each node.
        // If the action delegate returns false, stop the walk.
        // 
        // Return true if the entire tree has been walked. 
        // Otherwise returns false.
        //
        internal bool InOrderTreeWalk(TreeWalkAction<T> action)
        {
            if (root == null)
            {
                return true;
            }

            // The maximum height of a red-black tree is 2*lg(n+1).
            // See page 264 of "Introduction to algorithms" by Thomas H. Cormen
            Stack<Node> stack = new Stack<Node>(2*(int) Math.Log(Count + 1));
            Node current = root;
            while (current != null)
            {
                stack.Push(current);
                current = current.Left;
            }

            while (stack.Count != 0)
            {
                current = stack.Pop();
                if (!action(current))
                {
                    return false;
                }

                Node node = current.Right;
                while (node != null)
                {
                    stack.Push(node);
                    node = node.Left;
                }
            }
            return true;
        }

        public void CopyTo(T[] array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException();
            }

            if (index < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            if (array.Length - index < Count)
            {
                throw new ArgumentException();
            }

            InOrderTreeWalk(delegate(Node node)
            {
                array[index++] = node.Item;
                return true;
            });
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException();
            }

            if (array.Rank != 1)
            {
                throw new ArgumentException();
            }

            if (array.GetLowerBound(0) != 0)
            {
                throw new ArgumentException();
            }

            if (index < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            if (array.Length - index < Count)
            {
                throw new ArgumentException();
            }

            T[] tarray = array as T[];
            if (tarray != null)
            {
                CopyTo(tarray, index);
            }
            else
            {
                object[] objects = array as object[];
                if (objects == null)
                {
                    throw new ArgumentException();
                }

                try
                {
                    InOrderTreeWalk(delegate(Node node)
                    {
                        objects[index++] = node.Item;
                        return true;
                    });
                }
                catch (ArrayTypeMismatchException)
                {
                    throw new ArgumentException();
                }
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        internal Node FindNode(T item)
        {
            Node current = root;
            while (current != null)
            {
                int order = comparer.Compare(item, current.Item);
                if (order == 0)
                {
                    return current;
                }
                else
                {
                    current = (order < 0) ? current.Left : current.Right;
                }
            }

            return null;
        }

        public bool Remove(T item)
        {
            if (root == null)
            {
                return false;
            }

            // Search for a node and then find its succesor. 
            // Then copy the item from the succesor to the matching node and delete the successor. 
            // If a node doesn't have a successor, we can replace it with its left child (if not empty.) 
            // or delete the matching node.
            // 
            // In top-down implementation, it is important to make sure the node to be deleted is not a 2-node.
            // Following code will make sure the node on the path is not a 2 Node. 
            // 
            Node current = root;
            Node parent = null;
            Node grandParent = null;
            Node match = null;
            Node parentOfMatch = null;
            bool foundMatch = false;
            while (current != null)
            {
                if (Is2Node(current))
                {
                    // fix up 2-Node
                    if (parent == null)
                    {
                        // current is root. Mark it as red
                        current.IsRed = true;
                    }
                    else
                    {
                        Node sibling = GetSibling(current, parent);
                        if (sibling.IsRed)
                        {
                            // If parent is a 3-node, flip the orientation of the red link. 
                            // We can acheive this by a single rotation		
                            // This case is converted to one of other cased below.
                            Debug.Assert(!parent.IsRed, "parent must be a black node!");
                            if (parent.Right == sibling)
                            {
                                RotateLeft(parent);
                            }
                            else
                            {
                                RotateRight(parent);
                            }

                            parent.IsRed = true;
                            sibling.IsRed = false; // parent's color
                            // sibling becomes child of grandParent or root after rotation. Update link from grandParent or root
                            ReplaceChildOfNodeOrRoot(grandParent, parent, sibling);
                            // sibling will become grandParent of current node 
                            grandParent = sibling;
                            if (parent == match)
                            {
                                parentOfMatch = sibling;
                            }

                            // update sibling, this is necessary for following processing
                            sibling = (parent.Left == current) ? parent.Right : parent.Left;
                        }
                        Debug.Assert(sibling != null || sibling.IsRed == false,
                            "sibling must not be null and it must be black!");

                        if (Is2Node(sibling))
                        {
                            Merge2Nodes(parent, current, sibling);
                        }
                        else
                        {
                            // current is a 2-node and sibling is either a 3-node or a 4-node.
                            // We can change the color of current to red by some rotation.
                            TreeRotation rotation = RotationNeeded(parent, current, sibling);
                            Node newGrandParent = null;
                            switch (rotation)
                            {
                                case TreeRotation.RightRotation:
                                    Debug.Assert(parent.Left == sibling, "sibling must be left child of parent!");
                                    Debug.Assert(sibling.Left.IsRed, "Left child of sibling must be red!");
                                    sibling.Left.IsRed = false;
                                    newGrandParent = RotateRight(parent);
                                    break;
                                case TreeRotation.LeftRotation:
                                    Debug.Assert(parent.Right == sibling, "sibling must be left child of parent!");
                                    Debug.Assert(sibling.Right.IsRed, "Right child of sibling must be red!");
                                    sibling.Right.IsRed = false;
                                    newGrandParent = RotateLeft(parent);
                                    break;

                                case TreeRotation.RightLeftRotation:
                                    Debug.Assert(parent.Right == sibling, "sibling must be left child of parent!");
                                    Debug.Assert(sibling.Left.IsRed, "Left child of sibling must be red!");
                                    newGrandParent = RotateRightLeft(parent);
                                    break;

                                case TreeRotation.LeftRightRotation:
                                    Debug.Assert(parent.Left == sibling, "sibling must be left child of parent!");
                                    Debug.Assert(sibling.Right.IsRed, "Right child of sibling must be red!");
                                    newGrandParent = RotateLeftRight(parent);
                                    break;
                            }

                            newGrandParent.IsRed = parent.IsRed;
                            parent.IsRed = false;
                            current.IsRed = true;
                            ReplaceChildOfNodeOrRoot(grandParent, parent, newGrandParent);
                            if (parent == match)
                            {
                                parentOfMatch = newGrandParent;
                            }
                            grandParent = newGrandParent;
                        }
                    }
                }

                // we don't need to compare any more once we found the match
                int order = foundMatch ? -1 : comparer.Compare(item, current.Item);
                if (order == 0)
                {
                    // save the matching node
                    foundMatch = true;
                    match = current;
                    parentOfMatch = parent;
                }

                grandParent = parent;
                parent = current;

                if (order < 0)
                {
                    current = current.Left;
                }
                else
                {
                    current = current.Right; // continue the search in  right sub tree after we find a match
                }
            }

            // move successor to the matching node position and replace links
            if (match != null)
            {
                ReplaceNode(match, parentOfMatch, parent, grandParent);
                --count;
            }

            if (root != null)
            {
                root.IsRed = false;
            }
            ++version;
            return foundMatch;
        }


        private static Node GetSibling(Node node, Node parent)
        {
            if (parent.Left == node)
            {
                return parent.Right;
            }
            return parent.Left;
        }

        // After calling InsertionBalance, we need to make sure current and parent up-to-date.
        // It doesn't matter if we keep grandParent and greatGrantParent up-to-date 
        // because we won't need to split again in the next node.
        // By the time we need to split again, everything will be correctly set.
        //  
        private void InsertionBalance(Node current, ref Node parent, Node grandParent, Node greatGrandParent)
        {
            Debug.Assert(grandParent != null, "Grand parent cannot be null here!");
            bool parentIsOnRight = (grandParent.Right == parent);
            bool currentIsOnRight = (parent.Right == current);

            Node newChildOfGreatGrandParent;
            if (parentIsOnRight == currentIsOnRight)
            {
                // same orientation, single rotation
                newChildOfGreatGrandParent = currentIsOnRight ? RotateLeft(grandParent) : RotateRight(grandParent);
            }
            else
            {
                // different orientaton, double rotation
                newChildOfGreatGrandParent = currentIsOnRight
                    ? RotateLeftRight(grandParent)
                    : RotateRightLeft(grandParent);
                // current node now becomes the child of greatgrandparent 
                parent = greatGrandParent;
            }
            // grand parent will become a child of either parent of current.
            grandParent.IsRed = true;
            newChildOfGreatGrandParent.IsRed = false;

            ReplaceChildOfNodeOrRoot(greatGrandParent, grandParent, newChildOfGreatGrandParent);
        }

        private static bool Is2Node(Node node)
        {
            Debug.Assert(node != null, "node cannot be null!");
            return IsBlack(node) && IsNullOrBlack(node.Left) && IsNullOrBlack(node.Right);
        }

        private static bool Is4Node(Node node)
        {
            return IsRed(node.Left) && IsRed(node.Right);
        }

        private static bool IsBlack(Node node)
        {
            return (node != null && !node.IsRed);
        }

        private static bool IsNullOrBlack(Node node)
        {
            return (node == null || !node.IsRed);
        }

        private static bool IsRed(Node node)
        {
            return (node != null && node.IsRed);
        }

        private static void Merge2Nodes(Node parent, Node child1, Node child2)
        {
            Debug.Assert(IsRed(parent), "parent must be be red");
            // combing two 2-nodes into a 4-node
            parent.IsRed = false;
            child1.IsRed = true;
            child2.IsRed = true;
        }

        // Replace the child of a parent node. 
        // If the parent node is null, replace the root.
        private void ReplaceChildOfNodeOrRoot(Node parent, Node child, Node newChild)
        {
            if (parent != null)
            {
                if (parent.Left == child)
                {
                    parent.Left = newChild;
                }
                else
                {
                    parent.Right = newChild;
                }
            }
            else
            {
                root = newChild;
            }
        }

        // Replace the matching node with its succesor.
        private void ReplaceNode(Node match, Node parentOfMatch, Node succesor, Node parentOfSuccesor)
        {
            if (succesor == match)
            {
                // this node has no successor, should only happen if right child of matching node is null.
                Debug.Assert(match.Right == null, "Right child must be null!");
                succesor = match.Left;
            }
            else
            {
                Debug.Assert(parentOfSuccesor != null, "parent of successor cannot be null!");
                Debug.Assert(succesor.Left == null, "Left child of succesor must be null!");
                Debug.Assert((succesor.Right == null && succesor.IsRed) || (succesor.Right.IsRed && !succesor.IsRed),
                    "Succesor must be in valid state");
                if (succesor.Right != null)
                {
                    succesor.Right.IsRed = false;
                }

                if (parentOfSuccesor != match)
                {
                    // detach succesor from its parent and set its right child
                    parentOfSuccesor.Left = succesor.Right;
                    succesor.Right = match.Right;
                }

                succesor.Left = match.Left;
            }

            if (succesor != null)
            {
                succesor.IsRed = match.IsRed;
            }

            ReplaceChildOfNodeOrRoot(parentOfMatch, match, succesor);
        }

        internal void UpdateVersion()
        {
            ++version;
        }

        private static Node RotateLeft(Node node)
        {
            Node x = node.Right;
            node.Right = x.Left;
            x.Left = node;
            return x;
        }

        private static Node RotateLeftRight(Node node)
        {
            Node child = node.Left;
            Node grandChild = child.Right;

            node.Left = grandChild.Right;
            grandChild.Right = node;
            child.Right = grandChild.Left;
            grandChild.Left = child;
            return grandChild;
        }

        private static Node RotateRight(Node node)
        {
            Node x = node.Left;
            node.Left = x.Right;
            x.Right = node;
            return x;
        }

        private static Node RotateRightLeft(Node node)
        {
            Node child = node.Right;
            Node grandChild = child.Left;

            node.Right = grandChild.Left;
            grandChild.Left = node;
            child.Left = grandChild.Right;
            grandChild.Right = child;
            return grandChild;
        }

        private static TreeRotation RotationNeeded(Node parent, Node current, Node sibling)
        {
            Debug.Assert(IsRed(sibling.Left) || IsRed(sibling.Right), "sibling must have at least one red child");
            if (IsRed(sibling.Left))
            {
                if (parent.Left == current)
                {
                    return TreeRotation.RightLeftRotation;
                }
                return TreeRotation.RightRotation;
            }
            else
            {
                if (parent.Left == current)
                {
                    return TreeRotation.LeftRotation;
                }
                return TreeRotation.LeftRightRotation;
            }
        }

        private static void Split4Node(Node node)
        {
            node.IsRed = true;
            node.Left.IsRed = false;
            node.Right.IsRed = false;
        }

        internal class Node
        {
            private bool isRed;
            private T item;
            private Node left;
            private Node right;

            public Node(T item)
            {
                // The default color will be red, we never need to create a black node directly.				
                this.item = item;
                isRed = true;
            }

            public Node(T item, bool isRed)
            {
                // The default color will be red, we never need to create a black node directly.				
                this.item = item;
                this.isRed = isRed;
            }

            public T Item { get { return item; } set { item = value; } }

            public Node Left { get { return left; } set { left = value; } }

            public Node Right { get { return right; } set { right = value; } }

            public bool IsRed { get { return isRed; } set { isRed = value; } }
        }

        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private readonly TreeSet<T> tree;
            private readonly int version;
            private readonly Stack<TreeSet<T>.Node> stack;
            private TreeSet<T>.Node current;
            private static TreeSet<T>.Node dummyNode = new TreeSet<T>.Node(default(T));

            private const string TreeName = "Tree";
            private const string NodeValueName = "Item";
            private const string EnumStartName = "EnumStarted";
            private const string VersionName = "Version";


            internal Enumerator(TreeSet<T> set)
            {
                tree = set;
                version = tree.version;

                // 2lg(n + 1) is the maximum height
                stack = new Stack<TreeSet<T>.Node>(2*(int) Math.Log(set.Count + 1));
                current = null;
                Intialize();
            }

            private void Intialize()
            {
                current = null;
                TreeSet<T>.Node node = tree.root;
                while (node != null)
                {
                    stack.Push(node);
                    node = node.Left;
                }
            }

            public bool MoveNext()
            {
                if (version != tree.version)
                {
                    throw new InvalidOperationException();
                }

                if (stack.Count == 0)
                {
                    current = null;
                    return false;
                }

                current = stack.Pop();
                TreeSet<T>.Node node = current.Right;
                while (node != null)
                {
                    stack.Push(node);
                    node = node.Left;
                }
                return true;
            }

            public void Dispose()
            {
            }

            public T Current
            {
                get
                {
                    if (current != null)
                    {
                        return current.Item;
                    }
                    return default(T);
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    if (current == null)
                    {
                        throw new InvalidOperationException();
                    }

                    return current.Item;
                }
            }

            internal bool NotStartedOrEnded { get { return current == null; } }

            internal void Reset()
            {
                if (version != tree.version)
                {
                    throw new InvalidOperationException();
                }

                stack.Clear();
                Intialize();
            }

            void IEnumerator.Reset()
            {
                Reset();
            }
        }
    }
}