using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;


// Integer AABB to avoid needing to do epsilon calculations when comparing them
namespace TVGL.Voxelization
{
    public class AABBi
    {
        public int MinX = int.MaxValue;
        public int MaxX = int.MinValue;

        public int MinY = int.MaxValue;
        public int MaxY = int.MinValue;

        public int MinZ = int.MaxValue;
        public int MaxZ = int.MinValue;

        public int Width { get { return MaxX - MinX; } }
        public int Height { get { return MaxY - MinY; } }
        public int Depth { get { return MaxZ - MinZ; } }
        public int X { get { return (MaxX + MinX) / 2; } }
        public int Y { get { return (MaxY + MinY) / 2; } }
        public int Z { get { return (MaxZ + MinZ) / 2; } }

        public AABBi() { }

        public AABBi(int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
        {
            MinX = minX;
            MaxX = maxX;

            MinY = minY;
            MaxY = maxY;

            MinZ = minZ;
            MaxZ = maxZ;
        }

        public AABBi(float minX, float minY, float minZ, float maxX, float maxY, float maxZ)
        {
            Add(minX, minY, minZ);
            Add(maxX, maxY, maxZ);
        }

        public AABBi(AABBi other)
        {
            Clear();
            Set(other);
        }


        public bool IsEmpty()
        {
            return (MinX >= MaxX ||
                    MinY >= MaxY ||
                    MinZ >= MaxZ);
        }

        public void Clear()
        {
            MinX = int.MaxValue;
            MaxX = int.MinValue;

            MinY = int.MaxValue;
            MaxY = int.MinValue;

            MinZ = int.MaxValue;
            MaxZ = int.MinValue;
        }

        public void Add(Vertex inCoordinate)
        {
            Add(inCoordinate.X, inCoordinate.Y, inCoordinate.Z);
        }

        public void Add(int inX, int inY, int inZ)
        {
            MinX = Math.Min(MinX, inX);
            MinY = Math.Min(MinY, inY);
            MinZ = Math.Min(MinZ, inZ);

            MaxX = Math.Max(MaxX, inX);
            MaxY = Math.Max(MaxY, inY);
            MaxZ = Math.Max(MaxZ, inZ);
        }

        public void Add(double inX, double inY, double inZ)
        {
            Debug.Assert(!double.IsInfinity(inX) && !double.IsNaN(inX));
            Debug.Assert(!double.IsInfinity(inY) && !double.IsNaN(inY));
            Debug.Assert(!double.IsInfinity(inZ) && !double.IsNaN(inZ));

            MinX = Math.Min(MinX, (int)Math.Floor(inX));
            MinY = Math.Min(MinY, (int)Math.Floor(inY));
            MinZ = Math.Min(MinZ, (int)Math.Floor(inZ));

            MaxX = Math.Max(MaxX, (int)Math.Ceiling(inX));
            MaxY = Math.Max(MaxY, (int)Math.Ceiling(inY));
            MaxZ = Math.Max(MaxZ, (int)Math.Ceiling(inZ));
        }

        public void Add(AABBi bounds)
        {
            MinX = Math.Min(MinX, bounds.MinX);
            MinY = Math.Min(MinY, bounds.MinY);
            MinZ = Math.Min(MinZ, bounds.MinZ);

            MaxX = Math.Max(MaxX, bounds.MaxX);
            MaxY = Math.Max(MaxY, bounds.MaxY);
            MaxZ = Math.Max(MaxZ, bounds.MaxZ);
        }

        public void Set(AABBi bounds)
        {
            this.MinX = bounds.MinX;
            this.MinY = bounds.MinY;
            this.MinZ = bounds.MinZ;

            this.MaxX = bounds.MaxX;
            this.MaxY = bounds.MaxY;
            this.MaxZ = bounds.MaxZ;
        }

        public void Translate(int X, int Y, int Z)
        {
            this.MinX = this.MinX + X;
            this.MinY = this.MinY + Y;
            this.MinZ = this.MinZ + Z;

            this.MaxX = this.MaxX + X;
            this.MaxY = this.MaxY + Y;
            this.MaxZ = this.MaxZ + Z;
        }

        public void Translate(Vertex translation)
        {
            Set(this, translation);
        }

        public AABBi Translated(int[] translation)
        {
            return new AABBi(this.MinX + translation[0],
                            this.MinY + translation[1],
                            this.MinZ + translation[2],

                            this.MaxX + translation[0],
                            this.MaxY + translation[1],
                            this.MaxZ + translation[2]);

        }

        public void Set(AABBi from, Vertex translation)
        {
            MinX = (int)Math.Floor(from.MinX + translation.X);
            MinY = (int)Math.Floor(from.MinY + translation.Y);
            MinZ = (int)Math.Floor(from.MinZ + translation.Z);

            MaxX = (int)Math.Ceiling(from.MaxX + translation.X);
            MaxY = (int)Math.Ceiling(from.MaxY + translation.Y);
            MaxZ = (int)Math.Ceiling(from.MaxZ + translation.Z);
        }

        public bool IsOutside(AABBi other)
        {
            return (this.MaxX - other.MinX) < 0 || (this.MinX - other.MaxX) > 0 ||
                    (this.MaxY - other.MinY) < 0 || (this.MinY - other.MaxY) > 0 ||
                    (this.MaxZ - other.MinZ) < 0 || (this.MinZ - other.MaxZ) > 0;
        }

        public static bool IsOutside(AABBi left, AABBi right)
        {
            return (left.MaxX - right.MinX) < 0 || (left.MinX - right.MaxX) > 0 ||
                    (left.MaxY - right.MinY) < 0 || (left.MinY - right.MaxY) > 0 ||
                    (left.MaxZ - right.MinZ) < 0 || (left.MinZ - right.MaxZ) > 0;
        }

        public static bool IsOutside(AABBi left, Vertex translation, AABBi right)
        {
            return ((left.MaxX + translation.X) - right.MinX) < 0 || ((left.MinX + translation.X) - right.MaxX) > 0 ||
                    ((left.MaxY + translation.Y) - right.MinY) < 0 || ((left.MinY + translation.Y) - right.MaxY) > 0 ||
                    ((left.MaxZ + translation.Z) - right.MinZ) < 0 || ((left.MinZ + translation.Z) - right.MaxZ) > 0;
        }

        public bool Contains(Vertex point)
        {
            return (this.MaxX >= point.X) && (this.MinX <= point.X) &&
                   (this.MaxY >= point.Y) && (this.MinY <= point.Y) &&
                   (this.MaxZ >= point.Z) && (this.MinZ <= point.Z);
        }

        public bool Intersects(AABBi other)
        {
            return
                (MaxX > other.MinX && MaxX < other.MaxX) ||
                (MinX > other.MinX && MinX < other.MaxX) ||
                (MaxY > other.MinY && MaxY < other.MaxY) ||
                (MinY > other.MinY && MinY < other.MaxY) ||
                (MaxZ > other.MinZ && MaxZ < other.MaxZ) ||
                (MinZ > other.MinZ && MinZ < other.MaxZ);
        }

        public AABBi Clone()
        {
            AABBi clone = new AABBi();
            clone.MaxX = this.MaxX;
            clone.MaxY = this.MaxY;
            clone.MaxZ = this.MaxZ;
            clone.MinX = this.MinX;
            clone.MinY = this.MinY;
            clone.MinZ = this.MinZ;

            return clone;
        }

        public void Clone(AABBi clone)
        {
            clone.MaxX = this.MaxX;
            clone.MaxY = this.MaxY;
            clone.MaxZ = this.MaxZ;
            clone.MinX = this.MinX;
            clone.MinY = this.MinY;
            clone.MinZ = this.MinZ;
        }
    }
}
