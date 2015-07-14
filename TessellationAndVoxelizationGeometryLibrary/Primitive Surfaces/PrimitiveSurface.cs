using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{

    /// <summary>
    /// Class PrimitiveSurface.
    /// </summary>
    public abstract class PrimitiveSurface
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="PrimitiveSurface"/> class.
        /// </summary>
        /// <param name="faces">The faces.</param>
        protected PrimitiveSurface(IEnumerable<PolygonalFace> faces)
        {
            Faces = faces.ToList();
            Area = Faces.Sum(f => f.Area);

            var outerEdges = new HashSet<Edge>();
            var innerEdges = new HashSet<Edge>();
            foreach (var face in Faces)
            {
                foreach (var edge in face.Edges)
                {
                    if (innerEdges.Contains(edge)) continue;
                    else if (!outerEdges.Contains(edge)) outerEdges.Add(edge);
                    else
                    {
                        innerEdges.Add(edge);
                        outerEdges.Remove(edge);
                    }
                }
            }
            OuterEdges = new List<Edge>(outerEdges);
            InnerEdges =new List<Edge>(innerEdges);
            Vertices = Faces.SelectMany(f => f.Vertices).Distinct().ToList();
        }
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="PrimitiveSurface"/> class.
        /// </summary>
        protected PrimitiveSurface()
        {
        }

        /// <summary>
        /// Gets the area.
        /// </summary>
        /// <value>
        /// The area.
        /// </value>
        public double Area { get; internal set; }

        /// <summary>
        /// Gets or sets the polygonal faces.
        /// </summary>
        /// <value>
        /// The polygonal faces.
        /// </value>
        public List<PolygonalFace> Faces { get; internal set; }

        /// <summary>
        /// Gets or sets the transformation.
        /// </summary>
        /// <value>
        /// The transformation.
        /// </value>
        public double[,] Transformation { get; internal set; }

        /// <summary>
        /// Gets the inner edges.
        /// </summary>
        /// <value>
        /// The inner edges.
        /// </value>
        public List<Edge> InnerEdges { get; internal set; }


        /// <summary>
        /// Gets the outer edges.
        /// </summary>
        /// <value>
        /// The outer edges.
        /// </value>
        public List<Edge> OuterEdges { get; internal set; }

        /// <summary>
        /// Gets the vertices.
        /// </summary>
        /// <value>
        /// The vertices.
        /// </value>
        public List<Vertex> Vertices { get; internal set; }

        public abstract Boolean IsNewMemberOf(PolygonalFace face);

        public virtual void UpdateWith(PolygonalFace face)
        {
            Area += face.Area;
            foreach (var v in face.Vertices)
                if (!Vertices.Contains(v))
                    Vertices.Add(v);
            foreach (var e in face.Edges)
            {
                if (InnerEdges.Contains(e)) continue; //throw new Exception("edge of new face is already an inner edge of surface, WTF?!");
                if (OuterEdges.Contains(e))
                {
                    OuterEdges.Remove(e);
                    InnerEdges.Add(e);
                }
                else OuterEdges.Add(e);
            }
            Faces.Add(face);
        }

        /*public static List<PrimitiveSurface> OverlappingPrimitiveSurfaces(TessellatedSolid a, TessellatedSolid b, List<PrimitiveSurface> aP, List<PrimitiveSurface> bP)
        {
            BoundingBoxOverlappingCheck(a, b);
            ConvexHullOverlappingCheck(a, b);
            var directions = new HashSet<double[]>();
            GaussSphereDirectionGeneration(directions);
            List<PrimitiveSurface> overlapped = PrimitiveOverlapping(aP, bP, directions);
        }

        private static void GaussSphereDirectionGeneration(HashSet<double[]> directions)
        {
            var planes = new List<double[]>
            {
                new double[] {-1,0,0},
                new double[] {1,0,0},
                new double[] {0,-1,0},
                new double[] {0,1,0},
                new double[] {0,0,-1},
                new double[] {0,0,1}
            };

            //for the plane [1,0,0]
            foreach (var plane in planes)
            {
                if (plane[0] != 0)
                {
                    var i = plane[0];
                    for (double j = -1; j <= 1; j += 0.001)
                    {
                        for (double k = -1; k <= 1; k += 0.001)
                        {
                            var m = new[] { i, j, k };
                            directions.Add(m.normalize());
                        }
                    }
                }
                if (plane[1] != 0)
                {
                    var j = plane[1];
                    for (double i = -1; i <= 1; i += 0.001)
                    {
                        for (double k = -1; k <= 1; k += 0.001)
                        {
                            var m = new[] { j, j, k };
                            directions.Add(m.normalize());
                        }
                    }
                }
                if (plane[2] != 0)
                {
                    var k = plane[2];
                    for (double i = -1; i <= 1; i += 0.001)
                    {
                        for (double j = -1; j <= 1; j += 0.001)
                        {
                            var m = new[] { j, j, k };
                            directions.Add(m.normalize());
                        }
                    }
                }

                // there are some repeated numbers which are fine
            }
        }

        private static List<PrimitiveSurface> PrimitiveOverlapping(List<PrimitiveSurface> aP, List<PrimitiveSurface> bP, HashSet<double[]> directions)
        {
            foreach (var primitiveA in aP)
            {
                foreach (var primitiveB in bP)
                {
                    var overlap = false;
                    if (primitiveA is Flat && primitiveB is Flat)
                        overlap = FlatFlatOverlappingCheck((Flat)primitiveA, (Flat)primitiveB, directions); 
                    if (primitiveA is Flat && primitiveB is Cylinder)
                        overlap = FlatCylinderOverlappingCheck((Cylinder)primitiveB, (Flat)primitiveA, directions); 
                    if (primitiveA is Flat && primitiveB is Sphere)
                        overlap = FlatSphereOverlappingCheck((Sphere)primitiveB, (Flat)primitiveA, directions);
                    if (primitiveA is Flat && primitiveB is Cone)
                        overlap = FlatConeOverlappingCheck((Cone)primitiveB, (Flat)primitiveA, directions);
                    
                    if (primitiveA is Cylinder && primitiveB is Flat)
                        overlap = FlatCylinderOverlappingCheck((Cylinder)primitiveA, (Flat)primitiveB, directions);
                    if (primitiveA is Cylinder && primitiveB is Cylinder)
                        overlap = CylinderCylinderOverlappingCheck((Cylinder)primitiveA, (Cylinder)primitiveB, directions);
                    if (primitiveA is Cylinder && primitiveB is Sphere)
                        overlap = CylinderSphereOverlappingCheck((Cylinder)primitiveA, (Sphere)primitiveB, directions);
                    if (primitiveA is Cylinder && primitiveB is Cone)
                        overlap = ConeCylinderOverlappingCheck((Cone)primitiveB, (Cylinder)primitiveA, directions);

                    if (primitiveA is Sphere && primitiveB is Flat)
                        overlap = FlatSphereOverlappingCheck((Sphere)primitiveA, (Flat)primitiveB, directions);
                    if (primitiveA is Sphere && primitiveB is Cylinder)
                        overlap = CylinderSphereOverlappingCheck((Cylinder)primitiveB, (Sphere)primitiveA, directions);
                    if (primitiveA is Sphere && primitiveB is Sphere)
                        overlap = SphereSphereOverlappingCheck((Sphere)primitiveA, (Sphere)primitiveB, directions);
                    if (primitiveA is Sphere && primitiveB is Cone)
                        overlap = ConeSphereOverlappingCheck((Cone)primitiveB, (Sphere)primitiveA, directions);

                    if (primitiveA is Cone && primitiveB is Flat)
                        overlap = FlatConeOverlappingCheck((Cone)primitiveA, (Flat)primitiveB, directions);
                    if (primitiveA is Cone && primitiveB is Cylinder)
                        overlap = ConeCylinderOverlappingCheck((Cone)primitiveA, (Cylinder)primitiveB, directions);
                    if (primitiveA is Cone && primitiveB is Sphere)
                        overlap = ConeSphereOverlappingCheck((Cone)primitiveA, (Sphere)primitiveB, directions);
                    if (primitiveA is Cone && primitiveB is Cone)
                        overlap = ConeConeOverlappingCheck((Cone)primitiveA, (Cone)primitiveB, directions);
                    

                }
            }
        }

        private static bool ConeSphereOverlappingCheck(Cone cone, Sphere sphere, HashSet<double[]> directions)
        {
            if (!cone.IsPositive && sphere.IsPositive)
                return NegConePosSphereOverlappingCheck(cone, sphere, directions);
            return false;
        }

        private static bool NegConePosSphereOverlappingCheck(Cone cone, Sphere sphere, HashSet<double[]> directions)
        {
            var overlap = false;
            var t1 = (sphere.Center[0] - cone.Apex[0]) / (cone.Axis[0]);
            var t2 = (sphere.Center[1] - cone.Apex[1]) / (cone.Axis[1]);
            var t3 = (sphere.Center[2] - cone.Apex[2]) / (cone.Axis[2]);
            if (Math.Abs(t1 - t2) < 0.00001 && Math.Abs(t1 - t3) < 0.00001 && Math.Abs(t3 - t2) < 0.00001)
            {
                foreach (var f1 in cone.Faces)
                {
                    foreach (var f2 in sphere.Faces.Where(f2=> TwoTrianglesParallelCheck(f1.Normal,f2.Normal)
                        && TwoTrianglesSamePlaneCheck(f1, f2)))
                    {
                        overlap = TwoTriangleOverlapCheck(f1, f2);
                    }
                }
            }
            if (overlap)
            {
                // the axis of the cylinder is the removal direction
                foreach (var dir in directions)
                {
                    var sin = Math.Sin(Math.Acos(cone.Axis.normalize().dotProduct(dir)));
                    if (Math.Abs(sin) < 0.01) continue;
                    directions.Remove(dir);
                }
            }
            return overlap;
        }

        private static bool ConeCylinderOverlappingCheck(Cone cone, Cylinder cylinder, HashSet<double[]> directions)
        {
            if (!cone.IsPositive || !cylinder.IsPositive) return false;
            return PosConePosCylinderOverlappingCheck(cone, cylinder, directions);
        }

        private static bool PosConePosCylinderOverlappingCheck(Cone cone, Cylinder cylinder, HashSet<double[]> directions)
        {
            var overlap = false;
            foreach (var fA in cone.Faces)
            {
                foreach (var fB in cylinder.Faces.Where(fB => TwoTrianglesParallelCheck(fA.Normal, fB.Normal) &&
                    TwoTrianglesSamePlaneCheck(fA, fB)))
                {
                    overlap = TwoTriangleOverlapCheck(fA, fB);
                    if (overlap)
                    {
                        foreach (var dir in directions.Where(dir => fB.Normal.dotProduct(dir) > 0.0))
                        {
                            directions.Remove(dir);
                        }
                    }
                }
            }
            return overlap;
        }

        private static bool FlatConeOverlappingCheck(Cone cone, Flat flat, HashSet<double[]> directions)
        {
            if (!cone.IsPositive) return false;
            var r = new Random();
            var rndFaceB = flat.Faces[r.Next(flat.Faces.Count)];
            var overlap = false;
            foreach (var coneFace in cone.Faces)
            {
                if (TwoTrianglesParallelCheck(coneFace.Normal, flat.Normal))
                {
                    if (TwoTrianglesSamePlaneCheck(coneFace, rndFaceB))
                    {
                        // now check if they overlap or not
                        foreach (var fFace in flat.Faces)
                        {
                            overlap = TwoTriangleOverlapCheck(coneFace, fFace);
                        }
                    }
                }
            }
            if (overlap)
            {
                // exactly like flat-flat
                foreach (var dir in directions.Where(dir => flat.Normal.dotProduct(dir) < 0.0))
                {
                    directions.Remove(dir);
                }
            }
            return overlap;
        }

        private static bool SphereSphereOverlappingCheck(Sphere sphere1, Sphere sphere2, HashSet<double[]> directions)
        {
            if (!sphere1.IsPositive && !sphere2.IsPositive) return false;
            if (sphere1.IsPositive && sphere2.IsPositive)
                return PosSpherePosSphereOverlappingCheck(sphere1, sphere1, directions);
            
            return PosSphereNegSphereOverlappingCheck(sphere1, sphere1, directions);

        }

        private static bool CylinderSphereOverlappingCheck(Cylinder cylinder, Sphere sphere, HashSet<double[]> directions)
        {
            if (cylinder.IsPositive || !sphere.IsPositive) return false;
            return NegCylinderPosSphereOverlappingCheck(cylinder, sphere, directions);
        }

        private static bool CylinderCylinderOverlappingCheck(Cylinder primitiveA, Cylinder primitiveB, HashSet<double[]> directions)
        {
            if (!primitiveA.IsPositive && primitiveB.IsPositive)
                return NegCylinderPosCylinderOverlappingCheck(primitiveA, primitiveB, directions);
            if (primitiveA.IsPositive && !primitiveB.IsPositive)
                return NegCylinderPosCylinderOverlappingCheck(primitiveB, primitiveA, directions);
            if (primitiveA.IsPositive && primitiveB.IsPositive)
                return PosCylinderPosCylinderOverlappingCheck(primitiveA, primitiveB, directions);
            if (!primitiveA.IsPositive && !primitiveB.IsPositive) return false;
           return false;
        }

        private static bool ConeConeOverlappingCheck(Cone cone1, Cone cone2, HashSet<double[]> directions)
        {
            
            if (!cone1.IsPositive && cone2.IsPositive)
                return NegConePosConeOverlappingCheck(cone1, cone2, directions);
            if (cone1.IsPositive && !cone2.IsPositive)
                return NegConePosConeOverlappingCheck(cone2, cone1, directions);
            if (cone1.IsPositive && cone2.IsPositive)
                return PosConePosConeOverlappingCheck(cone1, cone1, directions);
            if (!cone1.IsPositive && !cone2.IsPositive) return false;
           return false;
        }

        private static bool PosConePosConeOverlappingCheck(Cone cone1, Cone cone2, HashSet<double[]> directions)
        {
            var overlap = false;
            foreach (var fA in cone1.Faces)
            {
                foreach (var fB in cone2.Faces.Where(fB => TwoTrianglesParallelCheck(fA.Normal, fB.Normal) &&
                    TwoTrianglesSamePlaneCheck(fA, fB)))
                {
                    overlap = TwoTriangleOverlapCheck(fA, fB);
                    if (overlap)
                    {
                        foreach (var dir in directions.Where(dir => fB.Normal.dotProduct(dir) > 0.0))
                        {
                            directions.Remove(dir);
                        }
                    }
                }
            }
            return overlap;
        }

        private static bool NegConePosConeOverlappingCheck(Cone cone1, Cone cone2, HashSet<double[]> directions)
        {

            // cone1 is negative cone and cone2 is positive cone.
            var overlap = false;
            if (Math.Abs(cone1.Axis.normalize().dotProduct(cone2.Axis.normalize())) - 1 < 0.00001)
            {
                foreach (var f1 in cone1.Faces)
                {
                    foreach (var f2 in cone2.Faces)
                    {
                        if (TwoTrianglesParallelCheck(f1.Normal, f2.Normal))
                        {
                            if (TwoTrianglesSamePlaneCheck(f1, f2))
                            {
                                if (TwoTriangleOverlapCheck(f1, f2))
                                {
                                    overlap = true;
                                }
                            }
                        }
                    }
                }
            }
            if (overlap)
            {
                // only keep the directions along the axis of the cylinder. Keep the ones with the angle close to zero.
                foreach (var dir in directions)
                {
                    var sin = Math.Sin(Math.Acos(cone1.Axis.normalize().dotProduct(dir)));
                    if (Math.Abs(sin) < 0.01) continue;
                    directions.Remove(dir);
                }

            }
            return overlap;
        }

        private static bool FlatFlatOverlappingCheck(Flat primitiveA, Flat primitiveB, HashSet<double[]> directions)
        {
            // Find the equation of a plane and see if all of the vertices of another primitive are in the plane or not (with a delta).
            // if yes, now check and see if these primitives overlapp or not.
            //primitiveA.Normal;
            bool overlap = false;
            var aFaces = primitiveA.Faces;
            var bFaces = primitiveB.Faces;
            // Take a random face and make a plane.
            var r = new Random();
            var rndFaceA = aFaces[r.Next(aFaces.Count)];
            var rndFaceB = aFaces[r.Next(bFaces.Count)];

            if (TwoTrianglesParallelCheck(primitiveA.Normal, primitiveB.Normal))
            {
                bool samePlane = TwoTrianglesSamePlaneCheck(rndFaceA, rndFaceB);
                if (samePlane)
                {
                    // now check and see if any area of a is inside the boundaries of b or vicee versa
                    foreach (var f1 in primitiveA.Faces)
                    {
                        foreach (var f2 in primitiveA.Faces)
                        {
                            if (TwoTriangleOverlapCheck(f1, f2))
                                overlap = true;
                        }
                    }
                }
            }
            // if they overlap, update the directions
            if (overlap)
            {
                // take one of the pparts, for example A, then in the directions, remove the ones which make a positive dot product with the normal
                foreach (var dir in directions.Where(dir => primitiveB.Normal.dotProduct(dir) > 0.0))
                {
                    directions.Remove(dir);
                }

            }
            return overlap;
        }

        private static bool FlatCylinderOverlappingCheck(Cylinder primitiveA, Flat primitiveB, HashSet<double[]> directions)
        {
            // This must be a positive cylinder. There is no flat and negative cylinder. A cyliner, B flat
            // if there is any triangle on the cylinder with a parralel normal to the flat patch (and opposite direction). And then
            // if the distance between them is close to zero, then, check if they overlap o not.
            if (!primitiveA.IsPositive) return false;
            var r = new Random();
            var rndFaceB = primitiveB.Faces[r.Next(primitiveB.Faces.Count)];
            var overlap = false;
            foreach (var cylFace in primitiveA.Faces)
            {
                if (TwoTrianglesParallelCheck(cylFace.Normal, primitiveB.Normal))
                {
                    if (TwoTrianglesSamePlaneCheck(cylFace, rndFaceB))
                    {
                        // now check if they overlap or not
                        foreach (var fFace in primitiveB.Faces)
                        {
                            overlap = TwoTriangleOverlapCheck(cylFace, fFace);
                        }
                    }
                }
            }
            if (overlap)
            {
                // exactly like flat-flat
                foreach (var dir in directions.Where(dir => primitiveB.Normal.dotProduct(dir) < 0.0))
                {
                    directions.Remove(dir);
                }
            }
            return overlap;
        }

        private static bool NegCylinderPosCylinderOverlappingCheck(Cylinder primitiveA, Cylinder primitiveB, HashSet<double[]> directions)
        {
            // this is actually positive cylinder with negative cylinder. primitiveA is negative cylinder and 
            // primitiveB is positive cylinder. Like a normal 
            // check the centerlines. Is the vector of the center lines the same? 
            // now check the radius. 
            var overlap = false;
            if (Math.Abs(primitiveA.Axis.dotProduct(primitiveB.Axis))-1 < 0.00001)
            {
                // now centerlines are either parallel or the same. Now check and see if they are exactly the same
                // Take the anchor of B, using the axis of B, write the equation of the line. Check and see if 
                // the anchor of A is on the line equation.
                var t1 = (primitiveA.Anchor[0] - primitiveB.Anchor[0]) / (primitiveB.Axis[0]);
                var t2 = (primitiveA.Anchor[1] - primitiveB.Anchor[1]) / (primitiveB.Axis[1]);
                var t3 = (primitiveA.Anchor[2] - primitiveB.Anchor[2]) / (primitiveB.Axis[2]);
                if (Math.Abs(t1 - t2) < 0.00001 && Math.Abs(t1 - t3) < 0.00001 && Math.Abs(t3 - t2) < 0.00001)
                {
                    // Now check the radius
                    if (Math.Abs(primitiveA.Radius - primitiveB.Radius) < 0.0001)
                    {
                        foreach (var f1 in primitiveA.Faces)
                        {
                            foreach (var f2 in primitiveB.Faces)
                            {
                                overlap = TwoTriangleOverlapCheck(f1, f2);
                            }
                        }
                    }
                }
            }
            if (overlap)
            {
                // only keep the directions along the axis of the cylinder. Keep the ones with the angle close to zero.
                foreach (var dir in directions)
                {
                    var sin = Math.Sin(Math.Acos(primitiveA.Axis.normalize().dotProduct(dir)));
                    if (Math.Abs(sin) < 0.01) continue;
                    directions.Remove(dir);
                }
                
            }
            return overlap;
        }

        private static bool PosCylinderPosCylinderOverlappingCheck(Cylinder cylinder1, Cylinder cylinder2, HashSet<double[]> directions)
        {
            var overlap = false;
            foreach (var fA in cylinder1.Faces)
            {
                foreach (var fB in cylinder2.Faces.Where(fB => TwoTrianglesParallelCheck(fA.Normal, fB.Normal) &&
                    TwoTrianglesSamePlaneCheck(fA, fB)))
                {
                    overlap = TwoTriangleOverlapCheck(fA, fB);
                    if (overlap)
                    {
                        foreach (var dir in directions.Where(dir => fB.Normal.dotProduct(dir) > 0.0))
                        {
                            directions.Remove(dir);
                        }
                    }
                }
            }
            return overlap;
        }

        private static bool PosSphereNegSphereOverlappingCheck(Sphere primitiveA, Sphere primitiveB, HashSet<double[]> directions)
        {
            //postive(A)-negative(B)
            // if their centers are the same or really close
            // if their radius is equal or close
            double[] centerA = primitiveA.Center;
            double[] centerB = primitiveB.Center;
            if (Math.Abs(centerA[0] - centerB[0]) < 0.0001 &&
                Math.Abs(centerA[1] - centerB[1]) < 0.0001 &&
                Math.Abs(centerA[2] - centerB[2]) < 0.0001)
            {
                if (Math.Abs(primitiveA.Radius - primitiveB.Radius) < 0.001)
                    return true;
            }
            return false;
        }

        private static bool PosSpherePosSphereOverlappingCheck(Sphere primitiveA, Sphere primitiveB, HashSet<double[]> directions)
        {
            //postive(A)-Positive(B)
            // Seems to be really time consuming
            var overlap = false;
            foreach (var fA in primitiveA.Faces)
            {
                foreach (var fB in primitiveB.Faces.Where(fB => TwoTrianglesParallelCheck(fA.Normal, fB.Normal) &&
                    TwoTrianglesSamePlaneCheck(fA, fB)))
                {
                    overlap = TwoTriangleOverlapCheck(fA, fB);
                    if (overlap)
                    {
                        foreach (var dir in directions.Where(dir => fB.Normal.dotProduct(dir) > 0.0))
                        {
                            directions.Remove(dir);
                        }
                    }
                }
            }
        }

        private static bool FlatSphereOverlappingCheck(Sphere sphere, Flat primitiveB, HashSet<double[]> directions)
        {
            if (!sphere.IsPositive) return false;
            // Positive sphere (primitiveA) and primitiveB is flat.
            // similar to flat-cylinder
            var overlap = false;
            var r = new Random();
            var rndFaceB = primitiveB.Faces[r.Next(primitiveB.Faces.Count)];
            foreach (var sophFace in sphere.Faces.Where(sophFace => TwoTrianglesParallelCheck(sophFace.Normal, primitiveB.Normal))
                                                     .Where(sophFace => TwoTrianglesSamePlaneCheck(sophFace, rndFaceB)))
            {
                foreach (var fFace in primitiveB.Faces)
                {
                    if (TwoTriangleOverlapCheck(sophFace, fFace))
                        overlap = true;
                }
            }
            if (overlap)
            {
                foreach (var dir in directions.Where(dir => primitiveB.Normal.dotProduct(dir) > 0.0))
                {
                    directions.Remove(dir);
                }
            }
            return overlap;
        }

        private static bool NegCylinderPosSphereOverlappingCheck(Cylinder primitiveA, Sphere primitiveB, HashSet<double[]> directions)
        {
            // if the center of the sphere is on the cylinder centerline.
            // or again: two faces parralel, on the same plane and overlap
            var overlap = false;
            var t1 = (primitiveB.Center[0] - primitiveA.Anchor[0]) / (primitiveA.Axis[0]);
            var t2 = (primitiveB.Center[1] - primitiveA.Anchor[1]) / (primitiveA.Axis[1]);
            var t3 = (primitiveB.Center[2] - primitiveA.Anchor[2]) / (primitiveA.Axis[2]);
            if (Math.Abs(t1 - t2) < 0.00001 && Math.Abs(t1 - t3) < 0.00001 && Math.Abs(t3 - t2) < 0.00001)
            {
                // Now check the radius
                if (Math.Abs(primitiveA.Radius - primitiveA.Radius) < 0.0001)
                {
                    foreach (var f1 in primitiveA.Faces)
                    {
                        foreach (var f2 in primitiveB.Faces.Where(f2 => TwoTrianglesParallelCheck(f1.Normal, f2.Normal)
                        && TwoTrianglesSamePlaneCheck(f1, f2)))
                        {
                            overlap = TwoTriangleOverlapCheck(f1, f2);
                        }
                    }
                }
            }
            if (overlap)
            {
                // the axis of the cylinder is the removal direction
                foreach (var dir in directions)
                {
                    var sin = Math.Sin(Math.Acos(primitiveA.Axis.normalize().dotProduct(dir)));
                    if (Math.Abs(sin) < 0.01) continue;
                    directions.Remove(dir);
                }
            }
            return overlap;
        }


        private static bool TwoTrianglesSamePlaneCheck(PolygonalFace rndFaceA, PolygonalFace rndFaceB)
        {
            var q = rndFaceA.Center;
            var p = rndFaceB.Center;
            var pq = new[] { q[0] - p[0], q[1] - p[1], q[2] - p[2] };
            var d = Math.Abs(StarMath.dotProduct(pq, rndFaceA.Normal)) /
            (Math.Sqrt(Math.Pow(rndFaceA.Normal[0], 2.0) + Math.Pow(rndFaceA.Normal[1], 2.0) + Math.Pow(rndFaceA.Normal[2], 2.0)));
            return d < 0.0001;
        }

        private static bool TwoTrianglesParallelCheck(double[] aNormal, double[] bNormal)
        {
            // they must be parralel but in the opposite direction. the boolian must be changed
            return Math.Abs(bNormal.dotProduct(aNormal) + 1) < 0.00001;
        }

        private static bool TwoTriangleOverlapCheck(PolygonalFace fA, PolygonalFace fB)
        {
            foreach (var edge in fA.Edges)
            {
                var edgeVector = edge.Vector;
                var third = fA.Vertices.Where(a => a != edge.From && a != edge.To).ToList()[0].Position;
                var checkVec = new[] {third[0] - edge.From.Position[0], third[1] - edge.From.Position[1], 
                    third[2] - edge.From.Position[2]};
                double[] cross1 = StarMath.crossProduct(edgeVector, checkVec);
                var c = 0;
                foreach (var vertexB in fB.Vertices)
                {
                    var newVec = new[] {vertexB.Position[0] - edge.From.Position[0], vertexB.Position[1] - edge.From.Position[1], 
                    vertexB.Position[2] - edge.From.Position[2]};
                    double[] cross2 = StarMath.crossProduct(edgeVector, newVec);
                    if ((Math.Sign(cross1[0]) != Math.Sign(cross2[0]) || (Math.Sign(cross1[0]) == 0 && Math.Sign(cross2[0]) == 0)) &&
                        (Math.Sign(cross1[1]) != Math.Sign(cross2[1]) || (Math.Sign(cross1[1]) == 0 && Math.Sign(cross2[1]) == 0)) &&
                        (Math.Sign(cross1[2]) != Math.Sign(cross2[2]) || (Math.Sign(cross1[2]) == 0 && Math.Sign(cross2[2]) == 0)))
                    {
                        c++;
                    }
                }
                if (c == 3)
                {
                    return false;
                }
            }
            return true;
        }*/
    }
}
