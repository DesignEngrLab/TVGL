using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace TVGL
{
    public class SphericalHashLikeCollection<T> : SphericalHashLikeCollection
    {
        private readonly List<T> items;

        public SphericalHashLikeCollection(bool ignoreRadius, double dotTolerance) : base(ignoreRadius, dotTolerance)
        {
            items = new List<T>();
        }

        public bool Add(SphericalAnglePair spherical, T item) => AddIfNotPresent(spherical, spherical.ToVector3(), item, out _);
        public new bool Add(SphericalAnglePair spherical)
        {
            throw new InvalidOperationException("This method is not supported for this collection. Use the Add(SphericalAnglePair, T) method instead.");
        }
        public bool Add(Vector3 vector, T item) => AddIfNotPresent(new SphericalAnglePair(vector), vector, item, out _);
        public new bool Add(Vector3 vector)
        {
            throw new InvalidOperationException("This method is not supported for this collection. Use the Add(Vector3, T) method instead.");
        }
        private bool AddIfNotPresent(SphericalAnglePair spherical, Vector3 cartesian, T item, out int i)
        {
            var radius = ignoreRadius ? 1 : cartesian.Length();
            i = ignoreRadius ? BinarySearchIgnoreRadius(spherical, cartesian, out var matchFound) :
                BinarySearchWithRadius(spherical, cartesian, radius, out matchFound);
            if (matchFound)
                return false;
            if (i == sphericals.Count)
            {
                sphericals.Add(spherical);
                cartesians.Add(cartesian);
                items.Add(item);
            }
            else
            {
                sphericals.Insert(i, spherical);
                cartesians.Insert(i, cartesian);
                items.Insert(i, item);
            }
            return true;
        }

        public new void Clear()
        {
            sphericals.Clear();
            cartesians.Clear();
            items.Clear();
        }




        public bool TryGet(Vector3 cartesian, out T item, out Vector3 matchingCartesian)
            => TryGet(new SphericalAnglePair(cartesian), cartesian, out item, out _, out matchingCartesian);
        public bool TryGet(SphericalAnglePair spherical, out T item, out SphericalAnglePair matchingAnglePair)
            => TryGet(spherical, spherical.ToVector3(), out item, out matchingAnglePair, out _);
        private bool TryGet(SphericalAnglePair spherical, Vector3 cartesian, out T item, out SphericalAnglePair matchingSpherical, out Vector3 matchingCartesian)
        {
            var radius = ignoreRadius ? 1 : cartesian.Length();
            var i = ignoreRadius ? BinarySearchIgnoreRadius(spherical, cartesian, out var matchFound) :
                  BinarySearchWithRadius(spherical, cartesian, radius, out matchFound);
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

        public new bool Remove(Vector3 cartesian) => Remove(new SphericalAnglePair(cartesian), cartesian);
        public new bool Remove(SphericalAnglePair spherical) => Remove(spherical, spherical.ToVector3());
        public new bool Remove(SphericalAnglePair spherical, Vector3 cartesian)
        {
            var radius = ignoreRadius ? 1 : cartesian.Length();
            var i = ignoreRadius ? BinarySearchIgnoreRadius(spherical, cartesian, out var matchFound) :
                  BinarySearchWithRadius(spherical, cartesian, radius, out matchFound);
            if (matchFound)
            {
                sphericals.RemoveAt(i);
                cartesians.RemoveAt(i);
                items.RemoveAt(i);
                return true;
            }
            return false;
        }
    }

    public class SphericalHashLikeCollection : ICollection<SphericalAnglePair>
    {
        private protected readonly bool ignoreRadius;
        private protected readonly double dotTolerance;
        private protected readonly double angleTolerance;
        private protected readonly double distanceTolerance;
        private protected readonly List<SphericalAnglePair> sphericals;
        private protected readonly List<Vector3> cartesians;
        private protected readonly List<double> radii;

        public SphericalHashLikeCollection(bool ignoreRadius, double angleDegreesTolerance)
        {
            this.ignoreRadius = ignoreRadius;
            angleTolerance = Math.PI * angleDegreesTolerance / 180.0;
            dotTolerance = Math.Cos(angleTolerance);  //cos(angle) should be above this value to be considered a match

            // using the law of cosines the distance tolerance and assuming a unit sphere, the length of the chord
            // from tolerance, c, would be c^2 = 2(1-cos(angleTolerance)) = 2(1-dotTolerance)
            distanceTolerance = Math.Sqrt(2 * (1 - dotTolerance));
            sphericals = new List<SphericalAnglePair>();
            cartesians = new List<Vector3>();
            if (!ignoreRadius)
                radii = new List<double>();
        }

        public int Count => sphericals.Count;

        public bool IsReadOnly => false;

        public List<SphericalAnglePair> AsAnglePairs()
        {
            return sphericals;
        }
        public List<Vector3> AsVector3s()
        {
            return cartesians;
        }
        void ICollection<SphericalAnglePair>.Add(SphericalAnglePair spherical) => AddIfNotPresent(spherical, spherical.ToVector3(), out _);
        public bool Add(SphericalAnglePair spherical) => AddIfNotPresent(spherical, spherical.ToVector3(), out _);
        public bool Add(Vector3 vector) => AddIfNotPresent(new SphericalAnglePair(vector), vector, out _);
        private bool AddIfNotPresent(SphericalAnglePair spherical, Vector3 cartesian, out int i)
        {
            var radius = ignoreRadius ? 1 : cartesian.Length();
            i = ignoreRadius ? BinarySearchIgnoreRadius(spherical, cartesian, out var matchFound) :
                BinarySearchWithRadius(spherical, cartesian, radius, out matchFound);
            if (matchFound)
                return false;
            if (i == sphericals.Count)
            {
                sphericals.Add(spherical);
                cartesians.Add(cartesian);
                if (!ignoreRadius)
                    radii.Add(radius);
            }
            else
            {
                sphericals.Insert(i, spherical);
                cartesians.Insert(i, cartesian);
                if (!ignoreRadius)
                    radii.Insert(0, radius);
            }
            return true;
        }

        // This binary search is modified/simplified from Array.BinarySearch
        // (https://referencesource.microsoft.com/mscorlib/a.html#b92d187c91d4c9a9)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected int BinarySearchIgnoreRadius(SphericalAnglePair spherical, Vector3 cartesian, out bool matchFound)
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

                if (cartesians[i].Dot(cartesian) >= dotTolerance)
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
            matchFound = ScanHoopIgnoreRadius(ref index, spherical, cartesian);
            return index;
        }

        private bool ScanHoopIgnoreRadius(ref int index, SphericalAnglePair spherical, Vector3 cartesian)
        {
            var i = index;
            // given that the list is sorted in polar angle, we can scan the hoop up to
            // the angle tolerance in either direction
            // this is particularly important since the azimuth angle has its values near
            // pi and -pi at the same location
            while (++i < sphericals.Count && sphericals[i].PolarAngle - spherical.PolarAngle < angleTolerance)
            {
                if (cartesians[i].Dot(cartesian) >= dotTolerance)
                {
                    index = i;
                    return true;
                }
            }
            i = index;
            while (--i >= 0 && spherical.PolarAngle - sphericals[i].PolarAngle < angleTolerance)
            {
                if (cartesians[i].Dot(cartesian) >= dotTolerance)
                {
                    index = i;
                    return true;
                }
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected int BinarySearchWithRadius(SphericalAnglePair spherical, Vector3 cartesian, double radius, out bool matchFound)
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
                if (Math.Abs(radius - radii[i]) <= distanceTolerance
                    && cartesians[i].Dot(cartesian) >= radius * radii[i] * dotTolerance)
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
            matchFound = ScanHoopWithRadius(ref index, spherical, cartesian, radius);
            return index;
        }

        private bool ScanHoopWithRadius(ref int index, SphericalAnglePair spherical, Vector3 cartesian, double radius)
        {
            var i = index;
            // given that the list is sorted in polar angle, we can scan the hoop up to
            // the angle tolerance in either direction
            // this is particularly important since the azimuth angle has its values near
            // pi and -pi at the same location
            while (++i < sphericals.Count && sphericals[i].PolarAngle - spherical.PolarAngle < angleTolerance)
                if (Math.Abs(radius - radii[i]) <= distanceTolerance
                    && cartesians[i].Dot(cartesian) >= radius * radii[i] * dotTolerance)
                {
                    index = i;
                    return true;
                }
            i = index;
            while (--i >= 0 && spherical.PolarAngle - sphericals[i].PolarAngle < angleTolerance)
                if (Math.Abs(radius - radii[i]) <= distanceTolerance
                    && cartesians[i].Dot(cartesian) >= radius * radii[i] * dotTolerance)
                {
                    index = i;
                    return true;
                }
            return false;
        }

        public void Clear()
        {
            sphericals.Clear();
            cartesians.Clear();
        }

        public bool Contains(Vector3 item) => Contains(new SphericalAnglePair(item), item);
        public bool Contains(SphericalAnglePair spherical) => Contains(spherical, spherical.ToVector3());
        public bool Contains(SphericalAnglePair spherical, Vector3 cartesian)
        {
            bool matchFound;
            if (ignoreRadius)
                BinarySearchIgnoreRadius(spherical, cartesian, out matchFound);
            else
            {
                var radius = cartesian.Length();
                BinarySearchWithRadius(spherical, cartesian, radius, out matchFound);

            }
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
            var radius = ignoreRadius ? 1 : cartesian.Length();
            var i = ignoreRadius ? BinarySearchIgnoreRadius(spherical, cartesian, out var matchFound) :
                    BinarySearchWithRadius(spherical, cartesian, radius, out matchFound);
            if (matchFound)
            {
                sphericals.RemoveAt(i);
                cartesians.RemoveAt(i);
                return true;
            }
            return false;
        }
    }
}
