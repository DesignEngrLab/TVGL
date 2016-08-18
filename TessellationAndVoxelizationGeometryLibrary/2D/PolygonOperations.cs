using System;
using System.Collections.Generic;
using System.Linq;
using PropertyTools.Wpf;
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
            //ToDo: Simplify within new Union function
           // subject = Union2(subject);
            var polygonSubject = subject.Select(path => new Polygon(path)).ToList();
            var polygonClip = new List<Polygon>();
            if (clip != null)
            {
               // clip = Union2(clip);
                polygonClip.AddRange(clip.Select(path => new Polygon(path)).ToList());
            }
            return BooleanOperation(polygonSubject, polygonClip, BooleanOperationType.Union);
        }

        #region Top Level Boolean Operation Method
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
            var sortedPoints = new SweepPoints(allPoints.OrderBy(point => point.X).ThenBy(point => point.Y).ToList());

            //Find intersections with vertical sweep line moving from left to right 
            //Subdividing the edges as we go.
            var sweepLines = new SweepList();
            while (sortedPoints.Any())
            {
                var point = sortedPoints.First();
                sortedPoints.RemoveAt(0);
                if (point.Ignore) continue;
                //right endpoint, remove all the lines that attach to the left of the point.
                foreach (var line in point.Lines.Where(line => line.RightPoint == point))
                {
                    var next = sweepLines.Next(line);
                    var prev = sweepLines.Previous(line);
                    sweepLines.Remove(line);
                    CheckAndResolveIntersections(next, new List<Line> { prev }, ref sweepLines, ref currentLineSetIndex, ref sortedPoints);
                }
                //left endpoint, add all the lines that attach to the right of the point.
                foreach (var line in point.Lines.Where(line => line.LeftPoint == point))
                {
                    sweepLines.Insert(line, point);
                    CheckAndResolveIntersections(line, new List<Line> { sweepLines.Next(line), sweepLines.Previous(line) }, ref sweepLines, ref currentLineSetIndex, ref sortedPoints);
                }
            }

            //Section 2.
            sortedPoints = new SweepPoints(allPoints.OrderBy(p => p.X).ThenBy(p => p.Y).ToList());
            var solution = new Paths();
            foreach (var point in sortedPoints.OrderedPoints.Where(point => !point.Processed))
            {
                bool isHole;
                var newPath = GetPathFromStartingPoint(point, booleanOperationType, out isHole);
                if (newPath != null)
                {
                    solution.Add(isHole ? CWNegative(newPath) : CCWPositive(newPath));
                }
            }
            return solution;
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
                            if (line.InsideOther) continue; //only intereseted in outside lines for the union.
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
        #endregion

        #region Check and Resolve Intersection between two lines
        private static void CheckAndResolveIntersections(Line line0, List<Line> possibleIntersectionLines, ref SweepList sweepLines, ref int currentLineSetIndex, ref SweepPoints orderedPoints)
        {
            //The reason we do these together is that line0 could intersect both lines.
            if (line0 == null || possibleIntersectionLines.Any()) return;

            //First, get all the intersection points and determine if lines are collinear.
            //If lines are collinear, the intersection point for that LineLineIntersection will return true, but possibileIntersectionLine will be null.
            var intersectionsPoints = new List<Point>();
            var isCollinear = new bool[possibleIntersectionLines.Count];
            for (var i = 0; i < possibleIntersectionLines.Count ; i++)
            {
                var line = possibleIntersectionLines[i];
                if (line0.ToPoint.Equals(line.ToPoint) || line0.FromPoint.Equals(line.ToPoint) ||
                    line0.ToPoint.Equals(line.FromPoint) || line0.FromPoint.Equals(line.FromPoint))
                {
                    isCollinear[i] = false;
                    intersectionsPoints.Add(null);
                }       
                else
                {
                    Point intersectionPoint = null;
                    if (MiscFunctions.LineLineIntersection(line0, line, out intersectionPoint, true) &&
                        intersectionPoint == null)
                    {
                        isCollinear[i] = true;
                        intersectionsPoints.Add(null);
                    }
                    else
                    {
                        isCollinear[i] = false;
                        intersectionsPoints.Add(intersectionPoint);
                    }
                }
            }

            #region Handle Collinear Lines
            //If any of the lines are collinear, do them first, then romove them for the possibleIntersectionLines
            if(isCollinear.Where(c => c).Count() > 1) throw new NotImplementedException("I have not considered this special case yet");
            for (var i = 0; i < possibleIntersectionLines.Count; i++)
            {
                if (!isCollinear[i]) continue;
                var intLine = possibleIntersectionLines[i];
                //Else handle the special case of collinearity
                //First order all the points in question
                var collinearPoints = new List<Point>
                {
                    line0.FromPoint,
                    line0.ToPoint,
                    intLine.FromPoint,
                    intLine.ToPoint
                };
                var collinearOrderedPoints = collinearPoints.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
                var newLine1 = new Line(collinearOrderedPoints[0], collinearOrderedPoints[1], false) {IndexInList = currentLineSetIndex};
                currentLineSetIndex++;
                var newLine2 = new Line(collinearOrderedPoints[1], collinearOrderedPoints[2], false) { IndexInList = currentLineSetIndex };
                currentLineSetIndex++;
                var newLine3 = new Line(collinearOrderedPoints[2], collinearOrderedPoints[3], false) { IndexInList = currentLineSetIndex }; ;
                currentLineSetIndex++;
                sweepLines.Remove(line0);

                //New Line 3
                if (newLine3.Length.IsNegligible())
                {
                    //Erase point 3 and update reference by updating the lines
                    collinearOrderedPoints[1].Ignore = true;
                    foreach (var line in collinearOrderedPoints[3].Lines)
                    {
                        if (line.ToPoint.Equals(collinearOrderedPoints[3]))
                        {
                            line.ReplaceFromPoint(collinearOrderedPoints[2]);
                        }
                        else
                        {
                            line.ReplaceToPoint(collinearOrderedPoints[2]);
                        }
                    }
                }
                else
                {
                    newLine3.ToPoint.Lines.Add(newLine3);
                    newLine3.FromPoint.Lines.Add(newLine3);
                }

                //New Line 2
                if (newLine2.Length.IsNegligible())
                {
                    //Erase point 2 and update reference by updating the lines
                    collinearOrderedPoints[1].Ignore = true;
                    foreach (var line in collinearOrderedPoints[2].Lines)
                    {
                        if (line.ToPoint.Equals(collinearOrderedPoints[2]))
                        {
                            line.ReplaceFromPoint(collinearOrderedPoints[1]);
                        }
                        else
                        {
                            line.ReplaceToPoint(collinearOrderedPoints[1]);
                        }
                    }

                    if(newLine3.Length.IsNegligible()) throw new NotImplementedException();
                    //Add New Line 3
                    sweepLines.Insert(newLine3, collinearOrderedPoints[1]);
                    line0 = newLine3;
                }
                else
                {
                    newLine2.ToPoint.Lines.Add(newLine2);
                    newLine2.FromPoint.Lines.Add(newLine2);
                    sweepLines.Insert(newLine2, collinearOrderedPoints[1]);
                    line0 = newLine2;
                }

                //New Line 1
                if (newLine1.Length.IsNegligible())
                {
                    //Erase point 1 and update reference by updating the lines
                    collinearOrderedPoints[1].Ignore = true;
                    foreach (var line in collinearOrderedPoints[1].Lines)
                    {
                        if (line.ToPoint.Equals(collinearOrderedPoints[1]))
                        {
                            line.ReplaceFromPoint(collinearOrderedPoints[0]);
                        }
                        else
                        {
                            line.ReplaceToPoint(collinearOrderedPoints[0]);
                        }
                    }
                }
                else
                {
                    newLine1.ToPoint.Lines.Add(newLine1);
                    newLine1.FromPoint.Lines.Add(newLine1);
                }
            }
            #endregion

            //Set some variables
            var p1 = intersectionsPoints[0];
            var p2 = intersectionsPoints[1];
            if (p1 == null && p2 == null) return;
            var line1 = possibleIntersectionLines[0];
            var line2 = possibleIntersectionLines[1];

            //SPECIAL CASE: Intersection points are the same (compared with is negligible)
            if (p1 != null && p2 != null && p1.X.IsPracticallySame(p2.X) &&
                p1.Y.IsPracticallySame(p2.Y))
            {
                Line leftLine0;
                Line leftLine1;
                Line leftLine2;
                
                //p1 Lines list is set during SegmentLine, as well as, the other points Lines lists
                SegmentLine(line0, p1, out leftLine0, ref currentLineSetIndex);
                SegmentLine(line1, p1, out leftLine1, ref currentLineSetIndex);
                SegmentLine(line2, p1, out leftLine2, ref currentLineSetIndex);
                sweepLines.Insert(leftLine0, p1);
                sweepLines.Insert(leftLine1, p1);
                sweepLines.Insert(leftLine2, p1);
                orderedPoints.Insert(p1);
                return;
            }

            //Now do the rest of the intersections, starting with the right most intersection
            //CASE 1
            if (p1 != null && (p2 == null || p1.X > p2.X))
            {
                Line leftLine0;
                Line leftLine1;
                SegmentLine(line0, p1, out leftLine0, ref currentLineSetIndex);
                SegmentLine(line1, p1, out leftLine1, ref currentLineSetIndex);
                sweepLines.Insert(leftLine1, p1);
                orderedPoints.Insert(p1);
                if (p2 != null)
                {
                    Line leftLine00;
                    Line leftLine2;
                    SegmentLine(leftLine1, p2, out leftLine00, ref currentLineSetIndex);
                    SegmentLine(line2, p2, out leftLine2, ref currentLineSetIndex);
                    sweepLines.Insert(leftLine00, p2);
                    sweepLines.Insert(leftLine2, p2);
                    orderedPoints.Insert(p2);
                }
                else
                {
                    sweepLines.Insert(leftLine0, p1);
                }
                return;
            }
            //OR CASE 2
            if (p1 == null || p2.X > p1.X)
            {
                Line leftLine0;
                Line leftLine2;
                SegmentLine(line0, p2, out leftLine0, ref currentLineSetIndex);
                SegmentLine(line2, p2, out leftLine2, ref currentLineSetIndex);
                sweepLines.Insert(leftLine2, p2);
                orderedPoints.Insert(p2);
                if (p1 != null)
                {
                    Line leftLine00;
                    Line leftLine1;
                    SegmentLine(leftLine2, p1, out leftLine00, ref currentLineSetIndex);
                    SegmentLine(line1, p1, out leftLine1, ref currentLineSetIndex);
                    sweepLines.Insert(leftLine00, p1);
                    sweepLines.Insert(leftLine1, p1);
                    orderedPoints.Insert(p1);
                }
                else
                {
                    sweepLines.Insert(leftLine0, p2);
                }
                return;
            }

            throw new NotImplementedException();
        }

        private static void SegmentLine(Line line, Point point, out Line leftLine, ref int currentLineSetIndex)
        {
            line.ToPoint.Lines.Remove(line);
            line.FromPoint.Lines.Remove(line);
            leftLine = new Line(line.LeftPoint, point, true) {IndexInList = currentLineSetIndex };
            currentLineSetIndex++;
            var rightLine = new Line(point, line.RightPoint, true) { IndexInList = currentLineSetIndex };
            currentLineSetIndex++;
        }

        #endregion

        #region SweepList for Boolean Operations
        private class SweepList
        {
            private Dictionary<int, int> _indexInSweepGivenLineIndex;

            private List<Line> Lines;

            public Line Next(Line line)
            {
                var indexInLines = _indexInSweepGivenLineIndex[line.IndexInList];
                if (indexInLines == Lines.Count -1 ) return null;
                return Lines[indexInLines + 1];
            }

            public Line Previous(Line line)
            {
                var indexInLines = _indexInSweepGivenLineIndex[line.IndexInList];
                if (indexInLines == 0) return null;
                return Lines[indexInLines - 1];
            }

            public void Remove(Line line)
            {
                var indexInLines = _indexInSweepGivenLineIndex[line.IndexInList];
                Lines.RemoveAt(indexInLines);
                UpdateIndexDictionary();
            }

            private void UpdateIndexDictionary()
            {
                _indexInSweepGivenLineIndex.Clear();
                for (var i = 0; i < Lines.Count; i++)
                {
                    _indexInSweepGivenLineIndex.Add(Lines[i].IndexInList, i);
                }
            }

            public void Insert(Line line, Point point)
            {
                var index = -1;
                if (Lines == null)
                {
                    Lines = new List<Line> {line};
                    index = 0;
                    _indexInSweepGivenLineIndex = new Dictionary<int, int> { {line.IndexInList, index} };
                }
                else
                {
                    var leftPoint = line.LeftPoint;
                    var lineY = leftPoint.Y;
                    var insertAtEnd = true;
                    for (var i = 0; i < Lines.Count; i++)
                    {
                        var otherLine = Lines[i];
                        if (otherLine.LeftPoint == leftPoint)
                        {
                            if (line.OtherPoint(leftPoint).Y > otherLine.OtherPoint(otherLine.LeftPoint).Y)
                            {
                                //Insert after other line
                                index = i + 1;
                                Lines.Insert(i + 1, line);
                            }
                            else
                            {
                                //Insert before other line
                                index = i;
                                Lines.Insert(i, line);
                            }
                            //Finished
                            insertAtEnd = false;
                            break;
                        }
                        else
                        {
                            var otherLineY = otherLine.YGivenX(leftPoint.X);
                            if (lineY.IsPracticallySame(otherLineY))
                            {
                                if (line.RightPoint.Y.IsPracticallySame(otherLine.RightPoint.Y))
                                {
                                    //Insert according to lower X value
                                    if(line.RightPoint.X.IsPracticallySame(otherLine.RightPoint.X)) throw new Exception("Lines are colinear and attached");
                                    if (line.LeftPoint.X < otherLine.LeftPoint.X)
                                    {
                                        //Insert before the other line
                                        index = i;
                                        Lines.Insert(i, line);
                                    }
                                    else
                                    {
                                        //Insert after the other line
                                        index = i + 1;
                                        Lines.Insert(i + 1, line);
                                    }
                                }
                                else if (line.RightPoint.Y < otherLine.RightPoint.Y)
                                {
                                    //Insert before the other line
                                    index = i;
                                    Lines.Insert(i, line);
                                }
                                else
                                {
                                    //Insert after the other line
                                    index = i+1;
                                    Lines.Insert(i+1, line);
                                }
                                //Finished
                                insertAtEnd = false;
                                break;
                            }

                            if (!(lineY < otherLineY)) continue;
                            //Insert before the other line
                            index = i;
                            Lines.Insert(i, line);
                            //Finished
                            insertAtEnd = false;
                            break;
                        }
                    }
                    if (insertAtEnd)
                    {
                        Lines.Add(line);
                        index = Lines.Count - 1;
                    }
                    UpdateIndexDictionary();
                }
                if (index == -1) throw new Exception("error in implementation");

                //Set information
                var linesAboveFromOther = 0;
                var linesBelowFromOther = 0;
                var linesAboveFromSame = 0;
                var linesBelowFromSame = 0;
                for (var i = 0; i < Lines.Count; i++)
                {
                    var line2 = Lines[i];
                    if (line2.ToPoint == point || line2.FromPoint == point)
                        continue;
                    //Nope. this next statement will cause an error if a hole lines up with a point on the lines above or below
                    //if (line2.ToPoint.X.IsPracticallySame(point.X) ||
                    //    line2.FromPoint.X.IsPracticallySame(point.X)) continue;
                    //else
                    var polyType = line.PolygonType;
                    if (i < index)
                    {
                        if (line2.PolygonType == polyType)
                        {
                            linesBelowFromSame++;
                        }
                        else
                        {
                            linesBelowFromOther++;
                        }
                    }
                    else //i > index (i = index results in the same point)
                    {
                        if (line2.PolygonType == polyType)
                        {
                            linesAboveFromSame++;
                        }
                        else
                        {
                            linesAboveFromOther++;
                        }
                    }
                }
                //Check if even or odd
                if (linesBelowFromSame % 2 == 0 && linesAboveFromSame % 2 == 0) //both even
                {
                    line.InsideSame = false;
                }
                else if (linesBelowFromSame % 2 != 0 && linesAboveFromSame % 2 != 0) //both odd
                {
                    line.InsideSame = true;
                }
                //Note: we will only ever use the startline Inside booleans, so it doesn't matter if we have some cases that fail
                //else throw new Exception("Inconsistent Reading. Fix code. May need to add more detail.");
                if (linesBelowFromOther % 2 == 0 && linesAboveFromOther % 2 == 0)
                {
                    line.InsideOther = false;
                }
                if (linesBelowFromOther % 2 != 0 && linesAboveFromOther % 2 != 0)
                {
                    line.InsideOther = true;
                }
            }

            public void Replace(Line line, Line newLine)
            {
                var indexInLines = _indexInSweepGivenLineIndex[line.IndexInList];
                Lines.RemoveAt(indexInLines);
                Lines.Add(newLine);
                newLine.InsideOther = line.InsideOther;
                newLine.InsideSame = line.InsideSame;
                _indexInSweepGivenLineIndex.Remove(line.IndexInList);
                _indexInSweepGivenLineIndex.Add(newLine.IndexInList, indexInLines);
            }

            public int IndexOf(Line line)
            {
                return _indexInSweepGivenLineIndex[line.IndexInList];
            }
        }
        #endregion

        #region SweepPoint List for Boolean Operations
        private class SweepPoints
        {
            public readonly List<Point> OrderedPoints;

            public SweepPoints(IEnumerable<Point> unorderedPoints)
            {
                OrderedPoints = new List<Point>();
                foreach (var point in unorderedPoints)
                {
                    Insert(point);
                }
            }

            public void Insert(Point p1)
            {
                //Find the index for p1
                var i = 0;
                var breakIfNotNear = false;
                foreach (var p2 in OrderedPoints)
                {
                    if (p1.X.IsPracticallySame(p2.X))
                    {
                        if(p1.Y.IsPracticallySame(p2.Y)) throw new NotImplementedException();
                        if (p1.Y < p2.Y) break;
                        breakIfNotNear = true;
                        i++;
                    }
                    else if (breakIfNotNear) break;
                    else if (p1.X < p2.X) break;
                    i++;
                }
                OrderedPoints.Insert(i, p1);
            }

            public bool Any()
            {
                return OrderedPoints.Any();
            }

            public Point First()
            {
                return OrderedPoints.First();
            }

            public void RemoveAt(int i)
            {
                OrderedPoints.RemoveAt(i);
            }
        }
        #endregion
    }
}
