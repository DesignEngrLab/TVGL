///
/// Adopted from Oxel, http://www.nickdarnell.com/oxel/ 
/// https://bitbucket.org/NickDarnell/oxel
/// 
/// by Brandon Massoni. 8.29.2017

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using StarMathLib;
using TVGL;

namespace TVGL.Voxelization
{
    /// <summary>
    /// Voxel Cell Status Enum Values
    /// </summary>
    public enum CellStatus
    {
        Inside,
        Outside,
        Intersecting,
        IntersectingBounds,
        Unknown
    }

    [DebuggerDisplay("Cell ({Children.Count}, {Status})")]
    public class VoxelizingOctreeCell
    {
        public VoxelizingOctree Tree;
        public List<VoxelizingOctreeCell> Children = new List<VoxelizingOctreeCell>();
        public List<PolygonalFace> Triangles = new List<PolygonalFace>();
        public AABB Bounds;
        public AABBi VoxelBounds;
        public Vertex Center;
        public double Length;
        public VoxelizingOctreeCell Root;
        public VoxelizingOctreeCell Parent;
        public CellStatus Status = CellStatus.Unknown;
        public int[] CellStatusAccumulation = new int[2];
        public int Level;

        public VoxelizingOctreeCell(VoxelizingOctree tree, VoxelizingOctreeCell root, Vertex center, double length, int level)
        {
            Tree = tree;
            Root = root;
            Center = center;
            Length = length;
            Level = level;

            var halfLength = length / 2.0f;

            Bounds = new AABB
            {
                MinX = center.X - halfLength,
                MinY = center.Y - halfLength,
                MinZ = center.Z - halfLength,
                MaxX = center.X + halfLength,
                MaxY = center.Y + halfLength,
                MaxZ = center.Z + halfLength
            };

            VoxelBounds = new AABBi(
                (int)Math.Round((Bounds.MinX - tree.VoxelBounds.MinX) / tree.SmallestVoxelSideLength, MidpointRounding.AwayFromZero),
                (int)Math.Round((Bounds.MinY - tree.VoxelBounds.MinY) / tree.SmallestVoxelSideLength, MidpointRounding.AwayFromZero),
                (int)Math.Round((Bounds.MinZ - tree.VoxelBounds.MinZ) / tree.SmallestVoxelSideLength, MidpointRounding.AwayFromZero),
                (int)Math.Round((Bounds.MaxX - tree.VoxelBounds.MinX) / tree.SmallestVoxelSideLength, MidpointRounding.AwayFromZero),
                (int)Math.Round((Bounds.MaxY - tree.VoxelBounds.MinY) / tree.SmallestVoxelSideLength, MidpointRounding.AwayFromZero),
                (int)Math.Round((Bounds.MaxZ - tree.VoxelBounds.MinZ) / tree.SmallestVoxelSideLength, MidpointRounding.AwayFromZero));
        }

        public bool IsLeaf
        {
            get { return Children.Count == 0; }
        }

        public bool AccumulateStatus(CellStatus status)
        {
            Debug.Assert(status == CellStatus.Inside || status == CellStatus.Outside);

            CellStatusAccumulation[(int)status]++;

            if (CellStatusAccumulation[(int)status] >= Tree.CellStatusAccumulationConfirmationThreshold)
            {
                this.Status = status;
                return true;
            }

            return false;
        }

        public bool Contains(ref PolygonalFace triangle)
        {
            return Bounds.Contains(triangle.Vertices[0]) && Bounds.Contains(triangle.Vertices[1]) && Bounds.Contains(triangle.Vertices[2]);
        }

        public bool Intersects(ref PolygonalFace triangle)
        {
            var boxhalfsize = new [] {Length / 2.0f, Length / 2.0f, Length / 2.0f};
            var boxcenter = new [] {Bounds.MinX + boxhalfsize[0], Bounds.MinY + boxhalfsize[1], Bounds.MinZ + boxhalfsize[2]};

            //return VoxelizerCPU.IsTriangleCollidingWithVoxel(ref triangle, ref deltap, ref minpt);
            return VoxelizingOctree.TriBoxOverlap(ref boxcenter, ref boxhalfsize, ref triangle);
        }

        public bool IntersectsMeshBounds()
        {
            return Tree.MeshBounds.Intersects(Bounds);
        }

        public bool IsOutsideMeshBounds()
        {
            return Tree.MeshBounds.IsOutside(Bounds);
        }

        public void EncloseTriangles(VoxelizingOctreeCell parent)
        {
            for (int i = 0; i < parent.Triangles.Count; i++)
            {
                PolygonalFace t = parent.Triangles[i];
                if (Contains(ref t))
                {
                    Triangles.Add(t);
                    Parent.Triangles.RemoveAt(i);
                    i--;
                }
            }

            if (parent.IsIntersecting)
            {
                TestTriangleIntersection();
            }
        }

        public void TestTriangleIntersection()
        {
            VoxelizingOctreeCell p = this;

            while (p != null)
            {
                for (int i = 0; i < p.Triangles.Count; i++)
                {
                    PolygonalFace t = p.Triangles[i];
                    if (Intersects(ref t))
                    {
                        Status = CellStatus.Intersecting;
                        return;
                    }
                }

                p = p.Parent;
            }

            // If we're not intersecting any triangles make sure we're also not intersecting the mesh bounds.
            if (IntersectsMeshBounds())
            {
                Status = CellStatus.IntersectingBounds;
            }
        }

        public void RecursiveSubdivide(int level)
        {
            if (level <= 0)
                return;

            if (Subdivide())
            {
                for (int i = 0; i < Children.Count; i++)
                {
                    Children[i].RecursiveSubdivide(level - 1);
                }
            }
        }

        public bool Subdivide()
        {
            double quarterLength = Length / 4.0f;

            int stop = 8;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        var centerPosition =
                            Center.Position.add(new double[] {x*quarterLength, y*quarterLength, z*quarterLength});
                        var newCell = new VoxelizingOctreeCell(Tree, Root,
                            new Vertex(centerPosition),
                            quarterLength*2.0f,
                            Level + 1
                            ) {Parent = this};

                        newCell.EncloseTriangles(this);

                        if (newCell.IsOutsideMeshBounds())
                            newCell.Status = CellStatus.Outside;

                        if (!newCell.IsIntersecting)
                            stop--;

                        Children.Add(newCell);
                    }
                }
            }

            if (stop == 0)
            {
                //Debug.Assert(!IsIntersecting);
                if (IsIntersecting)
                {
                    //Debugger.Break();
                }

                Children.Clear();
            }

            return stop != 0;
        }
                
        //public void Draw(int level, CellStatus status, Vertex color, double width)
        //{
        //    if (status == Status && level == 0)
        //    {
        //        GL.LineWidth(width);
        //        GL.Color4(color);

        //        GL.Begin(BeginMode.Lines);
        //        // Top
        //        GL.Vertex3(Bounds.MaxX, Bounds.MaxY, Bounds.MaxZ);
        //        GL.Vertex3(Bounds.MaxX, Bounds.MaxY, Bounds.MinZ);
        //        GL.Vertex3(Bounds.MaxX, Bounds.MaxY, Bounds.MinZ);
        //        GL.Vertex3(Bounds.MinX, Bounds.MaxY, Bounds.MinZ);
        //        GL.Vertex3(Bounds.MinX, Bounds.MaxY, Bounds.MinZ);
        //        GL.Vertex3(Bounds.MinX, Bounds.MaxY, Bounds.MaxZ);
        //        GL.Vertex3(Bounds.MinX, Bounds.MaxY, Bounds.MaxZ);
        //        GL.Vertex3(Bounds.MaxX, Bounds.MaxY, Bounds.MaxZ);
        //        // Bottom
        //        GL.Vertex3(Bounds.MaxX, Bounds.MinY, Bounds.MaxZ);
        //        GL.Vertex3(Bounds.MaxX, Bounds.MinY, Bounds.MinZ);
        //        GL.Vertex3(Bounds.MaxX, Bounds.MinY, Bounds.MinZ);
        //        GL.Vertex3(Bounds.MinX, Bounds.MinY, Bounds.MinZ);
        //        GL.Vertex3(Bounds.MinX, Bounds.MinY, Bounds.MinZ);
        //        GL.Vertex3(Bounds.MinX, Bounds.MinY, Bounds.MaxZ);
        //        GL.Vertex3(Bounds.MinX, Bounds.MinY, Bounds.MaxZ);
        //        GL.Vertex3(Bounds.MaxX, Bounds.MinY, Bounds.MaxZ);
        //        // Sides
        //        GL.Vertex3(Bounds.MaxX, Bounds.MaxY, Bounds.MaxZ);
        //        GL.Vertex3(Bounds.MaxX, Bounds.MinY, Bounds.MaxZ);
        //        GL.Vertex3(Bounds.MinX, Bounds.MaxY, Bounds.MaxZ);
        //        GL.Vertex3(Bounds.MinX, Bounds.MinY, Bounds.MaxZ);
        //        GL.Vertex3(Bounds.MinX, Bounds.MaxY, Bounds.MinZ);
        //        GL.Vertex3(Bounds.MinX, Bounds.MinY, Bounds.MinZ);
        //        GL.Vertex3(Bounds.MaxX, Bounds.MaxY, Bounds.MinZ);
        //        GL.Vertex3(Bounds.MaxX, Bounds.MinY, Bounds.MinZ);
        //        GL.End();
        //    }

        //    foreach (var cell in Children)
        //    {
        //        cell.Draw(level - 1, status, color, width);
        //    }
        //}

        public void Find(List<VoxelizingOctreeCell> cellList, CellStatus status)
        {
            if (status == Status)
            {
                cellList.Add(this);
            }

            foreach (var cell in Children)
            {
                cell.Find(cellList, status);
            }
        }

        public void AccumulateChildren(List<List<VoxelizingOctreeCell>> cellList, int level)
        {
            cellList[level].Add(this);

            foreach (var cell in Children)
            {
                cell.AccumulateChildren(cellList, level + 1);
            }
        }

        public bool IsIntersecting
        {
            get { return Status == CellStatus.Intersecting || Status == CellStatus.IntersectingBounds; }
        }
    }

    public class VoxelizingOctree
    {
        public readonly int CellStatusAccumulationConfirmationThreshold;

        private readonly int _maxLevels;
        private VoxelizingOctreeCell m_root;

        public AABB MeshBounds;
        public AABB VoxelBounds;
        public double SideLength;
        public int[] VoxelSize;
        public double[] WorldVoxelOffset;
        public double SmallestVoxelSideLength;

        public VoxelizingOctree(int maxLevels)
        {
            if (maxLevels < 1)
                throw new ArgumentException("maxLevels must be >= 1.");

            CellStatusAccumulationConfirmationThreshold = 1 << (maxLevels - 1);

            _maxLevels = maxLevels;
        }

        public VoxelizingOctreeCell Root
        {
            get { return m_root; }
        }

        public int MaxLevels
        {
            get { return _maxLevels; }
        }

        public void AccumulateChildren(out List<List<VoxelizingOctreeCell>> cellList)
        {
            cellList = new List<List<VoxelizingOctreeCell>>();
            for (int i = 0; i < MaxLevels; i++)
                cellList.Add(new List<VoxelizingOctreeCell>());

            Root.AccumulateChildren(cellList, 0);
        }

        public bool GenerateOctree(TessellatedSolid mesh)
        {
            if (mesh == null)
                return false;

            // Create a list of triangles from the list of faces in the model
            // We want these linked to the actual tesselated solid's faces, not copies.
            var triangles = new List<PolygonalFace>(mesh.Faces);

            // Determine the axis-aligned bounding box for the triangles
            Vertex center;
            CreateUniformBoundingBox(triangles, out MeshBounds, out VoxelBounds, out center, out SideLength);

            {
                SmallestVoxelSideLength = SideLength;
                for (int i = 1; i < _maxLevels; i++)
                    SmallestVoxelSideLength *= 0.5;

                VoxelSize = new[]
                {
                    (int)Math.Pow(2, _maxLevels),
                    (int)Math.Pow(2, _maxLevels),
                    (int)Math.Pow(2, _maxLevels)
                };
            }

            m_root = new VoxelizingOctreeCell(this, null, center, SideLength, 0);
            m_root.Root = m_root;
            m_root.Triangles = new List<PolygonalFace>(triangles);
            m_root.Status = CellStatus.IntersectingBounds;
            m_root.RecursiveSubdivide(_maxLevels - 1);

            WorldVoxelOffset = new double[]
            {
                0 - Root.VoxelBounds.MinX,
                0 - Root.VoxelBounds.MinY,
                0 - Root.VoxelBounds.MinZ
            };

            return true;
        }

        private static void CreateUniformBoundingBox(IEnumerable<PolygonalFace> triangles, 
            out AABB originalBounds, out AABB voxelBounds, out Vertex center, out double length)
        {
            originalBounds = new AABB(triangles);
            var size = new Vertex(new[] 
                {
                    originalBounds.MaxX - originalBounds.MinX,
                    originalBounds.MaxY - originalBounds.MinY,
                    originalBounds.MaxZ - originalBounds.MinZ
                });
            var maxSize = Math.Max(size.X, Math.Max(size.Y, size.Z));

            center = new Vertex(new[]
            {
                originalBounds.MinX + (size.X / 2.0f),
                originalBounds.MinY + (size.Y / 2.0f),
                originalBounds.MinZ + (size.Z / 2.0f)
            });

            length = maxSize;

            voxelBounds = new AABB
            {
                MinX = center.X - (length*0.5f),
                MinY = center.Y - (length*0.5f),
                MinZ = center.Z - (length*0.5f),
                MaxX = center.X + (length*0.5f),
                MaxY = center.Y + (length*0.5f),
                MaxZ = center.Z + (length*0.5f)
            };
        }

        //public void Draw(int level, CellStatus status, Vector4 color, double width)
        //{
        //    m_root.Draw(level, status, color, width);
        //}

        public List<VoxelizingOctreeCell> Find(CellStatus status)
        {
            List<VoxelizingOctreeCell> cellList = new List<VoxelizingOctreeCell>();
            m_root.Find(cellList, status);
            return cellList;
        }

        public static void FindMinMax(double x0, double x1, double x2, out double min, out double max)
        {
            min = max = x0;
            if (x1 < min) min = x1;
            if (x1 > max) max = x1;
            if (x2 < min) min = x2;
            if (x2 > max) max = x2;
        }

        public static bool PlaneBoxOverlap(double[] normal, double d, double[] maxbox)
        {
            var vmin = new Vertex(new[]
            {
                (normal[0] > 0.0f) ? -maxbox[0] : maxbox[0],
                (normal[1] > 0.0f) ? -maxbox[1] : maxbox[1],
                (normal[2] > 0.0f) ? -maxbox[2] : maxbox[2]
            });


            var vmax = new Vertex(new[]
            {
                (normal[0] > 0.0f) ? maxbox[0] : -maxbox[0],
                (normal[1] > 0.0f) ? maxbox[1] : -maxbox[1],
                (normal[2] > 0.0f) ? maxbox[2] : -maxbox[2]
            });

            if (vmin.Position.dotProduct(normal) + d > 0.0f) return false;
            if (vmax.Position.dotProduct(normal) + d >= 0.0f) return true;

            return false;
        }


        /*======================== X-tests ========================*/
        public static bool AXISTEST_X01(double a, double b, double fa, double fb, ref Vertex v0, ref Vertex v2, ref double[] boxhalfsize)
        {
            double min = 0, max = 0;
            double p0 = a * v0.Y - b * v0.Z;
            double p2 = a * v2.Y - b * v2.Z;
            if (p0 < p2) { min = p0; max = p2; } else { min = p2; max = p0; }
            double rad = fa * boxhalfsize[1] + fb * boxhalfsize[2];
            if (min > rad || max < -rad)
                return true;
            return false;
        }

        public static bool AXISTEST_X2(double a, double b, double fa, double fb, ref Vertex v0, ref Vertex v1, ref double[] boxhalfsize)
        {
            double min = 0, max = 0;
            double p0 = a * v0.Y - b * v0.Z;
            double p1 = a * v1.Y - b * v1.Z;
            if (p0 < p1) { min = p0; max = p1; } else { min = p1; max = p0; }
            double rad = fa * boxhalfsize[1] + fb * boxhalfsize[2];
            if (min > rad || max < -rad)
                return true;
            return false;
        }

        /*======================== Y-tests ========================*/
        public static bool AXISTEST_Y02(double a, double b, double fa, double fb, ref Vertex v0, ref Vertex v2, ref double[] boxhalfsize)
        {
            double min = 0, max = 0;
            double p0 = -a * v0.X + b * v0.Z;
            double p2 = -a * v2.X + b * v2.Z;
            if (p0 < p2) { min = p0; max = p2; } else { min = p2; max = p0; }
            double rad = fa * boxhalfsize[0] + fb * boxhalfsize[2];
            if (min > rad || max < -rad)
                return true;
            return false;
        }

        public static bool AXISTEST_Y1(double a, double b, double fa, double fb, ref Vertex v0, ref Vertex v1, ref double[] boxhalfsize)
        {
            double min = 0, max = 0;
            double p0 = -a * v0.X + b * v0.Z;
            double p1 = -a * v1.X + b * v1.Z;
            if (p0 < p1) { min = p0; max = p1; } else { min = p1; max = p0; }
            double rad = fa * boxhalfsize[0] + fb * boxhalfsize[2];
            if (min > rad || max < -rad)
                return true;
            return false;
        }

        /*======================== Z-tests ========================*/

        public static bool AXISTEST_Z12(double a, double b, double fa, double fb, ref Vertex v1, ref Vertex v2, ref double[] boxhalfsize)
        {
            double min = 0, max = 0;
            double p1 = a * v1.X - b * v1.Y;
            double p2 = a * v2.X - b * v2.Y;
            if (p2 < p1) { min = p2; max = p1; } else { min = p1; max = p2; }
            double rad = fa * boxhalfsize[0] + fb * boxhalfsize[1];
            if (min > rad || max < -rad)
                return true;
            return false;
        }

        public static bool AXISTEST_Z0(double a, double b, double fa, double fb, ref Vertex v0, ref Vertex v1, ref double[] boxhalfsize)
        {
            double min = 0, max = 0;
            double p0 = a * v0.X - b * v0.Y;
            double p1 = a * v1.X - b * v1.Y;
            if (p0 < p1) { min = p0; max = p1; } else { min = p1; max = p0; }
            double rad = fa * boxhalfsize[0] + fb * boxhalfsize[1];
            if (min > rad || max < -rad) return true;
            return false;
        }

        public static bool TriBoxOverlap(ref double[] boxcenter, ref double[] boxhalfsize, ref PolygonalFace tri)
        {
            /*    use separating axis theorem to test overlap between triangle and box */
            /*    need to test for overlap in these directions: */
            /*    1) the {x,y,z}-directions (actually, since we use the AABB of the triangle */
            /*       we do not even need to test these) */
            /*    2) normal of the triangle */
            /*    3) crossproduct(edge from tri, {x,y,z}-directin) */
            /*       this gives 3x3=9 more tests */

            /* This is the fastest branch on Sun */
            /* move everything so that the boxcenter is in (0,0,0) */
            var v0 = new Vertex(tri.Vertices[0].Position.subtract(boxcenter));
            var v1 = new Vertex(tri.Vertices[1].Position.subtract(boxcenter));
            var v2 = new Vertex(tri.Vertices[2].Position.subtract(boxcenter));

            /* compute triangle edges */
            var e0 = new Vertex(v1.Position.subtract(v0.Position));      /* tri edge 0 */
            var e1 = new Vertex(v2.Position.subtract(v1.Position));      /* tri edge 1 */
            var e2 = new Vertex(v0.Position.subtract(v2.Position));      /* tri edge 2 */

            /* Bullet 3:  */
            /*  test the 9 tests first (this was faster) */
            double fex = Math.Abs(e0.X);
            double fey = Math.Abs(e0.Y);
            double fez = Math.Abs(e0.Z);
            if (AXISTEST_X01(e0.Z, e0.Y, fez, fey, ref v0, ref v2, ref boxhalfsize))
                return false;
            if (AXISTEST_Y02(e0.Z, e0.X, fez, fex, ref v0, ref v2, ref boxhalfsize))
                return false;
            if (AXISTEST_Z12(e0.Y, e0.X, fey, fex, ref v1, ref v2, ref boxhalfsize))
                return false;

            fex = Math.Abs(e1.X);
            fey = Math.Abs(e1.Y);
            fez = Math.Abs(e1.Z);
            if (AXISTEST_X01(e1.Z, e1.Y, fez, fey, ref v0, ref v2, ref boxhalfsize))
                return false;
            if (AXISTEST_Y02(e1.Z, e1.X, fez, fex, ref v0, ref v2, ref boxhalfsize))
                return false;
            if (AXISTEST_Z0(e1.Y, e1.X, fey, fex, ref v0, ref v1, ref boxhalfsize))
                return false;

            fex = Math.Abs(e2.X);
            fey = Math.Abs(e2.Y);
            fez = Math.Abs(e2.Z);
            if (AXISTEST_X2(e2.Z, e2.Y, fez, fey, ref v0, ref v1, ref boxhalfsize))
                return false;
            if (AXISTEST_Y1(e2.Z, e2.X, fez, fex, ref v0, ref v1, ref boxhalfsize))
                return false;
            if (AXISTEST_Z12(e2.Y, e2.X, fey, fex, ref v1, ref v2, ref boxhalfsize))
                return false;

            /* Bullet 1: */
            /*  first test overlap in the {x,y,z}-directions */
            /*  find min, max of the triangle each direction, and test for overlap in */
            /*  that direction -- this is equivalent to testing a minimal AABB around */
            /*  the triangle against the AABB */

            /* test in X-direction */
            double min, max;
            FindMinMax(v0.X, v1.X, v2.X, out min, out max);
            if (min > boxhalfsize[0] || max < -boxhalfsize[0])
                return false;

            /* test in Y-direction */
            FindMinMax(v0.Y, v1.Y, v2.Y, out min, out max);
            if (min > boxhalfsize[1] || max < -boxhalfsize[1])
                return false;

            /* test in Z-direction */
            FindMinMax(v0.Z, v1.Z, v2.Z, out min, out max);
            if (min > boxhalfsize[2] || max < -boxhalfsize[2])
                return false;

            /* Bullet 2: */
            /*  test if the box intersects the plane of the triangle */
            /*  compute plane equation of triangle: normal*x+d=0 */
            var normal = e0.Position.crossProduct(e1.Position);
            var d = normal.dotProduct(v0.Position);/* plane eq: normal.x+d=0 */
            d = -d;
            if (!PlaneBoxOverlap(normal, d, boxhalfsize))
                return false;

            return true;   /* box and triangle overlaps */
        }
    }
}
