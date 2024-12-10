// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="Vector2.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Numerics;
using System.Runtime.Intrinsics;

namespace TVGL
{
    /// <summary>
    /// A structure encapsulating two single precision floating point values and provides hardware accelerated methods.
    /// </summary>
    internal readonly partial struct Vector2IP : IEquatable<Vector2IP>, IFormattable, IVector2D
    {
        const Int128 InitialW = 45720000;
        internal Int128 X { get; init; }
        internal Int128 Y { get; init; }
        internal Int128 W { get; init; }

        internal Vector2IP(Vector2 vector)
        => new Vector2IP(vector.X, vector.Y, InitialW);
        internal Vector2IP(double x, double y)
        => new Vector2IP(x, y, InitialW);
        internal Vector2IP(double x, double y, Int128 w)
       => new Vector2IP((Int128)x * w, (Int128)y * w, w);
        internal Vector2IP(Int128 x, Int128 y, Int128 w)
        {
            X = x;
            Y = y;
            W = w;
        }
        public static Vector2IP Zero = default;
        public static Vector2IP UnitX = new Vector2IP(Int128.One, Int128.Zero, Int128.One);
        public static Vector2IP UnitX = new Vector2IP(Int128.Zero, Int128.One, Int128.One);

        public bool Equals(Vector2IP that)
        {
            return this.X * that.W == that.X * this.W &&
                this.Y * that.W == that.Y * this.W;
        }
        public Vector2IP Cross(Vector2IP that)
        {
            return new Vector2IP(this.Y * that.W - this.W * that.Y,
                this.W * that.X - this.X * that.W,
                this.X * that.Y - this.Y * that.W);
        }
    }
}