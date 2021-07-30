using ClipperLib2;
using System;
using System.Collections.Generic;
using System.Linq;
using TVGL.Numerics;

namespace TVGL.TwoDimensional
{
    internal enum BooleanOperationType
    {
        Union,
        Intersection
    };

    public enum PolygonFillType //http://www.angusj.com/delphi/clipper/documentation/Docs/Units/ClipperLib/Types/PolyFillType.htm
    {
        Positive, // (Most common if polygons are ordered correctly and not self-intersecting) All sub-regions with winding counts > 0 are filled.
        EvenOdd,  // (Most common when polygon directions are unknown) Odd numbered sub-regions are filled, while even numbered sub-regions are not.
        Negative, // (Rarely used) All sub-regions with winding counts < 0 are filled.
        NonZero //(Common if polygon directions are unknown) All non-zero sub-regions are filled (used in silhouette because we prefer filled regions).
    };

    /// <summary>
    /// A set of general operation for points and paths
    /// </summary>
    public static partial class PolygonOperations
    {
        const double scale = 1000000;
        #region Offset
        private static List<Polygon> OffsetViaClipper(Polygon polygon, double offset, bool notMiter, double tolerance, double deltaAngle)
        {
            return OffsetViaClipper(new[] { polygon }, offset, notMiter, tolerance, deltaAngle);
        }

        private static List<Polygon> OffsetViaClipper(IEnumerable<Polygon> polygons, double offset, bool notMiter, double tolerance, double deltaAngle)
        {
            var allPolygons = polygons.SelectMany(polygon => polygon.AllPolygons).ToList();
            if (double.IsNaN(tolerance) || tolerance.IsNegligible())
            {
                var totalLength = allPolygons.Sum(loop => loop.Perimeter);
                tolerance = totalLength * 0.001;
            }

            var joinType = notMiter ? (double.IsNaN(deltaAngle) ? JoinType.Square : JoinType.Round) : JoinType.Miter;
            //Convert Points (TVGL) to IntPoints (Clipper)
            var clipperSubject = allPolygons.Select(loop => loop.Vertices.Select(point => new Point64(point.X * scale, point.Y * scale)).ToList()).ToList();

            //Setup Clipper
            var clip = new ClipperOffset(2, Math.Abs(tolerance) * scale);
            clip.AddPaths(clipperSubject, joinType, EndType.Polygon);

            //Begin an evaluation
            var clipperSolution = new List<List<Point64>>();
            clip.Execute(ref clipperSolution, offset * scale);

            //Convert back to points and return solution
            var solution = clipperSolution.Select(clipperPath => new Polygon(clipperPath.Select(point => new Vector2(point.X / scale, point.Y / scale))));
            return solution.CreateShallowPolygonTrees(true);
        }

        public static List<Polygon> OffsetLineViaClipper(this IEnumerable<Vector2> polyline, double offset, double tolerance)
        {
            return OffsetLinesViaClipper(new[] { polyline }, offset, tolerance);
        }

        public static List<Polygon> OffsetLinesViaClipper(this IEnumerable<IEnumerable<Vector2>> polylines, double offset, double tolerance)
        {
            if (double.IsNaN(tolerance) || tolerance.IsNegligible()) tolerance = Constants.BaseTolerance;

            //Convert Points (TVGL) to IntPoints (Clipper)
            var clipperSubject = polylines.Select(line => line.Select(point => new Point64(point.X * scale, point.Y * scale)).ToList()).ToList();

            //Setup Clipper
            var clip = new ClipperOffset(2, tolerance * scale);
            clip.AddPaths(clipperSubject, JoinType.Square, EndType.OpenSquare);

            //Begin an evaluation
            var clipperSolution = new List<List<Point64>>();
            clip.Execute(ref clipperSolution, offset * scale);

            //Convert back to points and return solution
            var solution = clipperSolution.Select(clipperPath => new Polygon(clipperPath.Select(point => new Vector2(point.X / scale, point.Y / scale))));
            return solution.CreateShallowPolygonTrees(true);
        }


        #endregion

        #region Boolean Operations
        /// <summary>
        /// Performs the Boolean Operations from the Clipper Library
        /// </summary>
        /// <param name="clipType"></param>
        /// <param name="subject"></param>
        /// <param name="clip"></param>
        /// <param name="simplifyPriorToBooleanOperation"></param>
        /// <param name="scale"></param>
        /// <param name="fillMethod"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static List<Polygon> BooleanViaClipper(FillRule fillMethod, ClipType clipType, IEnumerable<Polygon> subject,
            IEnumerable<Polygon> clip = null, bool subjectIsClosed = true, bool clipIsClosed = true)
        {
            //Convert to int points and remove collinear edges
            var clipperSubject = new List<List<Point64>>();
            foreach (var polygon in subject)
            {
                foreach(var polygonElement in polygon.AllPolygons.Where(p => !p.PathArea.IsNegligible(Constants.BaseTolerance)))
                {
                    clipperSubject.Add(polygonElement.Path.Select(p => new Point64(p.X * scale, p.Y * scale)).ToList());
                }
            }
            var clipperClip = new List<List<Point64>>();
            if (clip != null)
            {
                foreach (var polygon in clip)
                {
                    foreach (var polygonElement in polygon.AllPolygons.Where(p => !p.PathArea.IsNegligible(Constants.BaseTolerance)))
                    {
                        clipperClip.Add(polygonElement.Path.Select(p => new Point64(p.X * scale, p.Y * scale)).ToList());
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
            var clipper = new Clipper();
            clipper.AddPaths(clipperSubject, PathType.Subject, !subjectIsClosed);

            //Don't add the clip unless it is not null (and has not been set to be the subject - see a few lines above) 
            if (clip != null && clipperClip.Any())
                clipper.AddPaths(clipperClip, PathType.Clip, !clipIsClosed);

            //Begin an evaluation
            var clipperSolution = new List<List<Point64>>();
            var result = clipper.Execute(clipType, clipperSolution, fillMethod);
            if (!result) throw new Exception("Clipper Union Failed");

            //Convert back to points and return solution
            var solution = clipperSolution.Select(clipperPath => new Polygon(clipperPath.Select(point => new Vector2(point.X / scale, point.Y / scale))));
            return solution.CreateShallowPolygonTrees(true);
        }
        #endregion
    }
}
