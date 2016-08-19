using System;
using System.Collections.Generic;
using System.Linq;
using StarMathLib;
using TVGL.Clipper;

namespace TVGL
{
    using Path = List<Point>;
    using Paths = List<List<Point>>;

    internal enum BooleanOperationType
    {
        Union,
        Intersection
    };

    /// <summary>
    /// A set of general operation for points and paths
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
            for (var i = 0; i < path.Count - 1; i++)
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
        /// <param name="path"></param>
        /// <returns></returns>
        public static List<List<Point>> Simplify(IList<Point> path)
        {
            var solution = Clipper.Clipper.SimplifyPolygon(new Path(path));
            return solution;
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


            //If simplification split the polygon into multiple paths. Union the paths together, removing the extraneous lines
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
        public static List<List<Point>> OffsetRound(IList<Point> path, double offset,
            double fractionOfPathLengthForMinLength = 0.001)
        {
            var pathLength = Length(path);
            var minLength = pathLength*fractionOfPathLengthForMinLength;
            if (minLength.IsNegligible()) minLength = pathLength*0.001;
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
        public static List<List<Point>> OffsetRound(IList<List<Point>> paths, double offset,
            double fractionOfPathsLengthForMinLength = 0.001)
        {
            var totalLength = paths.Sum(path => Length(path));
            var minLength = totalLength*fractionOfPathsLengthForMinLength;
            if (minLength.IsNegligible()) minLength = totalLength*0.001;
            return OffsetRoundByMinLength(paths, offset, minLength);
        }

        private static List<List<Point>> OffsetRoundByMinLength(IList<List<Point>> paths, double offset,
            double minLength)
        {
            if (minLength.IsNegligible())
            {
                var totalLength = paths.Sum(loop => Length(loop));
                minLength = totalLength*0.001;
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
        public static List<List<Point>> OffsetMiter(List<List<Point>> paths, double offset, double minLength = 0.0)
        {
            if (minLength.IsNegligible())
            {
                var totalLength = paths.Sum(loop => Length(loop));
                minLength = totalLength*0.001;
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
                minLength = totalLength*0.001;
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
        /// Union. Joins paths that are touching into merged larger paths.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clip"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Point>> Union2(IList<List<Point>> subject, IList<List<Point>> clip = null)
        {
            const PolyFillType fillMethod = PolyFillType.Positive;
            var solution = new Paths();

            //Setup Clipper
            var clipper = new Clipper.Clipper {StrictlySimple = true};

            //Check to make sure that each path's area is not negligible. If it is, ignore it.
            var clipperSubject =
                new Paths(subject.Where(path => !MiscFunctions.AreaOfPolygon(path).IsNegligible()).ToList());
            if (!clipperSubject.Any())
            {
                if (clip != null)
                {
                    var newSubject =
                        new Paths(clip.Where(path => !MiscFunctions.AreaOfPolygon(path).IsNegligible()).ToList());
                    if (newSubject.Any())
                    {
                        clipper.AddPaths(newSubject, PolyType.Subject, true);
                    }
                    else
                    {
                        return solution;
                    }
                }
                else
                {
                    return solution;
                }
            }
            else
            {
                clipper.AddPaths(clipperSubject, PolyType.Subject, true);

                if (clip != null)
                {
                    var clipperClip =
                        new Paths(clip.Where(path => !MiscFunctions.AreaOfPolygon(path).IsNegligible()).ToList());
                    if (clipperClip.Any())
                    {
                        clipper.AddPaths(clipperClip, PolyType.Clip, true);
                    }
                }
                else if (clipperSubject.Count == 1)
                {
                    return Simplify(clipperSubject.First());
                }
            }

            //Begin an evaluation
            var result = clipper.Execute(ClipType.Union, solution, fillMethod, fillMethod);
            if (!result) throw new Exception("Clipper Union Failed");
            return solution;
        }

        /// <summary>
        /// Union. Joins paths that are touching into merged larger paths.
        /// </summary>
        /// <param name="path1"></param>
        /// <param name="path2"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Point>> Union(List<Point> path1, List<Point> path2)
        {
            return Union(new List<List<Point>>() {path1}, new List<List<Point>>() {path2});
        }

        /// <summary>
        /// Union. Joins paths that are touching into merged larger paths.
        /// </summary>
        /// <param name="paths"></param>
        /// <param name="otherPolygon"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Point>> Union(IList<List<Point>> paths, List<Point> otherPolygon)
        {
            return Union(new List<List<Point>>(paths), new List<List<Point>> {otherPolygon});
        }

        /// <summary>
        /// Union based on Even/Odd methodology. Useful for correctly ordering a set of paths.
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
            var clipper = new Clipper.Clipper {StrictlySimple = true};
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
            var clipper = new Clipper.Clipper {StrictlySimple = true};
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
            return Difference(new List<List<Point>>() {subject}, new List<List<Point>>() {clip});
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
            return Difference(new List<List<Point>>(subjects), new List<List<Point>>() {clip});
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
            var clipper = new Clipper.Clipper {StrictlySimple = true};
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
            return Intersection(new List<List<Point>>() {subject}, new List<List<Point>>() {clip});
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
            return Intersection(new List<List<Point>>(subjects), new List<List<Point>>() {clip});
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
            return Intersection(new List<List<Point>>() {subject}, new List<List<Point>>(clips));
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
            var clipper = new Clipper.Clipper {StrictlySimple = true};
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
            return Xor(new List<List<Point>>() {subject}, new List<List<Point>>() {clip});
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
            return Xor(new List<List<Point>>(subjects), new List<List<Point>>() {clip});
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
            return Xor(new List<List<Point>>() {subject}, new List<List<Point>>(clips));
        }

        #endregion

        /// <summary>
        ///  Union. Joins paths that are touching into merged larger paths.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clip"></param>
        /// <returns></returns>
        public static List<List<Point>> Union(IList<List<Point>> subject, IList<List<Point>> clip = null)
        {
            var subject2 = new List<List<Point>>();
            foreach (var path in subject)
            {
                subject2.AddRange(Simplify(path));
            }
            var clip2 = new List<List<Point>>();
            if (clip != null)
            {
                foreach (var path in clip)
                {
                    clip2.AddRange(Simplify(path));
                }
            }
            return BooleanOperation(subject2, clip2, BooleanOperationType.Union);
        }

        #region Top Level Boolean Operation Method
        /// <reference>
        /// A simple algorithm for Boolean operations on polygons. Martínez, et. al. 2013. Advances in Engineering Software.
        /// </reference>
        private static List<List<Point>> BooleanOperation(IList<List<Point>> subject, IList<List<Point>> clip, BooleanOperationType booleanOperationType)
        {
            //1.Find intersections with vertical sweep line
            //1.Subdivide the edges of the polygons at their intersection points.
            //2.Select those subdivided edges that lie inside—or outside—the other polygon.
            //3.Join the selected edges to form the contours of the result polygon and compute the child contours.

            #region Build Sweep SweepEvents and Order Them Lexicographically
            //Build the sweep events and order them lexicographically (Low X to High X, then Low Y to High Y).
            var unsortedSweepEvents = new List<SweepEvent>();
            foreach (var path in subject)
            {
                var n = path.Count;
                for (var i = 0; i < n; i++)
                {
                    var j = (i + 1) % n; //Next position in path. Goes to 0 when i = n-1; 
                    var se1 = new SweepEvent(path[i], true, PolygonType.Subject);
                    var se2 = new SweepEvent(path[j], false, PolygonType.Subject);
                    se1.OtherEvent = se2;
                    se2.OtherEvent = se1;
                    unsortedSweepEvents.Add(se1);
                    unsortedSweepEvents.Add(se2);
                }
            }
            foreach (var path in clip)
            {
                var n = path.Count;
                for (var i = 0; i < n; i++)
                {
                    var j = (i + 1) % n; //Next position in path. Goes to 0 when i = n-1; 
                    var se1 = new SweepEvent(path[i], true, PolygonType.Clip);
                    var se2 = new SweepEvent(path[j], false, PolygonType.Clip);
                    se1.OtherEvent = se2;
                    se2.OtherEvent = se1;
                    unsortedSweepEvents.Add(se1);
                    unsortedSweepEvents.Add(se2);
                }
            }
            var orderedSweepEvents = new OrderedSweepEventList(unsortedSweepEvents);
            #endregion

            var result = new List<SweepEvent>();
            var sweepLines = new SweepList();
            while (orderedSweepEvents.Any())
            {
                var sweepEvent = orderedSweepEvents.First();
                orderedSweepEvents.RemoveAt(0);
                if (sweepEvent.Left) //left endpoint
                {
                    var index = sweepLines.Insert(sweepEvent);
                    //SetInformation(sweepEvent, sweepLines.Previous(index));
                    CheckAndResolveIntersection(sweepEvent, sweepLines.Next(index), ref sweepLines, ref orderedSweepEvents);
                    CheckAndResolveIntersection(sweepEvent, sweepLines.Previous(index), ref sweepLines, ref orderedSweepEvents);
                }
                else //The sweep event corresponds to the right endpoint
                {
                    var index = sweepLines.Find(sweepEvent);
                    var next = sweepLines.Next(index);
                    var prev = sweepLines.Previous(index);
                    sweepLines.RemoveAt(index);
                    CheckAndResolveIntersection(prev, next, ref sweepLines, ref orderedSweepEvents);
                }
                if (sweepEvent.ResultInOut)
                {
                    result.Add(sweepEvent);
                }
            }
            throw new NotImplementedException();
        }
        #endregion

        #region Get Paths
        private static List<Point> GetPathFromStartingPoint(Point startPoint, BooleanOperationType booleanOperationType, out bool isHole)
        {
            //Start ordering in CCW order. "point" must be the left most, bottom startPoint. 
            //Therefore, CCW should choose the lower Y value of the two lines
            if (startPoint.Lines.Count != 2) throw new Exception("This point is not correct or its list of lines is faulty");
            var startLine = startPoint.Lines[0].RightPoint.Y < startPoint.Lines[1].RightPoint.Y ? startPoint.Lines[0] : startPoint.Lines[1];
            isHole = startLine.InsideSame;
            startLine.Processed = true;
            var currentPoint = startLine.OtherPoint(startPoint);
            currentPoint.Processed = true;
            if (startLine.InsideOther) return null; //only intereseted in outside lines for the union.
            var path = new List<Point> {currentPoint};
            while (currentPoint != startPoint)
            {
                if (booleanOperationType == BooleanOperationType.Union)
                {
                    if (currentPoint.Lines.Count == 2) //not an intersection point
                    {
                        if (currentPoint.Lines[0].Processed)
                        {
                            path.Add(currentPoint.Lines[1].OtherPoint(currentPoint));
                            currentPoint.Lines[1].Processed = true;
                            currentPoint = path.Last();
                        }
                        else
                        {
                            path.Add(currentPoint.Lines[0].OtherPoint(currentPoint));
                            currentPoint.Lines[0].Processed = true;
                            currentPoint = path.Last();
                        }
                    }
                    else
                    {
                        var alreadyAddedPoint = false;
                        Point otherPoint = null;
                        foreach (var line in currentPoint.Lines)
                        {
                            otherPoint = line.OtherPoint(currentPoint);
                            if (line.Processed) continue;
                            line.Processed = true;
                            if (line.InsideOther) continue;  //only intereseted in outside lines for the union.
                            path.Add(otherPoint);
                            if (alreadyAddedPoint) throw new Exception("can only add one point");
                            alreadyAddedPoint = true;
                        }
                        currentPoint = otherPoint;
                        if (!alreadyAddedPoint || currentPoint == null)//No point was found. This path does not complete and is not used in the Union
                        {
                            return null;
                        }
                    }
                }
                currentPoint.Processed = true;
            }
            return path;
        }
        #endregion

        #region Check and Resolve Intersection between two lines
        private static void CheckAndResolveIntersection(SweepEvent se1, SweepEvent se2, ref SweepList sweepLines, ref OrderedSweepEventList orderedSweepEvents )
        {

            if (se1 == null || se2 == null) return;

            //GENERAL CASE: Lines share a point and cannot possibly intersect.
            if (se1.Point.Equals(se2.Point) || se1.Point.Equals(se2.OtherEvent.Point) ||
                se1.OtherEvent.Point.Equals(se2.Point) || se1.OtherEvent.Point.Equals(se2.OtherEvent.Point))
            {
                return;
            }

            //SPECIAL CASE: Lines both share a point with different references => needs to be merged
            if (se1.Point == se2.Point || se1.Point == se2.OtherEvent.Point ||
                se1.OtherEvent.Point == se2.Point || se1.OtherEvent.Point == se2.OtherEvent.Point) 
            {
                throw new NotImplementedException();
            }

            Point intersectionPoint;
            if (MiscFunctions.LineLineIntersection(new Line(se1.Point, se1.OtherEvent.Point),
                new Line(se2.Point, se2.OtherEvent.Point), out intersectionPoint, true) && intersectionPoint == null)
            {
                //SPECIAL CASE: Collinear
                var se1Other = se1.OtherEvent;
                var se2Other = se2.OtherEvent;
                if (se1.Point.X < se2.Point.X)
                {
                    var newSweepEvent1 = new SweepEvent(se2.Point, false, se1.PolygonType) {OtherEvent = se1};
                    se1.OtherEvent = newSweepEvent1;

                    var newSweepEvent2 = new SweepEvent(se1.OtherEvent.Point, true, se2.PolygonType) {OtherEvent = se2Other};
                    se2Other.OtherEvent = newSweepEvent2;

                    //Note that this makes the two polygon types for this pair of sweep events opposite
                    if (se1Other.PolygonType == se2.PolygonType) throw new NotImplementedException();
                    se1Other.OtherEvent = se2;
                    se2.OtherEvent = se1Other;

                    //Add all new sweep events
                    orderedSweepEvents.Insert(newSweepEvent1);
                    orderedSweepEvents.Insert(newSweepEvent2);
                    return;
                }
                else
                {
                    var newSweepEvent1 = new SweepEvent(se1.Point, false, se2.PolygonType) { OtherEvent = se2 };
                    se2.OtherEvent = newSweepEvent1;

                    var newSweepEvent2 = new SweepEvent(se2.OtherEvent.Point, true, se1.PolygonType) { OtherEvent = se2Other };
                    se1Other.OtherEvent = newSweepEvent2;

                    //Note that this makes the two polygon types for this pair of sweep events opposite
                    if (se2Other.PolygonType == se1.PolygonType) throw new NotImplementedException();
                    se2Other.OtherEvent = se1;
                    se1.OtherEvent = se2Other;

                    //Add all new sweep events
                    orderedSweepEvents.Insert(newSweepEvent1);
                    orderedSweepEvents.Insert(newSweepEvent2);
                    return;
                }
                
            }

            //GENERAL CASE: Lines do not intersect.
            if (intersectionPoint == null) return;

            //SPECIAL CASE: Intersection point is the same as one of se1's line end points.
            if (intersectionPoint == se1.Point)
            {
                throw new Exception("I don't think this can ever happen if everything is sorted properly");
                
            }
            if (intersectionPoint == se1.OtherEvent.Point)
            {
                var se2Other = se2.OtherEvent;

                var newSweepEvent1 = new SweepEvent(se1.OtherEvent.Point, false, se2.PolygonType) {OtherEvent = se2};
                se2.OtherEvent = newSweepEvent1;

                var newSweepEvent2 = new SweepEvent(se1.OtherEvent.Point, true, se2.PolygonType) {OtherEvent = se2Other};
                se2Other.OtherEvent = newSweepEvent2;

                //Add all new sweep events
                orderedSweepEvents.Insert(newSweepEvent1);
                orderedSweepEvents.Insert(newSweepEvent2);
                return;
            }

            //SPECIAL CASE: Intersection point is the same as one of se2's line end points. 
            if (intersectionPoint == se2.Point)
            {
                throw new Exception("I don't think this can ever happen if everything is sorted properly");

            }
            if (intersectionPoint == se2.OtherEvent.Point)
            {
                var se1Other = se1.OtherEvent;

                var newSweepEvent1 = new SweepEvent(se2.OtherEvent.Point, false, se2.PolygonType) { OtherEvent = se1 };
                se1.OtherEvent = newSweepEvent1;

                var newSweepEvent2 = new SweepEvent(se2.OtherEvent.Point, true, se2.PolygonType) { OtherEvent = se1Other };
                se1Other.OtherEvent = newSweepEvent2;

                //Add all new sweep events
                orderedSweepEvents.Insert(newSweepEvent1);
                orderedSweepEvents.Insert(newSweepEvent2);
                return;
            }

            //GENERAL CASE: Lines are not parallel and only intersct once, between the end points of both lines.
            else
            {
                var se1Other = se1.OtherEvent;
                var se2Other = se2.OtherEvent;

                //Split Sweep Event 1 (se1)
                var newSweepEvent1 = new SweepEvent(intersectionPoint, false, se1.PolygonType) { OtherEvent = se1 };
                se1.OtherEvent = newSweepEvent1;

                var newSweepEvent2 = new SweepEvent(intersectionPoint, true, se1.PolygonType) { OtherEvent = se1Other };
                se1Other.OtherEvent = newSweepEvent2;

                //Split Sweep Event 2 (se2)
                var newSweepEvent3 = new SweepEvent(intersectionPoint, false, se2.PolygonType) { OtherEvent = se2 };
                se2.OtherEvent = newSweepEvent3;

                var newSweepEvent4 = new SweepEvent(intersectionPoint, true, se2.PolygonType) { OtherEvent = se2Other };
                se2Other.OtherEvent = newSweepEvent4;

                //Add all new sweep events
                orderedSweepEvents.Insert(newSweepEvent1);
                orderedSweepEvents.Insert(newSweepEvent2);
                orderedSweepEvents.Insert(newSweepEvent3);
                orderedSweepEvents.Insert(newSweepEvent4);
            }
        }
        #endregion

        #region SweepList for Boolean Operations
        private class SweepList
        {
            private List<SweepEvent> SweepEvents;

            public int Count => SweepEvents.Count;

            public SweepEvent Next(int i)
            {
                return SweepEvents[i + 1];
            }

            public SweepEvent Previous(int i)
            {
                return SweepEvents[i - 1];
            }

            public void RemoveAt(int i)
            {
                SweepEvents.RemoveAt(i);
            }

            //Insert, ordered min Y to max Y for the intersection of the line with xval.
            public int Insert(SweepEvent se1)
            {
                if (se1 == null) throw new Exception("Must not be null");
                if (!se1.Left) throw new Exception("Right end point sweep events are not supposed to go into this list");
                
                if (SweepEvents == null)
                {
                    SweepEvents = new List<SweepEvent> {se1};
                    return 0;
                }

                var se1Y = se1.Point.Y;
                var i = 0;
                foreach(var se2 in SweepEvents)
                {
                    if (se1.Point.Equals(se2.Point))
                    {
                        if (se1.OtherEvent.Point.Y < se2.OtherEvent.Point.Y)
                        {
                            break;
                        }//Else, increment and continue.
                        i++;
                        continue;
                    }
                    var se2Y = LineIntercept(se2.Point, se2.OtherEvent.Point, se1.Point.X);
                    if(se1Y.IsPracticallySame(se2Y)) throw new NotImplementedException("These lines intersect and the points should be merged");
                    if (se1Y < se2Y)
                    {
                        break;
                    }
                    //Else, increment
                    i++;
                }
                SweepEvents.Insert(i, se1);
                return i;
            }

            private static double LineIntercept(Point p1, Point p2, double xval)
            {
                if (p1.X.IsPracticallySame(p2.X)) //Vertical line
                {
                    //return lower value Y
                    return p1.Y < p2.Y ? p1.Y : p2.Y;
                }
                if (p1.Y.IsPracticallySame(p2.Y))//Horizontal Line
                {
                    return p1.Y;
                }
                //Else, find the slope and then solve for y
                var m = (p2.Y - p1.Y)/(p2.X - p1.X);
                return m * (xval - p1.X) + p1.Y;
            }

            public int Find(SweepEvent se)
            {
                return SweepEvents.IndexOf(se);
            }

            public SweepEvent Item(int i)
            {
                return SweepEvents[i];
            }
        }
        #endregion

        #region SweepEvent and OrderedSweepEventList
        //Sweep Event is used for the boolean operations.
        //We don't want to use lines, because maintaining them (and their referencesis incredibly difficult.
        //There are two sweep events for each line, one for the left edge and one for the right edge
        //Only the sweep events for the left edge are ever added to the sweep list
        private class SweepEvent
        {
            public  Point Point { get; private set; } //the point for this sweep event
            public bool Left { get; private set; } //is the left endpoint of the line
            public SweepEvent OtherEvent { get; set; } //The event of the other endpoint of this line
            public PolygonType PolygonType { get; private set; } //Whether this line was part of the Subject or Clip
            public int PositionInPath { get; set; }
            public bool ResultInOut { get; set; }
            public int PathId { get; set; }
            public int ParentPathId { get; set; }
            public bool Processed { get; set; } //If this sweep event has already been processed in the sweep
            public int Depth { get; set; }

            public SweepEvent(Point point, bool isLeft, PolygonType polyType)
            {
                Point = point;
                Left = isLeft;
                PolygonType = polyType;
            }
        }

        private class OrderedSweepEventList
        {
            public readonly List<SweepEvent> SweepEvents;

            public OrderedSweepEventList(IEnumerable<SweepEvent> sweepEvents)
            {
                SweepEvents = new List<SweepEvent>();
                foreach (var sweepEvent in sweepEvents)
                {
                    Insert(sweepEvent);
                }
            }

            public void Insert(SweepEvent se1)
            {
                //Find the index for p1
                var i = 0;
                var breakIfNotNear = false;
                foreach (var se2 in SweepEvents)
                {
                    if (se1.Point.Equals(se2.Point)) //reference is the same
                    {
                        if (se1.Point.Y < se2.Point.Y)
                        {
                            //Insert before se2
                            break;
                        } //Else increment and continue;
                    }
                    else if (se1.Point.X.IsPracticallySame(se2.Point.X))
                    {
                        if (se1.Point.Y.IsPracticallySame(se2.Point.Y)) throw new NotImplementedException("Sweep Events need to be merged"); 
                        if (se1.Point.Y < se2.Point.Y) break;
                        breakIfNotNear = true;
                    }
                    else if (breakIfNotNear) break;
                    else if (se1.Point.X < se2.Point.X) break;
                    i++;
                }
                SweepEvents.Insert(i, se1);
            }
            public bool Any()
            {
                return SweepEvents.Any();
            }

            public SweepEvent First()
            {
                return SweepEvents.First();
            }

            public void RemoveAt(int i)
            {
                SweepEvents.RemoveAt(i);
            }
        }
        #endregion
    }

   
}
