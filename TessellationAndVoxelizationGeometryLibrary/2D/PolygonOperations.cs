using System;
using System.Collections.Generic;
using System.Linq;
using TVGL.Clipper;

namespace TVGL
{
    using Path = List<Point>;
    using Paths = List<List<Point>>;

    #region Polygon Operations
    /// <summary>
    /// A set of general operation for points and polygons
    /// </summary>
    public class PolygonOperations
    {
        #region Clockwise / CounterClockwise Ordering
        /// <summary>
        /// Sets a polygon to counter clock wise positive
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        /// <assumptions>
        /// 1. the polygon is closed
        /// 2. the last point is not repeated.
        /// 3. the polygon is simple (does not intersect itself or have holes)
        /// </assumptions>
        public static List<Point> CCWPositive(IList<Point> p)
        {
            var polygon = new List<Point>(p);
            var area = MiscFunctions.AreaOfPolygon(p.ToArray());
            if (area < 0) polygon.Reverse();
            return polygon;
        }

        /// <summary>
        /// Sets a polygon to clock wise negative
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static List<Point> CWNegative(IList<Point> p)
        {
            var polygon = new List<Point>(p);
            var area = MiscFunctions.AreaOfPolygon(p.ToArray());
            if (area > 0) polygon.Reverse();
            return polygon;
        }
        #endregion

        #region Simplify
        /// <summary>
        /// Simplifies a polygon, by removing self intersection. This may output several polygons.
        /// </summary>
        /// <param name="polygon"></param>
        /// <returns></returns>
        public static List<List<Point>> Simplify(IList<Point> polygon)
        {
            //Simplify
            var solution = Clipper.Clipper.SimplifyPolygon(new Path(polygon));

            var outputLoops = new List<List<Point>>();
            foreach (var loop in solution)
            {
                var offsetLoop = new List<Point>();
                for (var i = 0; i < loop.Count; i++)
                {
                    var Point = loop[i];
                    var x = Point.X;
                    var y = Point.Y;
                    offsetLoop.Add(new Point(new List<double> { x, y, 0.0 }) { References = Point.References });
                }
                outputLoops.Add(offsetLoop);
            }

            return outputLoops;
        }

        /// <summary>
        /// Simplifies a polygon, by removing self intersection. This results in one polygon, but may not be successful 
        /// if multiple polygons 
        /// </summary>
        /// <param name="polygon"></param>
        /// <param name="simplifiedPolygon"></param>
        /// <returns></returns>
        public static bool CanSimplifyToSinglePolygon(IList<Point> polygon, out List<Point> simplifiedPolygon)
        {
            //Initialize output parameter
            simplifiedPolygon = new List<Point>();

            //Simplify
            var solution = Clipper.Clipper.SimplifyPolygon(new Path(polygon));

            var outputLoops = new List<List<Point>>();
            foreach (var loop in solution)
            {
                var offsetLoop = new List<Point>();
                for (var i = 0; i < loop.Count; i++)
                {
                    var Point = loop[i];
                    var x = Point.X;
                    var y = Point.Y;
                    offsetLoop.Add(new Point(new List<double> { x, y, 0.0 }) { References = Point.References });
                }
                outputLoops.Add(offsetLoop);
            }


            //If simplification split the polygon into multiple polygons. Union the polygons together, removing the extraneous lines
            if (outputLoops.Count == 1)
            {
                simplifiedPolygon = outputLoops.First();
                return true;
            }
            if (outputLoops.Count == 0)
            {
                return false;
            }
            var positiveLoops = outputLoops.Select(CCWPositive).ToList();
            var unionLoops = Union(positiveLoops);
            if (unionLoops.Count != 1)
            {
                return false;
            }
            simplifiedPolygon = unionLoops.First();
            return true;
        }
        #endregion

        #region Offset
        /// <summary>
        /// Offets the given loop by the given offset, rounding corners.
        /// </summary>
        /// <param name="loop"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static List<Point> OffsetRound(IList<Point> loop, double offset)
        {
            var loops = new List<List<Point>> { new List<Point>(loop) };
            var offsetLoops = OffsetRound(loops, offset);
            return offsetLoops.First();
        }

        /// <summary>
        /// Offsets all loops by the given offset value. Rounds the corners.
        /// Offest value may be positive or negative.
        /// Loops must be ordered CCW positive.
        /// </summary>
        /// <param name="loops"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static List<List<Point>> OffsetRound(List<List<Point>> loops, double offset)
        {
            //Begin an evaluation
            var solution = new List<List<Point>>();
            var clip = new ClipperOffset();
            clip.AddPaths(loops, JoinType.Round, EndType.ClosedPolygon);
            clip.Execute(ref solution, offset);
            return solution;
        }
        #endregion

        #region Union
        /// <summary>
        /// Union. Joins polygons that are touching into merged larger polygons.
        /// </summary>
        /// <param name="polygons"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Point>> Union(IList<List<Point>> polygons)
        {
            const PolyFillType fillMethod = PolyFillType.Positive;
            var solution = new Paths();
            var clipper = new Clipper.Clipper {StrictlySimple = true};

            //Begin an evaluation
            clipper.AddPaths(new List<Path>(polygons), PolyType.Subject, true);

            var result = clipper.Execute(ClipType.Union, solution, fillMethod, fillMethod);
            if (!result) throw new Exception("Clipper Union Failed");
            return solution;
        }

        /// <summary>
        /// Union. Joins polygons that are touching into merged larger polygons.
        /// </summary>
        /// <param name="otherPolygon"></param>
        /// <param name="polygons"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Point>> Union(IList<List<Point>> polygons, IList<Point> otherPolygon)
        {
            const PolyFillType fillMethod = PolyFillType.Positive;
            var solution = new Paths();
            var clipper = new Clipper.Clipper();
            var subject = new List<Path>(polygons) {new Path(otherPolygon)};

            //Begin an evaluation
            clipper.StrictlySimple = true;
            clipper.AddPaths(subject, PolyType.Subject, true);

            var result = clipper.Execute(ClipType.Union, solution, fillMethod, fillMethod);
            if (!result) throw new Exception("Clipper Union Failed");
            return solution;
        }

        /// <summary>
        /// Union. Joins two polygons that are touching or overlapping. Returns false if they are not connected.
        /// </summary>
        /// <param name="polygon1"></param>
        /// <param name="polygon2"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Point>> Union(IList<Point> polygon1, IList<Point> polygon2)
        {
            const PolyFillType fillMethod = PolyFillType.Positive;
            var solution = new Paths();
            var subject = new List<Path> {new Path(polygon1), new Path(polygon2)};
            
            //Setup Clipper
            var clipper = new Clipper.Clipper {StrictlySimple = true};
            clipper.AddPaths(subject, PolyType.Subject, true);

            //Begin an evaluation
            var result = clipper.Execute(ClipType.Union, solution, fillMethod, fillMethod);
            if (!result) throw new Exception("Clipper Union Failed");
            return solution;
        }
        #endregion
    }
    #endregion
}
