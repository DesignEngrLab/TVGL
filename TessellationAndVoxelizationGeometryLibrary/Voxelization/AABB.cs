using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace TVGL.Voxelization
{
    [DebuggerDisplay("({MinX} {MinY} {MinZ}) ({MaxX} {MaxY} {MaxZ})")]
    public class AABB
    {
        public double MinX = double.MaxValue;
        public double MaxX = double.MinValue;

        public double MinY = double.MaxValue;
        public double MaxY = double.MinValue;

        public double MinZ = double.MaxValue;
        public double MaxZ = double.MinValue;

        public AABB()
        {
        }

        public AABB(IEnumerable<PolygonalFace> triangles)
        {
            var box = new AABB();
            foreach (var t in triangles)
            {
                box.Add(t.Vertices[0]);
                box.Add(t.Vertices[1]);
                box.Add(t.Vertices[2]);
            }
        }

        public AABB(double minX, double minY, double minZ, double maxX, double maxY, double maxZ)
        {
            MinX = minX;
            MaxX = maxX;

            MinY = minY;
            MaxY = maxY;

            MinZ = minZ;
            MaxZ = maxZ;
        }

        public void Clear()
        {
            MinX = double.MaxValue;
            MaxX = double.MinValue;

            MinY = double.MaxValue;
            MaxY = double.MinValue;

            MinZ = double.MaxValue;
            MaxZ = double.MinValue;
        }

        public void Add(Vertex point)
        {
            MinX = Math.Min(MinX, point.X);
            MaxX = Math.Max(MaxX, point.X);

            MinY = Math.Min(MinY, point.Y);
            MaxY = Math.Max(MaxY, point.Y);

            MinZ = Math.Min(MinZ, point.Z);
            MaxZ = Math.Max(MaxZ, point.Z);
        }

        public bool Contains(Vertex point)
        {
            return (this.MaxX >= point.X) && (this.MinX <= point.X) &&
                   (this.MaxY >= point.Y) && (this.MinY <= point.Y) &&
                   (this.MaxZ >= point.Z) && (this.MinZ <= point.Z);
        }

        public bool Intersects(AABB other)
        {
            return
                (MaxX > other.MinX && MaxX < other.MaxX) ||
                (MinX > other.MinX && MinX < other.MaxX) ||
                (MaxY > other.MinY && MaxY < other.MaxY) ||
                (MinY > other.MinY && MinY < other.MaxY) ||
                (MaxZ > other.MinZ && MaxZ < other.MaxZ) ||
                (MinZ > other.MinZ && MinZ < other.MaxZ);
        }

        public bool IsOutside(AABB other)
        {
            return (MaxX - other.MinX) < 0 || (MinX - other.MaxX) > 0 ||
                   (MaxY - other.MinY) < 0 || (MinY - other.MaxY) > 0 ||
                   (MaxZ - other.MinZ) < 0 || (MinZ - other.MaxZ) > 0;
        }

        public AABB Clone()
        {
            AABB clone = new AABB();
            clone.MaxX = this.MaxX;
            clone.MaxY = this.MaxY;
            clone.MaxZ = this.MaxZ;
            clone.MinX = this.MinX;
            clone.MinY = this.MinY;
            clone.MinZ = this.MinZ;

            return clone;
        }

        /// <summary>
        /// Finds the closest point on the box, if the point is inside the box, returns the point.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public Vertex ClosestPoint(Vertex point)
        {
            var closestPoint = new Vertex(new[]
            {
                (point.X < MinX) ? MinX : (point.X > MaxX) ? MaxX : point.X,
                (point.Y < MinY) ? MinY : (point.Y > MaxY) ? MaxY : point.Y,
                (point.Z < MinZ) ? MinZ : (point.Z > MaxZ) ? MaxZ : point.Z
            });

            return closestPoint;
        }

        public Vertex ClosestPointOnSurface(Vertex point)
        {
            double minXDist = Math.Min(Math.Abs(point.X - MinX), Math.Abs(point.X - MaxX));
            double minYDist = Math.Min(Math.Abs(point.Y - MinY), Math.Abs(point.Y - MaxY));
            double minZDist = Math.Min(Math.Abs(point.Z - MinZ), Math.Abs(point.Z - MaxZ));
            if (minXDist <= minYDist && minXDist <= minZDist)
            {
                var closestPoint = new Vertex(new[]
                {
                    (point.X < MinX) ? MinX: (point.X > MaxX) ? MaxX :  
                        (Math.Abs(point.X - MinX) < Math.Abs(point.X - MaxX) ? MinX : MaxX),
                    (point.Y < MinY) ? MinY : (point.Y > MaxY) ? MaxY : point.Y,
                    (point.Z < MinZ) ? MinZ : (point.Z > MaxZ) ? MaxZ : point.Z
                });
                return closestPoint;
            }
            else if (minYDist <= minXDist && minYDist <= minZDist)
            {
                var closestPoint = new Vertex(new[]
                {
                    (point.X < MinX) ? MinX : (point.X > MaxX) ? MaxX : point.X,
                    (point.Y < MinY) ? MinY : (point.Y > MaxY) ? MaxY : 
                        (Math.Abs(point.Y - MinY) < Math.Abs(point.Y - MaxY) ? MinY : MaxY),
                    (point.Z < MinZ) ? MinZ : (point.Z > MaxZ) ? MaxZ : point.Z
                });
                return closestPoint;
            }
            else // if (minZDist <= minXDist && minZDist <= minYDist)
            {
                var closestPoint = new Vertex(new[]
                {
                    (point.X < MinX) ? MinX : (point.X > MaxX) ? MaxX : point.X,
                    (point.Y < MinY) ? MinY : (point.Y > MaxY) ? MaxY : point.Y,
                    (point.Z < MinZ) ? MinZ : (point.Z > MaxZ) ? MaxZ : 
                        (Math.Abs(point.Z - MinZ) < Math.Abs(point.Z - MaxZ) ? MinZ : MaxZ)
                });
                return closestPoint;
            }
        }
    }

}
