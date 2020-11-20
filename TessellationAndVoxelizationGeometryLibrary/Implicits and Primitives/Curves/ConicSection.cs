using System;
using System.Collections.Generic;
using System.Text;
using TVGL.Numerics;
using TVGL.TwoDimensional;

namespace TVGL.Curves
{
    public enum ConicSectionType
    {
        StraightLine,
        Circle,
        Ellipse,
        Parabola,
        Hyperbola
    }

    /// <summary>
    ///  <para>Class ConicSection
    ///   A curve defined by the quadratic equation:
    ///   Ax^2 + Bxy + Cy^2 + Dx + Ey - 1 = 0 
    ///</summary>
    public class ConicSection
    {
        public ConicSectionType ConicType;
        public Plane Plane;
        public List<Vector2> Points;
        public List<(Edge, bool)> EdgesAndDirection;
        public double A;
        public double B;
        public double C;
        public double D;
        public double E;
        // F, the constant is at -1, or +1 when moved to the other side of the equation
        public bool ConstantIsZero;

        public Matrix4x4 Transform { get; private set; }

        internal static ConicSection DefineForLine(Vector3 coordinates, Vector3 lineDir)
        {
            var planeDir = (lineDir.X <= lineDir.Y && lineDir.X <= lineDir.Z) ? Vector3.UnitX :
                (lineDir.Y <= lineDir.X && lineDir.Y <= lineDir.Z) ? Vector3.UnitY : Vector3.UnitZ;
            var plane = new Plane(coordinates, planeDir);
            var anchor = coordinates.ConvertTo2DCoordinates(plane.AsTransformToXYPlane);
            var dir2D = lineDir.ConvertTo2DCoordinates(plane.AsTransformToXYPlane);
            var denom = anchor.X * dir2D.Y - anchor.Y * dir2D.X;
            if (denom.IsNegligible()) // then line goes through the origin, and we need to set the "F" to zero (ConstantIsZero)
                return new ConicSection
                {
                    ConicType = ConicSectionType.StraightLine,
                    A = 0,
                    B = 0,
                    C = 0,
                    D = dir2D.Y,
                    E = -dir2D.X,
                    ConstantIsZero = true,
                    Plane = plane
                };
            return new ConicSection
            {
                ConicType = ConicSectionType.StraightLine,
                A = 0,
                B = 0,
                C = 0,
                D = dir2D.Y / denom,
                E = -dir2D.X / denom,
                Plane = plane
            };
        }

        internal static ConicSection DefineForCircle(Plane plane, Circle2D circle)
        {
            var denom = circle.RadiusSquared - circle.Center.LengthSquared();
            if (denom.IsNegligible())
            {
                return new ConicSection
                {
                    ConicType = ConicSectionType.Circle,
                    A = 1,
                    B = 0,
                    C = 1,
                    D = -2 * circle.Center.X,
                    E = -2 * circle.Center.Y,
                    Plane = plane,
                    ConstantIsZero = true
                };
            }
            var oneOverDenom = 1 / denom;
            return new ConicSection
            {
                ConicType = ConicSectionType.Circle,
                A = oneOverDenom,
                B = 0,
                C = oneOverDenom,
                D = -2 * circle.Center.X * oneOverDenom,
                E = -2 * circle.Center.Y * oneOverDenom,
                Plane = plane
            };
        }

        internal void AddEnd(Edge edge, bool correctDirection)
        {
            EdgesAndDirection.Add((edge, correctDirection));
            if (Points.Count == 0)
            {
                throw new Exception();
                if (correctDirection) Points.Add(edge.From.ConvertTo2DCoordinates(Plane.AsTransformToXYPlane));
                else Points.Add(edge.To.ConvertTo2DCoordinates(Plane.AsTransformToXYPlane));
            }
            if (correctDirection) Points.Add(edge.To.ConvertTo2DCoordinates(Plane.AsTransformToXYPlane));
            else Points.Add(edge.From.ConvertTo2DCoordinates(Plane.AsTransformToXYPlane));
        }

        internal void AddStart(Edge edge, bool correctDirection)
        {
            EdgesAndDirection.Insert(0, (edge, correctDirection));
            if (correctDirection) Points.Add(edge.From.ConvertTo2DCoordinates(Plane.AsTransformToXYPlane));
            else Points.Add(edge.To.ConvertTo2DCoordinates(Plane.AsTransformToXYPlane));
        }


        private ConicSection()
        {
            Points = new List<Vector2>();
            EdgesAndDirection = new List<(Edge, bool)>();
        }

        internal double CalcError(Vector3 point)
        {
            return CalcError(point.ConvertTo2DCoordinates(Plane.AsTransformToXYPlane));
        }
        internal double CalcError(Vector2 point)
        {
            var x = point.X;
            var y = point.Y;
            if (ConstantIsZero)
                return Math.Abs(A * x * x + B * x * y + C * y * y + D * x + E * y);
            return Math.Abs(A * x * x + B * x * y + C * y * y + D * x + E * y - 1);
        }

        internal bool Upgrade(Vector3 newPoint, double tolerance)
        {
            return false;
            throw new NotImplementedException();
            // in the future - see if upgrading from straight to circle
            // then circle to parabola
            // or ellipse and hyperbola
            // make the new fit better.
        }
    }
}
