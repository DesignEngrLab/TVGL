using ClipperLib;
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
        static bool CLIPPER = true;
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
            var joinType = notMiter ? (double.IsNaN(deltaAngle) ? JoinType.jtSquare : JoinType.jtRound) : JoinType.jtMiter;
            //Convert Points (TVGL) to IntPoints (Clipper)
            var clipperSubject = allPolygons.Select(loop => loop.Vertices.Select(point => new IntPoint(point.X * scale, point.Y * scale)).ToList()).ToList();

            //Setup Clipper
            var clip = new ClipperOffset(2, tolerance * scale);
            clip.AddPaths(clipperSubject, joinType, EndType.etClosedPolygon);

            //Begin an evaluation
            var clipperSolution = new List<List<IntPoint>>();
            clip.Execute(clipperSolution, offset * scale);

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
        /// <param name="clipType"></param>
        /// <param name="subject"></param>
        /// <param name="clip"></param>
        /// <param name="simplifyPriorToBooleanOperation"></param>
        /// <param name="scale"></param>
        /// <param name="fillMethod"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static List<Polygon> BooleanViaClipper(PolyFillType fillMethod, ClipType clipType, IEnumerable<Polygon> subject,
            IEnumerable<Polygon> clip = null, bool subjectIsClosed = true, bool clipIsClosed = true)
        {
            //Remove any polygons that are only a line.
            //subject = subject.Where(p => p.Count > 2);
            //clip = clip?.Where(p => p.Count > 2);
            var simplifyPriorToBooleanOperation = true;
            if (simplifyPriorToBooleanOperation)
            {
                //subject = subject.Select(p=>SimplifyFuzzy(p));
                subject = subject.Select(p => Simplify(p, 0.0000003));
            }
            if (simplifyPriorToBooleanOperation)
            {
                //If not null
                //clip = clip?.Select(p => SimplifyFuzzy(p));
                clip = clip?.Select(p => Simplify(p, 0.0000003));
            }
            if (!subject.Any())
            {
                if (clip == null || !clip.Any())
                {
                    return new List<Polygon>();
                }
                //Use the clip as the subject if this is a union operation and the clip is not null.
                if (clipType == ClipType.ctUnion)
                {
                    subject = clip;
                    clip = null;
                }
            }
            var subjectAll = subject.SelectMany(p => p.AllPolygons).ToList();

            var clipperSolution = new List<List<IntPoint>>();
            //Convert Points (TVGL) to IntPoints (Clipper)
            var clipperSubject = 
                subjectAll.Select(loop => loop.Vertices.Select(point => new IntPoint(point.X * scale, point.Y * scale)).ToList()).ToList();

            //Setup Clipper
            var clipper = new ClipperLib.Clipper() { StrictlySimple = true };
            clipper.AddPaths(clipperSubject, PolyType.ptSubject, subjectIsClosed);

            if (clip != null)
            {
                var clipAll = clip.SelectMany(p => p.AllPolygons).ToList();

                var clipperClip =
                    clipAll.Select(loop => loop.Vertices.Select(point => new IntPoint(point.X * scale, point.Y * scale)).ToList()).ToList();
                clipper.AddPaths(clipperClip, PolyType.ptClip, clipIsClosed);
            }

            //Begin an evaluation
            var result = clipper.Execute(clipType, clipperSolution, fillMethod, fillMethod);
            if (!result) throw new Exception("Clipper Union Failed");

            //Convert back to points and return solution
            var solution = clipperSolution.Select(clipperPath => new Polygon(clipperPath.Select(point => new Vector2(point.X / scale, point.Y / scale))));
            return solution.CreateShallowPolygonTrees(true);
        }
        #endregion

    }
}
