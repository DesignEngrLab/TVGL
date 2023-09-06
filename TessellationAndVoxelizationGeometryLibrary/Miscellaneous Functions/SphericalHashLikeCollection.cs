using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace TVGL
{
    public class SphericalHashLikeCollection<T> : SphericalHashLikeCollection
    {
        private readonly List<T> items;
        private readonly IEqualityComparer<T> itemsComparer;

        /// <summary>
        /// Initializes a new instance of the <see cref="SphericalHashLikeCollection"/> class. Here, an itemEqualityComparer is to provided 
        /// to 
        /// </summary>
        /// <param name="ignoreRadius">If true, ignore radius.</param>
        /// <param name="treatReflectionsAsSame">If true, treat reflections as same.</param>
        /// <param name="dotTolerance">The dot tolerance.</param>
        /// <param name="itemEqualityComparer">The item equality comparer.</param>
        public SphericalHashLikeCollection(bool ignoreRadius, bool treatReflectionsAsSame, double dotTolerance, IEqualityComparer<T> itemEqualityComparer)
            : base(ignoreRadius, treatReflectionsAsSame, dotTolerance)
        {
            items = new List<T>();
            itemsComparer = itemEqualityComparer;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="SphericalHashLikeCollection"/> class.
        /// </summary>
        /// <param name="ignoreRadius">If true, ignore radius.</param>
        /// <param name="treatReflectionsAsSame">If true, treat reflections as same.</param>
        /// <param name="sameAngleTolerance">The dot tolerance.</param>
        public SphericalHashLikeCollection(bool ignoreRadius, bool treatReflectionsAsSame, double sameAngleTolerance)
            : base(ignoreRadius, treatReflectionsAsSame, sameAngleTolerance)
        {
            items = new List<T>();
        }

        /// <summary>Adds the specified element to the <see cref="HashSet{T}"/>.</summary>
        /// <param name="item">The element to add to the set.</param>
        /// <returns>true if the element is added to the <see cref="HashSet{T}"/> object; false if the element is already present.</returns>
        public bool Add(SphericalAnglePair spherical, T item) => AddIfNotPresent(spherical, spherical.ToVector3(), item, out _);
        public new bool Add(SphericalAnglePair spherical)
        {
            throw new InvalidOperationException("This method is not supported for this collection. Use the Add(SphericalAnglePair, T) method instead.");
        }

        /// <summary>Adds the specified element to the <see cref="HashSet{T}"/>.</summary>
        /// <param name="item">The element to add to the set.</param>
        /// <returns>true if the element is added to the <see cref="HashSet{T}"/> object; false if the element is already present.</returns>
        public bool Add(Vector3 vector, T item) => AddIfNotPresent(new SphericalAnglePair(vector), vector, item, out _);
        public new bool Add(Vector3 vector)
        {
            throw new InvalidOperationException("This method is not supported for this collection. Use the Add(Vector3, T) method instead.");
        }
        private bool AddIfNotPresent(SphericalAnglePair spherical, Vector3 cartesian, T item, out int i)
        {
            var radius = cartesian.Length();
            if (treatReflectionsAsSame && cartesian.Z < 0)
            {
                var newAzimuth = cartesian.Y < 0 ? spherical.AzimuthAngle + Math.PI : spherical.AzimuthAngle - Math.PI;
                spherical = new SphericalAnglePair(Math.PI - spherical.PolarAngle, newAzimuth);
                cartesian = -cartesian;
            }
            i = BinarySearch(spherical, cartesian, radius, out var matchFound);
            if (matchFound && (itemsComparer == null || item.Equals(default(T)) || itemsComparer.Equals(item, items[i])))
                    return false;
            if (i == sphericals.Count)
            {
                sphericals.Add(spherical);
                cartesians.Add(cartesian);
                items.Add(item);
                radii.Add(radius);
            }
            else
            {
                sphericals.Insert(i, spherical);
                cartesians.Insert(i, cartesian);
                items.Insert(i, item);
                radii.Insert(i, radius);
            }
            return true;
        }

        public new void Clear()
        {
            sphericals.Clear();
            cartesians.Clear();
            items.Clear();
            radii.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sphericalAngles"></param>
        /// <returns></returns>
        public T this[SphericalAnglePair sphericalAngles]
        {
            get { 
                this.TryGet(sphericalAngles, out T item, out _);
                return item;
            }
            set { this.Add(sphericalAngles, value); }
        }

        public bool TryGet(Vector3 cartesian, out T item, out Vector3 matchingCartesian)
            => TryGet(new SphericalAnglePair(cartesian), cartesian, out item, out _, out matchingCartesian);
        public bool TryGet(SphericalAnglePair spherical, out T item, out SphericalAnglePair matchingAnglePair)
            => TryGet(spherical, spherical.ToVector3(), out item, out matchingAnglePair, out _);
        private bool TryGet(SphericalAnglePair spherical, Vector3 cartesian, out T item, out SphericalAnglePair matchingSpherical, out Vector3 matchingCartesian)
        {
            var radius = cartesian.Length();
            if (treatReflectionsAsSame && cartesian.Z < 0)
            {
                var newAzimuth = cartesian.Y < 0 ? spherical.AzimuthAngle + Math.PI : spherical.AzimuthAngle - Math.PI;
                spherical = new SphericalAnglePair(Math.PI - spherical.PolarAngle, newAzimuth);
                cartesian = -cartesian;
            }
            var i = BinarySearch(spherical, cartesian, radius, out var matchFound);
            if (matchFound)
            {
                item = items[i];
                matchingSpherical = sphericals[i];
                matchingCartesian = cartesians[i];
                return true;
            }
            item = default;
            matchingSpherical = default;
            matchingCartesian = default;
            return false;
        }



        public void CopyTo(SphericalAnglePair[] array, int arrayIndex)
        {
            sphericals.CopyTo(array, arrayIndex);
        }

        public new bool Remove(Vector3 cartesian) => Remove(new SphericalAnglePair(cartesian), cartesian, default);
        public new bool Remove(SphericalAnglePair spherical) => Remove(spherical, spherical.ToVector3(), default);
        public new bool Remove(Vector3 cartesian, T item) => Remove(new SphericalAnglePair(cartesian), cartesian, item);
        public new bool Remove(SphericalAnglePair spherical, T item) => Remove(spherical, spherical.ToVector3(), item);
        public new bool Remove(SphericalAnglePair spherical, Vector3 cartesian, T item)
        {
            var radius = cartesian.Length();
            if (treatReflectionsAsSame && cartesian.Z < 0)
            {
                var newAzimuth = cartesian.Y < 0 ? spherical.AzimuthAngle + Math.PI : spherical.AzimuthAngle - Math.PI;
                spherical = new SphericalAnglePair(Math.PI - spherical.PolarAngle, newAzimuth);
                cartesian = -cartesian;
            }
            var i = BinarySearch(spherical, cartesian, radius, out var matchFound);
            if (matchFound && (itemsComparer == null || item.Equals(default(T)) || itemsComparer.Equals(item, items[i])))

            {
                sphericals.RemoveAt(i);
                cartesians.RemoveAt(i);
                items.RemoveAt(i);
                radii.RemoveAt(i);
                return true;
            }
            return false;
        }


        public bool Contains(Vector3 cartesian, T item) => Contains(new SphericalAnglePair(cartesian), cartesian, item);
        public bool Contains(SphericalAnglePair spherical, T item) => Contains(spherical, spherical.ToVector3(), item);
        public bool Contains(Vector3 cartesian) => Contains(new SphericalAnglePair(cartesian), cartesian, default);
        public bool Contains(SphericalAnglePair spherical) => Contains(spherical, spherical.ToVector3(), default);
        public bool Contains(SphericalAnglePair spherical, Vector3 cartesian, T item)
        {
            bool matchFound;
            if (treatReflectionsAsSame && cartesian.Z < 0)
            {
                var newAzimuth = cartesian.Y < 0 ? spherical.AzimuthAngle + Math.PI : spherical.AzimuthAngle - Math.PI;
                spherical = new SphericalAnglePair(Math.PI - spherical.PolarAngle, newAzimuth);
                cartesian = -cartesian;
            }
            var i = BinarySearch(spherical, cartesian, cartesian.Length(), out matchFound);
            return matchFound && (itemsComparer == null || item.Equals(default(T)) || itemsComparer.Equals(item, items[i]));
        }
    }

    public class SphericalHashLikeCollection : ICollection<SphericalAnglePair>
    {
        private protected readonly bool ignoreRadius;
        private protected readonly bool treatReflectionsAsSame;
        private protected readonly double dotTolerance;
        private protected readonly double angleTolerance;
        private protected readonly double distanceTolerance;
        private protected readonly List<SphericalAnglePair> sphericals;
        private protected readonly List<Vector3> cartesians;
        private protected readonly List<double> radii;

        public SphericalHashLikeCollection(bool ignoreRadius, bool treatReflectionsAsSame, double angleDegreesTolerance)
        {
            this.ignoreRadius = ignoreRadius;
            this.treatReflectionsAsSame = treatReflectionsAsSame;
            angleTolerance = Math.PI * angleDegreesTolerance / 180.0;
            dotTolerance = Math.Cos(angleTolerance);  //cos(angle) should be above this value to be considered a match

            // using the law of cosines the distance tolerance and assuming a unit sphere, the length of the chord
            // from tolerance, c, would be c^2 = 2(1-cos(angleTolerance)) = 2(1-dotTolerance)
            distanceTolerance = Math.Sqrt(2 * (1 - dotTolerance));
            sphericals = new List<SphericalAnglePair>();
            cartesians = new List<Vector3>();
            radii = new List<double>();
        }

        public int Count => sphericals.Count;

        /// <summary>
        /// Gets a value indicating whether read is only.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Returns the result as a list of SphericalAnglePairs. 
        /// </summary>
        /// <returns>A list of SphericalAnglePairs.</returns>
        public List<SphericalAnglePair> AsAnglePairs()
        {
            return sphericals;
        }
        /// <summary>
        /// Returns the result as a list of Cartesian Vector3's.
        /// </summary>
        /// <returns>A list of Vector3S.</returns>
        public List<Vector3> AsVector3s()
        {
            return cartesians;
        }


        void ICollection<SphericalAnglePair>.Add(SphericalAnglePair spherical) => AddIfNotPresent(spherical, spherical.ToVector3(), out _);
        /// <summary>Adds the specified element to the <see cref="SphericalHashLikeCollection"/>.</summary>
        /// <param name="item">The element to add to the set.</param>
        /// <returns>true if the element is added to the <see cref="HashSet{T}"/> object; false if the element is already present.</returns>
        public bool Add(SphericalAnglePair spherical) => AddIfNotPresent(spherical, spherical.ToVector3(), out _);

        /// <summary>Adds the specified element to the <see cref="SphericalHashLikeCollection"/>.</summary>
        /// <param name="item">The element to add to the set.</param>
        /// <returns>true if the element is added to the <see cref="HashSet{T}"/> object; false if the element is already present.</returns>
        public bool Add(Vector3 vector) => AddIfNotPresent(new SphericalAnglePair(vector), vector, out _);

        /// <summary>
        /// Adds the if not present.
        /// </summary>
        private bool AddIfNotPresent(SphericalAnglePair spherical, Vector3 cartesian, out int i)
        {
            var radius = cartesian.Length();
            if (treatReflectionsAsSame && cartesian.Z < 0)
            {
                var newAzimuth = cartesian.Y < 0 ? spherical.AzimuthAngle + Math.PI : spherical.AzimuthAngle - Math.PI;
                spherical = new SphericalAnglePair(Math.PI - spherical.PolarAngle, newAzimuth);
                cartesian = -cartesian;
            }
            i = BinarySearch(spherical, cartesian, radius, out var matchFound);
            if (matchFound)
                return false;
            if (i == sphericals.Count)
            {
                sphericals.Add(spherical);
                cartesians.Add(cartesian);
                radii.Add(radius);
            }
            else
            {
                sphericals.Insert(i, spherical);
                cartesians.Insert(i, cartesian);
                radii.Insert(0, radius);
            }
            return true;
        }

        /// <summary>
        /// This binary search is modified/simplified from Array.BinarySearch
        /// (https://referencesource.microsoft.com/mscorlib/a.html#b92d187c91d4c9a9)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected int BinarySearch(SphericalAnglePair spherical, Vector3 cartesian, double radius, out bool matchFound)
        {
            if (sphericals.Count == 0)
            {
                matchFound = false;
                return 0;
            }
            var lo = 0;
            var hi = sphericals.Count - 1;
            while (lo <= hi)
            {
                int i = lo + ((hi - lo) >> 1);
                if (IsTheSame(cartesian, spherical, radius, i))
                {
                    matchFound = true;
                    return i;
                }
                else if (sphericals[i].PolarAngle < spherical.PolarAngle)
                    lo = i + 1;
                else hi = i - 1;
            }
            int index = lo;
            // the following is a unique feature of this binary search
            matchFound = ScanHoop(ref index, spherical, cartesian, radius);
            return index;
        }


        private bool ScanHoop(ref int index, SphericalAnglePair spherical, Vector3 cartesian, double radius)
        {
            var i = index;
            // given that the list is sorted in polar angle, we can scan the hoop up to
            // the angle tolerance in either direction
            // this is particularly important since the azimuth angle has its values near
            // pi and -pi at the same location
            while (++i < sphericals.Count && sphericals[i].PolarAngle - spherical.PolarAngle < angleTolerance)
                if (IsTheSame(cartesian, spherical, radius, i))
                {
                    index = i;
                    return true;
                }
            i = index;
            while (--i >= 0 && spherical.PolarAngle - sphericals[i].PolarAngle < angleTolerance)
                if (IsTheSame(cartesian, spherical, radius, i))
                {
                    index = i;
                    return true;
                }
            return false;
        }

        bool IsTheSame(Vector3 v1, SphericalAnglePair s1, double r1, int existingIndex)
        {
            if (!ignoreRadius && Math.Abs(r1 - radii[existingIndex]) > distanceTolerance) return false;
            // when treatReflectionsAsSame is true, all vectors with negative z are reflected about the origin (NOT the xy plane)
            // this means that the polar angle should only be between 0 and pi/2 and the azimuth angle is shifted by pi
            // however, vectors near the xy plane will have a polar angle near pi/2 and the dot product is not a complete
            // comparison since reflections would yield a negative dot product.
            // this is only true when the two would be reflections would be less than the angle tolerance apart in the polar angles
            /// (pi/2 - s1.AzimuthAngle) + (pi/2 - s2.AzimuthAngle) < angleTolerance
            if (treatReflectionsAsSame && (Math.PI - s1.PolarAngle - sphericals[existingIndex].PolarAngle) < angleTolerance)
            {
                return Math.Abs(v1.Dot(cartesians[existingIndex])) >= r1 * radii[existingIndex] * dotTolerance;
            }
            else return v1.Dot(cartesians[existingIndex]) >= r1 * radii[existingIndex] * dotTolerance;
        }

        public bool TryGet(Vector3 cartesian, out Vector3 matchingCartesian)
            => TryGet(new SphericalAnglePair(cartesian), cartesian, out _, out matchingCartesian);
        public bool TryGet(SphericalAnglePair spherical, out SphericalAnglePair matchingAnglePair)
            => TryGet(spherical, spherical.ToVector3(), out matchingAnglePair, out _);
        private bool TryGet(SphericalAnglePair spherical, Vector3 cartesian, out SphericalAnglePair matchingSpherical, out Vector3 matchingCartesian)
        {
            var radius = cartesian.Length();
            if (treatReflectionsAsSame && cartesian.Z < 0)
            {
                var newAzimuth = cartesian.Y < 0 ? spherical.AzimuthAngle + Math.PI : spherical.AzimuthAngle - Math.PI;
                spherical = new SphericalAnglePair(Math.PI - spherical.PolarAngle, newAzimuth);
                cartesian = -cartesian;
            }
            var i = BinarySearch(spherical, cartesian, radius, out var matchFound);
            if (matchFound)
            {
                matchingSpherical = sphericals[i];
                matchingCartesian = cartesians[i];
                return true;
            }
            matchingSpherical = default;
            matchingCartesian = default;
            return false;
        }




        public void Clear()
        {
            sphericals.Clear();
            cartesians.Clear();
            radii.Clear();
        }

        public bool Contains(Vector3 item) => Contains(new SphericalAnglePair(item), item);
        public bool Contains(SphericalAnglePair spherical) => Contains(spherical, spherical.ToVector3());
        public bool Contains(SphericalAnglePair spherical, Vector3 cartesian)
        {
            bool matchFound;
            if (treatReflectionsAsSame && cartesian.Z < 0)
            {
                var newAzimuth = cartesian.Y < 0 ? spherical.AzimuthAngle + Math.PI : spherical.AzimuthAngle - Math.PI;
                spherical = new SphericalAnglePair(Math.PI - spherical.PolarAngle, newAzimuth);
                cartesian = -cartesian;
            }
            BinarySearch(spherical, cartesian, cartesian.Length(), out matchFound);
            return matchFound;
        }

        public IEnumerator<SphericalAnglePair> GetEnumerator()
        {
            return sphericals.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return sphericals.GetEnumerator();
        }

        public void CopyTo(SphericalAnglePair[] array, int arrayIndex)
        {
            sphericals.CopyTo(array, arrayIndex);
        }

        public bool Remove(Vector3 cartesian) => Remove(new SphericalAnglePair(cartesian), cartesian);
        public bool Remove(SphericalAnglePair spherical) => Remove(spherical, spherical.ToVector3());
        public bool Remove(SphericalAnglePair spherical, Vector3 cartesian)
        {
            var radius = cartesian.Length();
            if (treatReflectionsAsSame && cartesian.Z < 0)
            {
                var newAzimuth = cartesian.Y < 0 ? spherical.AzimuthAngle + Math.PI : spherical.AzimuthAngle - Math.PI;
                spherical = new SphericalAnglePair(Math.PI - spherical.PolarAngle, newAzimuth);
                cartesian = -cartesian;
            }
            var i = BinarySearch(spherical, cartesian, radius, out var matchFound);
            if (matchFound)
            {
                sphericals.RemoveAt(i);
                cartesians.RemoveAt(i);
                radii.RemoveAt(i);
                return true;
            }
            return false;
        }
    }
}
