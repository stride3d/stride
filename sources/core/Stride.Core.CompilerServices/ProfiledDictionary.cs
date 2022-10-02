using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Stride.Core.CompilerServices.Models;

namespace Stride.Core.CompilerServices
{
    public class ProfiledDictionary<K,V> : IEnumerable<KeyValuePair<(K, string),V>>
    {
        private const string DefaultProfile = "Default";
        private Dictionary<K, Dictionary<string, V>> store = new Dictionary<K, Dictionary<string, V>>();

        public void Add(K key, V value, string profile = DefaultProfile)
        {
            if (!store.TryGetValue(key, out var profiledDictionary))
            {
                profiledDictionary = new Dictionary<string, V>(1);
                store.Add(key, profiledDictionary);
            }

            profiledDictionary.Add(profile, value);
        }

        public bool ContainsKey(K key, string profile) => TryGetValue(key, profile, out _);
        public bool ContainsKey(K key) => store.ContainsKey(key);

        public bool TryGetValue(K key, out V firstValue)
        {
            if (store.TryGetValue(key, out var profiledDictionary))
            {
                if (profiledDictionary.TryGetValue(DefaultProfile, out firstValue))
                {
                    return true;
                }

                firstValue = profiledDictionary.Values.First();
                return true;
            }

            firstValue = default;
            return false;
        }

        public bool TryGetValue(K key, out IEnumerable<V> values)
        {
            if (store.TryGetValue(key, out var profiledDictionary))
            {
                values = profiledDictionary.Values;
                return true;
            }

            values = default;
            return false;
        }

        public bool TryGetValue(K key, string profile, out V value)
        {
            value = default;
            return store.TryGetValue(key, out var profiledDictionary) && profiledDictionary.TryGetValue(profile, out value);
        }

        public V this[K key, string profile]
        {
            get
            {
                return TryGetValue(key, profile, out var value) ? value : throw new KeyNotFoundException($"{key} {profile}");
            }
            set
            {
                if (store.TryGetValue(key, out var profiledDictionary))
                {
                    profiledDictionary[profile] = value;
                }
            }
        }

        public IEnumerable<V> Values => store.SelectMany(s => s.Value.Values);

        IEnumerator<KeyValuePair<(K, string), V>> IEnumerable<KeyValuePair<(K, string), V>>.GetEnumerator()
        {
            foreach (var pair in store)
            {
                foreach (var duo in pair.Value)
                {
                    yield return new KeyValuePair<(K, string), V>((pair.Key, duo.Key), duo.Value);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotSupportedException();
        }
    }
}
