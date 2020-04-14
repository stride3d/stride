// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
// 
// ----------------------------------------------------------------------
//  Gold Parser engine.
//  See more details on http://www.devincook.com/goldparser/
//  
//  Original code is written in VB by Devin Cook (GOLDParser@DevinCook.com)
// 
//  This translation is done by Vladimir Morozov (vmoroz@hotmail.com)
//  
//  The translation is based on the other engine translations:
//  Delphi engine by Alexandre Rai (riccio@gmx.at)
//  C# engine by Marcus Klimstra (klimstra@home.nl)
// ----------------------------------------------------------------------
#region Using directives

using System;

#endregion

namespace GoldParser
{
	/// <summary>
	/// Maps integer values used for transition vectors to objects.
	/// </summary>
    internal class ObjectMap
	{
		#region Fields

		private bool m_readonly;
		private MapProvider m_mapProvider;

		private const int MAXINDEX = 255;
		private const int GROWTH = 32;
		private const int MINSIZE = 32;
		private const int MAXARRAYCOUNT = 12;

		private const int INVALIDKEY = Int32.MaxValue;

		#endregion

		#region Constructors

		/// <summary>
		/// Creates new instance of <see cref="ObjectMap"/> class.
		/// </summary>
		public ObjectMap()
		{
			m_mapProvider = new SortedMapProvider(MINSIZE);
		}

		#endregion

		#region Public members.

		/// <summary>
		/// Gets number of entries in the map.
		/// </summary>
		public int Count
		{
			get	{ return m_mapProvider.m_count; }
		}

		/// <summary>
		/// Gets or sets read only flag.
		/// </summary>
		public bool ReadOnly 
		{
			get { return m_readonly; }
			set 
			{ 
				if (m_readonly != value)
				{
					SetMapProvider(value);
					m_readonly = value; 
				}
			}
		}

		/// <summary>
		/// Gets or sets value by key.
		/// </summary>
		public object this[int key]
		{
			get { return m_mapProvider[key]; }
			set { m_mapProvider.Add(key, value); }
		}

		/// <summary>
		/// Returns key by index.
		/// </summary>
		/// <param name="index">Zero based index of the requested key.</param>
		/// <returns>Returns key for the given index.</returns>
		public int GetKey(int index)
		{
			return m_mapProvider.GetEntry(index).Key;
		}

		/// <summary>
		/// Removes entry by its key.
		/// </summary>
		/// <param name="key"></param>
		public void Remove(int key)
		{
			m_mapProvider.Remove(key);
		}

		/// <summary>
		/// Adds a new key and value pair. 
		/// If key exists then value is applied to existing key.
		/// </summary>
		/// <param name="key">New key to add.</param>
		/// <param name="value">Value for the key.</param>
		public void Add(int key, object value)
		{
			m_mapProvider.Add(key, value);
		}

		#endregion

		#region Private members

		private void SetMapProvider(bool readOnly)
		{
			int count = m_mapProvider.m_count;
			MapProvider provider = m_mapProvider;
			if (readOnly)
			{
				SortedMapProvider pr = m_mapProvider as SortedMapProvider;
				if (pr.m_lastKey <= MAXINDEX)
				{
					provider = new IndexMapProvider();
				}
				else if (count <= MAXARRAYCOUNT)
				{
					provider = new ArrayMapProvider(m_mapProvider.m_count);
				}
			}
			else
			{
				if (! (provider is SortedMapProvider))
				{
					provider = new SortedMapProvider(m_mapProvider.m_count);
				}
			}
			if (provider != m_mapProvider)
			{
				for (int i = 0; i < count; i++)
				{
					Entry entry = m_mapProvider.GetEntry(i);
					provider.Add(entry.Key, entry.Value);
				}
				m_mapProvider = provider;
			}
		}

		#endregion

		#region Entry struct definition

		private struct Entry
		{
			internal int Key;
			internal object Value;

			internal Entry(int key, object value)
			{
				Key = key;
				Value = value;
			}
		}

		#endregion

		private abstract class MapProvider 
		{
			internal int m_count;        // Entry count in the collection.

			internal abstract object this[int key]
			{
				get;
			}

			internal abstract Entry GetEntry(int index);

			internal abstract void Add(int key, object value);

			internal virtual void Remove(int key)
			{
				throw new InvalidOperationException();
			}
		}

		private class SortedMapProvider : MapProvider
		{
			internal Entry[] m_entries; // Array of entries.

			internal int m_lastKey;      // Bigest key number.

			internal SortedMapProvider(int capacity)
			{
				m_entries = new Entry[capacity];
			}

			internal override object this[int key]
			{
				get 
				{
					int minIndex = 0;
					int maxIndex = m_count - 1;
					if (maxIndex >= 0 && key <= m_lastKey)
					{
						do
						{
							int midIndex = (maxIndex + minIndex) / 2;
							if (key <= m_entries[midIndex].Key)
							{
								maxIndex = midIndex;
							}
							else
							{
								minIndex = midIndex + 1;
							}
						} while (minIndex < maxIndex);
						if (key == m_entries[minIndex].Key)
						{
							return m_entries[minIndex].Value;
						}
					}
					return null;
				}
			}

			internal override Entry GetEntry(int index)
			{
				return m_entries[index];
			}

			internal override void Add(int key, object value)
			{
				bool found;
				int index = FindInsertIndex(key, out found);
				if (found)
				{
					m_entries[index].Value = value;
					return;
				}
				if (m_count >= m_entries.Length)
				{
					Entry[] entries = new Entry[m_entries.Length + GROWTH];
					Array.Copy(m_entries, 0, entries, 0, m_entries.Length);
					m_entries = entries;
				}
				if (index < m_count)
				{
					Array.Copy(m_entries, index, m_entries, index + 1, m_count - index);
				}
				else
				{
					m_lastKey = key;
				}
				m_entries[index].Key = key;
				m_entries[index].Value = value;
				m_count++;
			}

			internal override void Remove(int key)
			{
				int index = FindIndex(key);
				if (index >= 0)
				{
					int tailSize = (m_count - 1) - index;
					if (tailSize > 0)
					{
						Array.Copy(m_entries, index + 1, m_entries, index, tailSize);
					}
					else if (m_count > 1)
					{
						m_lastKey = m_entries[m_count - 2].Key;
					}
					else
					{
						m_lastKey = INVALIDKEY;
					}
					m_count--;
					m_entries[m_count].Key = INVALIDKEY;
					m_entries[m_count].Value = null;
				}
			}

			private int FindIndex(int key)
			{
				int minIndex = 0;
				if (m_count > 0 && key <= m_lastKey)
				{
					int maxIndex = m_count - 1;
					do
					{
						int midIndex = (maxIndex + minIndex) / 2;
						if (key <= m_entries[midIndex].Key)
						{
							maxIndex = midIndex;
						}
						else
						{
							minIndex = midIndex + 1;
						}
					} while (minIndex < maxIndex);
					if (key == m_entries[minIndex].Key)
					{
						return minIndex;
					}
				}
				return -1;
			}

			private int FindInsertIndex(int key, out bool found)
			{
				int minIndex = 0;
				if (m_count > 0 && key <= m_lastKey)
				{
					int maxIndex = m_count - 1;
					do
					{
						int midIndex = (maxIndex + minIndex) / 2;
						if (key <= m_entries[midIndex].Key)
						{
							maxIndex = midIndex;
						}
						else
						{
							minIndex = midIndex + 1;
						}
					} while (minIndex < maxIndex);
					found = (key == m_entries[minIndex].Key);
					return minIndex;
				}
				found = false;
				return m_count;
			}
		}

		private class IndexMapProvider : MapProvider
		{
			private object[] m_array; // Array of entries.			

			internal IndexMapProvider()
			{
				m_array = new object[MAXINDEX + 1];
				for (int i = m_array.Length; --i >= 0; )
				{
					m_array[i] = Unassigned.Value;
				}
			}

			internal override object this[int key]
			{
				get 
				{ 
					if (key >= m_array.Length || key < 0)
					{
						return null;
					}
					return m_array[key]; 
				}
			}

			internal override Entry GetEntry(int index)
			{
				int idx = -1;
				for (int i = 0; i < m_array.Length; i++)
				{
					object value = m_array[i];
					if (value != Unassigned.Value)
					{
						idx++;
					}
					if (idx == index)
					{
						return new Entry(i, value);
					}
				}
				return new Entry();
			}

			internal override void Add(int key, object value)
			{
				m_array[key] = value;
				m_count++;
			}
		}
		
		private class ArrayMapProvider : MapProvider
		{
			private Entry[] m_entries; // Array of entries.			

			internal ArrayMapProvider(int capacity)
			{
				m_entries = new Entry[capacity];
			}

			internal override object this[int key]
			{
				get 
				{ 
					for (int i = m_count; --i >= 0;)
					{
						Entry entry = m_entries[i];
						int entryKey = entry.Key;
						if (entryKey > key)
						{
							continue;
						}
						else if (entryKey == key)
						{
							return entry.Value;
						}
						else if (entryKey < key)
						{
							return null;
						}
					}
					return null; 
				}
			}

			internal override Entry GetEntry(int index)
			{
				return m_entries[index];
			}

			internal override void Add(int key, object value)
			{
				m_entries[m_count].Key = key;
				m_entries[m_count].Value = value;
				m_count++;
			}
		}

		private class Unassigned
		{
			internal static Unassigned Value = new Unassigned();
		}
	}
}
