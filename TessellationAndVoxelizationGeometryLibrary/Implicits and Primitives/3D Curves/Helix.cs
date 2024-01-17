// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="Helix.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;



namespace TVGL
{
    /// <summary>
    /// Struct Helix
    /// Implements the <see cref="TVGL.ICurve" />
    /// </summary>
    /// <seealso cref="TVGL.ICurve" />
    public readonly struct Helix : ICurve
    {

        /// <summary>
        /// The radius
        /// </summary>
        public readonly double Radius;

        /// <summary>
        /// The pitch
        /// </summary>
        public readonly double Pitch;
        /// <summary>
        /// The number threads
        /// </summary>
        public readonly double NumThreads;
        /// <summary>
        /// The right handed chirality
        /// </summary>
        public readonly bool RightHandedChirality;
        /// <summary>
        /// The anchor
        /// </summary>
        public readonly Vector3 Anchor;

        /// <summary>
        /// The direction
        /// </summary>
        public readonly Vector3 Direction;

        /// <summary>
        /// Initializes a new instance of the <see cref="Helix"/> struct.
        /// </summary>
        /// <param name="anchor">The anchor.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="pitch">The pitch.</param>
        /// <param name="numThreads">The number threads.</param>
        /// <param name="rightHanded">if set to <c>true</c> [right handed].</param>
        public Helix(Vector3 anchor, Vector3 direction, double radius, double pitch, double numThreads, bool rightHanded)
        {
            Anchor = anchor;
            Direction = direction;
            Radius = radius;
            Pitch = pitch;
            NumThreads = numThreads;
            RightHandedChirality = rightHanded;
        }

        /// <summary>
        /// Determines the pitch.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <param name="cyl">The cyl.</param>
        /// <returns>System.Double.</returns>
        public static double DeterminePitch(StraightLine2D line, Cylinder cyl)
        {
            return Constants.TwoPi * cyl.Radius * line.Direction.Y / line.Direction.X;
        }

        /// <summary>
        /// Determines the number threads.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="cyl">The cyl.</param>
        /// <returns>System.Double.</returns>
        public static double DetermineNumThreads(IEnumerable<Vector2> path, Cylinder cyl)
        {
            var start = path.First();
            var end = path.Last();
            /// because we already know it to be a straightline, we can just look at the ends
            return Math.Abs(start.X - end.X) / (Constants.TwoPi * cyl.Radius);
        }

        /// <summary>
        /// Defines the best fit of the curve for the given points.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="points">The points.</param>
        /// <param name="curve">The curve.</param>
        /// <param name="error">The error.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static bool CreateFromPoints<T>(IEnumerable<T> points, out ICurve curve, out double error) where T : IPoint2D
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns the squared error of new point. This should be the square of the
        /// actual distance to the curve. Squared is canonical since 1) usually fits
        /// would be minimum least squares, 2) saves from doing square root operation
        /// which is an undue computational expense
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="point">The point.</param>
        /// <returns>System.Double.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public double SquaredErrorOfNewPoint<T>(T point) where T : IPoint
        {
            throw new NotImplementedException();
        }

    }
}
