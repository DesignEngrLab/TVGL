using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace TVGL
{
    /// <summary>
    /// SphericalAnglePair is a simple struct that holds two angles, the polar angle and the azimuth angle.
    /// The convention follows the description of the first figure at https://wikipedia.org/wiki/Spherical_coordinate_system
    /// ISO physics convention, where polar angle is measured as deviation from z axis
    /// </summary>
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
            return new SphericalAnglePair(point - center);
        }

        /// <summary>
        /// Converts a cartesian coordinate to spherical angles based at the origin.
        /// </summary>
        /// <param name="p">The p.</param>
        /// <returns>System.ValueTuple&lt;System.Double, System.Double&gt;.</returns>
        public static SphericalAnglePair ConvertToSphericalAngles(Vector3 p)
        {
            return new SphericalAnglePair(p);
        }

        /// <summary>
        /// Converts a cartesian coordinate to spherical angles based at the origin.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="z">The z.</param>
        /// <returns>System.ValueTuple&lt;System.Double, System.Double&gt;.</returns>
        public static SphericalAnglePair ConvertToSphericalAngles(double x, double y, double z)
        => new SphericalAnglePair(new Vector3(x, y, z));


        public Vector3 ToVector3() => ConvertSphericalToCartesian(1, PolarAngle, AzimuthAngle);
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

        // the following code is trying to create a int32 hash from the two angles
        const int maxInt = 32768; // 2^15, so  this would occupy half of the bits of an int32
        const double maxIntDivPi = maxInt / Math.PI;
        const double sameDotTolerance = 5e-8; // this is based on the fact that the maxInt is 32768,

        // which means that the max difference in the angles is 2*pi/32768
        // cos(2*pi/32768) = 0.99999998161642933 (about 0.005 degrees), then I doubled that to be safe
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

    }

    public class SphericalHashLikeCollection : ICollection<SphericalAnglePair>
    {
        private readonly double dotTolerance;
        private readonly double angleTolerance;
        private readonly List<SphericalAnglePair> sphericals;
        private readonly List<Vector3> cartesians;

        public SphericalHashLikeCollection(double dotTolerance)
        {
            this.dotTolerance = dotTolerance;
            angleTolerance = Math.Asin(dotTolerance);
            sphericals = new List<SphericalAnglePair>();
            cartesians = new List<Vector3>();
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
            i = BinarySearch(spherical, cartesian, out var matchFound);
            if (matchFound)
                return false;
            if (i == sphericals.Count)
            {
                sphericals.Add(spherical);
                cartesians.Add(cartesian);
            }
            else
            {
                sphericals.Insert(i, spherical);
                cartesians.Insert(i, cartesian);
            }
            return true;
        }

        // This binary search is modified/simplified from Array.BinarySearch
        // (https://referencesource.microsoft.com/mscorlib/a.html#b92d187c91d4c9a9)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int BinarySearch(SphericalAnglePair spherical, Vector3 cartesian, out bool matchFound)
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

                if (cartesians[i].Dot(cartesian).IsPracticallySame(1, dotTolerance))
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
            matchFound = ScanHoop(ref index, spherical, cartesian);
            return index;
        }

        private bool ScanHoop(ref int index, SphericalAnglePair spherical, Vector3 cartesian)
        {
            var i = index;
            // given that the list is sorted in polar angle, we can scan the hoop up to
            // the angle tolerance in either direction
            // this is particularly important since the azimuth angle has its values near
            // pi and -pi at the same location
            while (++i < sphericals.Count && sphericals[i].PolarAngle - spherical.PolarAngle < angleTolerance)
                if (cartesians[i].Dot(cartesian).IsPracticallySame(1, dotTolerance))
                {
                    index = i;
                    return true;
                }
            i = index;
            while (--i >= 0 && spherical.PolarAngle - sphericals[i].PolarAngle < angleTolerance)
                if (cartesians[i].Dot(cartesian).IsPracticallySame(1, dotTolerance))
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
            BinarySearch(spherical, cartesian, out var matchFound);
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
            var i = BinarySearch(spherical, cartesian, out var matchFound);
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
