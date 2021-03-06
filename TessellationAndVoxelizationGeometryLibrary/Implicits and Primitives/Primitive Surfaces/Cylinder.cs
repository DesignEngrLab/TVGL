﻿// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
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
        /// Transforms the shape by the provided transformation matrix.
        /// </summary>
        /// <param name="transformMatrix">The transform matrix.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public override void Transform(Matrix4x4 transformMatrix)
        {
            Anchor = Anchor.Transform(transformMatrix);
            Axis = Axis.Transform(transformMatrix);
            Axis = Axis.Normalize();
            //how to adjust the radii?
            //throw new NotImplementedException();
        }

        public override double CalculateError(IEnumerable<Vector3> vertices = null)
        {
            if (vertices == null)
            {
                vertices = new List<Vector3>();
                vertices = Vertices.Select(v => v.Coordinates).ToList();
                ((List<Vector3>)vertices).AddRange(InnerEdges.Select(edge => (edge.To.Coordinates + edge.From.Coordinates) / 2));
                ((List<Vector3>)vertices).AddRange(OuterEdges.Select(edge => (edge.To.Coordinates + edge.From.Coordinates) / 2));
            }
            var sqDistanceSum = 0.0;
            var numVerts = 0;
            foreach (var c in vertices)
            {
                var d = (c - Anchor).Cross(Axis).Length() - Radius;
                sqDistanceSum += d * d;
                numVerts++;
            }
            return sqDistanceSum / numVerts;
        }

        #region Properties

        /// <summary>
        ///     Is the cylinder positive? (false is negative)
        /// </summary>
        public bool IsPositive { get; }


        /// <summary>
        ///     Gets the anchor.
        /// </summary>
        /// <value>The anchor.</value>
        public Vector3 Anchor { get; set; }
        /// <summary>
        ///     Gets the direction.
        /// </summary>
        /// <value>The direction.</value>
        public Vector3 Axis { get; set; }

        /// <summary>
        ///     Gets the radius.
        /// </summary>
        /// <value>The radius.</value>
        public double Radius { get; set; }


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
        public double Height { get; set; }

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
            //SmallFlats = featureFlats;
            //IsValid = BuildIfCylinderIsHole(isPositive);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Cylinder" /> class.
        /// </summary>
        /// <param name="facesAll">The faces all.</param>
        /// <param name="axis">The axis.</param>
        public Cylinder(IEnumerable<PolygonalFace> facesAll, Vector3 axis)
            : base(facesAll)
        {
            var faces = Faces.FacesWithDistinctNormals();
            var n = faces.Count;
            var centers = new List<Vector3>();
            var signedDistances = new List<double>();
            MiscFunctions.SkewedLineIntersection(faces[0].Center, faces[0].Normal,
                faces[n - 1].Center, faces[n - 1].Normal, out var center, out _, out _,
                out var t1, out var t2);
            if (!center.IsNull() && !center.IsNegligible())
            {
                centers.Add(center);
                signedDistances.Add(t1);
                signedDistances.Add(t2);
            }
            for (var i = 1; i < n; i++)
            {
                MiscFunctions.SkewedLineIntersection(faces[i].Center, faces[i].Normal,
                    faces[i - 1].Center, faces[i - 1].Normal, out center, out _, out _,
                    out t1, out t2);
                if (!center.IsNull() && !center.IsNegligible())
                {
                    centers.Add(center);
                    signedDistances.Add(t1);
                    signedDistances.Add(t2);
                }
            }
            center = Vector3.Zero;
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

        internal Cylinder() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Cylinder"/> class.
        /// </summary>
        /// <param name="axis">The axis.</param>
        /// <param name="anchor">The anchor.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="dxOfBottomPlane">The dx of bottom plane.</param>
        /// <param name="dxOfTopPlane">The dx of top plane.</param>
        public Cylinder(Vector3 axis, Vector3 anchor, double radius, double minDistanceAlongAxis,
            double maxDistanceAlongAxis)
        {
            Axis = axis;
            Anchor = anchor;
            Radius = radius;
            MinDistanceAlongAxis = minDistanceAlongAxis;
            MaxDistanceAlongAxis = maxDistanceAlongAxis;
            Height = MaxDistanceAlongAxis - MinDistanceAlongAxis;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Cylinder"/> class.
        /// </summary>
        /// <param name="axis">The axis.</param>
        /// <param name="anchor">The anchor.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="isPositive">if set to <c>true</c> [is positive].</param>
        /// <param name="faces">The faces.</param>
        public Cylinder(Vector3 axis, Vector3 anchor, double radius, bool isPositive, IEnumerable<PolygonalFace> faces) : base(faces)
        {
            Axis = axis;
            Anchor = anchor;
            Radius = radius;
            IsPositive = isPositive;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Cylinder"/> class.
        /// </summary>
        /// <param name="axis">The axis.</param>
        /// <param name="anchor">The anchor.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="isPositive">if set to <c>true</c> [is positive].</param>
        public Cylinder(Vector3 axis, Vector3 anchor, double radius, bool isPositive)
        {
            Axis = axis;
            Anchor = anchor;
            Radius = radius;
            IsPositive = isPositive;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Cylinder"/> class.
        /// </summary>
        /// <param name="originalToBeCopied">The original to be copied.</param>
        public Cylinder(Cylinder originalToBeCopied, TessellatedSolid copiedTessellatedSolid = null)
            : base(originalToBeCopied, copiedTessellatedSolid)
        {
            Axis = originalToBeCopied.Axis;
            Anchor = originalToBeCopied.Anchor;
            Radius = originalToBeCopied.Radius;
            IsPositive = originalToBeCopied.IsPositive;
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