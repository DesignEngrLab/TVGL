// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="ClipperPolygonOperations.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using Clipper2Lib;
using System;
using System.Collections.Generic;
using System.Linq;


namespace TVGL
{
    /// <summary>
    /// Enum PolygonFillType
    /// </summary>
    public enum PolygonFillType //http://www.angusj.com/delphi/clipper/documentation/Docs/Units/Clipper2Lib/Types/PolyFillType.htm
    {
        /// <summary>
        /// The positive
        /// </summary>
        Positive, // (Most common if polygons are ordered correctly and not self-intersecting) All sub-regions with winding counts > 0 are filled.
        /// <summary>
        /// The even odd
        /// </summary>
        EvenOdd,  // (Most common when polygon directions are unknown) Odd numbered sub-regions are filled, while even numbered sub-regions are not.
        /// <summary>
        /// The negative
        /// </summary>
        Negative, // (Rarely used) All sub-regions with winding counts < 0 are filled.
        /// <summary>
        /// The non zero
        /// </summary>
        NonZero //(Common if polygon directions are unknown) All non-zero sub-regions are filled (used in silhouette because we prefer filled regions).
    };

    /// <summary>
    /// A set of general operation for points and paths
    /// </summary>
    public static partial class PolygonOperations
    {
        /// <summary>
        /// The scale
        /// </summary>
        //const double scale = 1000000; // this is (2^6)*(3^2)*(5^4)*127 = 45720000
        const double scale = 45720000; // this is (2^6)*(3^2)*(5^4)*127 = 45720000
        // why this number? see my reasoning here: https://github.com/DesignEngrLab/TVGL/wiki/Determining-the-Double-to-Long-Dimension-Multiplier
        const double invScale = 1 / scale;
        #region Offset
        /// <summary>
        /// Offsets the via clipper.
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="notMiter">if set to <c>true</c> [not miter].</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <param name="deltaAngle">The delta angle.</param>
        /// <returns>List&lt;Polygon&gt;.</returns>
        private static List<Polygon> OffsetViaClipper(IEnumerable<Polygon> polygons, double offset, bool notMiter, double tolerance, double deltaAngle)
        {
            var allPolygons = polygons.SelectMany(polygon => polygon.AllPolygons).ToList();
            if (double.IsNaN(tolerance) || tolerance.IsNegligible())
            {
                var totalLength = allPolygons.Sum(loop => loop.Perimeter);
                tolerance = totalLength * 0.001;
            }

            var joinType = notMiter ? (double.IsNaN(deltaAngle) ? JoinType.Square : JoinType.Round) : JoinType.Miter;
            //Convert Points (TVGL) to Point64s (Clipper)
            var clipperSubject = new Paths64(allPolygons.Select(loop
                => new Path64(loop.Vertices.Select(point => new Point64(point.X * scale, point.Y * scale)))));

            //Setup Clipper
            var clip = new ClipperOffset(2, Math.Abs(tolerance) * scale);
            clip.AddPaths(clipperSubject, joinType, EndType.Polygon);

            //Begin an evaluation
            clip.Execute(offset * scale, clipperSubject);

            //Convert back to points and return solution
            var solution = clipperSubject.Select(clipperPath => new Polygon(clipperPath.Select(point => new Vector2(point.X * invScale, point.Y * invScale))));
            return solution.CreateShallowPolygonTrees(true);
        }

        /// <summary>
        /// Offsets the line via clipper.
        /// </summary>
        /// <param name="polyline">The polyline.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>List&lt;Polygon&gt;.</returns>
        public static List<Polygon> OffsetLineViaClipper(this IEnumerable<Vector2> polyline, double offset, double tolerance)
        {
            return OffsetLinesViaClipper([polyline], offset, tolerance);
        }

        /// <summary>
        /// Offsets the lines via clipper.
        /// </summary>
        /// <param name="polylines">The polylines.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>List&lt;Polygon&gt;.</returns>
        public static List<Polygon> OffsetLinesViaClipper(this IEnumerable<IEnumerable<Vector2>> polylines, double offset, double tolerance)
        {
            if (double.IsNaN(tolerance) || tolerance.IsNegligible()) tolerance = Constants.BaseTolerance;

            //Convert Points (TVGL) to Point64s (Clipper)
            var clipperSubject = new Paths64(polylines.Select(line => new Path64(line.Select(point => new Point64(point.X * scale, point.Y * scale)))));

            //Setup Clipper
            var clip = new ClipperOffset(2, Math.Abs(tolerance) * scale);
            clip.AddPaths(clipperSubject, JoinType.Square, EndType.Square);

            //Begin an evaluation
            clip.Execute(offset * scale, clipperSubject);

            //Convert back to points and return solution
            var solution = clipperSubject.Select(clipperPath => new Polygon(clipperPath.Select(point => new Vector2(point.X * invScale, point.Y * invScale))));
            return solution.CreateShallowPolygonTrees(true);
        }


        #endregion

        #region Boolean Operations
        /// <summary>
        /// Performs the Boolean Operations from the Clipper Library
        /// </summary>
        /// <param name="fillMethod">The fill method.</param>
        /// <param name="clipType">Type of the clip.</param>
        /// <param name="subject">The subject.</param>
        /// <param name="clip">The clip.</param>
        /// <param name="subjectIsClosed">if set to <c>true</c> [subject is closed].</param>
        /// <param name="clipIsClosed">if set to <c>true</c> [clip is closed].</param>
        /// <returns>List&lt;Polygon&gt;.</returns>
        /// <exception cref="System.Exception">Clipper Union Failed</exception>
        private static List<Polygon> BooleanViaClipper(FillRule fillMethod, ClipType clipType, IEnumerable<Polygon> subject,
            IEnumerable<Polygon> clip, PolygonCollection outputAsCollectionType)
        {
            //Convert to int points and remove collinear edges
            var clipperSubject = ConvertToClipperPaths(subject);
            var clipperClip = ConvertToClipperPaths(clip);

            var clipperSolution = Clipper.BooleanOp(clipType, clipperSubject, clipperClip, fillMethod);
            //Convert back to points and return solution
            var solution = clipperSolution.Select(clipperPath
                => new Polygon(clipperPath.Select(point => new Vector2(point.X / scale, point.Y / scale))));

            if (outputAsCollectionType == PolygonCollection.PolygonWithHoles)
                return solution.CreateShallowPolygonTrees(true);
            if (outputAsCollectionType == PolygonCollection.PolygonTrees)
                return solution.CreatePolygonTree(true);
            return solution.ToList();
        }

        internal static Paths64 ConvertToClipperPaths(IEnumerable<Polygon> subject)
        {
            if (subject == null) return new Paths64();
            var clipperSubject = new Paths64();
            foreach (var polygon in subject)
            {
                if (polygon == null) continue;
                foreach (var polygonElement in polygon.AllPolygons.Where(p => !p.PathArea.IsNegligible(Constants.BaseTolerance)))
                    clipperSubject.Add(new Path64(polygonElement.Path.Select(p => new Point64(p.X * scale, p.Y * scale))));
            }
            return clipperSubject;
        }
        #endregion
    }
}
