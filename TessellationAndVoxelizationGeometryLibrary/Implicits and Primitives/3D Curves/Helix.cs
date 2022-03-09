using MIConvexHull;
using System;
using System.Collections.Generic;
using TVGL.Numerics;

namespace TVGL.Primitives
{
    public readonly struct Helix : ICurve
    {

        public readonly double Radius;

        public readonly double Pitch;

        public readonly Vector3 Anchor;

        public readonly Vector3 Direction;

        public Helix(Vector3 anchor, Vector3 direction, double radius, double pitch)
        {
            Anchor = anchor;
            Direction = direction;
            Radius = radius;
            Pitch = pitch;
        }

        public static double DeterminePitch(StraightLine2D line, Cylinder cyl)
        {
            return Constants.TwoPi * cyl.Radius * line.Direction.Y / line.Direction.X;
        }

        public static bool CreateFromPoints<T>(IEnumerable<T> points, out ICurve curve, out double error) where T : IVertex2D
        {
            throw new NotImplementedException();
        }

        public double SquaredErrorOfNewPoint<T>(T point) where T : IVertex2D
        {
            throw new NotImplementedException();
        }
    }
}
