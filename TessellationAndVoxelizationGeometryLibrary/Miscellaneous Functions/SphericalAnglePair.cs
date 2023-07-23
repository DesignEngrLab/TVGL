using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace TVGL
{
    public readonly struct SphericalAnglePair : IEquatable<SphericalAnglePair>
    {
        public double PolarAngle { get; }
        public double AzimuthAngle { get; }
        public SphericalAnglePair(double polarAngle, double azimuthAngle)
        {
            PolarAngle = polarAngle;
            AzimuthAngle = azimuthAngle;
        }

        public SphericalAnglePair(Vector3 v)
        {
            var radius = Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
            PolarAngle = Math.Acos(v.Z / radius);
            AzimuthAngle = Math.Atan2(v.Y, v.X);
        }


        /// <summary>
        /// Converts a cartesian coordinate (with a provided center) to spherical angles.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="center">The center.</param>
        /// <returns>System.ValueTuple&lt;System.Double, System.Double&gt;.</returns>
        public static SphericalAnglePair ConvertToSphericalAngles(Vector3 point, Vector3 center)
        {
            return ConvertToSphericalAngles(point - center);
        }

        /// <summary>
        /// Converts a cartesian coordinate to spherical angles based at the origin.
        /// </summary>
        /// <param name="p">The p.</param>
        /// <returns>System.ValueTuple&lt;System.Double, System.Double&gt;.</returns>
        public static SphericalAnglePair ConvertToSphericalAngles(Vector3 p)
        {
            return ConvertToSphericalAngles(p.X, p.Y, p.Z);
        }

        /// <summary>
        /// Converts a cartesian coordinate to spherical angles based at the origin.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="z">The z.</param>
        /// <returns>System.ValueTuple&lt;System.Double, System.Double&gt;.</returns>
        public static SphericalAnglePair ConvertToSphericalAngles(double x, double y, double z)
        {
            // this follow the description and first figure at https://wikipedia.org/wiki/Spherical_coordinate_system
            // ISO physics convention, where polar angle is measured as deviation from z axis
            var radius = Math.Sqrt(x * x + y * y + z * z);
            var polarAngle = Math.Acos(z / radius);
            var azimuthAngle = Math.Atan2(y, x);
            return new SphericalAnglePair(polarAngle, azimuthAngle);
        }


        /// <summary>
        /// Converts the spherical coordinate to cartesian coordinates.
        /// </summary>
        /// <param name="radius">The radius.</param>
        /// <param name="polarAngle">The polar angle.</param>
        /// <param name="azimuthAngle">The azimuth angle.</param>
        /// <returns>Vector3.</returns>
        public static Vector3 ConvertSphericalToCartesian(double radius, double polarAngle, double azimuthAngle)
        {
            var sinPolar = Math.Sin(polarAngle);
            var cosPolar = Math.Cos(polarAngle);
            var sinAzimuth = Math.Sin(azimuthAngle);
            var cosAzimuth = Math.Cos(azimuthAngle);

            return new Vector3(radius * cosAzimuth * sinPolar, radius * sinAzimuth * sinPolar, radius * cosPolar);
        }

        const int maxInt = 32768;
        const double maxIntDivPi = maxInt / Math.PI;
        const double sameDotTolerance = 5e-8; // this is based on the fact that the maxInt is 32768,

        // which means that the max difference in the angles is 2*pi/32768
        // cos(2*pi/32768) = 0.99999998161642933, then I doubled that to be safe
        //const double piDivMaxInt = Math.PI / maxInt;
        public override int GetHashCode()
        {
            if (PolarAngle.IsPracticallySame(0, sameDotTolerance))
                return 0;
            if (PolarAngle.IsPracticallySame(Math.PI, sameDotTolerance))
                return maxInt;
            var polarInt = (int)(PolarAngle * maxIntDivPi);
            var azimuthInt = (AzimuthAngle < -Math.PI + sameDotTolerance)
                ? maxInt * maxInt : maxInt * (int)(AzimuthAngle * maxIntDivPi);
            return azimuthInt + polarInt;
        }

        public bool Equals(SphericalAnglePair other)
        {
            var v1 = ConvertSphericalToCartesian(1, PolarAngle, AzimuthAngle);
            var v2 = ConvertSphericalToCartesian(1, other.PolarAngle, other.AzimuthAngle);
            return (v1.Dot(v2).IsPracticallySame(1, sameDotTolerance));
        }
        public override bool Equals([NotNullWhen(true)] object obj)
        {
            if (obj is SphericalAnglePair otherSpherical)
                return Equals(otherSpherical);
            return false;
        }

        public Vector3 ToVector3()
        {
            var sinPolar = Math.Sin(PolarAngle);
            var cosPolar = Math.Cos(PolarAngle);
            var sinAzimuth = Math.Sin(AzimuthAngle);
            var cosAzimuth = Math.Cos(AzimuthAngle);

            return new Vector3(cosAzimuth * sinPolar, sinAzimuth * sinPolar, cosPolar);
        }
    }


    public class SphericalAngleComparer : IEqualityComparer<SphericalAnglePair>
    {
        public SphericalAngleComparer(double tolerance)
        {
            Tolerance = tolerance;
        }

        public double Tolerance { get; }

        public bool Equals(SphericalAnglePair x, SphericalAnglePair y)
        {
            var v1 = SphericalAnglePair.ConvertSphericalToCartesian(1, x.PolarAngle, x.AzimuthAngle);
            var v2 = SphericalAnglePair.ConvertSphericalToCartesian(1, y.PolarAngle, y.AzimuthAngle);
            return v1.Dot(v2).IsPracticallySame(1, Tolerance);
        }

        public int GetHashCode([DisallowNull] SphericalAnglePair obj)
        {
            return obj.GetHashCode();
        }
    }


    public class SphericalSorterPolarThenAzimuth : IComparer<SphericalAnglePair>
    {
        public int Compare(SphericalAnglePair x, SphericalAnglePair y)
        {
            if (x.PolarAngle.IsPracticallySame(y.PolarAngle))
                return (x.AzimuthAngle < y.AzimuthAngle) ? -1 : 1;
            return (x.PolarAngle < y.PolarAngle) ? -1 : 1;
        }
    }

    public class SphericalHashLikeCollection : ICollection<SphericalAnglePair>
    {
        private readonly double angleTolerance;
        private readonly List<SphericalAnglePair> sl;
        private readonly SphericalAngleComparer equalityComparer;
        private readonly SphericalSorterPolarThenAzimuth sorter;

        public SphericalHashLikeCollection(double tolerance)
        {
            angleTolerance = Math.Acos(tolerance);
            sl = new List<SphericalAnglePair>();
            equalityComparer = new SphericalAngleComparer(tolerance);
            sorter = new SphericalSorterPolarThenAzimuth();
        }

        public int Count => sl.Count;

        public bool IsReadOnly => false;

        public IEnumerable<SphericalAnglePair> AsAnglePairs()
        {
            return sl;
        }
        public IEnumerable<Vector3> AsVector3s()
        {
            return sl.Select(x => x.ToVector3());
        }
        void ICollection<SphericalAnglePair>.Add(SphericalAnglePair item) => AddIfNotPresent(item, out _);
        public bool Add(SphericalAnglePair item) => AddIfNotPresent(item, out _);
        public bool Add(Vector3 item) => AddIfNotPresent(new SphericalAnglePair(item), out _);
        private bool AddIfNotPresent(SphericalAnglePair item, out int i)
        {
            i = BinarySearch(item, out var matchFound);
            if (matchFound)
                return false;
            if (i == sl.Count)
                sl.Add(item);
            else sl.Insert(i, item);
            return true;
        }

        // This binary search is modified/simplified from Array.BinarySearch
        // (https://referencesource.microsoft.com/mscorlib/a.html#b92d187c91d4c9a9)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int BinarySearch(SphericalAnglePair value, out bool matchFound)
        {
            if (sl.Count == 0)
            {
                matchFound = false;
                return 0;
            }
            var lo = 0;
            var hi = sl.Count - 1;
            while (lo <= hi)
            {
                int i = lo + ((hi - lo) >> 1);
                if (equalityComparer.Equals(sl[i], value))
                {
                    matchFound = true;
                    return i;
                }
                var c = sorter.Compare(sl[i], value);
                if (c < 0) lo = i + 1;
                else hi = i - 1;
            }
            int index = lo;
            // the following is a unique feature of this binary search
            matchFound = ScanHoop(ref index, value);
            return index;
        }

        private bool ScanHoop(ref int index, SphericalAnglePair value)
        {
            var i = index;
            // given that the list is sorted in polar angle, we can scan the hoop up to
            // the angle tolerance in either direction
            // this is particularly important since the azimuth angle has its values near
            // pi and -pi at the same location
            while (++i < sl.Count && sl[i].PolarAngle - value.PolarAngle < angleTolerance)
                if (equalityComparer.Equals(sl[i], value))
                {
                    index = i;
                    return true;
                }
            i = index;
            while (--i >= 0 && value.PolarAngle - sl[i].PolarAngle < angleTolerance)
                if (equalityComparer.Equals(sl[i], value))
                {
                    index = i;
                    return true;
                }
            return false;
        }

        public void Clear()
        {
            sl.Clear();
        }

        public bool Contains(Vector3 item) => Contains(new SphericalAnglePair(item));
        public bool Contains(SphericalAnglePair item)
        {
            BinarySearch(item, out var matchFound);
            return matchFound;
        }

        public IEnumerator<SphericalAnglePair> GetEnumerator()
        {
            return sl.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return sl.GetEnumerator();
        }
        /*** these were going to be needed if implementing IList, but they don't make sense as
         *   we don't want users to be able to insert or remove items at arbitrary positions
        public int IndexOf(SphericalAnglePair item)
        {
            var i = BinarySearch(item, out var matchFound);
            if (matchFound)
                return i;
            return -1;
        }

        public SphericalAnglePair this[int index] { get => sl[index]; set => sl[index] = value; }
        public void Insert(int index, SphericalAnglePair item)
        {
            throw new NotSupportedException("Because the collection uses a sorted list underneath.");
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException("Because the collection uses a sorted list underneath.");
        }
        */

        public void CopyTo(SphericalAnglePair[] array, int arrayIndex)
        {
            sl.CopyTo(array, arrayIndex);
        }

        public bool Remove(Vector3 item) => Remove(new SphericalAnglePair(item));
        public bool Remove(SphericalAnglePair item)
        {
            var i = BinarySearch(item, out var matchFound);
            if (matchFound)
            {
                sl.RemoveAt(i);
                return true;
            }
            return false;
        }
    }
}
