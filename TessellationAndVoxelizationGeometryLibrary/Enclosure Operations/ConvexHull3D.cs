// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Matt Campbell
// Created          : 01-18-2024
//
// Last Modified By : --
// Last Modified On : --
// ***********************************************************************
// <copyright file="ConvexHull3D.cs" company="Design Engineering Lab">
//     2024
// </copyright>
// <summary></summary>
// ***********************************************************************
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    /// <summary>
    /// The Convex Hull of a Tesselated Solid
    /// </summary>
    public partial class ConvexHull3D : Solid
    {
        /// <summary>
        /// The volume of the Convex Hull.
        /// </summary>
        public double tolerance { get; internal init; }

        /// <summary>
        /// The vertices of the ConvexHull
        /// </summary>
        public readonly List<Vertex> Vertices = new List<Vertex>();
        //internal readonly List<CHFace> cHFaces = new List<CHFace>();
        /// <summary>
        /// Gets the convex hull faces.
        /// </summary>
        /// <value>The convex hull faces.</value>
        public readonly List<ConvexHullFace> Faces = new List<ConvexHullFace>();

        /// <summary>
        /// Gets the convex hull edges.
        /// </summary>
        /// <value>The convex hull edges.</value>
        public List<Edge> Edges = new List<Edge>();



        /// <summary>
        /// Calculates the center.
        /// </summary>
        protected override void CalculateCenter() => Faces.CalculateVolumeAndCenter(SameTolerance, out _volume, out _center);

        /// <summary>
        /// Calculates the volume.
        /// </summary>
        protected override void CalculateVolume() => Faces.CalculateVolumeAndCenter(SameTolerance, out _volume, out _center);

        /// <summary>
        /// Calculates the surface area.
        /// </summary>
        protected override void CalculateSurfaceArea() => _surfaceArea = Faces.Sum(face => face.Area);

        /// <summary>
        /// Calculates the inertia tensor.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        protected override void CalculateInertiaTensor() => _inertiaTensor = Faces.CalculateInertiaTensor(Center);

        public override void Transform(Matrix4x4 transformMatrix)
        {
            var xMin = double.PositiveInfinity;
            var yMin = double.PositiveInfinity;
            var zMin = double.PositiveInfinity;
            var xMax = double.NegativeInfinity;
            var yMax = double.NegativeInfinity;
            var zMax = double.NegativeInfinity;
            foreach (var v in Vertices)
            {
                v.Coordinates = v.Coordinates.Transform(transformMatrix);
                if (xMin > v.Coordinates.X) xMin = v.Coordinates.X;
                if (yMin > v.Coordinates.Y) yMin = v.Coordinates.Y;
                if (zMin > v.Coordinates.Z) zMin = v.Coordinates.Z;
                if (xMax < v.Coordinates.X) xMax = v.Coordinates.X;
                if (yMax < v.Coordinates.Y) yMax = v.Coordinates.Y;
                if (zMax < v.Coordinates.Z) zMax = v.Coordinates.Z;
            }
            Bounds = [new Vector3(xMin, yMin, zMin), new Vector3(xMax, yMax, zMax)];

            //Update the faces
            foreach (var face in Faces)
            {
                face.Update();// Transform(transformMatrix);
            }
            //Update the edges
            foreach (var edge in Edges)
            {
                edge.Update(true);
            }
            _center = _center.Transform(transformMatrix);
            // I'm not sure this is right, but I'm just using the 3x3 rotational submatrix to rotate the inertia tensor
            var rotMatrix = new Matrix3x3(transformMatrix.M11, transformMatrix.M12, transformMatrix.M13,
                    transformMatrix.M21, transformMatrix.M22, transformMatrix.M23,
                    transformMatrix.M31, transformMatrix.M32, transformMatrix.M33);
            _inertiaTensor *= rotMatrix;
        }

        public override Solid TransformToNewSolid(Matrix4x4 transformationMatrix)
        {
            throw new System.NotImplementedException();
        }

        public bool IsInside(Vector3 point, double offset = 0)
        {
            foreach (var face in Faces)
                if ((point - face.A.Coordinates).Dot(face.Normal) > offset) 
                    return false;
            return true;
        }
    }
    public class ConvexHullFace : TriangleFace
    {
        internal ConvexHullFace(Vertex vertex1, Vertex vertex2, Vertex vertex3, Vector3 planeNormal) : this(vertex1, vertex2, vertex3)
        {
            _normal = planeNormal;
        }
        internal ConvexHullFace(Vertex vertex1, Vertex vertex2, Vertex vertex3) : base(vertex1, vertex2, vertex3, false)
        {
            peakVertex = null;
            InteriorVertices = new List<Vertex>();
            PartOfConvexHull = true; // this is redundant but since it is a convex hull face, it is true. and would be confusing to leave it false.
        }

        internal Vertex peakVertex { get; set; }
        internal double peakDistance { get; set; }

        /// <summary>
        /// Gets the collection of vertices that are on the boundary of the convex hull but are not actively effecting the boundary representation
        /// of the convex hull
        /// </summary>
        public List<Vertex> InteriorVertices { get; internal set; }
        internal bool Visited { get; set; }
    }
}
