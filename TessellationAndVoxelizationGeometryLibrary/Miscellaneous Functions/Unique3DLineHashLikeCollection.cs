using ClipperLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Xml;

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
                TryGet(unique, out T item, out _);
                return item;
            }
            set
            {
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
            if (TryGetIndex(MiscFunctions.Unique3DLine(anchor, direction), out var i))
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
        public bool TryGet(Vector4 unique, out T item, out Vector4 matchingUnique)
        {
            if (TryGetIndex(unique, out var i))
            {
                item = items[i];
                matchingUnique = uniqueIDs[i];
                return true;
            }
            item = default;
            matchingUnique = uniqueIDs[i];
            return false;
        }

        public new bool Remove(Vector3 anchor, Vector3 direction) => Remove(MiscFunctions.Unique3DLine(anchor, direction));
        public new bool Remove(Vector4 unique)
        {
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
            bool matchFound = TryGetIndex(unique, out var i);
            return matchFound && item.Equals(items[i]);
        }


        public IEnumerable<T> GetItems()
        {
            return items;
        }
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
        public bool Add(Vector3 anchor, Vector3 direction) => AddIfNotPresent(MiscFunctions.Unique3DLine(anchor, direction));
        void ICollection<Vector4>.Add(Vector4 unique) => AddIfNotPresent(unique);

        /// <summary>
        /// Adds the if not present.
        /// </summary>
        private bool AddIfNotPresent(Vector4 unique)
        {
            var matchFound = TryGetIndex(unique, out var i);
            if (matchFound)
                return false;
            if (i == Count)
            {
                uniqueIDs.Add(unique);
                (var anchor, var dir) = MiscFunctions.Get3DLineValuesFromUnique(unique);
                anchors.Add(anchor);
                directions.Add(dir);
            }
            else
            {
                uniqueIDs.Insert(i, unique);
                (var anchor, var dir) = MiscFunctions.Get3DLineValuesFromUnique(unique);
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
            var matchFound = TryGetIndex(unique, out var i);
            if (matchFound)
            {
                matchingUnique = uniqueIDs[i];
                matchingAnchor = anchors[i];
                matchingDirection = directions[i];
                return true;
            }
            matchingUnique = default;
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
            if (treatReflectionsAsSame && query.Z > Constants.HalfPi)
            {
                var newAzimuth = query.W < 0 ? query.W + Math.PI : query.W - Math.PI;
                query = new Vector4(-query.X, query.Y, Math.PI - query.Z, newAzimuth);
            }

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
                else if (uniqueIDs[i].Z < query.Z)
                    lo = i + 1;
                else hi = i - 1;
            }
            i = lo;
            return ScanHoop(ref i, query, qCart);
        }


        private bool ScanHoop(ref int index, Vector4 query, Vector3 qCart)
        {
            var i = index;
            // given that the list is sorted in polar angle, we can scan the hoop up to
            // the angle tolerance in either direction
            // this is particularly important since the azimuth angle has its values near
            // pi and -pi at the same location
            while (++i < uniqueIDs.Count && uniqueIDs[i].Z - query.Z < angleTolerance)
                if (IsTheSame(query, qCart, uniqueIDs[i], directions[i]))
                {
                    index = i;
                    return true;
                }
            i = index;
            while (--i >= 0 && query.Z - uniqueIDs[i].Z < angleTolerance)
                if (IsTheSame(query, qCart, uniqueIDs[i], directions[i]))
                {
                    index = i;
                    return true;
                }
            return false;
        }

        private bool IsTheSame(Vector4 a, Vector3 aCartesian, Vector4 b, Vector3 bCartesian)
        {
            // this is only true when the two would be reflections would be less than the angle tolerance apart in the polar angles
            // (pi/2 - s1.AzimuthAngle) + (pi/2 - s2.AzimuthAngle) < angleTolerance
            if (treatReflectionsAsSame && (Math.PI - a.Z - b.Z) < angleTolerance)
            {
                return aCartesian.IsAlignedOrReverse(bCartesian, dotTolerance) && PlaneSquaredDistance(a, b) < sqdDistanceTolerance;
            }
            else return aCartesian.IsAligned(bCartesian, dotTolerance) && PlaneSquaredDistance(a, b) < sqdDistanceTolerance;
        }

        private static double PlaneSquaredDistance(Vector4 a, Vector4 b)
        {
            return (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y);
        }
        #endregion
    }
}
