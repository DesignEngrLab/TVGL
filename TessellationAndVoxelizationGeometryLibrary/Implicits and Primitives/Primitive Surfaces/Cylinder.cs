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
using TVGL.Numerics;
using TVGL.TwoDimensional;

namespace TVGL
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
            if (Math.Abs(face.Normal.Dot(Axis)) > Constants.ErrorForFaceInSurface)
                return false;
            foreach (var v in face.Vertices)
                if (Math.Abs(MiscFunctions.DistancePointToLine(v.Coordinates, Anchor, Axis) - Radius) >
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
            var distance = MiscFunctions.SkewedLineIntersection(face.Center, face.Normal, Anchor, Axis,
                out var inBetweenPoint);
            var fractionToMove = 1 / numFaces;
            var moveVector = Anchor.Cross(face.Normal);
            if (moveVector.Dot(face.Center.Subtract(inBetweenPoint)) < 0)
                moveVector = moveVector * -1;
            moveVector = moveVector.Normalize();
            /**** set new Anchor (by averaging in with last n values) ****/
            Anchor =
                Anchor + new Vector3(
                    moveVector.X * fractionToMove * distance, moveVector.Y * fractionToMove * distance,
                    moveVector.Z * fractionToMove * distance
               );

            /* to adjust the Axis, we will average the cross products of the new face with all the old faces */
            var totalAxis = new Vector3();
            foreach (var oldFace in Faces)
            {
                var newAxis = face.Normal.Cross(oldFace.Normal);
                if (newAxis.Dot(Axis) < 0)
                    newAxis = -1 * newAxis;
                totalAxis = totalAxis + newAxis;
            }
            var numPrevCrossProducts = numFaces * (numFaces - 1) / 2;
            totalAxis = totalAxis + (Axis * numPrevCrossProducts);
            /**** set new Axis (by averaging in with last n values) ****/
            Axis = totalAxis.Divide(numFaces + numPrevCrossProducts).Normalize();
            foreach (var v in face.Vertices)
                if (!Vertices.Contains(v))
                    Vertices.Add(v);
            var totalOfRadii = Vertices.Sum(v => MiscFunctions.DistancePointToLine(v.Coordinates, Anchor, Axis));
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
            var throughEdgeVectors = new Dictionary<Vertex, Vector3>();
            var dotFromSharpestEdgesConnectedToVertex = new Dictionary<Vertex, double>();
            foreach (var edge in InnerEdges)
            {
                //Skip those edges that are on "flat" surfaces
                var dot = edge.OwnedFace.Normal.Dot(edge.OtherFace.Normal);
                if (dot.IsPracticallySame(1.0, Constants.ErrorForFaceInSurface)) continue;
                //This uses a for loop to remove duplicate code, to decide which vertex to check with which loop
                for (var i = 0; i < 2; i++)
                {
                    var A = i == 0 ? edge.To : edge.From;
                    var B = i == 0 ? edge.From : edge.To;
                    var direction = edge.Vector.Normalize();
                    //Positive if B is further along
                    var previousDistance = direction.Dot(B.Coordinates);
                    var sign = Math.Sign(direction.Dot(B.Coordinates) - direction.Dot(A.Coordinates));
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
                                    var edgeDot = Math.Abs(otherEdge.Vector.Normalize().Dot(previousEdge.Vector.Normalize()));
                                    if (!edgeDot.IsPracticallySame(1.0, Constants.ErrorForFaceInSurface)) continue;
                                    var distance = sign * (direction.Dot(otherEdge.OtherVertex(previousVertex).Coordinates) - previousDistance);
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
                                throughEdgeVectors.Add(A, B.Coordinates.Subtract(A.Coordinates));
                                dotFromSharpestEdgesConnectedToVertex.Add(A, edge.InternalAngle);
                            }
                            else if (dot < dotFromSharpestEdgesConnectedToVertex[A])
                            {
                                throughEdgeVectors[A] = B.Coordinates.Subtract(A.Coordinates);
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
            var edgeVectors = new List<Vector3>(throughEdgeVectors.Values);
            var numEdges = edgeVectors.Count;
            var axis = new Vector3();
            foreach (var edgeVector in edgeVectors) axis = axis + edgeVector;
            Axis = axis.Normalize();

            /* to adjust the Axis, we will average the cross products of the new face with all the old faces */
            //Since we will be taking cross products, we need to be sure not to have faces along the same normal
            var faces = MiscFunctions.FacesWithDistinctNormals(Faces.ToList());
            var n = faces.Count;

            //Check if the loops are circular along the axis
            var path1 = Loop1.ProjectTo2DCoordinates(Axis, out var backTransform);
            if (!path1.IsCircular(out var centerCircle, Constants.MediumConfidence))
            {
                return false;
            }
            var path2 = Loop2.ProjectTo2DCoordinates(Axis, out var backTransform2);
            if (!path2.IsCircular(out var centerCircle2, Constants.MediumConfidence))
            {
                return false;
            }
            Radius = (centerCircle.Radius + centerCircle2.Radius) / 2; //Average
            //Set the Anchor/Center point
            var center = (centerCircle.Center + centerCircle2.Center) / 2.0;
            Anchor = center.ConvertTo3DLocation(backTransform);
            return true;
        }

        /// <summary>
        /// Transforms the shape by the provided transformation matrix.
        /// </summary>
        /// <param name="transformMatrix">The transform matrix.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public override void Transform(Matrix4x4 transformMatrix)
        {
            Anchor = Anchor.Transform(transformMatrix);
            Axis = Axis.Transform(transformMatrix);

            //how to adjust the radii?
            throw new NotImplementedException();
        }

        #region Properties

        /// <summary>
        ///     Is the cylinder positive? (false is negative)
        /// </summary>
        public bool IsPositive { get; private set; }

        /// <summary>
        ///     Did the cylinder pass the cylinder checks?
        /// </summary>
        public bool IsValid { get; private set; }

        /// <summary>
        ///     Gets the anchor.
        /// </summary>
        /// <value>The anchor.</value>
        public Vector3 Anchor { get; private set; }

        /// <summary>
        ///     Gets the direction.
        /// </summary>
        /// <value>The direction.</value>
        public Vector3 Axis { get; set; }

        /// <summary>
        ///     Gets the radius.
        /// </summary>
        /// <value>The radius.</value>
        public double Radius { get; private set; }


        /// <summary>
        /// Gets or sets the maximum distance along axis.
        /// </summary>
        /// <value>The maximum distance along axis.</value>
        public double MaxDistanceAlongAxis { get; set; }

        /// <summary>
        /// Gets or sets the minimum distance along axis.
        /// </summary>
        /// <value>The minimum distance along axis.</value>
        public double MinDistanceAlongAxis { get; set; }

        /// <summary>
        /// Gets the height.
        /// </summary>
        /// <value>The height.</value>
        public double Height { get; private set; }

        /// <summary>
        /// Gets the volume.
        /// </summary>
        /// <value>The volume.</value>
        public double Volume { get; }

        public HashSet<Vertex> Loop1 { get; set; }

        public HashSet<Vertex> Loop2 { get; set; }

        public List<Edge> EdgeLoop1 { get; set; }

        public List<Edge> EdgeLoop2 { get; set; }

        public HashSet<Plane> SmallFlats { get; set; }

        public List<Vector2> Loop2D { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="Cylinder" /> class.
        /// </summary>
        /// <param name="faces">The faces all.</param>
        /// <param name="axis">The axis.</param>
        public Cylinder(IEnumerable<PolygonalFace> faces, bool buildOnlyIfHole, bool isPositive,
            HashSet<Plane> featureFlats = null) : base(faces)
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
        public Cylinder(IEnumerable<PolygonalFace> facesAll, Vector3 axis)
            : base(facesAll)
        {
            Type = PrimitiveSurfaceType.Cylinder;
            var faces = Faces.FacesWithDistinctNormals();
            var n = faces.Count;
            var centers = new List<Vector3>();
            var signedDistances = new List<double>();
            MiscFunctions.SkewedLineIntersection(faces[0].Center, faces[0].Normal,
                faces[n - 1].Center, faces[n - 1].Normal, out var center, out var t1, out var t2);
            if (!center.IsNull() && !center.IsNegligible())
            {
                centers.Add(center);
                signedDistances.Add(t1);
                signedDistances.Add(t2);
            }
            for (var i = 1; i < n; i++)
            {
                MiscFunctions.SkewedLineIntersection(faces[i].Center, faces[i].Normal,
                    faces[i - 1].Center, faces[i - 1].Normal, out center, out t1, out t2);
                if (!center.IsNull() && !center.IsNegligible())
                {
                    centers.Add(center);
                    signedDistances.Add(t1);
                    signedDistances.Add(t2);
                }
            }
            center = new Vector3();
            center = centers.Aggregate(center, (current, c) => current + c);
            center = center.Divide(centers.Count);
            /* move center to origin plane */
            var distBackToOrigin = -1 * axis.Dot(center);
            center = center - (axis * distBackToOrigin);
            /* determine is positive or negative */
            var numNeg = signedDistances.Count(d => d < 0);
            var numPos = signedDistances.Count(d => d > 0);
            var isPositive = numNeg > numPos;
            var radii = new List<double>();
            foreach (var face in faces)
                radii.AddRange(face.Vertices.Select(v => MiscFunctions.DistancePointToLine(v.Coordinates, center, axis)));
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
            var axis = edge.OwnedFace.Normal.Cross(edge.OtherFace.Normal);
            var length = axis.Length();
            if (length.IsNegligible()) throw new Exception("Edge used to define cylinder is flat.");
            axis = axis.Normalize();
            var v1 = edge.From;
            var v2 = edge.To;
            var v3 = edge.OwnedFace.Vertices.First(v => v != v1 && v != v2);
            var v4 = edge.OtherFace.Vertices.First(v => v != v1 && v != v2);
            MiscFunctions.SkewedLineIntersection(edge.OwnedFace.Center, edge.OwnedFace.Normal,
                edge.OtherFace.Center, edge.OtherFace.Normal, out var center);
            /* determine is positive or negative */
            var isPositive = edge.Curvature == CurvatureType.Convex;
            /* move center to origin plane */
            var distToOrigin = axis.Dot(center);
            if (distToOrigin < 0)
            {
                distToOrigin *= -1;
                axis = -1 * axis;
            }
            center = new Vector3(center.X - distToOrigin * axis.X, center.Y - distToOrigin * axis.Y,
                center.Z - distToOrigin * axis.Z);
            var d1 = MiscFunctions.DistancePointToLine(v1.Coordinates, center, axis);
            var d2 = MiscFunctions.DistancePointToLine(v2.Coordinates, center, axis);
            var d3 = MiscFunctions.DistancePointToLine(v3.Coordinates, center, axis);
            var d4 = MiscFunctions.DistancePointToLine(v4.Coordinates, center, axis);
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

        public Cylinder(Vector3 axis, Vector3 anchor, double radius, double dxOfBottomPlane,
            double dxOfTopPlane)
        {
            Axis = axis;
            Anchor = anchor;
            Radius = radius;
            MinDistanceAlongAxis = dxOfBottomPlane;
            MaxDistanceAlongAxis = dxOfTopPlane;
            Height = MaxDistanceAlongAxis - MinDistanceAlongAxis;
            Volume = Math.PI * Radius * Radius * Height;
        }

        //public TessellatedSolid AsTessellatedSolid()
        //{
        //    var faces = new List<PolygonalFace>();
        //    foreach(var face in Faces)
        //    {
        //        var vertices = new Vertex[] { face.C, face.B, face.A }; //reverse the vertices
        //        faces.Add(new PolygonalFace(vertices, face.Normal * -1)));
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