using System;
using System.Collections.Generic;
using System.Linq;
using StarMathLib;
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
        /// <summary>
        /// Gets the length of a path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static double Length(IList<Point> path)
        {
            var editPath = new List<Point>(path) {path.First()};
            var length = 0.0;
            for (var i = 0; i < path.Count -1; i++)
            {
                var p1 = path[i];
                var p2 = path[i + 1];
                length += MiscFunctions.DistancePointToPoint(p1.Position2D, p2.Position2D);
            }
            return length;
        }

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
                    offsetLoop.Add(new Point(new List<double> {x, y, 0.0}) {References = Point.References});
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
                    offsetLoop.Add(new Point(new List<double> {x, y, 0.0}) {References = Point.References});
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
        /// Offets the given path by the given offset, rounding corners.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="offset"></param>
        /// <param name="fractionOfPathLengthForMinLength"></param>
        /// <returns></returns>
        public static List<List<Point>> OffsetRound(IList<Point> path, double offset, double fractionOfPathLengthForMinLength = 0.001)
        {
            var pathLength = Length(path);
            var minLength = pathLength * fractionOfPathLengthForMinLength;
            if (minLength.IsNegligible()) minLength = pathLength * 0.001;
            var loops = new List<List<Point>> {new List<Point>(path)};
            return OffsetRoundByMinLength(loops, offset, minLength);
        }

        /// <summary>
        /// Offsets all paths by the given offset value. Rounds the corners.
        /// Offest value may be positive or negative.
        /// Loops must be ordered CCW positive.
        /// </summary>
        /// <param name="paths"></param>
        /// <param name="offset"></param>
        /// <param name="fractionOfPathsLengthForMinLength"></param>
        /// <returns></returns>
        public static List<List<Point>> OffsetRound(IList<List<Point>> paths, double offset, double fractionOfPathsLengthForMinLength = 0.001)
        {
            var totalLength = paths.Sum(path => Length(path));
            var minLength = totalLength * fractionOfPathsLengthForMinLength;
            if (minLength.IsNegligible()) minLength = totalLength * 0.001;
            return OffsetRoundByMinLength(paths, offset, minLength);
        }

        private static List<List<Point>> OffsetRoundByMinLength(IList<List<Point>> paths, double offset, double minLength)
        {
            if (minLength.IsNegligible())
            {
                var totalLength = paths.Sum(loop => Length(loop));
                minLength = totalLength * 0.001;
            }

            //Begin an evaluation
            var solution = new List<List<Point>>();
            var clip = new ClipperOffset(minLength);
            clip.AddPaths(paths, JoinType.Round, EndType.ClosedPolygon);
            clip.Execute(ref solution, offset);
            return solution;
        }

        /// <summary>
        /// Offsets all paths by the given offset value. Miters the corners.
        /// Offest value may be positive or negative.
        /// Loops must be ordered CCW positive.
        /// </summary>
        /// <param name="paths"></param>
        /// <param name="minLength"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static List<List<Point>> OffsetMiter(List<List<Point>> paths,  double offset, double minLength = 0.0)
        {
            if (minLength.IsNegligible())
            {
                var totalLength = paths.Sum(loop => Length(loop));
                minLength = totalLength * 0.001;
            }

            //Begin an evaluation
            var solution = new List<List<Point>>();
            var clip = new ClipperOffset(minLength);
            clip.AddPaths(paths, JoinType.Miter, EndType.ClosedPolygon);
            clip.Execute(ref solution, offset);
            return solution;
        }

        /// <summary>
        /// Offsets all paths by the given offset value. Squares the corners.
        /// Offest value may be positive or negative.
        /// Loops must be ordered CCW positive.
        /// </summary>
        /// <param name="paths"></param>
        /// <param name="minLength"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static List<List<Point>> OffsetSquare(List<List<Point>> paths, double offset, double minLength = 0.0)
        {
            if (minLength.IsNegligible())
            {
                var totalLength = paths.Sum(loop => Length(loop));
                minLength = totalLength * 0.001;
            }

            //Begin an evaluation
            var solution = new List<List<Point>>();
            var clip = new ClipperOffset(minLength);
            clip.AddPaths(paths, JoinType.Square, EndType.ClosedPolygon);
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
            var subject = new List<Path>(polygons);

            //Setup Clipper
            var clipper = new Clipper.Clipper {StrictlySimple = true};
            clipper.AddPaths(subject, PolyType.Subject, true);

            //Begin an evaluation
            var result = clipper.Execute(ClipType.Union, solution, fillMethod, fillMethod);
            if (!result) throw new Exception("Clipper Union Failed");
            return solution;
        }

        /// <summary>
        /// Union. Joins polygons that are touching into merged larger polygons.
        /// </summary>
        /// <param name="polygon1"></param>
        /// <param name="polygon2"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Point>> Union(List<Point> polygon1, List<Point> polygon2)
        {
            return Union(new List<List<Point>>() {polygon1, polygon2});
        }

        /// <summary>
        /// Union. Joins polygons that are touching into merged larger polygons.
        /// </summary>
        /// <param name="polygons"></param>
        /// <param name="otherPolygon"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Point>> Union(IList<List<Point>> polygons, List<Point> otherPolygon)
        {
            return Union(new List<List<Point>>(polygons) { otherPolygon });
        }

        /// <summary>
        /// Union based on Even/Odd methodology. Useful for correctly ordering a set of polygons.
        /// </summary>
        /// <param name="polygons"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static Paths UnionEvenOdd(IList<List<Point>> polygons)
        {
            const PolyFillType fillMethod = PolyFillType.EvenOdd;
            var solution = new Paths();
            var subject = new List<Path>(polygons);

            //Setup Clipper
            var clipper = new Clipper.Clipper { StrictlySimple = true };
            clipper.AddPaths(subject, PolyType.Subject, true);

            //Begin an evaluation
            var result = clipper.Execute(ClipType.Union, solution, fillMethod, fillMethod);
            if (!result) throw new Exception("Clipper Union Failed");
            return solution;
        }
        #endregion

        #region Difference
        /// <summary>
        /// Difference. Gets the difference between two sets of polygons. 
        /// </summary>
        /// <param name="subjects"></param>
        /// <param name="clips"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Point>> Difference(IList<List<Point>> subjects, IList<List<Point>> clips)
        {
            const PolyFillType fillMethod = PolyFillType.Positive;
            var solution = new Paths();
            var subject = new List<Path>(subjects);
            var clip = new List<Path>(clips);

            //Setup Clipper
            var clipper = new Clipper.Clipper { StrictlySimple = true };
            clipper.AddPaths(subject, PolyType.Subject, true);
            clipper.AddPaths(clip, PolyType.Clip, true);

            //Begin an evaluation
            var result = clipper.Execute(ClipType.Difference, solution, fillMethod, fillMethod);
            if (!result) throw new Exception("Clipper Difference Failed");
            return solution;
        }

        /// <summary>
        /// Difference. Gets the difference between two sets of polygons. 
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clip"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Point>> Difference(List<Point> subject, List<Point> clip)
        {
            return Difference(new List<List<Point>>() { subject}, new List<List<Point>>() { clip });
        }

        /// <summary>
        /// Difference. Gets the difference between two sets of polygons. 
        /// </summary>
        /// <param name="subjects"></param>
        /// <param name="clip"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Point>> Difference(IList<List<Point>> subjects, List<Point> clip)
        {
            return Difference(new List<List<Point>>(subjects), new List<List<Point>>() { clip });
        }

        /// <summary>
        /// Difference. Gets the difference between two sets of polygons. 
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clips"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Point>> Difference(List<Point> subject, IList<List<Point>> clips)
        {
            return Difference(new List<List<Point>>() {subject}, new List<List<Point>>(clips));
        }
        #endregion

        #region Intersection
        /// <summary>
        /// Intersection. Gets the areas covered by both the subjects and the clips.
        /// </summary>
        /// <param name="subjects"></param>
        /// <param name="clips"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Point>> Intersection(IList<List<Point>> subjects, IList<List<Point>> clips)
        {
            const PolyFillType fillMethod = PolyFillType.Positive;
            var solution = new Paths();
            var subject = new List<Path>(subjects);
            var clip = new List<Path>(clips);

            //Setup Clipper
            var clipper = new Clipper.Clipper { StrictlySimple = true };
            clipper.AddPaths(subject, PolyType.Subject, true);
            clipper.AddPaths(clip, PolyType.Clip, true);

            //Begin an evaluation
            var result = clipper.Execute(ClipType.Intersection, solution, fillMethod, fillMethod);
            if (!result) throw new Exception("Clipper Difference Failed");
            return solution;
        }

        /// <summary>
        /// Intersection. Gets the areas covered by both the subjects and the clips. 
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clip"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Point>> Intersection(List<Point> subject, List<Point> clip)
        {
            return Intersection(new List<List<Point>>() { subject }, new List<List<Point>>() { clip });
        }

        /// <summary>
        /// Intersection. Gets the areas covered by both the subjects and the clips.
        /// </summary>
        /// <param name="subjects"></param>
        /// <param name="clip"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Point>> Intersection(IList<List<Point>> subjects, List<Point> clip)
        {
            return Intersection(new List<List<Point>>(subjects), new List<List<Point>>() { clip });
        }

        /// <summary>
        /// Intersection. Gets the areas covered by both the subjects and the clips. 
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clips"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Point>> Intersection(List<Point> subject, IList<List<Point>> clips)
        {
            return Intersection(new List<List<Point>>() { subject }, new List<List<Point>>(clips));
        }
        #endregion

        #region Xor
        /// <summary>
        /// XOR. Opposite of Intersection. Gets the areas covered by only either subjects or clips. 
        /// </summary>
        /// <param name="subjects"></param>
        /// <param name="clips"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Point>> Xor(IList<List<Point>> subjects, IList<List<Point>> clips)
        {
            const PolyFillType fillMethod = PolyFillType.Positive;
            var solution = new Paths();
            var subject = new List<Path>(subjects);
            var clip = new List<Path>(clips);

            //Setup Clipper
            var clipper = new Clipper.Clipper { StrictlySimple = true };
            clipper.AddPaths(subject, PolyType.Subject, true);
            clipper.AddPaths(clip, PolyType.Clip, true);

            //Begin an evaluation
            var result = clipper.Execute(ClipType.Xor, solution, fillMethod, fillMethod);
            if (!result) throw new Exception("Clipper Difference Failed");
            return solution;
        }

        /// <summary>
        /// XOR. Opposite of Intersection. Gets the areas covered by only either subjects or clips. 
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clip"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Point>> Xor(List<Point> subject, List<Point> clip)
        {
            return Xor(new List<List<Point>>() { subject }, new List<List<Point>>() { clip });
        }

        /// <summary>
        /// XOR. Opposite of Intersection. Gets the areas covered by only either subjects or clips. 
        /// </summary>
        /// <param name="subjects"></param>
        /// <param name="clip"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Point>> Xor(IList<List<Point>> subjects, List<Point> clip)
        {
            return Xor(new List<List<Point>>(subjects), new List<List<Point>>() { clip });
        }

        /// <summary>
        /// XOR. Opposite of Intersection. Gets the areas covered by only either subjects or clips.  
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clips"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Point>> Xor(List<Point> subject, IList<List<Point>> clips)
        {
            return Xor(new List<List<Point>>() { subject }, new List<List<Point>>(clips));
        }
        #endregion

        
    }
    #endregion
}
