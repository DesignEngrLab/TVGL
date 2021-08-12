using System;
using System.Collections.Generic;
using System.Linq;
using TVGL;
using TVGL.Numerics;
using TVGL.TwoDimensional;

namespace ClipperLib2Beta
{
    internal enum BooleanOperationType
    {
        Union,
        Intersection
    };



    /// <summary>
    /// A set of general operation for points and paths
    /// </summary>
    public static partial class PolygonOperations
    {
        const double scale = 1000000;
        #region Offset
        //private static List<Polygon> OffsetViaClipper(Polygon polygon, double offset, bool notMiter, double tolerance, double deltaAngle)
        //{
        //    return OffsetViaClipper(new[] { polygon }, offset, notMiter, tolerance, deltaAngle);
        //}

        //private static List<Polygon> OffsetViaClipper(IEnumerable<Polygon> polygons, double offset, bool notMiter, double tolerance, double deltaAngle)
        //{
        //    var allPolygons = polygons.SelectMany(polygon => polygon.AllPolygons).ToList();
        //    if (double.IsNaN(tolerance) || tolerance.IsNegligible())
        //    {
        //        var totalLength = allPolygons.Sum(loop => loop.Perimeter);
        //        tolerance = totalLength * 0.001;
        //    }

        //    var joinType = notMiter ? (double.IsNaN(deltaAngle) ? JoinType.Square : JoinType.Round) : JoinType.Miter;
        //    //Convert Points (TVGL) to IntPoints (Clipper)
        //    var clipperSubject = allPolygons.Select(loop => loop.Vertices.Select(point => new Point64(point.X * scale, point.Y * scale)).ToList()).ToList();

        //    //Setup Clipper
        //    var clip = new ClipperOffset(2, Math.Abs(tolerance) * scale);
        //    clip.AddPaths(clipperSubject, joinType, EndType.Polygon);

        //    //Begin an evaluation
        //    var clipperSolution = new List<List<Point64>>();
        //    clip.Execute(ref clipperSolution, offset * scale);

        //    //Convert back to points and return solution
        //    var solution = clipperSolution.Select(clipperPath => new Polygon(clipperPath.Select(point => new Vector2(point.X / scale, point.Y / scale))));
        //    return solution.CreateShallowPolygonTrees(true);
        //}

        //public static List<Polygon> OffsetLineViaClipper(this IEnumerable<Vector2> polyline, double offset, double tolerance)
        //{
        //    return OffsetLinesViaClipper(new[] { polyline }, offset, tolerance);
        //}

        //public static List<Polygon> OffsetLinesViaClipper(this IEnumerable<IEnumerable<Vector2>> polylines, double offset, double tolerance)
        //{
        //    if (double.IsNaN(tolerance) || tolerance.IsNegligible()) tolerance = Constants.BaseTolerance;

        //    //Convert Points (TVGL) to IntPoints (Clipper)
        //    var clipperSubject = polylines.Select(line => line.Select(point => new Point64(point.X * scale, point.Y * scale)).ToList()).ToList();

        //    //Setup Clipper
        //    var clip = new ClipperOffset(2, tolerance * scale);
        //    clip.AddPaths(clipperSubject, JoinType.Square, EndType.OpenSquare);

        //    //Begin an evaluation
        //    var clipperSolution = new List<List<Point64>>();
        //    clip.Execute(ref clipperSolution, offset * scale);

        //    //Convert back to points and return solution
        //    var solution = clipperSolution.Select(clipperPath => new Polygon(clipperPath.Select(point => new Vector2(point.X / scale, point.Y / scale))));
        //    return solution.CreateShallowPolygonTrees(true);
        //}


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
            IEnumerable<Polygon> clip = null)
        {
            //Convert to int points and remove collinear edges
            var clipperSubject = new PathsI();
            foreach (var polygon in subject)
            {
                foreach(var polygonElement in polygon.AllPolygons.Where(p => !p.PathArea.IsNegligible(Constants.BaseTolerance)))
                {
                    clipperSubject.Add(new PathI(polygonElement.Path.Select(p => new PointI((long)(p.X * scale), (long)(p.Y * scale)))));
                }
            }
            var clipperClip = new PathsI();
            if (clip != null)
            {
                foreach (var polygon in clip)
                {
                    foreach (var polygonElement in polygon.AllPolygons.Where(p => !p.PathArea.IsNegligible(Constants.BaseTolerance)))
                    {
                        clipperClip.Add(new PathI(polygonElement.Path.Select(p => new PointI((long)(p.X * scale), (long)(p.Y * scale)))));
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
            var clipper = new ClipperI();
            clipper.AddPaths(clipperSubject, PathType.Subject, false);

            //Don't add the clip unless it is not null (and has not been set to be the subject - see a few lines above) 
            if (clip != null && clipperClip.Any())
                clipper.AddPaths(clipperClip, PathType.Clip, false);

            //Begin an evaluation
            var clipperSolution = new PathsI();
            var result = clipper.Execute(clipType, fillMethod, clipperSolution);
            if (!result) throw new Exception("Clipper Union Failed");

            //Convert back to points and return solution
            var solution = clipperSolution.data.Select(clipperPath => new Polygon(clipperPath.Select(point => new Vector2(point.x / scale, point.y / scale))));
            return solution.CreateShallowPolygonTrees(true);
        }
        #endregion
    }
}
