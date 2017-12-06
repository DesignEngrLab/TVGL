using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Security;

namespace TVGL.Voxelization
{

    public class VoxelHashSet : IEnumerable<long>
    {
        // cutoff point, above which we won't do stackallocs. This corresponds to 100 integers.
        private const int StackAllocThreshold = 100;
        // when constructing a hashset from an existing collection, it may contain duplicates, 
        // so this is used as the max acceptable excess ratio of capacity to count. Note that
        // this is only used on the ctor and not to automatically shrink if the hashset has, e.g,
        // a lot of adds followed by removes. Users must explicitly shrink by calling TrimExcess.
        // This is set to 3 because capacity is acceptable as 2x rounded up to nearest prime.
        private const int ShrinkThreshold = 3;

        private int[] buckets;
        private Slot[] slots;
        private int count;
        private int lastIndex;
        private int freeList;
        private IEqualityComparer<long> comparer;
        private int version;


        #region Constructors

        public VoxelHashSet(IEqualityComparer<long> comparer, int level)
        {
            this.comparer = comparer;
            lastIndex = 0;
            count = 0;
            freeList = -1;
            version = 0;
            var suggestedCapacity = primes[0];
            if (level == 3) suggestedCapacity = primes[7];
            if (level == 4) suggestedCapacity = primes[13];
            Initialize(suggestedCapacity);

        }


        /// <summary>
        /// Implementation Notes:
        /// Since resizes are relatively expensive (require rehashing), this attempts to minimize 
        /// the need to resize by setting the initial capacity based on size of collection. 
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="comparer"></param>
        public VoxelHashSet(IEnumerable<long> collection, IEqualityComparer<long> comparer)
        {
            int suggestedCapacity = 0;
            var coll = collection as ICollection<long>;
            if (coll != null)
            {
                suggestedCapacity = coll.Count;
            }
            Initialize(suggestedCapacity);

            this.UnionWith(collection);
            if ((count == 0 && slots.Length > primes[0]) ||
                (count > 0 && slots.Length / count > ShrinkThreshold))
            {
                TrimExcess();
            }
            this.comparer = comparer;
            lastIndex = 0;
            count = 0;
            freeList = -1;
            version = 0;
        }
        #endregion
        #region New Methods not found in HashSet
        public VoxelRoleTypes[] ReadFlags(long item)
        {
            if (buckets != null)
            {
                int hashCode = InternalGetHashCode(item);
                // see note at "HashSet" level describing why "- 1" appears in for loop
                for (int i = buckets[hashCode % buckets.Length] - 1; i >= 0; i = slots[i].next)
                {
                    if (slots[i].hashCode == hashCode && comparer.Equals(slots[i].value, item))
                    {
                        return VoxelizedSolid.GetRoleFlags(slots[i].value);
                    }
                }
            }
            // either m_buckets is null or wasn't found
            return new VoxelRoleTypes[0];
        }



        #endregion

        #region ICollection<T> method
        /// <summary>
        /// Remove all items from this set. This clears the elements but not the underlying 
        /// buckets and slots array. Follow this call by TrimExcess to release these.
        /// </summary>
        public void Clear()
        {
            if (lastIndex > 0)
            {
                Debug.Assert(buckets != null, "m_buckets was null but m_lastIndex > 0");

                // clear the elements so that the gc can reclaim the references.
                // clear only up to m_lastIndex for m_slots 
                Array.Clear(slots, 0, lastIndex);
                Array.Clear(buckets, 0, buckets.Length);
                lastIndex = 0;
                count = 0;
                freeList = -1;
            }
            version++;
        }

        /// <summary>
        /// Checks if this hashset contains the item
        /// </summary>
        /// <param name="item">item to check for containment</param>
        /// <returns>true if item contained; false if not</returns>
        public bool Contains(long item)
        {
            if (buckets != null)
            {
                int hashCode = InternalGetHashCode(item);
                // see note at "HashSet" level describing why "- 1" appears in for loop
                for (int i = buckets[hashCode % buckets.Length] - 1; i >= 0; i = slots[i].next)
                {
                    if (slots[i].hashCode == hashCode && comparer.Equals(slots[i].value, item))
                    {
                        return true;
                    }
                }
            }
            // either m_buckets is null or wasn't found
            return false;
        }
        //public T GetID(T item)
        //{
        //    if (buckets != null)
        //    {
        //        int hashCode = InternalGetHashCode(item);
        //        // see note at "HashSet" level describing why "-1" appears in for loop
        //        for (int i = buckets[hashCode % buckets.Length] - 1; i >= 0; i = slots[i].next)
        //        {
        //            if (slots[i].hashCode == hashCode && comparer.Equals(slots[i].value, item))
        //            {
        //                return slots[i].value;
        //            }
        //        }
        //    }
        //    // either m_buckets is null or wasn't found
        //    //return ;
        //}

        /// <summary>
        /// Copy items in this hashset to array, starting at arrayIndex
        /// </summary>
        /// <param name="array">array to add items to</param>
        /// <param name="arrayIndex">index to start at</param>
        public void CopyTo(long[] array, int arrayIndex)
        {
            CopyTo(array, arrayIndex, count);
        }

        /// <summary>
        /// Remove item from this hashset
        /// </summary>
        /// <param name="item">item to remove</param>
        /// <returns>true if removed; false if not (i.e. if the item wasn't in the HashSet)</returns>
        public bool Remove(long item)
        {
            if (buckets != null)
            {
                int hashCode = InternalGetHashCode(item);
                int bucket = hashCode % buckets.Length;
                int last = -1;
                for (int i = buckets[bucket] - 1; i >= 0; last = i, i = slots[i].next)
                {
                    if (slots[i].hashCode == hashCode && comparer.Equals(slots[i].value, item))
                    {
                        if (last < 0)
                        {
                            // first iteration; update buckets
                            buckets[bucket] = slots[i].next + 1;
                        }
                        else
                        {
                            // subsequent iterations; update 'next' pointers
                            slots[last].next = slots[i].next;
                        }
                        slots[i].hashCode = -1;
                        slots[i].value = 0L;
                        slots[i].next = freeList;

                        count--;
                        version++;
                        if (count == 0)
                        {
                            lastIndex = 0;
                            freeList = -1;
                        }
                        else
                        {
                            freeList = i;
                        }
                        return true;
                    }
                }
            }
            // either m_buckets is null or wasn't found
            return false;
        }

        /// <summary>
        /// Number of elements in this hashset
        /// </summary>
        public int Count
        {
            get { return count; }
        }
        #endregion


        #region HashSet methods

        /// <summary>
        /// Add item to this HashSet. Returns bool indicating whether item was added (won't be 
        /// added if already present)
        /// </summary>
        /// <param name="item"></param>
        /// <returns>true if added, false if already present</returns>
        public bool Add(long item)
        {
            return AddIfNotPresent(item);
        }

        /// <summary>
        /// Take the union of this HashSet with other. Modifies this set.
        /// 
        /// Implementation note: GetSuggestedCapacity (to increase capacity in advance avoiding 
        /// multiple resizes ended up not being useful in practice; quickly gets to the 
        /// point where it's a wasteful check.
        /// </summary>
        /// <param name="other">enumerable with items to add</param>
        public void UnionWith(IEnumerable<long> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            Contract.EndContractBlock();

            foreach (var item in other)
            {
                AddIfNotPresent(item);
            }
        }

        /// <summary>
        /// Takes the intersection of this set with other. Modifies this set.
        /// 
        /// Implementation Notes: 
        /// We get better perf if other is a hashset using same equality comparer, because we 
        /// get constant contains check in other. Resulting cost is O(n1) to iterate over this.
        /// 
        /// If we can't go above route, iterate over the other and mark intersection by checking
        /// contains in this. Then loop over and delete any unmarked elements. Total cost is n2+n1. 
        /// 
        /// Attempts to return early based on counts alone, using the property that the 
        /// intersection of anything with the empty set is the empty set.
        /// </summary>
        /// <param name="other">enumerable with items to add </param>
        public void IntersectWith(IEnumerable<long> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            Contract.EndContractBlock();

            // intersection of anything with empty set is empty set, so return if count is 0
            if (count == 0)
            {
                return;
            }

            // if other is empty, intersection is empty set; remove all elements and we're done
            // can only figure this out if implements ICollection<T>. (IEnumerable<T> has no count)
            var otherAsCollection = other as ICollection<long>;
            if (otherAsCollection != null)
            {
                if (otherAsCollection.Count == 0)
                {
                    Clear();
                    return;
                }

                var otherAsSet = other as VoxelHashSet;
                // faster if other is a hashset using same equality comparer; so check 
                // that other is a hashset using the same equality comparer.
                if (otherAsSet != null && AreEqualityComparersEqual(this, otherAsSet))
                {
                    IntersectWithHashSetWithSameEC(otherAsSet);
                    return;
                }
            }
            throw new NotImplementedException("unable to compare with other enumerables other than voxelhashset");
        }

        /// <summary>
        /// Remove items in other from this set. Modifies this set.
        /// </summary>
        /// <param name="other">enumerable with items to remove</param>
        public void ExceptWith(IEnumerable<long> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            Contract.EndContractBlock();

            // this is already the enpty set; return
            if (count == 0)
            {
                return;
            }

            // special case if other is this; a set minus itself is the empty set
            if (other == this)
            {
                Clear();
                return;
            }

            // remove every element in other from this
            foreach (var element in other)
            {
                Remove(element);
            }
        }

        /// <summary>
        /// Takes symmetric difference (XOR) with other and this set. Modifies this set.
        /// </summary>
        /// <param name="other">enumerable with items to XOR</param>
        public void SymmetricExceptWith(IEnumerable<long> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            Contract.EndContractBlock();

            // if set is empty, then symmetric difference is other
            if (count == 0)
            {
                UnionWith(other);
                return;
            }

            // special case this; the symmetric difference of a set with itself is the empty set
            if (other == this)
            {
                Clear();
                return;
            }

            var otherAsSet = other as VoxelHashSet;
            // If other is a HashSet, it has unique elements according to its equality comparer,
            // but if they're using different equality comparers, then assumption of uniqueness
            // will fail. So first check if other is a hashset using the same equality comparer;
            // symmetric except is a lot faster and avoids bit array allocations if we can assume
            // uniqueness
            if (otherAsSet != null && AreEqualityComparersEqual(this, otherAsSet))
            {
                SymmetricExceptWithUniqueHashSet(otherAsSet);
            }
            else
            {
                throw new NotImplementedException("unable to compare with enumerables other than voxelhashset");
            }
        }

        /// <summary>
        /// Checks if this is a subset of other.
        /// 
        /// Implementation Notes:
        /// The following properties are used up-front to avoid element-wise checks:
        /// 1. If this is the empty set, then it's a subset of anything, including the empty set
        /// 2. If other has unique elements according to this equality comparer, and this has more
        /// elements than other, then it can't be a subset.
        /// 
        /// Furthermore, if other is a hashset using the same equality comparer, we can use a 
        /// faster element-wise check.
        /// </summary>
        /// <param name="other"></param>
        /// <returns>true if this is a subset of other; false if not</returns>
        public bool IsSubsetOf(IEnumerable<long> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            Contract.EndContractBlock();

            // The empty set is a subset of any set
            if (count == 0)
            {
                return true;
            }

            var otherAsSet = other as VoxelHashSet;
            // faster if other has unique elements according to this equality comparer; so check 
            // that other is a hashset using the same equality comparer.
            if (otherAsSet != null && AreEqualityComparersEqual(this, otherAsSet))
            {
                // if this has more elements then it can't be a subset
                if (count > otherAsSet.Count)
                {
                    return false;
                }

                // already checked that we're using same equality comparer. simply check that 
                // each element in this is contained in other.
                return IsSubsetOfHashSetWithSameEC(otherAsSet);
            }
            else
            {
                throw new NotImplementedException("unable to compare with other enumerables other than hashsets");
            }
        }

        /// <summary>
        /// Checks if this is a proper subset of other (i.e. strictly contained in)
        /// 
        /// Implementation Notes:
        /// The following properties are used up-front to avoid element-wise checks:
        /// 1. If this is the empty set, then it's a proper subset of a set that contains at least
        /// one element, but it's not a proper subset of the empty set.
        /// 2. If other has unique elements according to this equality comparer, and this has >=
        /// the number of elements in other, then this can't be a proper subset.
        /// 
        /// Furthermore, if other is a hashset using the same equality comparer, we can use a 
        /// faster element-wise check.
        /// </summary>
        /// <param name="other"></param>
        /// <returns>true if this is a proper subset of other; false if not</returns>
        public bool IsProperSubsetOf(IEnumerable<long> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            Contract.EndContractBlock();

            var otherAsCollection = other as ICollection<long>;
            if (otherAsCollection != null)
            {
                // the empty set is a proper subset of anything but the empty set
                if (count == 0)
                {
                    return otherAsCollection.Count > 0;
                }
                var otherAsSet = other as VoxelHashSet;
                // faster if other is a hashset (and we're using same equality comparer)
                if (otherAsSet != null && AreEqualityComparersEqual(this, otherAsSet))
                {
                    if (count >= otherAsSet.Count)
                    {
                        return false;
                    }
                    // this has strictly less than number of items in other, so the following
                    // check suffices for proper subset.
                    return IsSubsetOfHashSetWithSameEC(otherAsSet);
                }
            }
            throw new NotImplementedException("unable to compare with other enumerables other than hashsets");
        }

        /// <summary>
        /// Checks if this is a superset of other
        /// 
        /// Implementation Notes:
        /// The following properties are used up-front to avoid element-wise checks:
        /// 1. If other has no elements (it's the empty set), then this is a superset, even if this
        /// is also the empty set.
        /// 2. If other has unique elements according to this equality comparer, and this has less 
        /// than the number of elements in other, then this can't be a superset
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns>true if this is a superset of other; false if not</returns>
        public bool IsSupersetOf(IEnumerable<long> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            Contract.EndContractBlock();

            // try to fall out early based on counts
            var otherAsCollection = other as ICollection<long>;
            if (otherAsCollection != null)
            {
                // if other is the empty set then this is a superset
                if (otherAsCollection.Count == 0)
                {
                    return true;
                }
                var otherAsSet = other as VoxelHashSet;
                // try to compare based on counts alone if other is a hashset with
                // same equality comparer
                if (otherAsSet != null && AreEqualityComparersEqual(this, otherAsSet))
                {
                    if (otherAsSet.Count > count)
                    {
                        return false;
                    }
                }
            }

            return ContainsAllElements(other);
        }

        /// <summary>
        /// Checks if this is a proper superset of other (i.e. other strictly contained in this)
        /// 
        /// Implementation Notes: 
        /// This is slightly more complicated than above because we have to keep track if there
        /// was at least one element not contained in other.
        /// 
        /// The following properties are used up-front to avoid element-wise checks:
        /// 1. If this is the empty set, then it can't be a proper superset of any set, even if 
        /// other is the empty set.
        /// 2. If other is an empty set and this contains at least 1 element, then this is a proper
        /// superset.
        /// 3. If other has unique elements according to this equality comparer, and other's count
        /// is greater than or equal to this count, then this can't be a proper superset
        /// 
        /// Furthermore, if other has unique elements according to this equality comparer, we can
        /// use a faster element-wise check.
        /// </summary>
        /// <param name="other"></param>
        /// <returns>true if this is a proper superset of other; false if not</returns>
        public bool IsProperSupersetOf(IEnumerable<long> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            Contract.EndContractBlock();

            // the empty set isn't a proper superset of any set.
            if (count == 0)
            {
                return false;
            }

            var otherAsCollection = other as ICollection<long>;
            if (otherAsCollection != null)
            {
                // if other is the empty set then this is a superset
                if (otherAsCollection.Count == 0)
                {
                    // note that this has at least one element, based on above check
                    return true;
                }
                var otherAsSet = other as VoxelHashSet;
                // faster if other is a hashset with the same equality comparer
                if (otherAsSet != null && AreEqualityComparersEqual(this, otherAsSet))
                {
                    if (otherAsSet.Count >= count)
                    {
                        return false;
                    }
                    // now perform element check
                    return ContainsAllElements(otherAsSet);
                }
            }
            throw new NotImplementedException("unable to compare with other enumerables other than hashsets");
        }

        /// <summary>
        /// Checks if this set overlaps other (i.e. they share at least one item)
        /// </summary>
        /// <param name="other"></param>
        /// <returns>true if these have at least one common element; false if disjoint</returns>
        public bool Overlaps(IEnumerable<long> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            Contract.EndContractBlock();

            if (count == 0)
            {
                return false;
            }

            foreach (var element in other)
            {
                if (Contains(element))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if this and other contain the same elements. This is set equality: 
        /// duplicates and order are ignored
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool SetEquals(IEnumerable<long> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            Contract.EndContractBlock();

            var otherAsSet = other as VoxelHashSet;
            // faster if other is a hashset and we're using same equality comparer
            if (otherAsSet != null && AreEqualityComparersEqual(this, otherAsSet))
            {
                // attempt to return early: since both contain unique elements, if they have 
                // different counts, then they can't be equal
                if (count != otherAsSet.Count)
                {
                    return false;
                }

                // already confirmed that the sets have the same number of distinct elements, so if
                // one is a superset of the other then they must be equal
                return ContainsAllElements(otherAsSet);
            }
            else
            {
                var otherAsCollection = other as ICollection<long>;
                if (otherAsCollection != null)
                {
                    // if this count is 0 but other contains at least one element, they can't be equal
                    if (count == 0 && otherAsCollection.Count > 0)
                    {
                        return false;
                    }
                }
                throw new NotImplementedException("unable to compare with other enumerables other than hashsets");
            }
        }

        public void CopyTo(long[] array) { CopyTo(array, 0, count); }

        public void CopyTo(long[] array, int arrayIndex, int count)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            Contract.EndContractBlock();

            // check array index valid index into array
            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException("arrayIndex");
            }

            // also throw if count less than 0
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            // will array, starting at arrayIndex, be able to hold elements? Note: not
            // checking arrayIndex >= array.Length (consistency with list of allowing
            // count of 0; subsequent check takes care of the rest)
            if (arrayIndex > array.Length || count > array.Length - arrayIndex)
            {
                throw new ArgumentException();
            }

            int numCopied = 0;
            for (int i = 0; i < lastIndex && numCopied < count; i++)
            {
                if (slots[i].hashCode >= 0)
                {
                    array[arrayIndex + numCopied] = slots[i].value;
                    numCopied++;
                }
            }
        }

        /// <summary>
        /// Remove elements that match specified predicate. Returns the number of elements removed
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        public int RemoveWhere(Predicate<long> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException("match");
            }
            Contract.EndContractBlock();

            int numRemoved = 0;
            for (int i = 0; i < lastIndex; i++)
            {
                if (slots[i].hashCode >= 0)
                {
                    // cache value in case delegate removes it
                    var value = slots[i].value;
                    if (match(value))
                    {
                        // check again that remove actually removed it
                        if (Remove(value))
                        {
                            numRemoved++;
                        }
                    }
                }
            }
            return numRemoved;
        }

        /// <summary>
        /// Gets the IEqualityComparer that is used to determine equality of keys for 
        /// the HashSet.
        /// </summary>
        public IEqualityComparer<long> Comparer
        {
            get
            {
                return comparer;
            }
        }

        /// <summary>
        /// Sets the capacity of this list to the size of the list (rounded up to nearest prime),
        /// unless count is 0, in which case we release references.
        /// 
        /// This method can be used to minimize a list's memory overhead once it is known that no
        /// new elements will be added to the list. To completely clear a list and release all 
        /// memory referenced by the list, execute the following statements:
        /// 
        /// list.Clear();
        /// list.TrimExcess(); 
        /// </summary>
        public void TrimExcess()
        {
            if (count == 0)
            {
                // if count is zero, clear references
                buckets = null;
                slots = null;
                version++;
            }
            else
            {
                // similar to IncreaseCapacity but moves down elements in case add/remove/etc
                // caused fragmentation
                int newSize = count;
                while (!IsPrime(newSize)) { newSize++; }
                Slot[] newSlots = new Slot[newSize];
                int[] newBuckets = new int[newSize];

                // move down slots and rehash at the same time. newIndex keeps track of current 
                // position in newSlots array
                int newIndex = 0;
                for (int i = 0; i < lastIndex; i++)
                {
                    if (slots[i].hashCode >= 0)
                    {
                        newSlots[newIndex] = slots[i];

                        // rehash
                        int bucket = newSlots[newIndex].hashCode % newSize;
                        newSlots[newIndex].next = newBuckets[bucket] - 1;
                        newBuckets[bucket] = newIndex + 1;

                        newIndex++;
                    }
                }

                Debug.Assert(newSlots.Length <= slots.Length, "capacity increased after TrimExcess");

                lastIndex = newIndex;
                slots = newSlots;
                buckets = newBuckets;
                freeList = -1;
            }
        }

        #endregion

        #region Helper methods

        static readonly int[] primes = {
            251,//0 // these primes are made by starting with the max int32 size and going down to zero. If a prime is found
            673, // then it is divided by e (2.71...) so that the next one would be about a geometric distance away and
            1801, // going by 'e' is a nice ramp up. Here is the code:
            4871,             //    int prime = int.MaxValue;
            13217,            //    while (prime > 0)
            35869,//5         //    {
            97501,            //        if (IsPrime(prime))
            265037,           //        {
            720413,           //            Console.WriteLine(prime);
            1958287,          //            prime = (int)(prime / Math.E);
            5323093,//10      //        }
            14469667,         //    }
            39332593,
            59999999,   //these 5 (this one and the next 4) are added to approach the commonly arrived at 
            74699993,   //maximum for a hashset which is around 89.4 million. This is created by the midpoint
            82049983,  //15 //of the last one with the max (39332593+89399987) and then finding the nearest prime
            85724993,   // this is repeated for ever decreasing limits approach 89.4million so as to be careful to not exceed
            89399987,   // 89.4 million
            106916951,  // these remaining set would require one to set the attribute of the gcAllowVeryLargeObjects 
            290630341,  // to true in runtime environment.
            790015099, //20
            1468749377,  // like the same approach above, 5 new values are placed bewteen 790015099 and the int.MaxValue
            1808116511,  // which is 2147483647 in a sort of arrow's pardox of values half as close as the previous.
            1977800081,  // 
            2062641871,  // 
            2105062777,  // 25
            2147483647  // the int.MaxValue happens to be a prime. cool
        };

        // here is the same IsPrime(int candidate) function from the .NET implementation of a HashTable
        // http://referencesource.microsoft.com/#mscorlib/system/collections/hashtable.cs,964b5528f9dcae0f
        static bool IsPrime(int candidate)
        {
            if ((candidate & 1) != 0)
            {
                int limit = (int)Math.Sqrt(candidate);
                for (int divisor = 3; divisor <= limit; divisor += 2)
                {
                    if ((candidate % divisor) == 0)
                        return false;
                }
                return true;
            }
            return (candidate == 2);
        }

        /// <summary>
        /// Initializes buckets and slots arrays. Uses suggested capacity by finding next prime
        /// greater than or equal to capacity.
        /// </summary>
        /// <param name="capacity"></param>
        private void Initialize(int capacity)
        {
            int i = 0;
            while (i < primes.Length && primes[i] < count) { i++; }
            if (i == primes.Length)
            {
                i--;
                CapacityMaxedOut = true;
            }
            int size = primes[i];

            buckets = new int[size];
            slots = new Slot[size];
        }

        private bool CapacityMaxedOut;
        /// <summary>
        /// Expand to new capacity. New capacity is next prime greater than or equal to suggested 
        /// size. This is called when the underlying array is filled. This performs no 
        /// defragmentation, allowing faster execution; note that this is reasonable since 
        /// AddIfNotPresent attempts to insert new elements in re-opened spots.
        /// </summary>
        /// <param name="sizeSuggestion"></param>
        private void IncreaseCapacity()
        {
            if (CapacityMaxedOut) return;
            int i = 0;
            while (i < primes.Length && primes[i] <= count) { i++; }
            if (i == primes.Length)
            {
                i--;
                CapacityMaxedOut = true;
            }
            int newSize = primes[i];
            // Able to increase capacity; copy elements to larger array and rehash
            SetCapacity(newSize, false);
        }

        /// <summary>
        /// Set the underlying buckets array to size newSize and rehash.  Note that newSize
        /// *must* be a prime.  It is very likely that you want to call IncreaseCapacity()
        /// instead of this method.
        /// </summary>
        private void SetCapacity(int newSize, bool forceNewHashCodes)
        {
            Slot[] newSlots = new Slot[newSize];
            if (slots != null)
            {
                Array.Copy(slots, 0, newSlots, 0, lastIndex);
            }

            if (forceNewHashCodes)
            {
                for (int i = 0; i < lastIndex; i++)
                {
                    if (newSlots[i].hashCode != -1)
                    {
                        newSlots[i].hashCode = InternalGetHashCode(newSlots[i].value);
                    }
                }
            }

            int[] newBuckets = new int[newSize];
            for (int i = 0; i < lastIndex; i++)
            {
                int bucket = newSlots[i].hashCode % newSize;
                newSlots[i].next = newBuckets[bucket] - 1;
                newBuckets[bucket] = i + 1;
            }
            slots = newSlots;
            buckets = newBuckets;
        }

        /// <summary>
        /// Adds value to HashSet if not contained already
        /// Returns true if added and false if already present
        /// </summary>
        /// <param name="value">value to find</param>
        /// <returns></returns>
        private bool AddIfNotPresent(long value)
        {
            int hashCode = InternalGetHashCode(value);
            int bucket = hashCode % buckets.Length;

            for (int i = buckets[hashCode % buckets.Length] - 1; i >= 0; i = slots[i].next)
            {
                if (slots[i].hashCode == hashCode && comparer.Equals(slots[i].value, value))
                {
                    return false;
                }
            }

            int index;
            if (freeList >= 0)
            {
                index = freeList;
                freeList = slots[index].next;
            }
            else
            {
                if (lastIndex == slots.Length)
                {
                    IncreaseCapacity();
                    // this will change during resize
                    bucket = hashCode % buckets.Length;
                }
                index = lastIndex;
                lastIndex++;
            }
            slots[index].hashCode = hashCode;
            slots[index].value = value;
            slots[index].next = buckets[bucket] - 1;
            buckets[bucket] = index + 1;
            count++;
            version++;


            return true;
        }

        /// <summary>
        /// Checks if this contains of other's elements. Iterates over other's elements and 
        /// returns false as soon as it finds an element in other that's not in this.
        /// Used by SupersetOf, ProperSupersetOf, and SetEquals.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        private bool ContainsAllElements(IEnumerable<long> other)
        {
            foreach (var element in other)
            {
                if (!Contains(element))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Implementation Notes:
        /// If other is a hashset and is using same equality comparer, then checking subset is 
        /// faster. Simply check that each element in this is in other.
        /// 
        /// Note: if other doesn't use same equality comparer, then Contains check is invalid,
        /// which is why callers must take are of this.
        /// 
        /// If callers are concerned about whether this is a proper subset, they take care of that.
        ///
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        private bool IsSubsetOfHashSetWithSameEC(VoxelHashSet other)
        {

            foreach (var item in this)
            {
                if (!other.Contains(item))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// If other is a hashset that uses same equality comparer, intersect is much faster 
        /// because we can use other's Contains
        /// </summary>
        /// <param name="other"></param>
        private void IntersectWithHashSetWithSameEC(VoxelHashSet other)
        {
            for (int i = 0; i < lastIndex; i++)
            {
                if (slots[i].hashCode >= 0)
                {
                    var item = slots[i].value;
                    if (!other.Contains(item))
                    {
                        Remove(item);
                    }
                }
            }
        }


        /// <summary>
        /// Used internally by set operations which have to rely on bit array marking. This is like
        /// Contains but returns index in slots array. 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private int InternalIndexOf(long item)
        {
            Debug.Assert(buckets != null, "m_buckets was null; callers should check first");

            int hashCode = InternalGetHashCode(item);
            for (int i = buckets[hashCode % buckets.Length] - 1; i >= 0; i = slots[i].next)
            {
                if ((slots[i].hashCode) == hashCode && comparer.Equals(slots[i].value, item))
                {
                    return i;
                }
            }
            // wasn't found
            return -1;
        }

        /// <summary>
        /// if other is a set, we can assume it doesn't have duplicate elements, so use this
        /// technique: if can't remove, then it wasn't present in this set, so add.
        /// 
        /// As with other methods, callers take care of ensuring that other is a hashset using the
        /// same equality comparer.
        /// </summary>
        /// <param name="other"></param>
        private void SymmetricExceptWithUniqueHashSet(VoxelHashSet other)
        {
            foreach (var item in other)
            {
                if (!Remove(item))
                {
                    AddIfNotPresent(item);
                }
            }
        }


        /// <summary>
        /// Add if not already in hashset. Returns an out param indicating index where added. This 
        /// is used by SymmetricExcept because it needs to know the following things:
        /// - whether the item was already present in the collection or added from other
        /// - where it's located (if already present, it will get marked for removal, otherwise
        /// marked for keeping)
        /// </summary>
        /// <param name="value"></param>
        /// <param name="location"></param>
        /// <returns></returns>
        private bool AddOrGetLocation(long value, out int location)
        {
            Debug.Assert(buckets != null, "m_buckets is null, callers should have checked");

            int hashCode = InternalGetHashCode(value);
            int bucket = hashCode % buckets.Length;
            for (int i = buckets[hashCode % buckets.Length] - 1; i >= 0; i = slots[i].next)
            {
                if (slots[i].hashCode == hashCode && comparer.Equals(slots[i].value, value))
                {
                    location = i;
                    return false; //already present
                }
            }
            int index;
            if (freeList >= 0)
            {
                index = freeList;
                freeList = slots[index].next;
            }
            else
            {
                if (lastIndex == slots.Length)
                {
                    IncreaseCapacity();
                    // this will change during resize
                    bucket = hashCode % buckets.Length;
                }
                index = lastIndex;
                lastIndex++;
            }
            slots[index].hashCode = hashCode;
            slots[index].value = value;
            slots[index].next = buckets[bucket] - 1;
            buckets[bucket] = index + 1;
            count++;
            version++;
            location = index;
            return true;
        }


        /// <summary>
        /// Copies this to an array. Used for DebugView
        /// </summary>
        /// <returns></returns>
        internal long[] ToArray()
        {
            long[] newArray = new long[Count];
            CopyTo(newArray);
            return newArray;
        }

        /// <summary>
        /// Internal method used for HashSetEqualityComparer. Compares set1 and set2 according 
        /// to specified comparer.
        /// 
        /// Because items are hashed according to a specific equality comparer, we have to resort
        /// to n^2 search if they're using different equality comparers.
        /// </summary>
        /// <param name="set1"></param>
        /// <param name="set2"></param>
        /// <param name="comparer"></param>
        /// <returns></returns>
        internal static bool HashSetEquals(VoxelHashSet set1, VoxelHashSet set2, IEqualityComparer<long> comparer)
        {
            // handle null cases first
            if (set1 == null)
            {
                return (set2 == null);
            }
            else if (set2 == null)
            {
                // set1 != null
                return false;
            }

            // all comparers are the same; this is faster
            if (AreEqualityComparersEqual(set1, set2))
            {
                if (set1.Count != set2.Count)
                {
                    return false;
                }
                // suffices to check subset
                foreach (long item in set2)
                {
                    if (!set1.Contains(item))
                    {
                        return false;
                    }
                }
                return true;
            }
            else
            {  // n^2 search because items are hashed according to their respective ECs
                foreach (long set2Item in set2)
                {
                    bool found = false;
                    foreach (long set1Item in set1)
                    {
                        if (comparer.Equals(set2Item, set1Item))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// Checks if equality comparers are equal. This is used for algorithms that can
        /// speed up if it knows the other item has unique elements. I.e. if they're using 
        /// different equality comparers, then uniqueness assumption between sets break.
        /// </summary>
        /// <param name="set1"></param>
        /// <param name="set2"></param>
        /// <returns></returns>
        private static bool AreEqualityComparersEqual(VoxelHashSet set1, VoxelHashSet set2)
        {
            return set1.Comparer.Equals(set2.Comparer);
        }

        /// <summary>
        /// Workaround Comparers that throw ArgumentNullException for GetHashCode(null).
        /// </summary>
        /// <param name="item"></param>
        /// <returns>hash code</returns>
        private int InternalGetHashCode(long item)
        {
            if (item == null)
            {
                return 0;
            }
            return comparer.GetHashCode(item);
        }

        #region IEnumerable methods

        public IEnumerator<long> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }
        #endregion
        #endregion

        // used for set checking operations (using enumerables) that rely on counting
        internal struct ElementCount
        {
            internal int uniqueCount;
            internal int unfoundCount;
        }

        internal struct Slot
        {
            internal int hashCode;      // Lower 31 bits of hash code, -1 if unused
            internal long value;
            internal int next;          // Index of next entry, -1 if last
        }

        public struct Enumerator : IEnumerator<long>, System.Collections.IEnumerator
        {
            private VoxelHashSet set;
            private int index;
            private int version;
            private long current;

            internal Enumerator(VoxelHashSet set)
            {
                this.set = set;
                index = 0;
                version = set.version;
                current = 0L;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (version != set.version)
                {
                    throw new InvalidOperationException();
                }

                while (index < set.lastIndex)
                {
                    if (set.slots[index].hashCode >= 0)
                    {
                        current = set.slots[index].value;
                        index++;
                        return true;
                    }
                    index++;
                }
                index = set.lastIndex + 1;
                current = 0L;
                return false;
            }

            public long Current
            {
                get
                {
                    return current;
                }
            }

            Object System.Collections.IEnumerator.Current
            {
                get
                {
                    if (index == 0 || index == set.lastIndex + 1)
                    {
                        throw new InvalidOperationException();
                    }
                    return Current;
                }
            }

            void System.Collections.IEnumerator.Reset()
            {
                if (version != set.version)
                {
                    throw new InvalidOperationException();
                }

                index = 0;
                current = 0L;
            }
        }
    }

}