using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;

namespace TVGL.Voxelization
{
    /// <summary>
    /// Class VoxelHashSet.
    /// </summary>
    /// <seealso cref="System.Collections.Generic.IEnumerable{TVGL.Voxelization.IVoxel}" />
    internal class VoxelHashSet : IEnumerable<long>
    {
        internal readonly int level;
        private int[] buckets;
        private Slot[] slots;
        /// <summary>
        /// The count of the actual number of elements (i.e. voxels) in this hashset
        /// </summary>
        private int count;
        private int lastIndex;
        private int freeList;
        private VoxelComparer comparer;
        private bool CapacityMaxedOut;


        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="VoxelHashSet"/> class.
        /// </summary>
        /// <param name="comparer">The comparer.</param>
        /// <param name="solid">The solid.</param>
        internal VoxelHashSet(int level, int[] bitLevelDistribution)
        {
            this.level = level;
            var numParentBits = bitLevelDistribution.Take(level).Sum();
            if (level == 0)
            {
                //throw new ArgumentException("shouldn't pass level 0 here anymore");
                comparer = new VoxelComparerLevel0(bitLevelDistribution[0]);
            }
            else if (numParentBits - bitLevelDistribution[0] <= 10)
                comparer = new VoxelComparerMidLevels(bitLevelDistribution, numParentBits);
            else comparer = new VoxelComparerFine(bitLevelDistribution, numParentBits);
            lastIndex = 0;
            count = 0;
            freeList = -1;
            Initialize(primes[0]);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VoxelHashSet"/> class.
        /// </summary>
        /// <param name="comparer">The comparer.</param>
        /// <param name="solid">The solid.</param>
        /// <param name="startingSet">The starting set.</param>
        internal VoxelHashSet(int level, int[] bitLevelDistribution, IEnumerable<IVoxel> startingSet)
            : this(level, bitLevelDistribution)
        {
            foreach (var voxel in startingSet)
            {
                int hashCode = InternalGetHashCode(voxel.ID);
                int bucket = hashCode % buckets.Length;

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
                slots[index].value = voxel.ID;
                slots[index].next = buckets[bucket] - 1;
                buckets[bucket] = index + 1;
                count++;
            }
        }

        #endregion
        #region New Methods not found in HashSet
        internal long GetVoxel(long item)
        {
            if (buckets != null)
            {
                int hashCode = InternalGetHashCode(item);
                item = comparer.EqualsMask(item);
                // see note at "HashSet" level describing why "- 1" appears in for loop
                for (int i = buckets[hashCode % buckets.Length] - 1; i >= 0; i = slots[i].next)
                {
                    if (slots[i].hashCode == hashCode && comparer.Equals(slots[i].value, item))
                    {
                        return slots[i].value;
                    }
                }
            }
            // either m_buckets is null or wasn't found
            return 0L;
        }


        internal VoxelHashSet Copy(VoxelizedSolid solid)
        {
            var copy = new VoxelHashSet(this.level, solid.bitLevelDistribution)
            {
                buckets = (int[])this.buckets.Clone(),
                slots = (Slot[])slots.Clone(),
                count = this.count,
                lastIndex = this.lastIndex,
                freeList = this.freeList,
            };
            return copy;
        }


        internal void AddRange(ICollection<long> voxels)
        {
            foreach (var voxel in voxels)
                AddOrReplace(voxel);
        }

        #endregion

        #region ICollection<T> method

        /// <summary>
        /// Determines whether [contains] [the specified item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if [contains] [the specified item]; otherwise, <c>false</c>.</returns>
        internal bool Contains(IVoxel item)
        {
            if (item == null) return false;
            return Contains(item.ID);
        }
        /// <summary>
        /// Checks if this hashset contains the item
        /// </summary>
        /// <param name="item">item to check for containment</param>
        /// <returns>true if item contained; false if not</returns>
        internal bool Contains(long item)
        {
            if (buckets != null)
            {
                int hashCode = InternalGetHashCode(item);
                item = comparer.EqualsMask(item);
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

        internal bool Remove(IVoxel item)
        { return Remove(item.ID); }
        /// <summary>
        /// Remove item from this hashset
        /// </summary>
        /// <param name="item">item to remove</param>
        /// <returns>true if removed; false if not (i.e. if the item wasn't in the HashSet)</returns>
        internal bool Remove(long item)
        {
            if (buckets != null)
            {
                int hashCode = InternalGetHashCode(item);
                item = comparer.EqualsMask(item);
                int bucket = hashCode % buckets.Length;
                int previousI = -1;
                for (int i = buckets[bucket] - 1; i >= 0; previousI = i, i = slots[i].next)
                {
                    if (slots[i].hashCode == hashCode && comparer.Equals(slots[i].value, item))
                    {
                        if (previousI < 0)
                        {
                            // first iteration; update buckets
                            buckets[bucket] = slots[i].next + 1;
                        }
                        else
                        {
                            // subsequent iterations; update 'next' pointers
                            slots[previousI].next = slots[i].next;
                        }
                        slots[i].hashCode = -1;
                        // slots[i].value = 0L;
                        slots[i].next = freeList;

                        count--;
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
        internal int Count => count;

        #endregion


        #region HashSet methods


        /// <summary>
        /// Remove elements that match specified predicate. Returns the number of elements removed
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        internal int RemoveWhere(Predicate<long> match)
        {
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
                            numRemoved++;
                    }
                }
            }
            return numRemoved;
        }
        internal int RemoveDescendants(long ancestor, int ancestorLevel)
        {
            ancestor = comparer.ParentMask(ancestor, ancestorLevel);
            int numRemoved = 0;
            for (int i = 0; i < lastIndex; i++)
            {
                if (slots[i].hashCode >= 0)
                {
                    if (comparer.IsDescendantOf(slots[i].value, ancestor, ancestorLevel))
                    {
                        // check again that remove actually removed it
                        if (Remove(slots[i].value))
                            numRemoved++;
                    }
                }
            }
            return numRemoved;
        }
        internal List<long> GetDescendants(long ancestor, int ancestorLevel)
        {
            var descendants = new List<long>();
            ancestor = comparer.ParentMask(ancestor, ancestorLevel);
            for (int i = 0; i < lastIndex; i++)
            {
                if (slots[i].hashCode >= 0)
                {
                    if (comparer.IsDescendantOf(slots[i].value, ancestor, ancestorLevel))
                        descendants.Add(slots[i].value);
                }
            }
            return descendants;
        }
        internal int CountDescendants(long ancestor, int ancestorLevel)
        {
            ancestor = comparer.ParentMask(ancestor, ancestorLevel);
            int count = 0;
            for (int i = 0; i < lastIndex; i++)
            {
                if (slots[i].hashCode >= 0)
                {
                    if (comparer.IsDescendantOf(slots[i].value, ancestor, ancestorLevel))
                        count++;
                }
            }
            return count;
        }
        internal int CountDescendants(long ancestor, int ancestorLevel, VoxelRoleTypes role)
        {
            ancestor = comparer.ParentMask(ancestor, ancestorLevel);
            int count = 0;
            for (int i = 0; i < lastIndex; i++)
            {
                if (slots[i].hashCode >= 0)
                {
                    if (comparer.IsDescendantOf(slots[i].value, ancestor, ancestorLevel) && Constants.GetRole(slots[i].value) == role)
                        count++;
                }
            }
            return count;
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
        /// Add item to this HashSet if it is not within the set and returns true if it is
        /// new. If the voxel already exists, then it is replaced with the provided.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>true if added, false if already present</returns>
        internal bool AddOrReplace(long newVoxel)
        {
            long newVoxelID = newVoxel;
            int hashCode = InternalGetHashCode(newVoxelID);
            newVoxelID = comparer.EqualsMask(newVoxelID);
            int bucket = hashCode % buckets.Length;

            for (int i = buckets[hashCode % buckets.Length] - 1; i >= 0; i = slots[i].next)
            {
                if (slots[i].hashCode == hashCode && comparer.Equals(slots[i].value, newVoxelID))
                {
                    slots[i].value = newVoxel;
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
            slots[index].value = newVoxel;
            slots[index].next = buckets[bucket] - 1;
            buckets[bucket] = index + 1;
            count++;
            return true;
        }
        /// <summary>
        /// Copies this to an array. Used for DebugView
        /// </summary>
        /// <returns></returns>
        internal long[] ToArray()
        {
            return slots.Select(x => x.value).ToArray();
        }

        /// <summary>
        /// Workaround Comparers that throw ArgumentNullException for GetHashCode(null).
        /// </summary>
        /// <param name="item"></param>
        /// <returns>hash code</returns>
        private int InternalGetHashCode(long item)
        {
            return comparer.GetHashCode(item);
        }

        #region IEnumerable methods

        public IEnumerator<long> GetEnumerator()
        {
            return new VoxelEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion
        #endregion

        internal struct Slot
        {
            internal int hashCode;      // Lower 31 bits of hash code, -1 if unused
            internal long value;
            internal int next;          // Index of next entry, -1 if last
        }

        private struct VoxelEnumerator : IEnumerator<long>
        {
            private readonly VoxelHashSet set;
            private int index;
            private long current;

            internal VoxelEnumerator(VoxelHashSet set)
            {
                this.set = set;
                index = 0;
                current = 0L;// new Voxel(0L, null);
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
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
                // current = 0L;
                return false;
            }

            public long Current => current;

            Object IEnumerator.Current
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

            void IEnumerator.Reset()
            {
                index = 0;
                //  current = 0L;
            }
        }
    }

}