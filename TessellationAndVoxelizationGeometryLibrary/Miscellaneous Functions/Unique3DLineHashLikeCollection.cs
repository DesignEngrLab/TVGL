using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace TVGL
{
    /// <summary>
    /// This collection stores unique 3D lines represented by an anchor and a direction.
    /// Because the anchor may be anywhere on the line and the line direction is 
    /// essentially an orientation (i.e. polar and azimuthal angles) special tolerances
    /// are used to define valid matches.
    /// This particular one acts like a dictionary and can store an item of type T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Unique3DLineHashLikeCollection<T> : Unique3DLineHashLikeCollection
    {
        private readonly List<T> items;

        /// <summary>
        /// Initializes a new instance of the <see cref="Unique3DLineHashLikeCollection"/> class.
        /// </summary>
        /// <param name="ignoreRadius">If true, ignore radius.</param>
        /// <param name="treatReflectionsAsSame">If true, treat reflections as same.</param>
        /// <param name="sameAngleDegreesTolerance">The same angle degrees tolerance.</param>
        public Unique3DLineHashLikeCollection(bool treatReflectionsAsSame, double sameDistanceTolerance, double angleDegreesTolerance)
            : base(treatReflectionsAsSame, sameDistanceTolerance, angleDegreesTolerance)
        {
            items = new List<T>();
        }

        /// <summary>Adds the specified element to the <see cref="HashSet{T}"/>.</summary>
        /// <param name="item">The element to add to the set.</param>
        /// <returns>true if the element is added to the <see cref="HashSet{T}"/> object; false if the element is already present.</returns>
        public bool Add(Vector4 unique, T item) => AddIfNotPresent(unique, item);
        public new bool Add(Vector4 unique)
        {
            throw new InvalidOperationException("This method is not supported for this collection. Use the Add(Vector4, T) method instead.");
        }

        /// <summary>Adds the specified element to the <see cref="HashSet{T}"/>.</summary>
        /// <param name="item">The element to add to the set.</param>
        /// <returns>true if the element is added to the <see cref="HashSet{T}"/> object; false if the element is already present.</returns>
        public bool Add(Vector3 anchor, Vector3 direction, T item) => AddIfNotPresent(MiscFunctions.Unique3DLine(anchor, direction), item);

        public new bool Add(Vector3 anchor, Vector3 direction)
        {
            throw new InvalidOperationException("This method is not supported for this collection. Use the Add(Vector3, T) method instead.");
        }
        private bool AddIfNotPresent(Vector4 unique, T item)
        {
            unique = treatReflectionsAsSame ? Reflect(unique) : unique;
            if (TryGetIndex(unique, out var i))
                return false;
            (var anchor, var direction) = MiscFunctions.Get3DLineValuesFromUnique(unique);
            if (i == Count)
            {
                uniqueIDs.Add(unique);
                anchors.Add(anchor);
                directions.Add(direction);
                items.Add(item);
            }
            else
            {
                uniqueIDs.Insert(i, unique);
                anchors.Insert(i, anchor);
                directions.Insert(i, direction);
                items.Insert(i, item);
            }
            return true;
        }

        public new void Clear()
        {
            uniqueIDs.Clear();
            anchors.Clear();
            directions.Clear();
            items.Clear();
        }

        /// <summary>
        /// Iterator for getting or setting the item for a particular spherical angle pair
        /// </summary>
        /// <param name="unique"></param>
        /// <returns></returns>
        public T this[Vector4 unique]
        {
            get
            {
                TryGet(unique, out T item);
                return item;
            }
            set
            {
                unique = treatReflectionsAsSame ? Reflect(unique) : unique;
                if (TryGetIndex(unique, out var i))
                    items[i] = value;
                else
                {
                    (var anchor, var direction) = MiscFunctions.Get3DLineValuesFromUnique(unique);
                    if (i == Count)
                    {
                        uniqueIDs.Add(unique);
                        anchors.Add(anchor);
                        directions.Add(direction);
                        items.Add(value);
                    }
                    else
                    {
                        uniqueIDs.Insert(i, unique);
                        anchors.Insert(i, anchor);
                        directions.Insert(i, direction);
                        items.Insert(i, value);
                    }
                }
            }
        }

        public bool TryGet(Vector3 anchor, Vector3 direction, out T item, out Vector3 matchingAnchor, out Vector3 matchingDirection)
        {
            var query = MiscFunctions.Unique3DLine(anchor, direction);
            query = treatReflectionsAsSame ? Reflect(query) : query;
            if (TryGetIndex(query, out var i))
            {
                item = items[i];
                matchingAnchor = anchors[i];
                matchingDirection = directions[i];
                return true;
            }
            item = default;
            matchingAnchor = default;
            matchingDirection = default;
            return false;
        }
        public bool TryGet(Vector4 unique, out T item)
        {
            unique = treatReflectionsAsSame ? Reflect(unique) : unique;
            if (TryGetIndex(unique, out var i))
            {
                item = items[i];
                return true;
            }
            item = default;
            return false;
        }

        public new bool Remove(Vector3 anchor, Vector3 direction) => Remove(MiscFunctions.Unique3DLine(anchor, direction));
        public new bool Remove(Vector4 unique)
        {
            unique = treatReflectionsAsSame ? Reflect(unique) : unique;
            var matchFound = TryGetIndex(unique, out var i);
            if (matchFound)
            {
                uniqueIDs.RemoveAt(i);
                directions.RemoveAt(i);
                anchors.RemoveAt(i);
                items.RemoveAt(i);
                return true;
            }
            return false;
        }


        public bool Contains(Vector3 anchor, Vector3 direction, T item)
            => Contains(MiscFunctions.Unique3DLine(anchor, direction), item);
        public bool Contains(Vector4 unique, T item)
        {
            unique = treatReflectionsAsSame ? Reflect(unique) : unique;
            bool matchFound = TryGetIndex(unique, out var i);
            return matchFound && item.Equals(items[i]);
        }

        /// <summary>
        /// Gets the values stored in this collection.
        /// </summary>
        public List<T> Values => items;
    }


    /// <summary>
    /// This collection stores unique 3D lines represented by an anchor and a direction.
    /// Because the anchor may be anywhere on the line and the line direction is 
    /// essentially an orientation (i.e. polar and azimuthal angles) special tolerances
    /// are used to define valid matches.
    /// </summary>
    public class Unique3DLineHashLikeCollection : ICollection<Vector4>
    {
        private protected readonly bool treatReflectionsAsSame;
        private protected readonly double sqdDistanceTolerance;
        private protected readonly double dotTolerance;
        private protected readonly double angleTolerance;
        private protected readonly List<Vector4> uniqueIDs;
        private protected readonly List<Vector3> anchors;
        private protected readonly List<Vector3> directions;

        public Unique3DLineHashLikeCollection(bool treatReflectionsAsSame, double sameDistanceTolerance, double angleDegreesTolerance)
        {
            this.treatReflectionsAsSame = treatReflectionsAsSame;
            this.sqdDistanceTolerance = sameDistanceTolerance * sameDistanceTolerance;
            angleTolerance = Math.PI * angleDegreesTolerance / 180.0;
            dotTolerance = Math.Cos(angleTolerance);  //cos(angle) should be above this value to be considered a match
            uniqueIDs = new List<Vector4>();
            anchors = new List<Vector3>();
            directions = new List<Vector3>();
        }

        public int Count => uniqueIDs.Count;

        /// <summary>
        /// Gets a value indicating whether read is only.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Returns the result as a list of Vector4s. 
        /// </summary>
        /// <returns>A list of Vector4s.</returns>
        public List<Vector4> AsUniqueVector4()
        {
            return uniqueIDs;
        }
        /// <summary>
        /// Returns the result as a list of Cartesian Vector3's.
        /// </summary>
        /// <returns>A list of Vector3s.</returns>
        public (Vector3 anchor, Vector3 direction)[] AsVector3Pairs()
        {
            var result = new (Vector3 anchor, Vector3 direction)[Count];
            for (int i = 0; i < Count; i++)
                result[i] = (anchors[i], directions[i]);
            return result;
        }

        /// <summary>
        /// Add a new line to the collection
        /// </summary>
        /// <param name="anchor">An anchor point on the line</param>
        /// <param name="direction">The direction of the line</param>
        /// <returns>returns false if already present</returns>
        public bool Add(Vector3 anchor, Vector3 direction, out Vector4 unique)
            => AddIfNotPresent(MiscFunctions.Unique3DLine(anchor, direction), out unique);
        public bool Add(Vector4 unique, out Vector4 matching) => AddIfNotPresent(unique, out matching);
        void ICollection<Vector4>.Add(Vector4 item) => AddIfNotPresent(item, out _);

        /// <summary>
        /// Adds the if not present.
        /// </summary>
        private bool AddIfNotPresent(Vector4 unique, out Vector4 matching)
        {
            matching = treatReflectionsAsSame ? Reflect(unique) : unique;
            if (TryGetIndex(matching, out var i))
                return false;
            if (i == Count)
            {
                uniqueIDs.Add(matching);
                (var anchor, var dir) = MiscFunctions.Get3DLineValuesFromUnique(matching);
                anchors.Add(anchor);
                directions.Add(dir);
            }
            else
            {
                uniqueIDs.Insert(i, matching);
                (var anchor, var dir) = MiscFunctions.Get3DLineValuesFromUnique(matching);
                anchors.Insert(i, anchor);
                directions.Insert(i, dir);
            }
            return true;
        }

        public void Clear()
        {
            uniqueIDs.Clear();
            anchors.Clear();
            directions.Clear();
        }

        public bool Contains(Vector4 unique)
        {
            unique = treatReflectionsAsSame ? Reflect(unique) : unique;
            return TryGetIndex(unique, out _);
        }
        public bool Contains(Vector3 anchor, Vector3 direction)
            => Contains(MiscFunctions.Unique3DLine(anchor, direction));

        public IEnumerator<Vector4> GetEnumerator()
        {
            return uniqueIDs.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return uniqueIDs.GetEnumerator();
        }

        public void CopyTo(Vector4[] array, int arrayIndex)
        {
            uniqueIDs.CopyTo(array, arrayIndex);
        }

        public bool Remove(Vector3 anchor, Vector3 direction) => Remove(MiscFunctions.Unique3DLine(anchor, direction));
        public bool Remove(Vector4 unique)
        {
            unique = treatReflectionsAsSame ? Reflect(unique) : unique;
            var matchFound = TryGetIndex(unique, out var i);
            if (matchFound)
            {
                uniqueIDs.RemoveAt(i);
                directions.RemoveAt(i);
                anchors.RemoveAt(i);
                return true;
            }
            return false;
        }

        public bool TryGet(Vector3 anchor, Vector3 direction, out Vector3 matchingAnchor, out Vector3 matchingDirection)
            => TryGet(MiscFunctions.Unique3DLine(anchor, direction), out _, out matchingAnchor, out matchingDirection);
        public bool TryGet(Vector4 unique, out Vector4 matchingUnique)
            => TryGet(unique, out matchingUnique, out _, out _);
        private bool TryGet(Vector4 unique, out Vector4 matchingUnique, out Vector3 matchingAnchor, out Vector3 matchingDirection)
        {
            matchingUnique = treatReflectionsAsSame ? Reflect(unique) : unique;
            var matchFound = TryGetIndex(matchingUnique, out var i);
            if (matchFound)
            {
                matchingUnique = uniqueIDs[i];
                matchingAnchor = anchors[i];
                matchingDirection = directions[i];
                return true;
            }
            matchingAnchor = default;
            matchingDirection = default;
            return false;
        }



        #region Main Search Functions


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool TryGetIndex(Vector4 query, out int i)
        {
            // when treatReflectionsAsSame is true, all vectors with negative z are reflected about the origin (NOT the xy plane)
            // this means that the polar angle should only be between 0 and pi/2 and the azimuth angle is shifted by pi
            // however, vectors near the xy plane will have a polar angle near pi/2 and the dot product is not a complete
            // comparison since reflections would yield a negative dot product.
            // this is only true when the two would be reflections would be less than the angle tolerance apart in the polar angles
            /// (pi/2 - s1.AzimuthAngle) + (pi/2 - s2.AzimuthAngle) < angleTolerance
            //query = treatReflectionsAsSame ? Reflect(query) : query;

            // the following binary search is modified/simplified from Array.BinarySearch
            // (https://referencesource.microsoft.com/mscorlib/a.html#b92d187c91d4c9a9)
            if (uniqueIDs.Count == 0)
            {
                i = 0;
                return false;
            }
            var qCart = SphericalAnglePair.ConvertSphericalToCartesian(1, query.Z, query.W);
            var lo = 0;
            var hi = uniqueIDs.Count - 1;
            while (lo <= hi)
            {
                i = lo + ((hi - lo) >> 1);
                if (IsTheSame(query, qCart, uniqueIDs[i], directions[i]))
                    return true;

                if (uniqueIDs[i].Z.IsPracticallySame(query.Z, angleTolerance))
                    return ScanHoop(ref i, query, qCart);
                else if (uniqueIDs[i].Z < query.Z)
                    lo = i + 1;
                else hi = i - 1;
            }
            i = Math.Min(lo, Count - 1);
            return ScanHoop(ref i, query, qCart);
        }

        protected Vector4 Reflect(Vector4 query)
        {
            if (query.Z <= Constants.HalfPi)
                return query;
            if (query.Z.IsPracticallySame(Math.PI, angleTolerance))
                return new Vector4(-query.X, query.Y, 0, query.W);
            var newAzimuth = query.W.IsNegligible() ? Math.PI
                : query.W < 0 ? query.W + Math.PI : query.W - Math.PI;
            return new Vector4(query.X, -query.Y, Math.PI - query.Z, newAzimuth);
        }

        private bool ScanHoop(ref int index, Vector4 query, Vector3 qCart)
        {
            var i = index;
            var gtIndex = query.Z > uniqueIDs[i].Z;
            var insertLocationSet = false;
            var insertIndex = -1;
            // given that the list is sorted in polar angle, we can scan the hoop up to
            // the angle tolerance in either direction
            // this is particularly important since the azimuth angle has its values near
            // pi and -pi at the same location
            while (++i < uniqueIDs.Count && uniqueIDs[i].Z - query.Z < angleTolerance)
            {
                if (!insertLocationSet && gtIndex != query.Z > uniqueIDs[i].Z)
                {
                    insertIndex = i;
                    insertLocationSet = true;
                }
                if (IsTheSame(query, qCart, uniqueIDs[i], directions[i]))
                {
                    index = i;
                    return true;
                }
            }
            i = index;
            while (--i >= 0 && query.Z - uniqueIDs[i].Z < angleTolerance)
            {
                if (!insertLocationSet && gtIndex != query.Z > uniqueIDs[i].Z)
                {
                    insertIndex = i + 1;
                    insertLocationSet = true;
                }
                if (IsTheSame(query, qCart, uniqueIDs[i], directions[i]))
                {
                    index = i;
                    return true;
                }
            }
            if (!insertLocationSet)
            {
                insertIndex = index;
                if (gtIndex)
                {
                    while (insertIndex < uniqueIDs.Count && query.Z > uniqueIDs[insertIndex].Z)
                        insertIndex++;
                    index = insertIndex;
                }
                else
                {
                    while (insertIndex >= 0 && query.Z < uniqueIDs[insertIndex].Z)
                        insertIndex--;
                    index = insertIndex + 1;
                }
            }
            else index = insertIndex;
            return false;
        }

        private bool IsTheSame(Vector4 a, Vector3 aCartesian, Vector4 b, Vector3 bCartesian)
        {
            // There are three cases:
            // 1. if both are aligned but at the poles
            // 2. if reflections is on and both a & b are in the x-y plane and oppsites of each other
            // 3. nominal case

            // Case 1: if both are aligned but at the poles
            if ((a.Z < angleTolerance && b.Z < angleTolerance) || (Math.PI - a.Z < angleTolerance && Math.PI - b.Z < angleTolerance))
            {
                if (!aCartesian.IsAligned(bCartesian, dotTolerance))
                    // wait, what about reflections? well, if reflections are on, then the polar angle is always less than pi/2 anyway
                    // so they need to be aligned at the north pole to reach this point
                    return false;
                // now here's something tricky, the azimuth angles for the two directions could be all over the place, so, we
                // need to put one in the frame of the other
                var deltaAzimuth = a.W - b.W;
                var sinDeltaAzimuth = Math.Sin(deltaAzimuth);
                var cosDeltaAzimuth = Math.Cos(deltaAzimuth);
                var aX = a.X * cosDeltaAzimuth - a.Y * sinDeltaAzimuth;
                var aY = a.X * sinDeltaAzimuth + a.Y * cosDeltaAzimuth;
                return (aX - b.X) * (aX - b.X) + (aY - b.Y) * (aY - b.Y) < sqdDistanceTolerance;
            }

            // Case 2:reflections are both in the x-y plane
            if (treatReflectionsAsSame && Math.PI - a.Z - b.Z < angleTolerance &&
                aCartesian.IsAligned(-bCartesian, dotTolerance))
            {   // note that when reflections is on, polar angle goes from 0 to pi/2
                // if both are at pi/2 then this number would be close to zero
                return (a.X - b.X) * (a.X - b.X) + (a.Y + b.Y) * (a.Y + b.Y) < sqdDistanceTolerance;
                // what's weird here is that opposite azimuth angles at the equator have the same x direction
                // (downard toward the south pole), but opposte y-directions. Hence, the distance function reverses
                // the b.Y coordinate. that is why it is added (subtracting its negative)
             }
            // Case 3:directions are aligned and the x and y coordinates are close
            return aCartesian.IsAligned(bCartesian, dotTolerance)
                    && (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y) < sqdDistanceTolerance;
        }
        #endregion
    }
}
