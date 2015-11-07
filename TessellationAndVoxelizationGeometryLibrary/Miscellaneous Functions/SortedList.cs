/*************************************************************************
 *     This file & class is part of the TVGL PCL Version. It is a modified 
 *     version from the source found at:
 *     Type: System.Collections.Generic.SortedList
 *     Assembly: System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
 *     MVID: 67296426-5FEC-4466-BD0C-69BBFD2659CF
 *     Original Assembly location: C:\Windows\Microsoft.NET\Framework\v4.0.30319\System.dll
 *     
 *     Since SortedList is not included within the Portable .NET version, we
 *     are including it here.
 *************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;

namespace TVGL
{
    /// <summary>
    ///     Represents a collection of key/value pairs that are sorted by key based on the associated
    ///     <see cref="T:System.Collections.Generic.IComparer`1" /> implementation.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the collection.</typeparam>
    /// <typeparam name="TValue">The type of values in the collection.</typeparam>
    internal class SortedList<TKey, TValue> : IDictionary<TKey, TValue>, IDictionary
    {
        private static readonly TKey[] emptyKeys = new TKey[0];
        private static readonly TValue[] emptyValues = new TValue[0];
        private readonly IComparer<TKey> comparer;
        private int _size;
        private object _syncRoot;
        private KeyList keyList;
        private TKey[] keys;
        private ValueList valueList;
        private TValue[] values;
        private int version;

        static SortedList()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="T:System.Collections.Generic.SortedList`2" /> class that is empty, has
        ///     the default initial capacity, and uses the default <see cref="T:System.Collections.Generic.IComparer`1" />.
        /// </summary>
        internal SortedList()
        {
            keys = emptyKeys;
            values = emptyValues;
            _size = 0;
            comparer = Comparer<TKey>.Default;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="T:System.Collections.Generic.SortedList`2" /> class that is empty, has
        ///     the specified initial capacity, and uses the default <see cref="T:System.Collections.Generic.IComparer`1" />.
        /// </summary>
        /// <param name="capacity">
        ///     The initial number of elements that the <see cref="T:System.Collections.Generic.SortedList`2" />
        ///     can contain.
        /// </param>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="capacity" /> is less than zero.</exception>
        internal SortedList(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException();
            keys = new TKey[capacity];
            values = new TValue[capacity];
            comparer = Comparer<TKey>.Default;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="T:System.Collections.Generic.SortedList`2" /> class that is empty, has
        ///     the default initial capacity, and uses the specified <see cref="T:System.Collections.Generic.IComparer`1" />.
        /// </summary>
        /// <param name="comparer">
        ///     The <see cref="T:System.Collections.Generic.IComparer`1" /> implementation to use when comparing
        ///     keys.-or-null to use the default <see cref="T:System.Collections.Generic.Comparer`1" /> for the type of the key.
        /// </param>
        internal SortedList(IComparer<TKey> comparer)
            : this()
        {
            if (comparer == null)
                return;
            this.comparer = comparer;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="T:System.Collections.Generic.SortedList`2" /> class that is empty, has
        ///     the specified initial capacity, and uses the specified <see cref="T:System.Collections.Generic.IComparer`1" />.
        /// </summary>
        /// <param name="capacity">
        ///     The initial number of elements that the <see cref="T:System.Collections.Generic.SortedList`2" />
        ///     can contain.
        /// </param>
        /// <param name="comparer">
        ///     The <see cref="T:System.Collections.Generic.IComparer`1" /> implementation to use when comparing
        ///     keys.-or-null to use the default <see cref="T:System.Collections.Generic.Comparer`1" /> for the type of the key.
        /// </param>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="capacity" /> is less than zero.</exception>
        internal SortedList(int capacity, IComparer<TKey> comparer)
            : this(comparer)
        {
            Capacity = capacity;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="T:System.Collections.Generic.SortedList`2" /> class that contains
        ///     elements copied from the specified <see cref="T:System.Collections.Generic.IDictionary`2" />, has sufficient
        ///     capacity to accommodate the number of elements copied, and uses the default
        ///     <see cref="T:System.Collections.Generic.IComparer`1" />.
        /// </summary>
        /// <param name="dictionary">
        ///     The <see cref="T:System.Collections.Generic.IDictionary`2" /> whose elements are copied to the
        ///     new <see cref="T:System.Collections.Generic.SortedList`2" />.
        /// </param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="dictionary" /> is null.</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="dictionary" /> contains one or more duplicate keys.</exception>
        internal SortedList(IDictionary<TKey, TValue> dictionary)
            : this(dictionary, null)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="T:System.Collections.Generic.SortedList`2" /> class that contains
        ///     elements copied from the specified <see cref="T:System.Collections.Generic.IDictionary`2" />, has sufficient
        ///     capacity to accommodate the number of elements copied, and uses the specified
        ///     <see cref="T:System.Collections.Generic.IComparer`1" />.
        /// </summary>
        /// <param name="dictionary">
        ///     The <see cref="T:System.Collections.Generic.IDictionary`2" /> whose elements are copied to the
        ///     new <see cref="T:System.Collections.Generic.SortedList`2" />.
        /// </param>
        /// <param name="comparer">
        ///     The <see cref="T:System.Collections.Generic.IComparer`1" /> implementation to use when comparing
        ///     keys.-or-null to use the default <see cref="T:System.Collections.Generic.Comparer`1" /> for the type of the key.
        /// </param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="dictionary" /> is null.</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="dictionary" /> contains one or more duplicate keys.</exception>
        internal SortedList(IDictionary<TKey, TValue> dictionary, IComparer<TKey> comparer)
            : this(dictionary != null ? dictionary.Count : 0, comparer)
        {
            if (dictionary == null)
                throw new ArgumentNullException();
            dictionary.Keys.CopyTo(keys, 0);
            dictionary.Values.CopyTo(values, 0);
            // Array.Sort<TKey, TValue>(this.keys, this.values, comparer);
            _size = dictionary.Count;
        }

        /// <summary>
        ///     Gets or sets the number of elements that the <see cref="T:System.Collections.Generic.SortedList`2" /> can contain.
        /// </summary>
        /// <returns>
        ///     The number of elements that the <see cref="T:System.Collections.Generic.SortedList`2" /> can contain.
        /// </returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///     <see cref="P:System.Collections.Generic.SortedList`2.Capacity" /> is set to a value that is less than
        ///     <see cref="P:System.Collections.Generic.SortedList`2.Count" />.
        /// </exception>
        /// <exception cref="T:System.OutOfMemoryException">There is not enough memory available on the system.</exception>
        internal int Capacity
        {
            get { return keys.Length; }
            set
            {
                if (value == keys.Length)
                    return;
                if (value < _size)
                    throw new ArgumentOutOfRangeException();
                if (value > 0)
                {
                    var keyArray = new TKey[value];
                    var objArray = new TValue[value];
                    if (_size > 0)
                    {
                        Array.Copy(keys, 0, keyArray, 0, _size);
                        Array.Copy(values, 0, objArray, 0, _size);
                    }
                    keys = keyArray;
                    values = objArray;
                }
                else
                {
                    keys = emptyKeys;
                    values = emptyValues;
                }
            }
        }

        /// <summary>
        ///     Gets the <see cref="T:System.Collections.Generic.IComparer`1" /> for the sorted list.
        /// </summary>
        /// <returns>
        ///     The <see cref="T:System.IComparable`1" /> for the current <see cref="T:System.Collections.Generic.SortedList`2" />.
        /// </returns>
        internal IComparer<TKey> Comparer
        {
            get { return comparer; }
        }

        /// <summary>
        ///     Gets a collection containing the keys in the <see cref="T:System.Collections.Generic.SortedList`2" />.
        /// </summary>
        /// <returns>
        ///     A <see cref="T:System.Collections.Generic.IList`1" /> containing the keys in the
        ///     <see cref="T:System.Collections.Generic.SortedList`2" />.
        /// </returns>
        internal IList<TKey> Keys
        {
            get { return GetKeyListHelper(); }
        }

        /// <summary>
        ///     Gets a collection containing the values in the <see cref="T:System.Collections.Generic.SortedList`2" />.
        /// </summary>
        /// <returns>
        ///     A <see cref="T:System.Collections.Generic.IList`1" /> containing the values in the
        ///     <see cref="T:System.Collections.Generic.SortedList`2" />.
        /// </returns>
        internal IList<TValue> Values
        {
            get { return GetValueListHelper(); }
        }

        ICollection IDictionary.Keys
        {
            get { return GetKeyListHelper(); }
        }

        ICollection IDictionary.Values
        {
            get { return GetValueListHelper(); }
        }

        bool IDictionary.IsReadOnly
        {
            get { return false; }
        }

        bool IDictionary.IsFixedSize
        {
            get { return false; }
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get
            {
                if (_syncRoot == null)
                    Interlocked.CompareExchange(ref _syncRoot, new object(), null);
                return _syncRoot;
            }
        }

        object IDictionary.this[object key]
        {
            get
            {
                if (IsCompatibleKey(key))
                {
                    var index = IndexOfKey((TKey) key);
                    if (index >= 0)
                        return values[index];
                }
                return null;
            }
            set
            {
                if (!IsCompatibleKey(key))
                    throw new ArgumentNullException();
                throw new Exception();
            }
        }

        void IDictionary.Add(object key, object value)
        {
            if (key == null)
                throw new ArgumentNullException();
            throw new Exception();
        }

        bool IDictionary.Contains(object key)
        {
            if (IsCompatibleKey(key))
                return ContainsKey((TKey) key);
            return false;
        }

        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException();
            if (array.Rank != 1)
                throw new ArgumentException();
            if (array.GetLowerBound(0) != 0)
                throw new ArgumentException();
            if (arrayIndex < 0 || arrayIndex > array.Length)
                throw new ArgumentOutOfRangeException();
            if (array.Length - arrayIndex < Count)
                throw new ArgumentException();
            var keyValuePairArray = array as KeyValuePair<TKey, TValue>[];
            if (keyValuePairArray != null)
            {
                for (var index = 0; index < Count; ++index)
                    keyValuePairArray[index + arrayIndex] = new KeyValuePair<TKey, TValue>(keys[index], values[index]);
            }
            else
            {
                var objArray = array as object[];
                if (objArray == null)
                    throw new ArgumentException();
                try
                {
                    for (var index = 0; index < Count; ++index)
                        objArray[index + arrayIndex] = new KeyValuePair<TKey, TValue>(keys[index], values[index]);
                }
                catch (ArrayTypeMismatchException)
                {
                    throw new ArgumentException();
                }
            }
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return new Enumerator(this, 2);
        }

        void IDictionary.Remove(object key)
        {
            if (!IsCompatibleKey(key))
                return;
            Remove((TKey) key);
        }

        /// <summary>
        ///     Gets the number of key/value pairs contained in the <see cref="T:System.Collections.Generic.SortedList`2" />.
        /// </summary>
        /// <returns>
        ///     The number of key/value pairs contained in the <see cref="T:System.Collections.Generic.SortedList`2" />.
        /// </returns>
        public int Count
        {
            get { return _size; }
        }

        ICollection<TKey> IDictionary<TKey, TValue>.Keys
        {
            get { return GetKeyListHelper(); }
        }

        ICollection<TValue> IDictionary<TKey, TValue>.Values
        {
            get { return GetValueListHelper(); }
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        ///     Gets or sets the value associated with the specified key.
        /// </summary>
        /// <returns>
        ///     The value associated with the specified key. If the specified key is not found, a get operation throws a
        ///     <see cref="T:System.Collections.Generic.KeyNotFoundException" /> and a set operation creates a new element using
        ///     the specified key.
        /// </returns>
        /// <param name="key">The key whose value to get or set.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key" /> is null.</exception>
        /// <exception cref="T:System.Collections.Generic.KeyNotFoundException">
        ///     The property is retrieved and
        ///     <paramref name="key" /> does not exist in the collection.
        /// </exception>
        public TValue this[TKey key]
        {
            get
            {
                var index = IndexOfKey(key);
                if (index >= 0)
                    return values[index];
                throw new Exception();
            }
            set
            {
                if (key == null)
                    throw new ArgumentNullException();
                var index = Array.BinarySearch(keys, 0, _size, key, comparer);
                if (index >= 0)
                {
                    values[index] = value;
                    ++version;
                }
                else
                    Insert(~index, key, value);
            }
        }

        /// <summary>
        ///     Adds an element with the specified key and value into the <see cref="T:System.Collections.Generic.SortedList`2" />.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add. The value can be null for reference types.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key" /> is null.</exception>
        /// <exception cref="T:System.ArgumentException">
        ///     An element with the same key already exists in the
        ///     <see cref="T:System.Collections.Generic.SortedList`2" />.
        /// </exception>
        public void Add(TKey key, TValue value)
        {
            if (key == null)
                throw new ArgumentNullException();
            //Check to see if item is already in array.
            var num = Array.BinarySearch(keys, 0, _size, key, comparer);
            //"num" should be -1
            if (num >= 0) ;// Debug.WriteLine("Item is already contained in sorted list.");
            else Insert(~num, key, value);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> keyValuePair)
        {
            Add(keyValuePair.Key, keyValuePair.Value);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> keyValuePair)
        {
            var index = IndexOfKey(keyValuePair.Key);
            return index >= 0 && EqualityComparer<TValue>.Default.Equals(values[index], keyValuePair.Value);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> keyValuePair)
        {
            var index = IndexOfKey(keyValuePair.Key);
            if (index < 0 || !EqualityComparer<TValue>.Default.Equals(values[index], keyValuePair.Value))
                return false;
            RemoveAt(index);
            return true;
        }

        /// <summary>
        ///     Removes all elements from the <see cref="T:System.Collections.Generic.SortedList`2" />.
        /// </summary>
        public void Clear()
        {
            ++version;
            Array.Clear(keys, 0, _size);
            Array.Clear(values, 0, _size);
            _size = 0;
        }

        /// <summary>
        ///     Determines whether the <see cref="T:System.Collections.Generic.SortedList`2" /> contains a specific key.
        /// </summary>
        /// <returns>
        ///     true if the <see cref="T:System.Collections.Generic.SortedList`2" /> contains an element with the specified key;
        ///     otherwise, false.
        /// </returns>
        /// <param name="key">The key to locate in the <see cref="T:System.Collections.Generic.SortedList`2" />.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key" /> is null.</exception>
        public bool ContainsKey(TKey key)
        {
            return IndexOfKey(key) >= 0;
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException();
            if (arrayIndex < 0 || arrayIndex > array.Length)
                throw new ArgumentOutOfRangeException();
            if (array.Length - arrayIndex < Count)
                throw new ArgumentException();
            for (var index = 0; index < Count; ++index)
            {
                var keyValuePair = new KeyValuePair<TKey, TValue>(keys[index], values[index]);
                array[arrayIndex + index] = keyValuePair;
            }
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return new Enumerator(this, 1);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this, 1);
        }

        /// <summary>
        ///     Gets the value associated with the specified key.
        /// </summary>
        /// <returns>
        ///     true if the <see cref="T:System.Collections.Generic.SortedList`2" /> contains an element with the specified key;
        ///     otherwise, false.
        /// </returns>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">
        ///     When this method returns, the value associated with the specified key, if the key is found;
        ///     otherwise, the default value for the type of the <paramref name="value" /> parameter. This parameter is passed
        ///     uninitialized.
        /// </param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key" /> is null.</exception>
        public bool TryGetValue(TKey key, out TValue value)
        {
            var index = IndexOfKey(key);
            if (index >= 0)
            {
                value = values[index];
                return true;
            }
            value = default(TValue);
            return false;
        }

        /// <summary>
        ///     Removes the element with the specified key from the <see cref="T:System.Collections.Generic.SortedList`2" />.
        /// </summary>
        /// <returns>
        ///     true if the element is successfully removed; otherwise, false.  This method also returns false if
        ///     <paramref name="key" /> was not found in the original <see cref="T:System.Collections.Generic.SortedList`2" />.
        /// </returns>
        /// <param name="key">The key of the element to remove.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key" /> is null.</exception>
        public bool Remove(TKey key)
        {
            var index = IndexOfKey(key);
            if (index >= 0)
                RemoveAt(index);
            return index >= 0;
        }

        private KeyList GetKeyListHelper()
        {
            return keyList ?? (keyList = new KeyList(this));
        }

        private ValueList GetValueListHelper()
        {
            return valueList ?? (valueList = new ValueList(this));
        }

        /// <summary>
        ///     Determines whether the <see cref="T:System.Collections.Generic.SortedList`2" /> contains a specific value.
        /// </summary>
        /// <returns>
        ///     true if the <see cref="T:System.Collections.Generic.SortedList`2" /> contains an element with the specified value;
        ///     otherwise, false.
        /// </returns>
        /// <param name="value">
        ///     The value to locate in the <see cref="T:System.Collections.Generic.SortedList`2" />. The value can
        ///     be null for reference types.
        /// </param>
        internal bool ContainsValue(TValue value)
        {
            return IndexOfValue(value) >= 0;
        }

        private void EnsureCapacity(int min)
        {
            var num = keys.Length == 0 ? 4 : keys.Length*2;
            if ((uint) num > 2146435071U)
                num = 2146435071;
            if (num < min)
                num = min;
            Capacity = num;
        }

        private TValue GetByIndex(int index)
        {
            if (index < 0 || index >= _size)
                throw new ArgumentOutOfRangeException();
            return values[index];
        }

        /// <summary>
        ///     Returns an enumerator that iterates through the <see cref="T:System.Collections.Generic.SortedList`2" />.
        /// </summary>
        /// <returns>
        ///     An <see cref="T:System.Collections.Generic.IEnumerator`1" /> of type
        ///     <see cref="T:System.Collections.Generic.KeyValuePair`2" /> for the
        ///     <see cref="T:System.Collections.Generic.SortedList`2" />.
        /// </returns>
        internal IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return new Enumerator(this, 1);
        }

        private TKey GetKey(int index)
        {
            if (index < 0 || index >= _size)
                throw new ArgumentOutOfRangeException();
            return keys[index];
        }

        /// <summary>
        ///     Searches for the specified key and returns the zero-based index within the entire
        ///     <see cref="T:System.Collections.Generic.SortedList`2" />.
        /// </summary>
        /// <returns>
        ///     The zero-based index of <paramref name="key" /> within the entire
        ///     <see cref="T:System.Collections.Generic.SortedList`2" />, if found; otherwise, -1.
        /// </returns>
        /// <param name="key">The key to locate in the <see cref="T:System.Collections.Generic.SortedList`2" />.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key" /> is null.</exception>
        internal int IndexOfKey(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException();
            var num = Array.BinarySearch(keys, 0, _size, key, comparer);
            if (num < 0)
                return -1;
            return num;
        }

        /// <summary>
        ///     Searches for the specified value and returns the zero-based index of the first occurrence within the entire
        ///     <see cref="T:System.Collections.Generic.SortedList`2" />.
        /// </summary>
        /// <returns>
        ///     The zero-based index of the first occurrence of <paramref name="value" /> within the entire
        ///     <see cref="T:System.Collections.Generic.SortedList`2" />, if found; otherwise, -1.
        /// </returns>
        /// <param name="value">
        ///     The value to locate in the <see cref="T:System.Collections.Generic.SortedList`2" />.  The value can
        ///     be null for reference types.
        /// </param>
        internal int IndexOfValue(TValue value)
        {
            return Array.IndexOf(values, value, 0, _size);
        }

        private void Insert(int index, TKey key, TValue value)
        {
            if (_size == keys.Length)
                EnsureCapacity(_size + 1);
            if (index < _size)
            {
                Array.Copy(keys, index, keys, index + 1, _size - index);
                Array.Copy(values, index, values, index + 1, _size - index);
            }
            keys[index] = key;
            values[index] = value;
            ++_size;
            ++version;
        }

        /// <summary>
        ///     Removes the element at the specified index of the <see cref="T:System.Collections.Generic.SortedList`2" />.
        /// </summary>
        /// <param name="index">The zero-based index of the element to remove.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///     <paramref name="index" /> is less than zero.-or-
        ///     <paramref name="index" /> is equal to or greater than
        ///     <see cref="P:System.Collections.Generic.SortedList`2.Count" />.
        /// </exception>
        internal void RemoveAt(int index)
        {
            if (index < 0 || index >= _size)
                throw new ArgumentOutOfRangeException();
            --_size;
            if (index < _size)
            {
                Array.Copy(keys, index + 1, keys, index, _size - index);
                Array.Copy(values, index + 1, values, index, _size - index);
            }
            keys[_size] = default(TKey);
            values[_size] = default(TValue);
            ++version;
        }

        /// <summary>
        ///     Sets the capacity to the actual number of elements in the <see cref="T:System.Collections.Generic.SortedList`2" />,
        ///     if that number is less than 90 percent of current capacity.
        /// </summary>
        internal void TrimExcess()
        {
            if (_size >= (int) (keys.Length*0.9))
                return;
            Capacity = _size;
        }

        private static bool IsCompatibleKey(object key)
        {
            if (key == null)
                throw new ArgumentNullException();
            return key is TKey;
        }

        private struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IDictionaryEnumerator
        {
            private readonly SortedList<TKey, TValue> _sortedList;
            private readonly int getEnumeratorRetType;
            private readonly int version;
            private int index;
            private TKey key;
            private TValue value;

            internal Enumerator(SortedList<TKey, TValue> sortedList, int getEnumeratorRetType)
            {
                _sortedList = sortedList;
                index = 0;
                version = _sortedList.version;
                this.getEnumeratorRetType = getEnumeratorRetType;
                key = default(TKey);
                value = default(TValue);
            }

            object IDictionaryEnumerator.Key
            {
                get
                {
                    if (index == 0 || index == _sortedList.Count + 1)
                        throw new InvalidOperationException();
                    return key;
                }
            }

            DictionaryEntry IDictionaryEnumerator.Entry
            {
                get
                {
                    if (index == 0 || index == _sortedList.Count + 1)
                        throw new InvalidOperationException();
                    return new DictionaryEntry(key, value);
                }
            }

            object IDictionaryEnumerator.Value
            {
                get
                {
                    if (index == 0 || index == _sortedList.Count + 1)
                        throw new InvalidOperationException();
                    return value;
                }
            }

            public KeyValuePair<TKey, TValue> Current
            {
                get { return new KeyValuePair<TKey, TValue>(key, value); }
            }

            object IEnumerator.Current
            {
                get
                {
                    if (index == 0 || index == _sortedList.Count + 1)
                        throw new InvalidOperationException();
                    if (getEnumeratorRetType == 2)
                        return new DictionaryEntry(key, value);
                    return new KeyValuePair<TKey, TValue>(key, value);
                }
            }

            public void Dispose()
            {
                index = 0;
                key = default(TKey);
                value = default(TValue);
            }

            public bool MoveNext()
            {
                if (version != _sortedList.version)
                    throw new InvalidOperationException();
                if ((uint) index < (uint) _sortedList.Count)
                {
                    key = _sortedList.keys[index];
                    value = _sortedList.values[index];
                    ++index;
                    return true;
                }
                index = _sortedList.Count + 1;
                key = default(TKey);
                value = default(TValue);
                return false;
            }

            void IEnumerator.Reset()
            {
                if (version != _sortedList.version)
                    throw new InvalidOperationException();
                index = 0;
                key = default(TKey);
                value = default(TValue);
            }
        }

        private sealed class KeyList : IList<TKey>, ICollection
        {
            private readonly SortedList<TKey, TValue> _dict;

            internal KeyList(SortedList<TKey, TValue> dictionary)
            {
                _dict = dictionary;
            }

            bool ICollection.IsSynchronized
            {
                get { return false; }
            }

            object ICollection.SyncRoot
            {
                get { return ((ICollection) _dict).SyncRoot; }
            }

            void ICollection.CopyTo(Array array, int arrayIndex)
            {
                if (array.Rank != 1)
                    throw new ArgumentException();
                try
                {
                    Array.Copy(_dict.keys, 0, array, arrayIndex, _dict.Count);
                }
                catch (ArrayTypeMismatchException)
                {
                    throw new ArgumentException();
                }
            }

            public int Count
            {
                get { return _dict._size; }
            }

            public bool IsReadOnly
            {
                get { return true; }
            }

            public TKey this[int index]
            {
                get { return _dict.GetKey(index); }
                set { throw new NotSupportedException(); }
            }

            public void Add(TKey key)
            {
                throw new NotSupportedException();
            }

            public void Clear()
            {
                throw new NotSupportedException();
            }

            public bool Contains(TKey key)
            {
                return _dict.ContainsKey(key);
            }

            public void CopyTo(TKey[] array, int arrayIndex)
            {
                Array.Copy(_dict.keys, 0, array, arrayIndex, _dict.Count);
            }

            public void Insert(int index, TKey value)
            {
                throw new NotSupportedException();
            }

            public IEnumerator<TKey> GetEnumerator()
            {
                return new SortedListKeyEnumerator(_dict);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return new SortedListKeyEnumerator(_dict);
            }

            public int IndexOf(TKey key)
            {
                if (key == null)
                    throw new ArgumentNullException();
                var num = Array.BinarySearch(_dict.keys, 0, _dict.Count, key, _dict.comparer);
                if (num >= 0)
                    return num;
                return -1;
            }

            public bool Remove(TKey key)
            {
                throw new NotSupportedException();
            }

            public void RemoveAt(int index)
            {
                throw new NotSupportedException();
            }
        }

        private sealed class SortedListKeyEnumerator : IEnumerator<TKey>
        {
            private readonly SortedList<TKey, TValue> _sortedList;
            private readonly int version;
            private TKey currentKey;
            private int index;

            internal SortedListKeyEnumerator(SortedList<TKey, TValue> sortedList)
            {
                _sortedList = sortedList;
                version = sortedList.version;
            }

            public TKey Current
            {
                get { return currentKey; }
            }

            object IEnumerator.Current
            {
                get
                {
                    if (index == 0 || index == _sortedList.Count + 1)
                        throw new InvalidOperationException();
                    return currentKey;
                }
            }

            public void Dispose()
            {
                index = 0;
                currentKey = default(TKey);
            }

            public bool MoveNext()
            {
                if (version != _sortedList.version)
                    throw new InvalidOperationException();
                if ((uint) index < (uint) _sortedList.Count)
                {
                    currentKey = _sortedList.keys[index];
                    ++index;
                    return true;
                }
                index = _sortedList.Count + 1;
                currentKey = default(TKey);
                return false;
            }

            void IEnumerator.Reset()
            {
                if (version != _sortedList.version)
                    throw new InvalidOperationException();
                index = 0;
                currentKey = default(TKey);
            }
        }

        private sealed class SortedListValueEnumerator : IEnumerator<TValue>
        {
            private readonly SortedList<TKey, TValue> _sortedList;
            private readonly int version;
            private TValue currentValue;
            private int index;

            internal SortedListValueEnumerator(SortedList<TKey, TValue> sortedList)
            {
                _sortedList = sortedList;
                version = sortedList.version;
            }

            public TValue Current
            {
                get { return currentValue; }
            }

            object IEnumerator.Current
            {
                get
                {
                    if (index == 0 || index == _sortedList.Count + 1)
                        throw new InvalidOperationException();
                    return currentValue;
                }
            }

            public void Dispose()
            {
                index = 0;
                currentValue = default(TValue);
            }

            public bool MoveNext()
            {
                if (version != _sortedList.version)
                    throw new InvalidOperationException();
                if ((uint) index < (uint) _sortedList.Count)
                {
                    currentValue = _sortedList.values[index];
                    ++index;
                    return true;
                }
                index = _sortedList.Count + 1;
                currentValue = default(TValue);
                return false;
            }

            void IEnumerator.Reset()
            {
                if (version != _sortedList.version)
                    throw new InvalidOperationException();
                index = 0;
                currentValue = default(TValue);
            }
        }

        private sealed class ValueList : IList<TValue>, ICollection
        {
            private readonly SortedList<TKey, TValue> _dict;

            internal ValueList(SortedList<TKey, TValue> dictionary)
            {
                _dict = dictionary;
            }

            bool ICollection.IsSynchronized
            {
                get { return false; }
            }

            object ICollection.SyncRoot
            {
                get { return ((ICollection) _dict).SyncRoot; }
            }

            void ICollection.CopyTo(Array array, int arrayIndex)
            {
                if (array.Rank != 1)
                    throw new ArgumentException();
                try
                {
                    Array.Copy(_dict.values, 0, array, arrayIndex, _dict.Count);
                }
                catch (ArrayTypeMismatchException)
                {
                    throw new ArgumentException();
                }
            }

            public int Count
            {
                get { return _dict._size; }
            }

            public bool IsReadOnly
            {
                get { return true; }
            }

            public TValue this[int index]
            {
                get { return _dict.GetByIndex(index); }
                set { throw new NotSupportedException(); }
            }

            public void Add(TValue key)
            {
                throw new NotSupportedException();
            }

            public void Clear()
            {
                throw new NotSupportedException();
            }

            public bool Contains(TValue value)
            {
                return _dict.ContainsValue(value);
            }

            public void CopyTo(TValue[] array, int arrayIndex)
            {
                Array.Copy(_dict.values, 0, array, arrayIndex, _dict.Count);
            }

            public void Insert(int index, TValue value)
            {
                throw new NotSupportedException();
            }

            public IEnumerator<TValue> GetEnumerator()
            {
                return new SortedListValueEnumerator(_dict);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return new SortedListValueEnumerator(_dict);
            }

            public int IndexOf(TValue value)
            {
                return Array.IndexOf(_dict.values, value, 0, _dict.Count);
            }

            public bool Remove(TValue value)
            {
                throw new NotSupportedException();
            }

            public void RemoveAt(int index)
            {
                throw new NotSupportedException();
            }
        }
    }
}