// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 02-18-2015
// ***********************************************************************
// <copyright file="Cylinder.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using StarMathLib;

namespace OldTVGL
{
    /// <summary>
    ///     The class for Cylinder primitives.
    /// </summary>
    public class Cylinder : PrimitiveSurface
    {
        /// <summary>
        ///     Determines whether [is new member of] [the specified face].
        /// </summary>
        /// <param name="face">The face.</param>
        /// <returns><c>true</c> if [is new member of] [the specified face]; otherwise, <c>false</c>.</returns>
        public override bool IsNewMemberOf(PolygonalFace face)
        {
            if (Faces.Contains(face)) return false;
            if (Math.Abs(face.Normal.dotProduct(Axis, 3)) > Constants.ErrorForFaceInSurface)
                return false;
            foreach (var v in face.Vertices)
                if (Math.Abs(MiscFunctions.DistancePointToLine(v.Position, Anchor, Axis) - Radius) >
                    Constants.ErrorForFaceInSurface * Radius)
                    return false;
            return true;
        }

        /// <summary>
        ///     Updates the with.
        /// </summary>
        /// <param name="face">The face.</param>
        public override void UpdateWith(PolygonalFace face)
        {
            var numFaces = Faces.Count;
            double[] inBetweenPoint;
            var distance = MiscFunctions.SkewedLineIntersection(face.Center, face.Normal, Anchor, Axis,
                out inBetweenPoint);
            var fractionToMove = 1 / numFaces;
            var moveVector = Anchor.crossProduct(face.Normal);
            if (moveVector.dotProduct(face.Center.subtract(inBetweenPoint, 3)) < 0)
                moveVector = moveVector.multiply(-1);
            moveVector.normalizeInPlace(3);
            /**** set new Anchor (by averaging in with last n values) ****/
            Anchor =
                Anchor.add(new[]
                {
                    moveVector[0]*fractionToMove*distance, moveVector[1]*fractionToMove*distance,
                    moveVector[2]*fractionToMove*distance
                }, 3);

            /* to adjust the Axis, we will average the cross products of the new face with all the old faces */
            var totalAxis = new double[3];
            foreach (var oldFace in Faces)
            {
                var newAxis = face.Normal.crossProduct(oldFace.Normal);
                if (newAxis.dotProduct(Axis, 3) < 0)
                    newAxis.multiply(-1);
                totalAxis = totalAxis.add(newAxis, 3);
            }
            var numPrevCrossProducts = numFaces * (numFaces - 1) / 2;
            totalAxis = totalAxis.add(Axis.multiply(numPrevCrossProducts), 3);
            /**** set new Axis (by averaging in with last n values) ****/
            Axis = totalAxis.divide(numFaces + numPrevCrossProducts).normalize(3);
            foreach (var v in face.Vertices)
                if (!Vertices.Contains(v))
                    Vertices.Add(v);
            var totalOfRadii = Vertices.Sum(v => MiscFunctions.DistancePointToLine(v.Position, Anchor, Axis));
            /**** set new Radius (by averaging in with last n values) ****/
            Radius = totalOfRadii / Vertices.Count;
            base.UpdateWith(face);
        }

        /// <summary>
        ///     Updates the with.
        /// </summary>
        /// <param name="face">The face.</param>
        public bool BuildIfCylinderIsHole(bool isPositive)
        {
            if (isPositive) throw new Exception("BuildIfCylinderIsHole assumes that the faces have already been collected, " +
                "such that the cylinder is negative");
            IsPositive = false;

            //To truly be a hole, there should be two loops of vertices that form circles on either ends of the faces.
            //These are easy to capture because all the edges between them should be shared by two of the faces
            //Start by collecting the edges at either end. Each edge belongs to only two faces, so any edge that only
            //comes up once, must be at the edge of the cylinder (assuming it is a cylinder).
            var edges = new HashSet<Edge>();
            foreach (var face in Faces)
            {
                foreach (var edge in face.Edges)
                {
                    if (edges.Contains(edge))
                    {
                        edges.Remove(edge);
                    }
                    else edges.Add(edge);
                }
            }
            //Now we can loop through the edges to form two loops
            if (edges.Count < 5) return false; //5 is the minimum number of vertices to look remotely circular (8 is more likely)
            var (allLoopsClosed, edgeLoops, loops) = GetLoops(edges, true);
            if (loops.Count != 2) return false; //There must be two and only two loops.

            Loop1 = new HashSet<Vertex>(loops[0]);
            Loop2 = new HashSet<Vertex>(loops[1]);
            EdgeLoop1 = edgeLoops[0];
            EdgeLoop2 = edgeLoops[1];

            //Next, we need to get the central axis.
            //The ends of the cylinder could be any shape (flat, curved, angled) and the shapes
            //on each end do not need to match. This rules out using the vertex loops to form
            //a plane (most accurate) and creating a plane from edge midpoints (next most accurate).
            //The next most accurate thing is to use the edge vectors to set the axis. 
            //This is more precise than taking a bunch of cross products with the faces.
            //And it is more universal than creating a plane from the loops, since it works 
            //for holes that enter and exit at an angle.
            var throughEdgeVectors = new Dictionary<Vertex, double[]>();
            var dotFromSharpestEdgesConnectedToVertex = new Dictionary<Vertex, double>();
            foreach (var edge in InnerEdges)
            {
                //Skip those edges that are on "flat" surfaces
                var dot = edge.OwnedFace.Normal.dotProduct(edge.OtherFace.Normal);
                if (dot.IsPracticallySame(1.0, Constants.ErrorForFaceInSurface)) continue;
                //This uses a for loop to remove duplicate code, to decide which vertex to check with which loop
                for (var i = 0; i < 2; i++) 
                {
                    var A = i == 0 ? edge.To : edge.From;
                    var B = i == 0 ? edge.From : edge.To;
                    var direction = edge.Vector.normalize();
                    //Positive if B is further along
                    var previousDistance = direction.dotProduct(B.Position);
                    var sign = Math.Sign(direction.dotProduct(B.Position) - direction.dotProduct(A.Position));
                    if (Loop1.Contains(A))
                    {
                        bool reachedEnd = Loop2.Contains(B);
                        if (!reachedEnd)
                        {                           
                            //Check if this edge needs to "extended" to reach the end of the cylinder
                            var previousEdge = edge;
                            var previousVertex = B;
                            while (!reachedEnd)
                            {                          
                                var maxDot = 0.0;
                                Edge extensionEdge = null;
                                foreach (var otherEdge in previousVertex.Edges.Where(e => e != previousEdge))
                                {
                                    //This other edge must be contained in the InnerEdges and along the same direction
                                    if (!InnerEdges.Contains(otherEdge)) continue;
                                    var edgeDot = Math.Abs(otherEdge.Vector.normalize().dotProduct(previousEdge.Vector.normalize()));
                                    if (!edgeDot.IsPracticallySame(1.0, Constants.ErrorForFaceInSurface)) continue;
                                    var distance = sign * (direction.dotProduct(otherEdge.OtherVertex(previousVertex).Position) - previousDistance);
                                    if (!distance.IsGreaterThanNonNegligible()) continue; //This vertex is not any further along

                                    //Choose the edge that is most along the previous edge
                                    if (edgeDot > maxDot)
                                    {
                                        maxDot = edgeDot;
                                        extensionEdge = otherEdge;
                                    }
                                }
                                if (extensionEdge == null) break; //go to the next edge
                                if (Loop2.Contains(extensionEdge.OtherVertex(previousVertex)))
                                {
                                    reachedEnd = true;
                                    B = extensionEdge.OtherVertex(previousVertex);
                                }
                                else
                                {
                                    previousVertex = extensionEdge.OtherVertex(previousVertex);
                                    previousEdge = extensionEdge;
                                }
                            }                           
                        }
                        //If there was a vertex from the edge or edges in the second loop.
                        if (reachedEnd) 
                        { 
                            if (!dotFromSharpestEdgesConnectedToVertex.ContainsKey(A))
                            {
                                throughEdgeVectors.Add(A, B.Position.subtract(A.Position));
                                dotFromSharpestEdgesConnectedToVertex.Add(A, edge.InternalAngle);
                            }
                            else if (dot < dotFromSharpestEdgesConnectedToVertex[A])
                            {
                                throughEdgeVectors[A] = B.Position.subtract(A.Position);
                                dotFromSharpestEdgesConnectedToVertex[A] = dot;
                            }
                            break; //Go to the next edge
                        }
                    }
                }
            }
            if (throughEdgeVectors.Count < 3) return false;

            //Estimate the axis from the sum of the through edge vectors
            //The axis points from loop 1 to loop 2, since we always start the edge vector from Loop1
            var edgeVectors = new List<double[]>(throughEdgeVectors.Values);
            var numEdges = edgeVectors.Count;
            var axis = new double[] { 0.0, 0.0, 0.0 };
            foreach(var edgeVector in edgeVectors) axis = axis.add(edgeVector);
            Axis = axis.normalize();

            /* to adjust the Axis, we will average the cross products of the new face with all the old faces */
            //Since we will be taking cross products, we need to be sure not to have faces along the same normal
            var faces = MiscFunctions.FacesWithDistinctNormals(Faces.ToList());
            var n = faces.Count;

            //Check if the loops are circular along the axis
            var path1 = MiscFunctions.Get2DProjectionPointsAsLight(Loop1, Axis, out var backTransform);
            var poly = new PolygonLight(path1);
            if (!PolygonOperations.IsCircular(new Polygon(poly), out var centerCircle, Constants.MediumConfidence))
            {
                return false;
            }
            var path2 = MiscFunctions.Get2DProjectionPointsAsLight(Loop2, Axis, out var backTransform2);
            var poly2 = new PolygonLight(path2);
            if (!PolygonOperations.IsCircular(new Polygon(poly2), out var centerCircle2, Constants.MediumConfidence))
            {
                return false;
            }
            Radius = (centerCircle.Radius + centerCircle2.Radius) / 2; //Average
            return true;
        }

        /// <summary>
        /// Transforms the shape by the provided transformation matrix.
        /// </summary>
        /// <param name="transformMatrix">The transform matrix.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public override void Transform(double[,] transformMatrix)
        {
            var homoCoord = new[] { Anchor[0], Anchor[1], Anchor[2], 1.0 };
            homoCoord = transformMatrix.multiply(homoCoord);
            Anchor[0] = homoCoord[0]; Anchor[1] = homoCoord[1]; Anchor[2] = homoCoord[2];

            homoCoord = new[] { Axis[0], Axis[1], Axis[2], 1.0 };
            homoCoord = transformMatrix.multiply(homoCoord);
            Axis[0] = homoCoord[0]; Axis[1] = homoCoord[1]; Axis[2] = homoCoord[2];

            //how to adjust the radii?
            throw new NotImplementedException();
        }

        #region Properties

        /// <summary>
        ///     Is the cylinder positive? (false is negative)
        /// </summary>
        public bool IsPositive;

        /// <summary>
        ///     Did the cylinder pass the cylinder checks?
        /// </summary>
        public bool IsValid;

        /// <summary>
        ///     Gets the anchor.
        /// </summary>
        /// <value>The anchor.</value>
        public double[] Anchor { get;  set; }

        /// <summary>
        ///     Gets the direction.
        /// </summary>
        /// <value>The direction.</value>
        public double[] Axis { get;  set; }

        /// <summary>
        ///     Gets the radius.
        /// </summary>
        /// <value>The radius.</value>
        public double Radius { get;  set; }

        public HashSet<Vertex> Loop1 { get; set; }

        public HashSet<Vertex> Loop2 { get; set; }

        public List<Edge> EdgeLoop1 { get; set; }

        public List<Edge> EdgeLoop2 { get; set; }

        public HashSet<Flat> SmallFlats { get; set; }

        public PolygonLight Loop2D { get; set; }

        public double MaxDistanceAlongAxis { get; set; }

        public double MinDistanceAlongAxis { get; set; }
        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="Cylinder" /> class.
        /// </summary>
        /// <param name="faces">The faces all.</param>
        /// <param name="axis">The axis.</param>
        public Cylinder(IEnumerable<PolygonalFace> faces, bool buildOnlyIfHole, bool isPositive,
            HashSet<Flat> featureFlats = null) : base(faces)
        {
            if (!buildOnlyIfHole) throw new Exception("This Cylinder constructor only works when you want to find holes.");
            Type = PrimitiveSurfaceType.Cylinder;
            SmallFlats = featureFlats;
            IsValid = BuildIfCylinderIsHole(isPositive);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Cylinder" /> class.
        /// </summary>
        /// <param name="facesAll">The faces all.</param>
        /// <param name="axis">The axis.</param>
        public Cylinder(IEnumerable<PolygonalFace> facesAll, double[] axis)
            : base(facesAll)
        {
            Type = PrimitiveSurfaceType.Cylinder;
            var faces = MiscFunctions.FacesWithDistinctNormals(facesAll.ToList());
            var n = faces.Count;
            var centers = new List<double[]>();
            double[] center;
            double t1, t2;
            var signedDistances = new List<double>();
            MiscFunctions.SkewedLineIntersection(faces[0].Center, faces[0].Normal,
                faces[n - 1].Center, faces[n - 1].Normal, out center, out t1, out t2);
            if (!center.Any(double.IsNaN) || center.IsNegligible())
            {
                centers.Add(center);
                signedDistances.Add(t1);
                signedDistances.Add(t2);
            }
            for (var i = 1; i < n; i++)
            {
                MiscFunctions.SkewedLineIntersection(faces[i].Center, faces[i].Normal,
                    faces[i - 1].Center, faces[i - 1].Normal, out center, out t1, out t2);
                if (!center.Any(double.IsNaN) || center.IsNegligible())
                {
                    centers.Add(center);
                    signedDistances.Add(t1);
                    signedDistances.Add(t2);
                }
            }
            center = new double[3];
            center = centers.Aggregate(center, (current, c) => current.add(c, 3));
            center = center.divide(centers.Count);
            /* move center to origin plane */
            var distBackToOrigin = -1 * axis.dotProduct(center, 3);
            center = center.subtract(axis.multiply(distBackToOrigin), 3);
            /* determine is positive or negative */
            var numNeg = signedDistances.Count(d => d < 0);
            var numPos = signedDistances.Count(d => d > 0);
            var isPositive = numNeg > numPos;
            var radii = new List<double>();
            foreach (var face in faces)
                radii.AddRange(face.Vertices.Select(v => MiscFunctions.DistancePointToLine(v.Position, center, axis)));
            var averageRadius = radii.Average();

            Axis = axis;
            Anchor = center;
            IsPositive = isPositive;
            Radius = averageRadius;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Cylinder" /> class.
        /// </summary>
        /// <param name="edge">The edge.</param>
        /// <exception cref="System.Exception">Edge used to define cylinder is flat.</exception>
        internal Cylinder(Edge edge)
            : base(new List<PolygonalFace>(new[] { edge.OwnedFace, edge.OtherFace }))
        {
            Type = PrimitiveSurfaceType.Cylinder;
            var axis = edge.OwnedFace.Normal.crossProduct(edge.OtherFace.Normal);
            var length = axis.norm2();
            if (length.IsNegligible()) throw new Exception("Edge used to define cylinder is flat.");
            axis.normalizeInPlace(3);
            var v1 = edge.From;
            var v2 = edge.To;
            var v3 = edge.OwnedFace.Vertices.First(v => v != v1 && v != v2);
            var v4 = edge.OtherFace.Vertices.First(v => v != v1 && v != v2);
            double[] center;
            MiscFunctions.SkewedLineIntersection(edge.OwnedFace.Center, edge.OwnedFace.Normal,
                edge.OtherFace.Center, edge.OtherFace.Normal, out center);
            /* determine is positive or negative */
            var isPositive = edge.Curvature == CurvatureType.Convex;
            /* move center to origin plane */
            var distToOrigin = axis.dotProduct(center, 3);
            if (distToOrigin < 0)
            {
                distToOrigin *= -1;
                axis.multiply(-1);
            }
            center = new[]
            {
                center[0] - distToOrigin*axis[0],
                center[1] - distToOrigin*axis[1],
                center[2] - distToOrigin*axis[2]
            };
            var d1 = MiscFunctions.DistancePointToLine(v1.Position, center, axis);
            var d2 = MiscFunctions.DistancePointToLine(v2.Position, center, axis);
            var d3 = MiscFunctions.DistancePointToLine(v3.Position, center, axis);
            var d4 = MiscFunctions.DistancePointToLine(v4.Position, center, axis);
            var averageRadius = (d1 + d2 + d3 + d4) / 4;
            var outerEdges = new List<Edge>(edge.OwnedFace.Edges);
            outerEdges.AddRange(edge.OtherFace.Edges);
            outerEdges.Remove(edge);
            outerEdges.Remove(edge);

            Axis = axis;
            Anchor = center;
            IsPositive = isPositive;
            Radius = averageRadius;
        }

        internal Cylinder()
        { Type = PrimitiveSurfaceType.Cylinder; }

        //public TessellatedSolid AsTessellatedSolid()
        //{
        //    var faces = new List<PolygonalFace>();
        //    foreach(var face in Faces)
        //    {
        //        var vertices = new Vertex[] { face.C, face.B, face.A }; //reverse the vertices
        //        faces.Add(new PolygonalFace(vertices, face.Normal.multiply(-1)));
        //    }
        //    //Add the top and bottom faces
        //    //Build the cylinder along the axis
        //    //First, get the planes on the top and bottom.
        //    //Second, determine which plane is further along the axis. The faces on this plane will have a normal == axis
        //    //The bottom plane will have faces in the reverse of the axis.
        //    var plane1 = MiscFunctions.GetPlaneFromThreePoints(Loop1[0].Position, Loop1[1].Position, Loop1[2].Position);
        //    var plane
        //}
        #endregion
    }
}