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

        /// <summary>
        ///  Union. Joins paths that are touching into merged larger paths.
        /// </summary>
        /// <returns></returns>
        public static List<List<Point>> SimplifyForSilhouette(List<Point> path)
        {
            //Simplify a path. Trivial if not self intersecting.
            //1. Subdivide lines at intersection points.
            //2. Remove edges that are inside the path. (If closest line below is LeftToRight, then it is inside. Otherwise it is outside)
            //3. Create the new set of paths 
            #region Build Sweep PathID and Order Them Lexicographically
            var unsortedSweepEvents = new List<SweepEvent>();
            //Build the sweep events and order them lexicographically (Low X to High X, then Low Y to High Y).
            var n = path.Count;
            for (var i = 0; i < n; i++)
            {
                var j = (i + 1) % n; //Next position in path. Goes to 0 when i = n-1; 
                SweepEvent se1, se2;

                if (path[i].X.IsPracticallySame(path[j].X))
                {
                    if (path[i].Y.IsPracticallySame(path[j].Y)) continue; //Ignore this 
                    if (path[i].Y < path[j].Y)
                    {
                        se1 = new SweepEvent(path[i], true, true, PolygonType.Subject);
                        se2 = new SweepEvent(path[j], false, false, PolygonType.Subject);
                    }
                    else
                    {
                        se1 = new SweepEvent(path[i], false, true, PolygonType.Subject);
                        se2 = new SweepEvent(path[j], true, false, PolygonType.Subject);
                    }
                }
                else if (path[i].X < path[j].X)
                {
                    se1 = new SweepEvent(path[i], true, true, PolygonType.Subject);
                    se2 = new SweepEvent(path[j], false, false, PolygonType.Subject);
                }
                else
                {
                    se1 = new SweepEvent(path[i], false, true, PolygonType.Subject);
                    se2 = new SweepEvent(path[j], true, false, PolygonType.Subject);
                }
                se1.OtherEvent = se2;
                se2.OtherEvent = se1;
                unsortedSweepEvents.Add(se1);
                unsortedSweepEvents.Add(se2);
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
                    //Inserting the event into the sweepLines list
                    var index = sweepLines.Insert(sweepEvent);
                    sweepEvent.IndexInList = index;
                    var goBack1 = false; //goBack is used to processes line segments from some collinear intersections
                    CheckAndResolveSelfIntersection(sweepEvent, sweepLines.Next(index), ref sweepLines, ref orderedSweepEvents, out goBack1);
                    var goBack2 = false;
                    CheckAndResolveSelfIntersection(sweepLines.Previous(index), sweepEvent, ref sweepLines, ref orderedSweepEvents, out goBack2);
                    if (goBack1 || goBack2) continue;

                    //Select the closest edge downward that belongs to the other polygon.
                    //Set information updates the OtherInOut property and uses this to determine if the sweepEvent is part of the result.
                    SetSimplifyInformation(sweepEvent, sweepLines.PreviousSame(index));
                }
                else //The sweep event corresponds to the right endpoint
                {
                    var index = sweepLines.Find(sweepEvent.OtherEvent);
                    if (index == -1) throw new Exception("Other event not found in list. Error in implementation");
                    var next = sweepLines.Next(index);
                    var prev = sweepLines.Previous(index);
                    sweepLines.RemoveAt(index);
                    var goBack = false;
                    CheckAndResolveSelfIntersection(prev, next, ref sweepLines, ref orderedSweepEvents, out goBack);
                }
                if (sweepEvent.InResult || sweepEvent.OtherEvent.InResult)
                {
                    if (sweepEvent.InResult && !sweepEvent.Left) throw new Exception("error in implementation");
                    if (sweepEvent.OtherEvent.InResult && sweepEvent.Left) throw new Exception("error in implementation");
                    if (sweepEvent.Point == sweepEvent.OtherEvent.Point) continue; //Ignore this negligible length line.
                    result.Add(sweepEvent);
                }
            }

            //Next stage. Find the paths
            for (var i = 0; i < result.Count; i++)
            {
                result[i].PositionInResult = i;
                result[i].Processed = false;
            }
            var solution = new Paths();
            var currentPathID = 0;
            try
            {
                foreach (var se1 in result.Where(se1 => !se1.Processed))
                {
                    int parentID;
                    var depth = ComputeDepth(PreviousInResult(se1, result), out parentID);
                    var newPath = ComputePath(se1, currentPathID, depth, parentID, result);
                    if (depth % 2 != 0) //Odd
                    {
                        newPath = CWNegative(newPath);
                    }
                    solution.Add(newPath);
                    //if (parent != -1) //parent path ID
                    //{
                    //    solution[parent].AddChild(currentPathID);
                    //}
                    currentPathID++;
                }
            }
            catch
            {
                solution = new Paths{path};
                return solution;
            }

            return solution;
        }

        private static void SetSimplifyInformation(SweepEvent sweepEvent, object previousSame)
        {
            throw new NotImplementedException();
        }

        private static void CheckAndResolveSelfIntersection(SweepEvent sweepEvent, SweepEvent next, ref SweepList sweepLines, ref OrderedSweepEventList orderedSweepEvents, out bool goBack1)
        {
            throw new NotImplementedException();
        }

        #region Top Level Boolean Operation Method
        /// <reference>
        /// This aglorithm is based on on the paper:
        /// A simple algorithm for Boolean operations on polygons. Martínez, et. al. 2013. Advances in Engineering Software.
        /// Links to paper: http://dx.doi.org/10.1016/j.advengsoft.2013.04.004 OR http://www.sciencedirect.com/science/article/pii/S0965997813000379
        /// </reference>
        private static List<List<Point>> BooleanOperation(IList<List<Point>> subject, IList<List<Point>> clip, BooleanOperationType booleanOperationType)
        {
            //1.Find intersections with vertical sweep line
            //1.Subdivide the edges of the polygons at their intersection points.
            //2.Select those subdivided edges that lie inside—or outside—the other polygon.
            //3.Join the selected edges to form the contours of the result polygon and compute the child contours.

            #region Build Sweep PathID and Order Them Lexicographically
            var unsortedSweepEvents = new List<SweepEvent>();
            //Build the sweep events and order them lexicographically (Low X to High X, then Low Y to High Y).
            foreach (var path in subject)
            {
                var n = path.Count;
                for (var i = 0; i < n; i++)
                {
                    var j = (i + 1) % n; //Next position in path. Goes to 0 when i = n-1; 
                    SweepEvent se1, se2;
                    
                    if (path[i].X.IsPracticallySame(path[j].X))
                    {
                        if (path[i].Y.IsPracticallySame(path[j].Y)) continue; //Ignore this 
                        if (path[i].Y < path[j].Y)
                        {
                            se1 = new SweepEvent(path[i], true, true, PolygonType.Subject);
                            se2 = new SweepEvent(path[j], false, false, PolygonType.Subject);
                        }
                        else
                        {
                            se1 = new SweepEvent(path[i], false, true, PolygonType.Subject);
                            se2 = new SweepEvent(path[j], true, false, PolygonType.Subject);
                        }
                    }
                    else if (path[i].X < path[j].X)
                    {
                        se1 = new SweepEvent(path[i], true, true, PolygonType.Subject);
                        se2 = new SweepEvent(path[j], false, false, PolygonType.Subject); 
                    }
                    else
                    {
                        se1 = new SweepEvent(path[i], false, true, PolygonType.Subject);
                        se2 = new SweepEvent(path[j], true, false, PolygonType.Subject);
                    }
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
                    SweepEvent se1, se2;
                    path[i].IndexInPath = i;
                    if (path[i].X.IsPracticallySame(path[j].X))
                    {
                        if (path[i].Y.IsPracticallySame(path[j].Y)) continue; //Ignore this 
                        if (path[i].Y < path[j].Y)
                        {
                            se1 = new SweepEvent(path[i], true, true, PolygonType.Clip);
                            se2 = new SweepEvent(path[j], false, false, PolygonType.Clip);
                        }
                        else
                        {
                            se1 = new SweepEvent(path[i], false, true, PolygonType.Clip);
                            se2 = new SweepEvent(path[j], true, false, PolygonType.Clip);
                        }
                    }
                    else if (path[i].X < path[j].X)
                    {
                        se1 = new SweepEvent(path[i], true, true, PolygonType.Clip);
                        se2 = new SweepEvent(path[j], false, false, PolygonType.Clip);
                    }
                    else
                    {
                        se1 = new SweepEvent(path[i], false, true, PolygonType.Clip);
                        se2 = new SweepEvent(path[j], true, false, PolygonType.Clip);
                    }
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
                    //Inserting the event into the sweepLines list
                    var index = sweepLines.Insert(sweepEvent);
                    sweepEvent.IndexInList = index;
                    var goBack1 = false; //goBack is used to processes line segments from some collinear intersections
                    CheckAndResolveIntersection(sweepEvent, sweepLines.Next(index), ref sweepLines, ref orderedSweepEvents, out goBack1);
                    var goBack2 = false;
                    CheckAndResolveIntersection(sweepLines.Previous(index), sweepEvent, ref sweepLines, ref orderedSweepEvents, out goBack2);
                    if (goBack1 || goBack2) continue;

                    //Select the closest edge downward that belongs to the other polygon.
                    //Set information updates the OtherInOut property and uses this to determine if the sweepEvent is part of the result.
                    SetInformation(sweepEvent, sweepLines.PreviousOther(index), booleanOperationType);                    
                }
                else //The sweep event corresponds to the right endpoint
                {
                    var index = sweepLines.Find(sweepEvent.OtherEvent);
                    if(index == -1) throw new Exception("Other event not found in list. Error in implementation");
                    var next = sweepLines.Next(index);
                    var prev = sweepLines.Previous(index);
                    sweepLines.RemoveAt(index);
                    var goBack = false;
                    CheckAndResolveIntersection(prev, next, ref sweepLines, ref orderedSweepEvents, out goBack);
                }
                if (sweepEvent.InResult || sweepEvent.OtherEvent.InResult)
                {
                    if(sweepEvent.InResult && !sweepEvent.Left) throw new Exception("error in implementation");
                    if(sweepEvent.OtherEvent.InResult && sweepEvent.Left) throw new Exception("error in implementation");
                    if (sweepEvent.Point == sweepEvent.OtherEvent.Point) continue; //Ignore this negligible length line.
                    result.Add(sweepEvent);
                }
            }

            //Next stage. Find the paths
            for(var i = 0; i < result.Count; i++)
            {
                result[i].PositionInResult = i;
                result[i].Processed = false;
            }
            var solution = new Paths();
            var currentPathID = 0;
            try
            {
                foreach (var se1 in result.Where(se1 => !se1.Processed))
                {
                    int parentID;
                    var depth = ComputeDepth(PreviousInResult(se1, result), out parentID);
                    var path = ComputePath(se1, currentPathID, depth, parentID, result);
                    if (depth%2 != 0) //Odd
                    {
                        path = CWNegative(path);
                    }
                    solution.Add(path);
                    //if (parent != -1) //parent path ID
                    //{
                    //    solution[parent].AddChild(currentPathID);
                    //}
                    currentPathID ++;
                }
            }
            catch
            {
                solution = new Paths(subject);
                solution.AddRange(clip);
                return solution;
            }
            
            return solution;
        }
        #endregion

        #region Set Information
        private static void SetInformation(SweepEvent sweepEvent, SweepEvent previous, BooleanOperationType booleanOperationType)
        {
            //Consider whether the previous edge from the other polygon is an inside-outside transition or outside-inside, based on a vertical ray starting below
            // the previous edge and pointing upwards. //If the transition is outside-inside, the sweepEvent lays inside other polygon, otherwise it lays outside.
            if (previous == null || !previous.LeftToRight)
            {
                //Then it must lie outside the other polygon
                sweepEvent.OtherInOut = false;
                sweepEvent.OtherEvent.OtherInOut = false;
            }
            else //It lies inside the other polygon
            {
                sweepEvent.OtherInOut = true;
                sweepEvent.OtherEvent.OtherInOut = true;
            }

            //If duplicate (overlapping) edges, the edge is included for Union and Intersection if they the same LeftToRight flag.
            //Different for other boolean operations
            if (previous != null && sweepEvent.DuplicateEvent == previous)
            {
                if (booleanOperationType == BooleanOperationType.Union ||
                    booleanOperationType == BooleanOperationType.Intersection)
                {
                    if (!previous.InResult && previous.LeftToRight == sweepEvent.LeftToRight)
                    {
                        sweepEvent.InResult = true;
                    }
                    else sweepEvent.InResult = false;
                }
            }

            //Determine if it should be in the results
            else if (booleanOperationType == BooleanOperationType.Union)
            {
                sweepEvent.InResult = !sweepEvent.OtherInOut;
            }
            else if (booleanOperationType == BooleanOperationType.Intersection)
            {
                sweepEvent.InResult = sweepEvent.OtherInOut;
            }
            //ToDo: Figure out how to set this property or determine if it is necessary
            //sweepEvent.PrevInResult = previous;
        }
        #endregion

        #region Compute Depth
        private static int ComputeDepth(SweepEvent previousSweepEvent, out int parentID)
        {
            //This function needs to use many of the bools from the SweepEvent to determine depth and parentID
            //Since previousSweepEvent point is the first point of the path, it must be the left bottom (min X, then min Y) corner
            if (previousSweepEvent != null) 
            {
                if (previousSweepEvent.LeftToRight && previousSweepEvent.ParentPathID != -1)
                { 
                    //It must share the same parent path and depth
                    parentID = previousSweepEvent.ParentPathID;
                    return previousSweepEvent.Depth;
                }
                //else, outside-inside transition
                parentID = previousSweepEvent.PathID;
                return previousSweepEvent.Depth + 1;
            }
            //else, not inside any other polygons
            parentID = -1;
            return 0;
        }

        #endregion

        #region Compute Paths
        private static List<Point> ComputePath(SweepEvent startEvent, int pathID, int depth, int parentID, IList<SweepEvent> result)
        {
            var updateAll = new List<SweepEvent> {startEvent};
            //ToDo: set the following property or determine if it is actually necessary: sweepEvent.ResultInsideOut;
            if (!startEvent.From) startEvent = startEvent.OtherEvent; //Make sure we start with the "From".
            var path = new Path();
            startEvent.Processed = false; //This will be to true right at the end of the while loop. 
            var currentSweepEvent = startEvent;

            do
            {
                //Get the other event (endpoint) for this line. 
                currentSweepEvent = currentSweepEvent.OtherEvent;
                currentSweepEvent.Processed = true;
                updateAll.Add(currentSweepEvent);

                //Since result is sorted lexicographically, the event we are looking for will be adjacent to the current sweep event (note that we are staying on the same point)
                currentSweepEvent = FindNeighbor(currentSweepEvent, result);
                currentSweepEvent.Processed = true;
                updateAll.Add(currentSweepEvent);

                if (!currentSweepEvent.From) throw new Exception("Error in implementation");
                path.Add(currentSweepEvent.Point); //Add the "From" Point    
                            
            } while (currentSweepEvent != startEvent);

            //Once all the events of the path are found, update their PathID, ParentID, Depth fields
            foreach (var sweepEvent in updateAll)
            {
                sweepEvent.PathID = pathID;
                sweepEvent.Depth = depth;
                sweepEvent.ParentPathID = parentID;
                
            }
            return path;
        }

        private static SweepEvent FindNeighbor(SweepEvent se1, IList<SweepEvent> result)
        {
            int positionOfNeighbor;
            if (se1.PositionInResult < result.Count -1 && se1.Point == result[se1.PositionInResult + 1].Point) positionOfNeighbor = se1.PositionInResult + 1;
            else if (se1.Point == result[se1.PositionInResult -1 ].Point) positionOfNeighbor = se1.PositionInResult - 1;
            else throw new Exception("Error. One of the two cases above must be true");    
            var se2 = result[positionOfNeighbor];
            if (se2.Processed) throw new Exception("this should be the first time we interact with this event");

            //The field ResultInsideOut is set to true if the right event precedes the left pevent in the path.
            //if (se2.Left)
            //{
            //    //then right came before left
            //    previousSweepEvent.ResultInsideOut = true;
            //    se2.ResultInsideOut = true;
            //}
            //else //then left came before right
            //{
            //    previousSweepEvent.ResultInsideOut = false;
            //    se2.ResultInsideOut = false;
            //}
            return se2;
        }

        #endregion

        #region Check and Resolve Intersection between two lines
        private static void CheckAndResolveIntersection(SweepEvent se1, SweepEvent se2, ref SweepList sweepLines, ref OrderedSweepEventList orderedSweepEvents, out bool goBack )
        {
            goBack = false;
            if (se1 == null || se2 == null) return;
            if (se1.DuplicateEvent == se2) return;

            var newSweepEvents = new List<SweepEvent>();

            Point intersectionPoint;
            if (MiscFunctions.LineLineIntersection(new Line(se1.Point, se1.OtherEvent.Point),
                new Line(se2.Point, se2.OtherEvent.Point), out intersectionPoint, true) && intersectionPoint == null)
            {
                #region SPECIAL CASE: Collinear
                //SPECIAL CASE: Collinear
                if (se1.Point == se2.Point)
                {
                    if (se1.OtherEvent.Point.X.IsPracticallySame(se2.OtherEvent.Point.X))
                    {
                        //if (se1.PolygonType == se2.PolygonType) throw new NotImplementedException();
                        //Else set duplicates
                    }
                    else if (se1.OtherEvent.Point.X < se2.OtherEvent.Point.X)
                    {
                        //Order goes (1) se1.Point == se2.Point, (2) se1.OtherEvent.Point, (3) se2.OtherEvent.Point
                        //Segment se2 
                        newSweepEvents.AddRange(Segment(se2, se1.OtherEvent.Point));
                    }
                    else
                    {
                        //Order goes (1) se1.Point == se2.Point, (2) se2.OtherEvent.Point, (3) se1.OtherEvent.Point
                        //Segment se1 
                        newSweepEvents.AddRange(Segment(se1, se2.OtherEvent.Point));
                    }
                    //Set DuplicateEvents
                    se1.DuplicateEvent = se2;
                    se1.OtherEvent.DuplicateEvent = se2.OtherEvent;
                    se2.DuplicateEvent = se1;
                    se2.OtherEvent.DuplicateEvent = se1.OtherEvent;
                }

                else
                {
                    //Reorder if necessary (reduces the amount of code)
                    if (se1.Point.X > se2.Point.X)
                    {
                        var temp = se1;
                        se2 = se1;
                        se1 = temp;
                    }

                    if (se1.OtherEvent.Point == se2.OtherEvent.Point)
                    {
                        //Order goes, (1) se1.Point, (2) se2.Point, (3) se1.OtherEvent.Point == se2.OtherEvent.Point
                        goBack = true;
                        sweepLines.RemoveAt(se2.IndexInList);
                        orderedSweepEvents.Insert(se2);

                        //Segment se1
                        var se1Other = se1.OtherEvent;
                        newSweepEvents.AddRange(Segment(se1, se2.Point));

                        //Set DuplicateEvents
                        se2.DuplicateEvent = newSweepEvents[1];
                        newSweepEvents[1].DuplicateEvent = se2;
                        se1Other.DuplicateEvent = se2.OtherEvent;
                        se2.OtherEvent.DuplicateEvent = se1Other;
                    }
                    else if (se1.OtherEvent.Point.X.IsPracticallySame(se2.OtherEvent.Point.X)) throw new NotImplementedException();
                    else if (se1.OtherEvent.Point.X < se2.OtherEvent.Point.X)
                    {
                        //Order goes, (1) se1.Point, (2) se2.Point, (3) se1.OtherEvent.Point, (4) se2.OtherEvent.Point
                        goBack = true;
                        sweepLines.RemoveAt(se2.IndexInList);
                        orderedSweepEvents.Insert(se2);

                        //Segment se1
                        var se1Other = se1.OtherEvent;
                        newSweepEvents.AddRange(Segment(se1, se2.Point));

                        //Segment se2
                        newSweepEvents.AddRange(Segment(se2, se1.OtherEvent.Point));

                        //Set DuplicateEvents
                        se2.DuplicateEvent = newSweepEvents[1];
                        newSweepEvents[1].DuplicateEvent = se2;
                        se1Other.DuplicateEvent = se2.OtherEvent;
                        se2.OtherEvent.DuplicateEvent = se1Other;
                    }
                    else
                    {
                        //Order goes, (1) se1.Point, (2) se2.Point, (3) se2.OtherEvent.Point, (4) se1.OtherEvent.Point
                        goBack = true;
                        sweepLines.RemoveAt(se2.IndexInList);
                        orderedSweepEvents.Insert(se2);

                        //Segment se1
                        newSweepEvents.AddRange(Segment(se1, se2.Point));

                        //Segment second new sweep event
                        newSweepEvents.AddRange(Segment(newSweepEvents[1], se2.OtherEvent.Point));

                        //Set DuplicateEvents
                        se2.DuplicateEvent = newSweepEvents[1];
                        newSweepEvents[1].DuplicateEvent = se2;
                        se2.OtherEvent.DuplicateEvent = newSweepEvents[2];
                        newSweepEvents[2].DuplicateEvent = se2.OtherEvent;
                    }
                }

                //Add all new sweep events
                foreach (var sweepEvent in newSweepEvents)
                {
                    orderedSweepEvents.Insert(sweepEvent);
                }
                return;
                #endregion
            }

            //GENERAL CASE: Lines share a point and cannot possibly intersect. It was not collinear, so return.
            if (se1.Point == se2.Point || se1.Point == se2.OtherEvent.Point ||
                se1.OtherEvent.Point == se2.Point || se1.OtherEvent.Point == se2.OtherEvent.Point)
            {
                return;
            }
            //GENERAL CASE: Lines do not intersect.
            if (intersectionPoint == null) return;

            //SPECIAL CASE: Intersection point is the same as one of previousSweepEvent's line end points.
            if (intersectionPoint == se1.Point)
            {
                var se2Other = se2.OtherEvent;

                var newSweepEvent1 = new SweepEvent(se1.Point, false, !se2.From, se2.PolygonType) { OtherEvent = se2 };
                se2.OtherEvent = newSweepEvent1;

                var newSweepEvent2 = new SweepEvent(se1.Point, true, !se2Other.From, se2.PolygonType) { OtherEvent = se2Other };
                se2Other.OtherEvent = newSweepEvent2;

                //Add all new sweep events
                orderedSweepEvents.Insert(newSweepEvent1);
                orderedSweepEvents.Insert(newSweepEvent2);
            }

            else if (intersectionPoint == se1.OtherEvent.Point)
            {
                var se2Other = se2.OtherEvent;

                var newSweepEvent1 = new SweepEvent(se1.OtherEvent.Point, false, !se2.From, se2.PolygonType) {OtherEvent = se2};
                se2.OtherEvent = newSweepEvent1;

                var newSweepEvent2 = new SweepEvent(se1.OtherEvent.Point, true, !se2Other.From, se2.PolygonType) {OtherEvent = se2Other};
                se2Other.OtherEvent = newSweepEvent2;

                //Add all new sweep events
                orderedSweepEvents.Insert(newSweepEvent1);
                orderedSweepEvents.Insert(newSweepEvent2);
            }

            //SPECIAL CASE: Intersection point is the same as one of se2's line end points. 
            else if (intersectionPoint == se2.Point)
            {
                var se1Other = se1.OtherEvent;

                var newSweepEvent1 = new SweepEvent(se2.Point, false, !se1.From, se1.PolygonType) { OtherEvent = se1 };
                se1.OtherEvent = newSweepEvent1;

                var newSweepEvent2 = new SweepEvent(se2.Point, true, !se1Other.From, se1.PolygonType) { OtherEvent = se1Other };
                se1Other.OtherEvent = newSweepEvent2;

                //Add all new sweep events
                orderedSweepEvents.Insert(newSweepEvent1);
                orderedSweepEvents.Insert(newSweepEvent2);
            }

            else if (intersectionPoint == se2.OtherEvent.Point)
            {
                var se1Other = se1.OtherEvent;

                var newSweepEvent1 = new SweepEvent(se2.OtherEvent.Point, false, !se1.From, se1.PolygonType) { OtherEvent = se1 };
                se1.OtherEvent = newSweepEvent1;

                var newSweepEvent2 = new SweepEvent(se2.OtherEvent.Point, true, !se1Other.From, se1.PolygonType) { OtherEvent = se1Other };
                se1Other.OtherEvent = newSweepEvent2;

                //Add all new sweep events
                orderedSweepEvents.Insert(newSweepEvent1);
                orderedSweepEvents.Insert(newSweepEvent2);
            }

            //GENERAL CASE: Lines are not parallel and only intersct once, between the end points of both lines.
            else
            {
                var se1Other = se1.OtherEvent;
                var se2Other = se2.OtherEvent;

                //Split Sweep Event 1 (previousSweepEvent)
                var newSweepEvent1 = new SweepEvent(intersectionPoint, false, !se1.From, se1.PolygonType) { OtherEvent = se1 };
                se1.OtherEvent = newSweepEvent1;

                var newSweepEvent2 = new SweepEvent(intersectionPoint, true, !se1Other.From, se1.PolygonType) { OtherEvent = se1Other };
                se1Other.OtherEvent = newSweepEvent2;

                //Split Sweep Event 2 (se2)
                var newSweepEvent3 = new SweepEvent(intersectionPoint, false, !se2.From, se2.PolygonType) { OtherEvent = se2 };
                se2.OtherEvent = newSweepEvent3;

                var newSweepEvent4 = new SweepEvent(intersectionPoint, true, !se2Other.From, se2.PolygonType) { OtherEvent = se2Other };
                se2Other.OtherEvent = newSweepEvent4;

                //Add all new sweep events
                orderedSweepEvents.Insert(newSweepEvent1);
                orderedSweepEvents.Insert(newSweepEvent2);
                orderedSweepEvents.Insert(newSweepEvent3);
                orderedSweepEvents.Insert(newSweepEvent4);
            }
        }

        private static IEnumerable<SweepEvent> Segment(SweepEvent sweepEvent, Point point)
        {
            var sweepEventOther = sweepEvent.OtherEvent;
            //Split Sweep Event 1 (previousSweepEvent)
            var newSweepEvent1 = new SweepEvent(point, false, !sweepEvent.From, sweepEvent.PolygonType) { OtherEvent = sweepEvent };
            sweepEvent.OtherEvent = newSweepEvent1;

            var newSweepEvent2 = new SweepEvent(point, true, !sweepEventOther.From, sweepEvent.PolygonType) { OtherEvent = sweepEventOther };
            sweepEventOther.OtherEvent = newSweepEvent2;

            return new List<SweepEvent> { newSweepEvent1, newSweepEvent2 };
        }

        #endregion

        #region SweepList for Boolean Operations
        private class SweepList
        {
            private List<SweepEvent> _sweepEvents;

            public int Count => _sweepEvents.Count;

            public SweepEvent Next(int i)
            {
                if (i == _sweepEvents.Count - 1) return null;
                var sweepEvent = _sweepEvents[i + 1];
                sweepEvent.IndexInList = i + 1;
                return sweepEvent;
            }

            public SweepEvent Previous(int i)
            {
                if (i == 0) return null;
                var sweepEvent = _sweepEvents[i - 1];
                sweepEvent.IndexInList = i - 1;
                return sweepEvent;
            }

            public SweepEvent PreviousOther(int i)
            {
                var current = _sweepEvents[i];
                while (i > 0)
                {
                    i--; //Decrement
                    var previous = _sweepEvents[i];
                    if (current.PolygonType == previous.PolygonType) continue;
                    if (current.Point.Y.IsPracticallySame(previous.Point.Y)) return previous; //The Y's are the same, so use the upper most sweepEvent (earliest in list) to determine if inside.
                    if (current.Point.Y < previous.Point.Y && current.Point.Y < previous.OtherEvent.Point.Y)
                    {
                        //Note that it is possible for either the previous.Point or previous.OtherEvent.Point to be below the current point, as long as the previous point is to the left
                        //of the current.Point and is sloped below or above the current point.
                        throw new Exception("Error in implemenation (sorting?). This should never happen.");
                    }   
                    return previous;
                }
                //No other polygon event was found. Return null (or duplicate event if it exists).
                return current.DuplicateEvent;           
                
            }

            public object PreviousSame(int i)
            {
                var current = _sweepEvents[i];
                while (i > 0)
                {
                    i--; //Decrement
                    var previous = _sweepEvents[i];
                    if (current.PolygonType != previous.PolygonType) continue;
                    if (current.Point.Y.IsPracticallySame(previous.Point.Y)) return previous; //The Y's are the same, so use the upper most sweepEvent (earliest in list) to determine if inside.
                    if (current.Point.Y < previous.Point.Y && current.Point.Y < previous.OtherEvent.Point.Y)
                    {
                        //Note that it is possible for either the previous.Point or previous.OtherEvent.Point to be below the current point, as long as the previous point is to the left
                        //of the current.Point and is sloped below or above the current point.
                        throw new Exception("Error in implemenation (sorting?). This should never happen.");
                    }
                    return previous;
                }
                //No other polygon event was found. Return null (or duplicate event if it exists).
                if(current.DuplicateEvent != null) throw new NotImplementedException();
                return null;
            }

            public void RemoveAt(int i)
            {
                _sweepEvents.RemoveAt(i);
            }

            //Insert, ordered min Y to max Y for the intersection of the line with xval.
            public int Insert(SweepEvent se1)
            {
                if (se1 == null) throw new Exception("Must not be null");
                if (!se1.Left) throw new Exception("Right end point sweep events are not supposed to go into this list");
                
                if (_sweepEvents == null)
                {
                    _sweepEvents = new List<SweepEvent> {se1};
                    return 0;
                }

                var se1Y = se1.Point.Y;
                var i = 0;
                foreach(var se2 in _sweepEvents)
                {
                    if (se1.Point == se2.Point)
                    {
                        if (se1.OtherEvent.Point.Y < se2.OtherEvent.Point.Y)
                        {
                            break;
                        }//Else, increment and continue.
                        i++;
                        continue;
                    }
                    var se2Y = LineIntercept(se2.Point, se2.OtherEvent.Point, se1.Point.X);
                    if (se1Y.IsPracticallySame(se2Y))
                    {
                        if (se1.OtherEvent.Point.Y.IsPracticallySame(se2.OtherEvent.Point.Y))
                        {
                            if (se1.Point.Y.IsPracticallySame(se2.Point.Y)) //increment and continue.
                            {
                                //throw new NotImplementedException(
                                //    "These lines intersect and the points should be merged");
                            }
                            else if (se1.Point.Y < se2.Point.Y)
                            {
                                break;
                            }//Else, increment and continue.
                            i++;
                            continue;
                        }
                        if (se1.OtherEvent.Point.Y < se2.OtherEvent.Point.Y)
                        {
                            break;
                        }//Else, increment and continue.
                        i++;
                        continue;

                    }
                    if (se1Y < se2Y)
                    {
                        break;
                    }
                    //Else, increment
                    i++;
                }
                _sweepEvents.Insert(i, se1);
                return i;
            }


            public int Find(SweepEvent se)
            {
                //ToDo: Could store the position to avoid this time consuming function call
                return _sweepEvents.IndexOf(se);
            }

            public SweepEvent Item(int i)
            {
                return _sweepEvents[i];
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
            public int IndexInList { get; set; }
            public  Point Point { get; } //the point for this sweep event
            public bool Left { get; } //The left endpoint of the line
            public bool From { get; } //The point comes first in the path.
            public SweepEvent OtherEvent { get; set; } //The event of the other endpoint of this line
            public PolygonType PolygonType { get; } //Whether this line was part of the Subject or Clip
            public bool LeftToRight { get; } //represents an inside/outside transition in the its polygon tree (Suject or Clip). This occurs when the edge's "Left" has a higher X value.
            public bool OtherInOut { get; set; } //represents an inside/outside transition in the other polygon tree (Suject or Clip). This occurs when the edge's "Left" has a higher X value.
            public bool InResult { get; set; } //A bool to track which sweep events are part of the result (set depending on boolean operation).
            public SweepEvent PrevInResult { get; set; } //A pointer to the closest ende downwards in S that belongs to the result polgyon. Used to calculate depth and parentIDs.
            public int PositionInResult { get; set; }
            //public bool ResultInsideOut { get; set; } //The field ResultInsideOut is set to true if the right endpoint sweep event precedes the left endpoint sweepevent in the path.
            public int PathID { get; set; }
            public int ParentPathID { get; set; }
            public bool Processed { get; set; } //If this sweep event has already been processed in the sweep
            public int Depth { get; set; }
            public SweepEvent DuplicateEvent { get; set; }


            public SweepEvent(Point point, bool isLeft, bool isFrom, PolygonType polyType)
            {
                Point = point;
                Left = isLeft;
                From = isFrom;
                PolygonType = polyType;
                LeftToRight = From == Left; //If both left and from, or both right and To, then LeftToRight = true;
                DuplicateEvent = null;
            }
        }

        private class OrderedSweepEventList
        {
            private readonly List<SweepEvent> _sweepEvents;

            public OrderedSweepEventList(IEnumerable<SweepEvent> sweepEvents)
            {
                _sweepEvents = new List<SweepEvent>();
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
                foreach (var se2 in _sweepEvents)
                {
                    if (se1.Point == se2.Point) //reference is the same
                    {
                        if (se1.OtherEvent.Point.X.IsPracticallySame(se2.OtherEvent.Point.X))
                        {
                            if (se1.OtherEvent.Point.Y.IsPracticallySame(se2.OtherEvent.Point.Y))
                            {
                                 break; //ok to insert before (Will be marked as a duplicate event)
                            }
                            
                            if (se1.OtherEvent.Point.Y < se2.OtherEvent.Point.Y)
                            {
                                //Insert before se2
                                break;
                            }   //Else increment and continue;
                        }
                        else if (se1.Left && se2.Left) //If both left endpoints, add the lower event first.
                        {
                            if (se1.OtherEvent.Point.Y < se2.OtherEvent.Point.Y)
                            {
                                //Insert before se2
                                break;
                            }   //Else increment and continue;
                        }    
                        else if (se1.OtherEvent.Point.X < se2.OtherEvent.Point.X)
                        {
                            //Insert before se2
                            break;
                        }   //Else increment and continue;
                    }
                    else if (se1.Point.X.IsPracticallySame(se2.Point.X))
                    {
                        //if (se1.Point.Y.IsPracticallySame(se2.Point.Y)) throw new NotImplementedException("Sweep Events need to be merged"); 
                        if (se1.Point.Y < se2.Point.Y) break;
                        breakIfNotNear = true;
                    }
                    else if (breakIfNotNear) break;
                    else if (se1.Point.X < se2.Point.X) break;
                    i++;
                }
                _sweepEvents.Insert(i, se1);
            }
            public bool Any()
            {
                return _sweepEvents.Any();
            }

            public SweepEvent First()
            {
                return _sweepEvents.First();
            }

            public void RemoveAt(int i)
            {
                _sweepEvents.RemoveAt(i);
            }
        }
        #endregion

        #region Other Various Private Functions: PreviousInResult, LineIntercept, & IsPointOnSegment
        private static SweepEvent PreviousInResult(SweepEvent se1, IList<SweepEvent> result)
        {
            //Get the first sweep event that goes below previousSweepEvent
            var i = se1.PositionInResult;
            var se1Y = se1.Point.Y;
            while (i > 0)
            {
                i--; //Decrement
                var se2 = result[i];
                var se2Y = LineIntercept(se2.Point, se2.OtherEvent.Point, se1.Point.X);
                var tempPoint = new Point(se1.Point.X, se2Y);
                if(se2Y < se1Y && IsPointOnSegment(se2.Point, se2.OtherEvent.Point, tempPoint))
                {
                    return se2;
                }
            }
            return null;
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
            var m = (p2.Y - p1.Y) / (p2.X - p1.X);
            return m * (xval - p1.X) + p1.Y;
        }

        private static bool IsPointOnSegment(Point p1, Point p2, Point pointInQuestion)
        {
            if ((pointInQuestion.X < p1.X && pointInQuestion.X < p2.X) ||
                (pointInQuestion.X > p1.X && pointInQuestion.X > p2.X) ||
                (pointInQuestion.Y < p1.Y && pointInQuestion.Y < p2.Y) ||
                (pointInQuestion.Y > p1.Y && pointInQuestion.Y > p2.Y)) return false;
            return true;
        }
        #endregion
    }
}
