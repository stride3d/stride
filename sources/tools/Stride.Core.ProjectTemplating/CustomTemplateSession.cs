// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.TextTemplating;

namespace Stride.Core.ProjectTemplating
{
    internal class CustomTemplateSession : ITextTemplatingSession
    {
        private readonly ExpandoObject expando;
        private readonly IDictionary<string, object> backend;

        public CustomTemplateSession(IEnumerable<KeyValuePair<string, object>> options)
        {
            if (options == null) throw new ArgumentNullException("options");
            Id = Guid.NewGuid();
            expando = new ExpandoObject();
            this.backend = expando;

            // Copy back options to backend dictionary
            foreach (var option in options)
            {
                this[option.Key] = option.Value;
            }
        }

        public dynamic Expando
        {
            get
            {
                return expando;
            }
        }

        public bool Equals(ITextTemplatingSession other)
        {
            return ReferenceEquals(this, other);
        }

        public bool Equals(Guid other)
        {
            return Id == other;
        }

        public Guid Id { get; set; }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return backend.GetEnumerator();
        }

        public void Add(KeyValuePair<string, object> item)
        {
            backend.Add(item);
        }

        public void Clear()
        {
            backend.Clear();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            return backend.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            backend.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            return backend.Remove(item);
        }

        public int Count
        {
            get
            {
                return backend.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return backend.IsReadOnly;
            }
        }

        public bool ContainsKey(string key)
        {
            return backend.ContainsKey(key);
        }

        public void Add(string key, object value)
        {
            backend.Add(key, value);
        }

        public bool Remove(string key)
        {
            return backend.Remove(key);
        }

        public bool TryGetValue(string key, out object value)
        {
            return backend.TryGetValue(key, out value);
        }

        public object this[string key]
        {
            get
            {
                object value;
                backend.TryGetValue(key, out value);
                return value;
            }
            set
            {
                backend[key] = value;
            }
        }

        public ICollection<string> Keys
        {
            get
            {
                return backend.Keys;
            }
        }

        public ICollection<object> Values
        {
            get
            {
                return backend.Values;
            }
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException();
        }
    }
}
