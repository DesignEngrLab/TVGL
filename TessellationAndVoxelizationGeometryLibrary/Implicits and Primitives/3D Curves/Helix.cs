using MIConvexHull;
using System;
using System.Collections.Generic;
using System.Linq;



namespace TVGL
{
    public readonly struct Helix : ICurve
    {

        public readonly double Radius;

        public readonly double Pitch;
        public readonly double NumThreads;
        public readonly bool RightHandedChirality;
        public readonly Vector3 Anchor;

        public readonly Vector3 Direction;

        public Helix(Vector3 anchor, Vector3 direction, double radius, double pitch, double numThreads, bool rightHanded)
        {
            Anchor = anchor;
            Direction = direction;
            Radius = radius;
            Pitch = pitch;
            NumThreads = numThreads;
            RightHandedChirality = rightHanded;
        }

        public static double DeterminePitch(StraightLine2D line, Cylinder cyl)
        {
            return Constants.TwoPi * cyl.Radius * line.Direction.Y / line.Direction.X;
        }

        public static double DetermineNumThreads(IEnumerable<Vector2> path, Cylinder cyl)
        {
            var start = path.First();
            var end = path.Last();
            /// because we already know it to be a straightline, we can just look at the ends
            return Math.Abs(start.X - end.X) / (Constants.TwoPi * cyl.Radius);
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
