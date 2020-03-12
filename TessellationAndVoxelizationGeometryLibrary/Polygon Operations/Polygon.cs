using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using TVGL.Numerics;

using TVGL.IOFunctions;

namespace TVGL
{
    [KnownType(typeof(List<Vector2>))]
    public readonly struct PolygonLight
    {
        /// <summary>
        /// Gets the Vector2s that make up the polygon
        /// </summary>
        [JsonIgnore]
        public readonly List<Vector2> Path;

        /// <summary>
        /// Gets the area of the polygon. Negative Area for holes.
        /// </summary>
        public readonly double Area;

        /// <summary>
        /// Maximum X value
        /// </summary>
        public readonly double MaxX;

        /// <summary>
        /// Minimum X value
        /// </summary>
        public readonly double MinX;

        /// <summary>
        /// Maximum Y value
        /// </summary>
        public readonly double MaxY;

        /// <summary>
        /// Minimum Y value
        /// </summary>
        public readonly double MinY;

        public PolygonLight(Polygon polygon)
        {
            Area = polygon.Area;
            Path = new List<Vector2>();
            foreach (var point in polygon.Path)
                Path.Add(point);

            MaxX = polygon.MaxX;
            MaxY = polygon.MaxY;
            MinX = polygon.MinX;
            MinY = polygon.MinY;
        }

        public PolygonLight(IEnumerable<Vector2> points)
        {
            Path = new List<Vector2>(points);
            Area = MiscFunctions.AreaOfPolygon(Path);
            MaxX = double.MinValue;
            MinX = double.MaxValue;
            MaxY = double.MinValue;
            MinY = double.MaxValue;
            foreach (var point in Path)
            {
                if (point.X > MaxX) MaxX = point.X;
                if (point.X < MinX) MinX = point.X;
                if (point.Y > MaxY) MaxY = point.Y;
                if (point.Y < MinY) MinY = point.Y;
            }
        }

        public static PolygonLight Reverse(PolygonLight original)
        {
            var path = new List<Vector2>(original.Path);
            path.Reverse();
            var newPoly = new PolygonLight(path);
            return newPoly;
        }

        public double Length => MiscFunctions.Perimeter(Path);

        public bool IsPositive => Area >= 0;

        public void Serialize(string filename)
        {
            using (var writer = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                var ser = new DataContractSerializer(typeof(PolygonLight));
                ser.WriteObject(writer, this);
            }
        }

        public static PolygonLight Deserialize(string filename)
        {
            using (var reader = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                var ser = new DataContractSerializer(typeof(PolygonLight));
                return (PolygonLight)ser.ReadObject(reader);
            }
        }

        internal IEnumerable<double> ConvertToDoublesArray()
        {
            return Path.SelectMany(p => new[] { p.X, p.Y });
        }

        internal static PolygonLight MakeFromBinaryString(double[] coordinates)
        {
            var points = new List<Vector2>();
            for (int i = 0; i < coordinates.Length; i += 2)
                points.Add(new Vector2(coordinates[i], coordinates[i + 1]));
            return new PolygonLight(points);
        }
    }

    internal enum PolygonType
    {
        Subject,
        Clip
    };

    /// <summary>
    /// A list of 2D points
    /// </summary>
    public class Polygon
    {
        /// <summary>
        /// The list of 2D points that make up a polygon.
        /// </summary>
        public List<Vector2> Path;

        /// <summary>
        /// The list of 2D points that make up a polygon.
        /// </summary>
        public PolygonLight Light => new PolygonLight(this);

        /// <summary>
        /// The list of lines that make up a polygon. This is not set by default.
        /// </summary>
        public List<Line> PathLines;

        /// <summary>
        /// A list of the polygons inside this polygon.
        /// </summary>
        public List<Polygon> Childern;

        /// <summary>
        /// The polygon that this polygon is inside of.
        /// </summary>
        public Polygon Parent;

        /// <summary>
        /// The index of this child in its parent's child list.
        /// </summary>
        public int Index
        {
            get { return _index; }
            set
            {
                _index = value;
                //foreach (var point in Path)
                //{
                //    point.PolygonIndex = _index;
                //}
            }
        }

        private int _index;

        /// <summary>
        /// Gets or sets whether the path is CCW positive. This will reverse the path if it was ordered CW.
        /// </summary>
        public bool IsPositive
        {
            get { return !(Area < 0); }
            set
            {
                if (value == true)
                {
                    SetToCCWPositive();
                }
                else SetToCWNegative();
            }
        }

        /// <summary>
        /// This reverses the polygon, including updates to area and the point path.
        /// </summary>
        public void Reverse()
        {
            if (IsPositive) SetToCWNegative();
            else SetToCCWPositive();
        }

        /// <summary>
        /// Gets the length of the polygon.
        /// </summary>
        public double Length;

        /// <summary>
        /// Gets the area of the polygon. Negative Area for holes.
        /// </summary>
        public double Area;

        /// <summary>
        /// Maxiumum X value
        /// </summary>
        public double MaxX;

        /// <summary>
        /// Miniumum X value
        /// </summary>
        public double MinX;

        /// <summary>
        /// Maxiumum Y value
        /// </summary>
        public double MaxY;

        /// <summary>
        /// Minimum Y value
        /// </summary>
        public double MinY;

        /// <summary>
        /// Polygon Constructor. Assumes path is closed and not self-intersecting.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="setLines"></param>
        /// <param name="index"></param>
        public Polygon(IEnumerable<Vector2> points, bool setLines = false, int index = -1)
        {
            Path = new List<Vector2>(points);
            //set index in path
            MaxX = double.MinValue;
            MinX = double.MaxValue;
            MaxY = double.MinValue;
            MinY = double.MaxValue;
            for (var i = 0; i < Path.Count; i++)
            {
                var point = Path[i];
                if (point.X > MaxX) MaxX = point.X;
                if (point.X < MinX) MinX = point.X;
                if (point.Y > MaxY) MaxY = point.Y;
                if (point.Y < MinY) MinY = point.Y;
                //point.Lines = new List<Line>(); //erase any previous connection to lines.
            }
            Index = index;
            Area = CalculateArea();
            Length = SetLength();
            PathLines = null;
            Parent = null;
            Childern = new List<Polygon>();

            if (setLines)
            {
                SetPathLines();
            }
        }

        public Polygon(PolygonLight poly, bool setLines = false) : this(poly.Path, setLines)
        {
        }

        private double SetLength()
        {
            return MiscFunctions.Perimeter(Path);
        }

        /// <summary>
        /// Sets a polygon to counter clock wise positive
        /// </summary>
        private void SetToCCWPositive()
        {
            //Check if already positive ccw.
            if (!(Area < 0)) return;

            //It is negative. Reverse the path and path lines.
            var path = new List<Vector2>(Path);
            path.Reverse();
            Area = -Area;
            Path = path;

            //Only reverse the lines if they have been generated
            if (PathLines == null) return;
            var lines = new List<Line>(PathLines);
            lines.Reverse();
            for (var i = 0; i < lines.Count; i++)
            {
                lines[i].IndexInPath = i;
            }
            PathLines = lines;
        }

        private void SetToCWNegative()
        {
            //Check if already negative cw.
            if (Area < 0) return;

            //It is positive. Reverse the path and path lines.
            var path = new List<Vector2>(Path);
            path.Reverse();
            Area = -Area;
            Path = path;

            //Only reverse the lines if they have been generated
            if (PathLines == null) return;
            var lines = new List<Line>(PathLines);
            lines.Reverse();
            for (var i = 0; i < lines.Count; i++)
            {
                lines[i].IndexInPath = i;
            }
            PathLines = lines;
        }

        private double CalculateArea()
        {
            return MiscFunctions.AreaOfPolygon(Path.ToArray());
        }

        /// <summary>
        /// Returns a list of lines that make up the path of this polygon
        /// </summary>
        /// <returns></returns>
        public void SetPathLines()
        {
            var lines = new List<Line>();
            var n = Path.Count;
            for (var i = 0; i < n; i++)
            {
                var j = (i + 1) % n;
                lines.Add(new Line(Path[i], Path[j], true) { IndexInPath = i });
            }
            PathLines = new List<Line>(lines);
        }

        /// <summary>
        /// Gets the next point in the path, given the current point index. 
        /// This function automatically wraps back to the first point.
        /// </summary>
        /// <param name="currentPointIndex"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public Vector2 NextPoint(int currentPointIndex)
        {
            return Path[NextPointIndex(currentPointIndex)];
        }

        /// <summary>
        /// Gets the index of the next point in the path, given the current point index. 
        /// This function automatically wraps back to index 0.
        /// </summary>
        /// <param name="currentPointIndex"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public int NextPointIndex(int currentPointIndex)
        {
            if (Path[currentPointIndex].IndexInPath != currentPointIndex)
                throw new Exception("Path has been altered and the indices do not match up");
            if (Path.Count == currentPointIndex + 1)
            {
                return 0;
            }
            return currentPointIndex + 1;
        }

        /// <summary>
        /// Gets the next line in the path, given the current line index. 
        /// This function automatically wraps back to the first line.
        /// </summary>
        /// <param name="currentLineIndex"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public Line NextLine(int currentLineIndex)
        {
            return PathLines[NextLineIndex(currentLineIndex)];
        }

        /// <summary>
        /// Gets the index of the next line in the path, given the current line index. 
        /// This function automatically wraps back to index 0.
        /// </summary>
        /// <param name="currentLineIndex"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public int NextLineIndex(int currentLineIndex)
        {
            if (PathLines[currentLineIndex].IndexInPath != currentLineIndex)
                throw new Exception("Path has been altered and the indices do not match up");
            if (PathLines.Count == currentLineIndex + 1)
            {
                return 0;
            }
            return currentLineIndex + 1;
        }

        public bool IsConvex()
        {
            if (!Area.IsGreaterThanNonNegligible()) return false; //It must have an area greater than zero
            if (PathLines == null) SetPathLines();
            var firstLine = PathLines.Last();
            foreach (var secondLine in PathLines)
            {
                var cross = firstLine.dX * secondLine.dY - firstLine.dY * secondLine.dX;
                if (secondLine.Length.IsNegligible(0.0000001)) continue;// without updating the first line             
                if (cross < 0)
                {
                    return false;
                }
                firstLine = secondLine;
            }
            return true;
        }
    }
}


