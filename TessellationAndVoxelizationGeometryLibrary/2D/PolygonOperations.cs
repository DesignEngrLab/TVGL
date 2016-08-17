using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using StarMathLib;
using TVGL.Clipper;

namespace TVGL
{
    using Path = List<Point>;
    using Paths = List<List<Point>>;

    #region Polygon Operations
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
            var polygonSubject = subject.Select(path => new Polygon(path)).ToList();
            var polygonClip = new List<Polygon>();
            if (clip != null)
            {
                polygonClip.AddRange(clip.Select(path => new Polygon(path)).ToList());
            }
            return BooleanOperation(polygonSubject, polygonClip, BooleanOperationType.Union);
        }


        /// <reference>
        /// A simple algorithm for Boolean operations on polygons. Martínez, et. al. 2013. Advances in Engineering Software.
        /// </reference>
        private static List<List<Point>> BooleanOperation(IList<Polygon> subject, IList<Polygon> clip, BooleanOperationType booleanOperationType)
        {
            //1.Find intersections with vertical sweep line
            //1.Subdivide the edges of the polygons at their intersection points.
            //2.Select those subdivided edges that lie inside—or outside—the other polygon.
            //3.Join the selected edges to form the contours of the result polygon and compute the child contours.

            //Set all line to unique index values.
            var subjectLines = subject.SelectMany(polygon => polygon.PathLines).ToList();
            var clipLines = clip.SelectMany(polygon => polygon.PathLines).ToList();
            var currentLineSetIndex = 0; //This index will give every line a unique index. New lines added later will also get an index (globally in this function).
            foreach (var line in subjectLines)
            {
                line.IndexInList = currentLineSetIndex;
                line.PolygonType = PolygonType.Subject;
                currentLineSetIndex ++;
            }
            foreach (var line in clipLines)
            {
                line.IndexInList = currentLineSetIndex;
                line.PolygonType = PolygonType.Clip;
                currentLineSetIndex++;
            }

            //Sort points lexographically. Low X to High X, then Low Y to High Y.
            var allPoints = subject.SelectMany(polygon => polygon.Path).ToList();
            allPoints.AddRange(clip.SelectMany(polygon => polygon.Path));
            //ToDo: Should sort using StarMaths IsNegligible.
            var sortedPoints = allPoints.OrderBy(point => point.X).ThenBy(point => point.Y).ToList();

            //Find intersections with vertical sweep line moving from left to right 
            //Subdividing the edges as we go.
            var result = new List<Line>();
            var sweepLines = new SweepList();
            while (sortedPoints.Any())
            {
                var point = sortedPoints.First();
                sortedPoints.RemoveAt(0);
                foreach (var line in point.Lines)
                {

                    if (line.LeftPoint == point) //the point is the left endpoint
                    {
                        sweepLines.Insert(line);
                        //SetInformation(point, sweepLines.Previous(line));
                        Point intersectionPoint1, intersectionPoint2;
                        CheckAndResolveIntersection(line, sweepLines.Next(line), ref sweepLines,
                            ref currentLineSetIndex, out intersectionPoint1);
                        CheckAndResolveIntersection(line, sweepLines.Previous(line), ref sweepLines,
                            ref currentLineSetIndex, out intersectionPoint2);
                        if (intersectionPoint1 != null)
                        {
                            sortedPoints.Add(intersectionPoint1);
                            allPoints.Add(intersectionPoint1);
                        }
                        if (intersectionPoint2 != null)
                        {
                            sortedPoints.Add(intersectionPoint2);
                            allPoints.Add(intersectionPoint2);
                        }

                        //If either produced an intersection point that was added to the list. Reorder the list.
                        //ToDo: Insert into the correct position in list and get rid of this order by function.
                        if (intersectionPoint1 != null || intersectionPoint2 != null)
                        {
                            var temp = sortedPoints.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
                            sortedPoints = temp;
                        }
                    }
                    else //the point is the right endpoint
                    {
                        var next = sweepLines.Next(line);
                        var prev = sweepLines.Previous(line);
                        sweepLines.Remove(line);
                        Point intersectionPoint;
                        CheckAndResolveIntersection(next, prev, ref sweepLines, ref currentLineSetIndex,
                            out intersectionPoint);
                        if (intersectionPoint != null)
                        {
                            sortedPoints.Add(intersectionPoint);
                            allPoints.Add(intersectionPoint);
                            var temp = sortedPoints.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
                            sortedPoints = temp;
                        }
                    }
                    //if (line.InResult)
                    //{
                    //    result.Add(line);
                    //}
                }
            }

            //Section 2.
            sortedPoints = allPoints.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
            var PathID = 0;
            var solution = new Paths();
            foreach (var point in sortedPoints)
            {
                if (!point.Processed)
                {
                    var newPath = GetPathFromStartingPoint(point, subject, clip, booleanOperationType);
                    if (newPath != null)
                    {
                        solution.Add(newPath);
                    }
                }
            }
            //ToDo: Order CCW or CW 

            return solution;
        }

        private static List<Point> GetPathFromStartingPoint(Point startPoint, IList<Polygon> subject, IList<Polygon> clip, BooleanOperationType booleanOperationType)
        {
            //Start ordering in CCW order. "point" must be the left most, bottom startPoint. 
            //Therefore, CCW should choose the lower Y value of the two lines
            if (startPoint.Lines.Count != 2) throw new Exception("This point is not correct or its list of lines is faulty");
            var startLine = startPoint.Lines[0].RightPoint.Y < startPoint.Lines[1].RightPoint.Y ? startPoint.Lines[0] : startPoint.Lines[1];
            startLine.Processed = true;
            var currentPoint = startLine.OtherPoint(startPoint);
            currentPoint.Processed = true;
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
                        foreach (var line in currentPoint.Lines)
                        {
                            var otherPoint = line.OtherPoint(currentPoint);
                            if (line.Processed) continue;
                            line.Processed = true;
                            if (otherPoint.Lines.Count > 2) continue; //skip intersection point line
                            var lineCenter = new Point(line.Center);
                            if (PointIsInsideOther(line, lineCenter, subject, clip)) continue; //skip if point is inside other (not part of Union)
                            //else
                            path.Add(otherPoint);
                            currentPoint = otherPoint;
                            if (alreadyAddedPoint) throw new Exception("can only add one point");
                            alreadyAddedPoint = true;
                        }
                        if (!alreadyAddedPoint)//No point was found. This path does not complete and is not used in the Union
                        {
                            return null;
                        }
                    }
                }
                currentPoint.Processed = true;
            }
            return path;
        }

        private static bool PointIsInsideOther(Line line, Point pointOfInterest, IList<Polygon> subject, IList<Polygon> clip)
        {
            var otherPaths = new List<List<Point>>();
            if (line.PolygonType == PolygonType.Subject)
            {
                otherPaths.AddRange(clip.Select(polygon => new Path(polygon.Path)));
            }
            else
            {
                otherPaths.AddRange(subject.Select(polygon => new Path(polygon.Path)));
            }
            foreach (var path in otherPaths)
            {
                if (MiscFunctions.IsPointInsidePolygon(path, pointOfInterest, false))
                {
                    return true;
                }
            }
            return false;
        }

        //private static void SetInformation(Point point, Line previous)
        //{
        //    throw new NotImplementedException();
        //}

        #region Check and Resolve Intersection between two lines
        private static void CheckAndResolveIntersection(Line line1, Line line2, ref SweepList sweepLines, ref int currentLineSetIndex, out Point intersectionPoint)
        {
            if (!MiscFunctions.LineLineIntersection(line1, line2, out intersectionPoint, true)) return;
            //It does itersect.

            //Remove references to line1 and line2
            line1.LeftPoint.Lines.Remove(line1);
            line1.RightPoint.Lines.Remove(line1);
            line2.LeftPoint.Lines.Remove(line2);
            line2.RightPoint.Lines.Remove(line2);

            //Subdivide the lines. Start with the two lines we will add to sweepLines. (left of intersection point)
            var newLine1 = new Line(line1.LeftPoint, intersectionPoint)
            {
                PolygonType = line1.PolygonType,
                IndexInList = currentLineSetIndex,
            };
            currentLineSetIndex++;
            sweepLines.Replace(line1, newLine1);

            var newLine2 = new Line(line2.LeftPoint, intersectionPoint)
            {
                PolygonType = line2.PolygonType,
                IndexInList = currentLineSetIndex,
            };
            currentLineSetIndex++;
            sweepLines.Replace(line2, newLine2);

            //Now do the other two lines. The point reference will call these lines once the intersection startPoint comes up in sortedPoints.
            var newLine3 = new Line(intersectionPoint, line1.RightPoint)
            {
                PolygonType = line1.PolygonType,
                IndexInList = currentLineSetIndex,
            };
            currentLineSetIndex++;

            var newLine4 = new Line(intersectionPoint, line2.RightPoint)
            {
                PolygonType = line2.PolygonType,
                IndexInList = currentLineSetIndex,
            };
            currentLineSetIndex++;
            return;
        }
        #endregion

        #region SweepList for Boolean Operations
        private class SweepList
        {
            private Dictionary<int, int> _indexInSweepGivenLineIndex;

            private List<Line> _lines;

            public Line Next(Line line)
            {
                var indexInLines = _indexInSweepGivenLineIndex[line.IndexInList];
                return _lines[indexInLines + 1];
            }

            public Line Previous(Line line)
            {
                var indexInLines = _indexInSweepGivenLineIndex[line.IndexInList];
                return _lines[indexInLines - 1];
            }

            public void Remove(Line line)
            {
                var indexInLines = _indexInSweepGivenLineIndex[line.IndexInList];
                _lines.RemoveAt(indexInLines);

                //Recreate IndexInSweepGivenLineIndex
                _indexInSweepGivenLineIndex.Clear();
                for (var i = 0; i < _lines.Count; i++)
                {
                    _indexInSweepGivenLineIndex.Add(_lines[i].IndexInList, i);
                }
            }

            public void Insert(Line line)
            {
                if (_lines == null)
                {
                    _lines = new List<Line> {line};
                    _indexInSweepGivenLineIndex = new Dictionary<int, int> { {line.IndexInList, 0}};
                }
                else
                {
                    var leftPoint = line.LeftPoint;
                    var lineY = leftPoint.Y;
                    var insertAtEnd = true;
                    for (var i = 0; i < _lines.Count; i++)
                    {
                        var otherLine = _lines[i];
                        if (otherLine.LeftPoint == leftPoint)
                        {
                            if (line.OtherPoint(leftPoint).Y < otherLine.OtherPoint(otherLine.LeftPoint).Y)
                            {
                                //Insert after other line
                                _lines.Insert(i + 1, line);
                                _indexInSweepGivenLineIndex.Add(line.IndexInList, i+1);
                            }
                            else
                            {
                                //Insert before other line
                                _lines.Insert(i, line);
                                _indexInSweepGivenLineIndex.Add(line.IndexInList, i);
                            }
                            //Finished
                            insertAtEnd = false;
                            break;
                        }
                        else
                        {
                            var otherLineY = otherLine.YGivenX(leftPoint.X);
                            if (lineY.IsPracticallySame(otherLineY)) throw new NotImplementedException();
                            if (lineY < otherLineY)
                            {
                                //Insert before the other line
                                _lines.Insert(i, line);
                                _indexInSweepGivenLineIndex.Add(line.IndexInList, i);
                                //Finished
                                insertAtEnd = false;
                                break;
                            }
                        }
                    }
                    if (insertAtEnd)
                    {
                        _lines.Add(line);
                        _indexInSweepGivenLineIndex.Add(line.IndexInList, _lines.Count-1);
                    }
                }
            }

            public void Replace(Line line, Line newLine)
            {
                var indexInLines = _indexInSweepGivenLineIndex[line.IndexInList];
                _lines.RemoveAt(indexInLines);
                _lines.Add(newLine);
                _indexInSweepGivenLineIndex.Remove(line.IndexInList);
                _indexInSweepGivenLineIndex.Add(newLine.IndexInList, indexInLines);
            }
        }
        #endregion

    }
    #endregion
}
