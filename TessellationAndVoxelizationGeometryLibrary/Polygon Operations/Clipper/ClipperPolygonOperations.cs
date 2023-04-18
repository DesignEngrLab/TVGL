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
using ClipperLib;
using System;
using System.Collections.Generic;
using System.Linq;


namespace TVGL
{
    /// <summary>
    /// Enum PolygonFillType
    /// </summary>
    public enum PolygonFillType //http://www.angusj.com/delphi/clipper/documentation/Docs/Units/ClipperLib/Types/PolyFillType.htm
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
        const double scale = 1000000;
        #region Offset
        /// <summary>
        /// Offsets the via clipper.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="notMiter">if set to <c>true</c> [not miter].</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <param name="deltaAngle">The delta angle.</param>
        /// <returns>List&lt;Polygon&gt;.</returns>
        private static List<Polygon> OffsetViaClipper(Polygon polygon, double offset, bool notMiter, double tolerance, double deltaAngle)
        {
            return OffsetViaClipper(new[] { polygon }, offset, notMiter, tolerance, deltaAngle);
        }

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

            var joinType = notMiter ? (double.IsNaN(deltaAngle) ? JoinType.jtSquare : JoinType.jtRound) : JoinType.jtMiter;
            //Convert Points (TVGL) to IntPoints (Clipper)
            var clipperSubject = allPolygons.Select(loop => loop.Vertices.Select(point => new IntPoint(point.X * scale, point.Y * scale)).ToList()).ToList();

            //Setup Clipper
            var clip = new ClipperOffset(2, Math.Abs(tolerance) * scale);
            clip.AddPaths(clipperSubject, joinType, EndType.etClosedPolygon);

            //Begin an evaluation
            var clipperSolution = new List<List<IntPoint>>();
            clip.Execute(clipperSolution, offset * scale);

            //Convert back to points and return solution
            var solution = clipperSolution.Select(clipperPath => new Polygon(clipperPath.Select(point => new Vector2(point.X / scale, point.Y / scale))));
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
            return OffsetLinesViaClipper(new[] { polyline }, offset, tolerance);
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

            //Convert Points (TVGL) to IntPoints (Clipper)
            var clipperSubject = polylines.Select(line => line.Select(point => new IntPoint(point.X * scale, point.Y * scale)).ToList()).ToList();

            //Setup Clipper
            var clip = new ClipperOffset(2, tolerance * scale);
            clip.AddPaths(clipperSubject, JoinType.jtSquare, EndType.etOpenSquare);

            //Begin an evaluation
            var clipperSolution = new List<List<IntPoint>>();
            clip.Execute(clipperSolution, offset * scale);

            //Convert back to points and return solution
            var solution = clipperSolution.Select(clipperPath => new Polygon(clipperPath.Select(point => new Vector2(point.X / scale, point.Y / scale))));
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
        private static List<Polygon> BooleanViaClipper(PolyFillType fillMethod, ClipType clipType, IEnumerable<Polygon> subject,
            IEnumerable<Polygon> clip = null, bool subjectIsClosed = true, bool clipIsClosed = true)
        {
            //Convert to int points and remove collinear edges
            var clipperSubject = new List<List<IntPoint>>();
            foreach (var polygon in subject)
            {
                foreach(var polygonElement in polygon.AllPolygons.Where(p => !p.PathArea.IsNegligible(Constants.BaseTolerance)))
                {
                    clipperSubject.Add(polygonElement.Path.Select(p => new IntPoint(p.X * scale, p.Y * scale)).ToList());
                }
            }
            var clipperClip = new List<List<IntPoint>>();
            if (clip != null)
            {
                foreach (var polygon in clip)
                {
                    foreach (var polygonElement in polygon.AllPolygons.Where(p => !p.PathArea.IsNegligible(Constants.BaseTolerance)))
                    {
                        clipperClip.Add(polygonElement.Path.Select(p => new IntPoint(p.X * scale, p.Y * scale)).ToList());
                    }
                }
            }
            //If subject is null, use the clip as the subject for unions. Else, return empty.
            if (!clipperSubject.Any())
            {
                if (clip == null || !clipperClip.Any())
                {
                    return new List<Polygon>();
                }
                //Use the clip as the subject if this is a union operation and the clip is not null.
                if (clipType == ClipType.Union)
                {
                    clipperSubject = clipperClip;
                    clip = null;
                }
            }

            //Setup Clipper
            var clipper = new ClipperLib.Clipper() { StrictlySimple = true };
            clipper.AddPaths(clipperSubject, PolyType.ptSubject, subjectIsClosed);

            //Don't add the clip unless it is not null (and has not been set to be the subject - see a few lines above) 
            if (clip != null && clipperClip.Any())
                clipper.AddPaths(clipperClip, PolyType.ptClip, clipIsClosed);

            //Begin an evaluation
            var clipperSolution = new List<List<IntPoint>>();
            var result = clipper.Execute(clipType, clipperSolution, fillMethod, fillMethod);
            if (!result) throw new Exception("Clipper Union Failed");

            //Convert back to points and return solution
            var solution = clipperSolution.Select(clipperPath => new Polygon(clipperPath.Select(point => new Vector2(point.X / scale, point.Y / scale))));
            return solution.CreateShallowPolygonTrees(true);
        }
        #endregion
    }
}
