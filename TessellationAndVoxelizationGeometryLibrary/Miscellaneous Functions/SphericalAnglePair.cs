using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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


}
