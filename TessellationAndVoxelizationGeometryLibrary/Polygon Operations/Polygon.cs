using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using TVGL.Numerics;

using TVGL.IOFunctions;

namespace TVGL.TwoDimensional
{
    //[KnownType(typeof(List<Vector2>))]
    //public class > : List<Vector2>
    //{
    //    /// <summary>
    //    /// Gets the Vector2s that make up the polygon
    //    /// </summary>
    //    [JsonIgnore]
    //    public readonly List<Vector2> Path;

    //    /// <summary>
    //    /// Gets the area of the polygon. Negative Area for holes.
    //    /// </summary>
    //    public readonly double Area;

    //    /// <summary>
    //    /// Maximum X value
    //    /// </summary>
    //    public readonly double MaxX;

    //    /// <summary>
    //    /// Minimum X value
    //    /// </summary>
    //    public readonly double MinX;

    //    /// <summary>
    //    /// Maximum Y value
    //    /// </summary>
    //    public readonly double MaxY;

    //    /// <summary>
    //    /// Minimum Y value
    //    /// </summary>
    //    public readonly double MinY;

    //    public >(Polygon polygon)
    //    {
    //        Area = polygon.Area;
    //        Path = new List<Vector2>();
    //        foreach (var point in polygon.Path)
    //            Path.Add(point);

    //        MaxX = polygon.MaxX;
    //        MaxY = polygon.MaxY;
    //        MinX = polygon.MinX;
    //        MinY = polygon.MinY;
    //    }

    //    public >(IEnumerable<Vector2> points)
    //    {
    //        Path = new List<Vector2>(points);
    //        Area = MiscFunctions.AreaOfPolygon(Path);
    //        MaxX = double.MinValue;
    //        MinX = double.MaxValue;
    //        MaxY = double.MinValue;
    //        MinY = double.MaxValue;
    //        foreach (var point in Path)
    //        {
    //            if (point.X > MaxX) MaxX = point.X;
    //            if (point.X < MinX) MinX = point.X;
    //            if (point.Y > MaxY) MaxY = point.Y;
    //            if (point.Y < MinY) MinY = point.Y;
    //        }
    //    }

    //    public static > Reverse(> original)
    //    {
    //        var path = new List<Vector2>(original.Path);
    //        path.Reverse();
    //        var newPoly = new >(path);
    //        return newPoly;
    //    }

    //    public double Length => MiscFunctions.Perimeter(Path);

    //    public bool IsPositive => Area >= 0;

    //    public void Serialize(string filename)
    //    {
    //        using (var writer = new FileStream(filename, FileMode.Create, FileAccess.Write))
    //        {
    //            var ser = new DataContractSerializer(typeof(>));
    //            ser.WriteObject(writer, this);
    //        }
    //    }

    //    public static > Deserialize(string filename)
    //    {
    //        using (var reader = new FileStream(filename, FileMode.Open, FileAccess.Read))
    //        {
    //            var ser = new DataContractSerializer(typeof(>));
    //            return (>)ser.ReadObject(reader);
    //        }
    //    }

    //    internal IEnumerable<double> ConvertToDoublesArray()
    //    {
    //        return Path.SelectMany(p => new[] { p.X, p.Y });
    //    }

    //    internal static > MakeFromBinaryString(double[] coordinates)
    //    {
    //        var points = new List<Vector2>();
    //        for (int i = 0; i < coordinates.Length; i += 2)
    //            points.Add(new Vector2(coordinates[i], coordinates[i + 1]));
    //        return new >(points);
    //    }
    //}

    /// <summary>
    /// A list of 2D points
    /// </summary>
    public class Polygon
    {
        /// <summary>
        /// The list of 2D points that make up a polygon.
        /// </summary>
        public List<Vector2> Path;
        public List<Node> Points;

        /// <summary>
        /// The list of 2D points that make up a polygon.
        /// </summary>
        //public > Light => new >(this);

        /// <summary>
        /// The list of lines that make up a polygon. This is not set by default.
        /// </summary>
        public List<PolygonSegment> Lines
        {
            get
            {
                if (_lines == null)
                {
                    _lines = new List<PolygonSegment>();
                    var n = Path.Count - 1;
                    for (var i = 0; i < n; i++)
                        Lines.Add(new PolygonSegment(Points[i], Points[i + 1]));
                    Lines.Add(new PolygonSegment(Points[n], Points[0]));
                }
                return _lines;
            }
        }
        List<PolygonSegment> _lines;

        /// <summary>
        /// A list of the polygons inside this polygon.
        /// </summary>
        public List<Polygon> Children;

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
            Parent = null;
            Children = new List<Polygon>();
        }

        /// <summary>
        /// Sets a polygon to counter clock wise positive
        /// </summary>
        private void SetToCCWPositive()
        {
            //Check if already positive ccw.
            if (Area >= 0) return;

            //It is negative. Reverse the path and path lines.
            Path.Reverse();
            Area = -Area;

            //Only reverse the lines if they have been generated
            if (_lines == null) return;
            var lines = _lines;
            _lines = new List<PolygonSegment>();
            var n = lines.Count;
            for (var i = 0; i < n; i++)
                _lines[i] = lines[n - i - 1].Reverse();
        }

        private void SetToCWNegative()
        {
            //Check if already negative cw.
            if (Area < 0) return;

            //It is positive. Reverse the path and path lines.
            Path.Reverse();
            Area = -Area;

            //Only reverse the lines if they have been generated
            if (_lines == null) return;
            var lines = _lines;
            _lines = new List<PolygonSegment>();
            var n = lines.Count;
            for (var i = 0; i < n; i++)
                _lines[i] = lines[n - i - 1].Reverse();
        }

        private double CalculateArea()
        {
            return Path.Area();
        }


        public bool IsConvex()
        {
            if (!Area.IsGreaterThanNonNegligible()) return false; //It must have an area greater than zero
            var firstLine = Lines.Last();
            foreach (var secondLine in Lines)
            {
                var cross = firstLine.Vector.Cross(secondLine.Vector);
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


