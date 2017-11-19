// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 09-21-2017
// ***********************************************************************
// <copyright file="VoxelizedSolid.cs" company="Design Engineering Lab">
//     Copyright ©  2017
// </copyright>
// <summary></summary>
// ***********************************************************************

using StarMathLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace TVGL.Voxelization
{
    /// <summary>
    /// Class VoxelizedSolid.
    /// </summary>
    public class VoxelizedSolid
    {
        #region Properties
        /// <summary>
        /// The discretization
        /// </summary>
        public VoxelDiscretization Discretization { get; private set; }
        /// <summary>
        /// Gets the number voxels total.
        /// </summary>
        /// <value>
        /// The number voxels total.
        /// </value>
        public int NumVoxelsTotal => voxelHashSetLevel0.Count + voxelDictionaryLevel0.Values.Sum(voxel => voxel.Count());
        /// <summary>
        /// Gets the number voxels.
        /// </summary>
        /// <value>
        /// The number voxels.
        /// </value>
        public int[] NumVoxels { get; private set; }
        /// <summary>
        /// The voxel side length. It's a square, so all sides are the same length.
        /// </summary>
        public double VoxelSideLength { get; private set; }
        /// <summary>
        /// Gets the scale to integer space.
        /// </summary>
        /// <value>The scale to int space.</value>
        public double ScaleToIntegerSpace { get; private set; }
        /// <summary>
        /// Gets the offset that moves the model s.t. the lowest elements are at 0,0,0.
        /// </summary>
        /// <value>
        /// The offset.
        /// </value>
        public double[] Offset { get; private set; }
        #endregion

        #region Private Fields
        private readonly double[][] transformedCoordinates;
        private int longestDimensionIndex;
        private readonly Dictionary<long, Voxel> voxelDictionaryLevel0;
        private readonly Dictionary<long, Voxel> voxelDictionaryLevel1;
        private readonly HashSet<long> voxelHashSetLevel0;
        #endregion

        #region Constructor from Tessellated Solid (the "makeVoxels..." functions)
        /// <summary>
        /// Initializes a new instance of the <see cref="VoxelizedSolid" /> class.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="voxelDiscretization">The voxel discretization.</param>
        /// <param name="onlyDefineBoundary">if set to <c>true</c> [only define boundary].</param>
        public VoxelizedSolid(TessellatedSolid ts, VoxelDiscretization voxelDiscretization, bool onlyDefineBoundary = false)
        {
            var level = 0;
            // tessellatedSolid = ts;
            Discretization = voxelDiscretization;
            SetUpIndexingParameters(ts);
            voxelDictionaryLevel0 = new Dictionary<long, Voxel>(); //todo:approximate capacity based on tessellated volume
            voxelHashSetLevel0 = new HashSet<long>();
            transformedCoordinates = new double[ts.NumberOfVertices][];
            Parallel.For(0, ts.NumberOfVertices, i =>
            {
                var vertex = ts.Vertices[i];
                var coordinates = vertex.Position.multiply(ScaleToIntegerSpace).add(Offset);
                transformedCoordinates[i] = coordinates;
                makeVoxelForVertexLevel0And1(vertex, coordinates);
            });
            makeVoxelsForFacesAndEdges(ts);
            if (!onlyDefineBoundary)
                makeVoxelsInInterior();
            makeVoxelsInInterior();
        }

        /// <summary>
        /// Sets up indexing parameters.
        /// </summary>
        /// <param name="ts"></param>
        /// <exception cref="System.Exception">Int64 will not work for a voxel space this large, using the current index setup</exception>
        private void SetUpIndexingParameters(TessellatedSolid ts)
        {
            var dimensions = new double[3];
            for (int i = 0; i < 3; i++)
                dimensions[i] = ts.Bounds[1][i] - ts.Bounds[0][i];
            var maxDim = dimensions.Max();
            longestDimensionIndex = dimensions.FindIndex(d => d == maxDim);
            VoxelSideLength = maxDim / (int)Discretization;
            ScaleToIntegerSpace = 1.0 / VoxelSideLength;
            Offset = ts.Bounds[0].multiply(ScaleToIntegerSpace);
        }
        #region Making Voxels for Levels 0 and 1
        private void makeVoxelForVertexLevel0And1(Vertex vertex, double[] coordinates)
        {
            int x, y, z;
            if (coordinates.Any(atIntegerValue))
            {
                var edgeVectors = vertex.Edges.Select(e => e.To == vertex ? e.Vector : e.Vector.multiply(-1));
                if (edgeVectors.All(ev => ev[0] >= 0))
                    x = (int)(coordinates[0] - 1);
                else x = (int)Math.Floor(coordinates[0]);
                if (edgeVectors.All(ev => ev[1] >= 0))
                    y = (int)(coordinates[1] - 1);
                else y = (int)Math.Floor(coordinates[1]);
                if (edgeVectors.All(ev => ev[2] >= 0))
                    z = (int)(coordinates[2] - 1);
                else z = (int)Math.Floor(coordinates[2]);
            }
            else
            {
                //Gets the integer coordinates, rounded down for the point.
                x = (int)Math.Floor(coordinates[0]);
                y = (int)Math.Floor(coordinates[1]);
                z = (int)Math.Floor(coordinates[2]);
            }
            MakeAndStoreVoxelLevel0And1(x, y, z, vertex);
        }

        #region the Level 0 and 1 Face and Edge Sweep Functions
        /// <summary>
        /// Makes the voxels for faces and edges. This is a complicated function! Originally, the scheme used in 
        /// OpenVDB was employed where a voxel group (3x3x3) is initiated at one of the vertices of a face so that 
        /// it is guaranteed to intersect with the face. Then it performs a depth first search outward and makes 
        /// many calls to the closest point on the triangle function (see under proximity). This was implemented
        /// in https://github.com/DesignEngrLab/TVGL/commit/b366f25fa8be05a6d75e0272ff1efb15660880d9 but it was
        /// shown to be 10 times slower than the method devised here. This method simply follows the edges, 
        /// which are obviously straight-lines in space to see what voxels it passes through. For the faces, 
        /// these are done in a sweep across the face progressively with the creation of the edge voxels.
        /// Details of this method are to be presented
        /// on the wiki page: https://github.com/DesignEngrLab/TVGL/wiki/Creating-Voxels-from-Tessellation
        /// </summary>
        /// <param name="linkToTessellatedSolid">if set to <c>true</c> [link to tessellated solid].</param>
        private void makeVoxelsForFacesAndEdges(TessellatedSolid tessellatedSolid)
        {
            foreach (var face in tessellatedSolid.Faces) //loop over the faces
            {
                if (simpleCase(face)) continue;
                Vertex startVertex, leftVertex, rightVertex;
                Edge leftEdge, rightEdge;
                int uDim, vDim, sweepDim;
                double maxSweepValue;
                setUpFaceSweepDetails(face, out startVertex, out leftVertex, out rightVertex, out leftEdge,
                    out rightEdge, out uDim, out vDim,
                    out sweepDim, out maxSweepValue);
                var leftStartPoint = (double[])transformedCoordinates[startVertex.IndexInList].Clone();
                var rightStartPoint = (double[])leftStartPoint.Clone();
                var sweepValue = (int)(atIntegerValue(leftStartPoint[sweepDim])
                    ? leftStartPoint[sweepDim] + 1
                    : Math.Ceiling(leftStartPoint[sweepDim]));
                var leftEndPoint = transformedCoordinates[leftVertex.IndexInList];
                var rightEndPoint = transformedCoordinates[rightVertex.IndexInList];

                // the following 2 booleans determine if the edge needs to be voxelized as it may have
                // been done in a previous visited face
                var voxelizeLeft = leftEdge.Voxels == null || !leftEdge.Voxels.Any();
                var voxelizeRight = rightEdge.Voxels == null || !rightEdge.Voxels.Any();
                while (sweepValue <= maxSweepValue) // this is the sweep along the face
                {
                    // first fill in any voxels for face between the start points. Why do this here?! These 2 lines of code  were
                    // the last added. There are cases (not in the first loop, mind you) where it is necessary. Note this happens again
                    // at the bottom of the while-loop for the same sweep value, but for the next startpoints
                    makeVoxelsAlongLineInPlane(leftStartPoint[uDim], leftStartPoint[vDim], rightStartPoint[uDim],
                        rightStartPoint[vDim], sweepValue, uDim, vDim,
                        sweepDim, face.Normal[vDim] >= 0, face);
                    makeVoxelsAlongLineInPlane(leftStartPoint[vDim], leftStartPoint[uDim], rightStartPoint[vDim],
                        rightStartPoint[uDim], sweepValue, vDim, uDim,
                        sweepDim, face.Normal[uDim] >= 0, face);
                    // now two big calls for the edges: one for the left edge and one for the right. by the way, the naming of left and right are 
                    // completely arbitrary here. They are not indicative of any real position.
                    voxelizeLeft = makeVoxelsForEdgeWithinSweep(ref leftStartPoint, ref leftEndPoint, sweepValue,
                        sweepDim, uDim, vDim, voxelizeLeft, leftEdge, face, rightEndPoint, startVertex);
                    voxelizeRight = makeVoxelsForEdgeWithinSweep(ref rightStartPoint, ref rightEndPoint, sweepValue,
                        sweepDim, uDim, vDim, voxelizeRight, rightEdge, face, leftEndPoint, startVertex);
                    // now that the end points of the edges have moved, fill in more of the faces.
                    makeVoxelsAlongLineInPlane(leftStartPoint[uDim], leftStartPoint[vDim], rightStartPoint[uDim],
                        rightStartPoint[vDim], sweepValue, uDim, vDim,
                        sweepDim, face.Normal[vDim] >= 0, face);
                    makeVoxelsAlongLineInPlane(leftStartPoint[vDim], leftStartPoint[uDim], rightStartPoint[vDim],
                        rightStartPoint[uDim], sweepValue, vDim, uDim,
                        sweepDim, face.Normal[uDim] >= 0, face);
                    sweepValue++; //increment sweepValue and repeat!
                }
            }
        }

        /// <summary>
        /// If it is a simple case, just solve it and return true.
        /// </summary>
        /// <param name="face">The face.</param>
        /// <param name="level">The level.</param>
        /// <returns>
        ///   <c>true</c> if XXXX, <c>false</c> otherwise.
        /// </returns>
        private bool simpleCase(PolygonalFace face)
        {
            int level = 1; //
            var faceAVoxel = face.A.Voxels.First(v => v.Level == level);
            var faceBVoxel = face.B.Voxels.First(v => v.Level == level);
            var faceCVoxel = face.C.Voxels.First(v => v.Level == level);
            // The first simple case is that all vertices are within the same voxel. 
            if (faceAVoxel.Equals(faceBVoxel) && faceAVoxel.Equals(faceCVoxel))
            {
                var voxel = face.A.Voxels.First(v => v.Level == level);
                voxel.Add(face);
                foreach (var edge in face.Edges)
                    voxel.Add(edge);
                return true;
            }
            var faceAVoxelID = faceAVoxel.ID;
            var faceBVoxelID = faceBVoxel.ID;
            var faceCVoxelID = faceCVoxel.ID;
            // the second, third, and fourth simple cases are if the triangle
            // fits within a line of voxels.
            // this condition checks that all voxels have same x & y values (hence aligned in z-direction)
            if (GetCoordinateFromID(faceAVoxelID, 0, level) == GetCoordinateFromID(faceBVoxelID, 0, level) &&
                GetCoordinateFromID(faceAVoxelID, 0, level) == GetCoordinateFromID(faceCVoxelID, 0, level) &&
                GetCoordinateFromID(faceAVoxelID, 1, level) == GetCoordinateFromID(faceBVoxelID, 1, level) &&
                GetCoordinateFromID(faceAVoxelID, 1, level) == GetCoordinateFromID(faceCVoxelID, 1, level))
            {
                makeVoxelsForFaceInCardinalLine(face, 2, level);
                return true;
            }
            // this condition checks that all voxels have same x & z values (hence aligned in y-direction)
            if (GetCoordinateFromID(faceAVoxelID, 0, level) == GetCoordinateFromID(faceBVoxelID, 0, level) &&
                GetCoordinateFromID(faceAVoxelID, 0, level) == GetCoordinateFromID(faceCVoxelID, 0, level) &&
                GetCoordinateFromID(faceAVoxelID, 2, level) == GetCoordinateFromID(faceBVoxelID, 2, level) &&
                GetCoordinateFromID(faceAVoxelID, 2, level) == GetCoordinateFromID(faceCVoxelID, 2, level))
            {
                makeVoxelsForFaceInCardinalLine(face, 1, level);
                return true;
            }
            // this condition checks that all voxels have same y & z values (hence aligned in x-direction)
            if (GetCoordinateFromID(faceAVoxelID, 1, level) == GetCoordinateFromID(faceBVoxelID, 1, level) &&
                GetCoordinateFromID(faceAVoxelID, 1, level) == GetCoordinateFromID(faceCVoxelID, 1, level) &&
                GetCoordinateFromID(faceAVoxelID, 2, level) == GetCoordinateFromID(faceBVoxelID, 2, level) &&
                GetCoordinateFromID(faceAVoxelID, 2, level) == GetCoordinateFromID(faceCVoxelID, 2, level))
            {
                makeVoxelsForFaceInCardinalLine(face, 0, level);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Makes the voxels for face in cardinal line. This is only called by the preceding function, simpleCase.
        /// </summary>
        /// <param name="face">The face.</param>
        /// <param name="dim">The dim.</param>
        /// <param name="linkToTessellatedSolid"></param>
        private void makeVoxelsForFaceInCardinalLine(PolygonalFace face, int dim, int level)
        {
            var coordA = GetCoordinateFromID(face.A.Voxels.First(v => v.Level == level).ID, dim, level);
            var coordB = GetCoordinateFromID(face.B.Voxels.First(v => v.Level == level).ID, dim, level);
            var coordC = GetCoordinateFromID(face.C.Voxels.First(v => v.Level == level).ID, dim, level);
            var minCoord = coordA;
            var maxCoord = coordA;
            if (coordB < minCoord) minCoord = coordB;
            else if (coordB > maxCoord) maxCoord = coordB;
            if (coordC < minCoord) minCoord = coordC;
            else if (coordC > maxCoord) maxCoord = coordC;
            var coordinates = GetCoordinatesFromID(face.A.Voxels.First(v => v.Level == level).ID, level);
            for (int i = minCoord; i <= maxCoord; i++)
            {   // set up voxels for the face
                coordinates[dim] = i;
                MakeAndStoreVoxelLevel0And1(coordinates[0], coordinates[1], coordinates[2], face);
            }
            foreach (var faceEdge in face.Edges)
            {   // cycle over the edges to link them to the voxels
                var fromIndex = GetCoordinateFromID(faceEdge.From.Voxels.First(v => v.Level == level).ID, dim, level);
                var toIndex = GetCoordinateFromID(faceEdge.To.Voxels.First(v => v.Level == level).ID, dim, level);
                var step = Math.Sign(toIndex - fromIndex);
                for (int i = fromIndex; i <= toIndex; i += step)
                {
                    coordinates[dim] = i;
                    MakeAndStoreVoxelLevel0And1(coordinates[0], coordinates[1], coordinates[2], faceEdge);
                }
            }
        }


        /// <summary>
        /// Sets up face sweep details such as which dimension to sweep over and assign start and end points
        /// </summary>
        /// <param name="face">The face.</param>
        /// <param name="startVertex">The start vertex.</param>
        /// <param name="leftVertex">The left vertex.</param>
        /// <param name="rightVertex">The right vertex.</param>
        /// <param name="leftEdge">The left edge.</param>
        /// <param name="rightEdge">The right edge.</param>
        /// <param name="uDim">The u dim.</param>
        /// <param name="vDim">The v dim.</param>
        /// <param name="maxSweepValue">The maximum sweep value.</param>
        private void setUpFaceSweepDetails(PolygonalFace face, out Vertex startVertex, out Vertex leftVertex,
            out Vertex rightVertex, out Edge leftEdge, out Edge rightEdge, out int uDim, out int vDim, out int sweepDim,
            out double maxSweepValue)
        {
            var xLength = Math.Max(Math.Max(Math.Abs(face.A.X - face.B.X), Math.Abs(face.B.X - face.C.X)),
                Math.Abs(face.C.X - face.A.X));
            var yLength = Math.Max(Math.Max(Math.Abs(face.A.Y - face.B.Y), Math.Abs(face.B.Y - face.C.Y)),
                Math.Abs(face.C.Y - face.A.Y));
            var zLength = Math.Max(Math.Max(Math.Abs(face.A.Z - face.B.Z), Math.Abs(face.B.Z - face.C.Z)),
                Math.Abs(face.C.Z - face.A.Z));
            sweepDim = 0;
            uDim = 1;
            vDim = 2;
            if (yLength > xLength)
            {
                sweepDim = 1;
                uDim = 2;
                vDim = 0;
            }
            if (zLength > yLength && zLength > xLength)
            {
                sweepDim = 2;
                uDim = 0;
                vDim = 1;
            }
            startVertex = face.A;
            leftVertex = face.B;
            leftEdge = face.Edges[0];
            rightVertex = face.C;
            rightEdge = face.Edges[2];
            maxSweepValue = (int)Math.Ceiling(Math.Max(transformedCoordinates[face.B.IndexInList][sweepDim],
                transformedCoordinates[face.C.IndexInList][sweepDim]));
            if (face.B.Position[sweepDim] < face.A.Position[sweepDim])
            {
                startVertex = face.B;
                leftVertex = face.C;
                leftEdge = face.Edges[1];
                rightVertex = face.A;
                rightEdge = face.Edges[0];
                maxSweepValue = (int)Math.Ceiling(Math.Max(transformedCoordinates[face.C.IndexInList][sweepDim],
                    transformedCoordinates[face.A.IndexInList][sweepDim]));
            }
            if (face.C.Position[sweepDim] < face.B.Position[sweepDim] &&
                face.C.Position[sweepDim] < face.A.Position[sweepDim])
            {
                startVertex = face.C;
                leftVertex = face.A;
                leftEdge = face.Edges[2];
                rightVertex = face.B;
                rightEdge = face.Edges[1];
                maxSweepValue = (int)Math.Ceiling(Math.Max(transformedCoordinates[face.A.IndexInList][sweepDim],
                    transformedCoordinates[face.B.IndexInList][sweepDim]));
            }
        }

        /// <summary>
        /// Makes the voxels for edge within sweep.
        /// </summary>
        /// <param name="startPoint">The start point.</param>
        /// <param name="endPoint">The end point.</param>
        /// <param name="sweepValue">The sweep value.</param>
        /// <param name="sweepDim">The sweep dim.</param>
        /// <param name="uDim">The u dim.</param>
        /// <param name="vDim">The v dim.</param>
        /// <param name="linkToTessellatedSolid">if set to <c>true</c> [link to tessellated solid].</param>
        /// <param name="voxelize">if set to <c>true</c> [voxelize].</param>
        /// <param name="edge">The edge.</param>
        /// <param name="face">The face.</param>
        /// <param name="nextEndPoint">The next end point.</param>
        /// <param name="startVertex">The start vertex.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private bool makeVoxelsForEdgeWithinSweep(ref double[] startPoint, ref double[] endPoint, int sweepValue,
            int sweepDim, int uDim, int vDim, bool voxelize, Edge edge, PolygonalFace face, double[] nextEndPoint,
            Vertex startVertex)
        {
            double u, v;
            var reachedOtherVertex = findWhereLineCrossesPlane(startPoint, endPoint, sweepDim,
                sweepValue, out u, out v);
            if (voxelize)
            {
                makeVoxelsAlongLineInPlane(startPoint[uDim], startPoint[vDim], u, v,
                    sweepValue, uDim, vDim, sweepDim, face.Normal[vDim] >= 0, edge);
                makeVoxelsAlongLineInPlane(startPoint[vDim], startPoint[uDim], v, u,
                    sweepValue, vDim, uDim, sweepDim, face.Normal[uDim] >= 0, edge);
            }
            if (reachedOtherVertex)
            {
                startPoint = (double[])endPoint.Clone();
                endPoint = nextEndPoint;
                edge = face.OtherEdge(startVertex);
                voxelize = edge.Voxels == null || !edge.Voxels.Any();

                findWhereLineCrossesPlane(startPoint, endPoint, sweepDim,
                    sweepValue, out u, out v);
                if (voxelize)
                {
                    makeVoxelsAlongLineInPlane(startPoint[uDim], startPoint[vDim], u, v,
                        sweepValue, uDim, vDim, sweepDim, face.Normal[vDim] >= 0, edge);
                    makeVoxelsAlongLineInPlane(startPoint[vDim], startPoint[uDim], v, u,
                        sweepValue, vDim, uDim, sweepDim, face.Normal[uDim] >= 0, edge);
                }
            }
            startPoint[uDim] = u;
            startPoint[vDim] = v;
            startPoint[sweepDim] = sweepValue;
            return voxelize;
        }

        /// <summary>
        /// Makes the voxels along line in plane. This is a tricky little function that took a lot of debugging.
        /// At this point, we are looking at a simple 2D problem. The startU, startV, endU, and endV are the
        /// local/barycentric coordinates and correspond to actual x, y, and z via the uDim, and yDim. The
        /// out-of-plane dimension is the sweepDim and its value (sweepValue) are provided as "read only" parameters.
        /// - meaning they are given as constants that are used only to define the voxels. This method works by 
        /// finding where the v values are that cross the u integer lines. There are some subtle
        /// issues working in the negative direction (indicated by uRange and vRange) as you need to create the voxel
        /// at the next lower integer cell - not the one specified by the line crossing.
        /// </summary>
        /// <param name="startU">The start x.</param>
        /// <param name="startV">The start y.</param>
        /// <param name="endU">The end x.</param>
        /// <param name="endV">The end y.</param>
        /// <param name="sweepValue">The value sweep dimension.</param>
        /// <param name="uDim">The index of the u dimension.</param>
        /// <param name="vDim">The index of the v dimension.</param>
        /// <param name="sweepDim">The index of the sweep dimension.</param>
        /// <param name="insideIsLowerV">if set to <c>true</c> [then the inside of the part is on the lower-side of the v-value].</param>
        /// <param name="tsObject">The ts object.</param>
        private void makeVoxelsAlongLineInPlane(double startU, double startV, double endU, double endV, int sweepValue,
            int uDim, int vDim, int sweepDim, bool insideIsLowerV, TessellationBaseClass tsObject)
        {
            var uRange = endU - startU;
            if (uRange.IsNegligible()) return;
            var increment = Math.Sign(uRange);
            var u = atIntegerValue(startU) && uRange <= 0 ? (int)(startU - 1) : (int)Math.Floor(startU);
            // if you are starting an integer value for u, but you're going in the negative directive, then decrement u to the next lower value
            // this is because voxels are defined by their lowest index values.
            var vRange = endV - startV;
            var v = atIntegerValue(startV) && (vRange < 0 || (vRange == 0 && insideIsLowerV)) ? (int)(startV - 1) : (int)Math.Floor(startV);
            // likewise for v. if you are starting an integer value and you're going in the negative directive, OR if you're at an integer
            // value and happen to be vertical line then decrement v to the next lower value
            var ijk = new int[3];
            ijk[sweepDim] = sweepValue - 1;
            while (increment * u < increment * endU)
            {
                ijk[uDim] = u;
                ijk[vDim] = v;
                MakeAndStoreVoxelLevel0And1(ijk[0], ijk[1], ijk[2], tsObject);
                // now move to the next increment, of course, you may not use it if the while condition is not met
                u += increment;
                var vDouble = vRange * (u - startU) / uRange + startV;
                v = atIntegerValue(vDouble) && (vRange < 0 || (vRange == 0 && insideIsLowerV)) ? (int)(vDouble - 1) : (int)Math.Floor(vDouble);
            }
        }


        /// <summary>
        /// Finds the where line that is the edge length crosses sweep plane. It may be that the edge terminates
        /// before the plane. If that is the case, then this function returns true to inform the big loop above.
        /// </summary>
        /// <param name="startPoint">The start point.</param>
        /// <param name="endPoint">The end point.</param>
        /// <param name="sweepDim">The sweep dim.</param>
        /// <param name="valueSweepDim">The value sweep dim.</param>
        /// <param name="valueD1">The value d1.</param>
        /// <param name="valueD2">The value d2.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private bool findWhereLineCrossesPlane(double[] startPoint, double[] endPoint, int sweepDim, double valueSweepDim, out double valueD1, out double valueD2)
        {
            if (endPoint[sweepDim] <= valueSweepDim)
            {
                valueD1 = endPoint[(sweepDim + 1) % 3];
                valueD2 = endPoint[(sweepDim + 2) % 3];
                return true;
            }
            var fraction = (valueSweepDim - startPoint[sweepDim]) / (endPoint[sweepDim] - startPoint[sweepDim]);
            var dim = (sweepDim + 1) % 3;
            valueD1 = fraction * (endPoint[dim] - startPoint[dim]) + startPoint[dim];
            dim = (dim + 1) % 3;
            valueD2 = fraction * (endPoint[dim] - startPoint[dim]) + startPoint[dim];
            return false;
        }


        #endregion
        #region Level 0 and 1 Interior Voxel Creation
        /// <summary>
        /// Makes the voxels in interior.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        private void makeVoxelsInInterior()
        {
            var sweepDim = longestDimensionIndex;
            var ids = voxelDictionaryLevel1.Values.Select(vx => MakeCoordinateZero(vx.ID, sweepDim))
                .Distinct()
                .AsParallel();
            var dict = ids.ToDictionary(id => id, id => new Tuple<SortedSet<Voxel>, SortedSet<Voxel>>(
                new SortedSet<Voxel>(new SortByVoxelIndex(sweepDim)),
                new SortedSet<Voxel>(new SortByVoxelIndex(sweepDim))));
            Parallel.ForEach(voxelDictionaryLevel0.Values, voxel =>
            {
                var id = MakeCoordinateZero(voxel.ID, sweepDim);
                var sortedSets = dict[id];
                var negativeFaceVoxels = sortedSets.Item1;
                var positiveFaceVoxels = sortedSets.Item2;
                var faces = voxel.Faces;
                if (faces.Any(f => f.Normal[sweepDim] >= 0))
                    lock (positiveFaceVoxels) positiveFaceVoxels.Add(voxel);
                if (faces.Any(f => f.Normal[sweepDim] <= 0))
                    lock (negativeFaceVoxels) negativeFaceVoxels.Add(voxel);
            });
            var interiorVoxels = dict.Values
                .Where(entry => entry.Item1.Any() && entry.Item2.Any())
                .SelectMany(entry => MakeInteriorVoxelsAlongLine(entry.Item1, entry.Item2, sweepDim))
                .AsParallel();
            foreach (var interiorVoxel in interiorVoxels)
            {
                if (voxelDictionaryLevel1.ContainsKey(interiorVoxel.ID)) continue; // why would this happen though?
                voxelDictionaryLevel1.Add(interiorVoxel.ID, interiorVoxel);
                var containingVoxel = voxelDictionaryLevel0[GetContainingVoxel(interiorVoxel.ID, 0)];
                containingVoxel.Add(interiorVoxel.ID);
            }
        }


        private List<Voxel> MakeInteriorVoxelsAlongLine(SortedSet<Voxel> sortedNegatives,
            SortedSet<Voxel> sortedPositives, int sweepDim)
        {
            var baseID = sortedNegatives.First().ID;
            var newVoxels = new List<Voxel>();
            var negativeQueue = new Queue<Voxel>(sortedNegatives);
            var positiveQueue = new Queue<Voxel>(sortedPositives);
            while (negativeQueue.Any() && positiveQueue.Any())
            {
                var startIndex = GetCoordinateFromID(negativeQueue.Dequeue().ID, sweepDim, 1);
                if (negativeQueue.Any() && GetCoordinateFromID(negativeQueue.Peek().ID, sweepDim, 1) - startIndex <= 1) continue;
                int endIndex = Int32.MinValue;
                while (endIndex < startIndex && positiveQueue.Any())
                    endIndex = GetCoordinateFromID(positiveQueue.Dequeue().ID, sweepDim, 1);
                for (int i = startIndex + 1; i < endIndex; i++)
                {
                    var voxelID = ChangeCoordinate(baseID, i, sweepDim);
                    newVoxels.Add(new Voxel(voxelID, VoxelRoleTypes.Full, 1));
                }
            }
            return newVoxels;
        }
        #endregion

        private void MakeAndStoreVoxelLevel0And1(int x, int y, int z, TessellationBaseClass tsObject)
        {
            var voxelID = MakeVoxelID(x, y, z, 0, VoxelRoleTypes.Partial);
            if (voxelHashSetLevel0.Contains(voxelID))
            {
                // the level 0 voxel is already made, just add the tsObject to it
                var voxelLevel0 = voxelDictionaryLevel0[voxelID];
                voxelLevel0.Add(tsObject);
                voxelID = MakeVoxelID(x, y, z, 1, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial);
                if (voxelLevel0.Contains(voxelID))
                {
                    // and the level1 voxel is already made, so just add the tsObject there as well
                    voxelDictionaryLevel1[voxelID].Add(tsObject);
                }
                else
                {
                    // okay, the voxelLevel0 exists, but not the voxelLevel1
                    voxelLevel0.Add(voxelID);
                    voxelDictionaryLevel1.Add(voxelID, new Voxel(voxelID, VoxelRoleTypes.Partial, 1, tsObject));
                }
            }
            else
            {
                //voxel at level 0 doesn't exist, which means the voxel at level 1 also doesn't exist. Add both
                voxelHashSetLevel0.Add(voxelID);
                var voxelLevel0 = new Voxel(voxelID, VoxelRoleTypes.Partial, 0, tsObject);
                voxelDictionaryLevel0.Add(voxelID, voxelLevel0);
                voxelID = MakeVoxelID(x, y, z, 1, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial);
                voxelLevel0.Add(voxelID);
                voxelDictionaryLevel1.Add(voxelID, new Voxel(voxelID, VoxelRoleTypes.Partial, 1, tsObject));
            }
        }


        #endregion


        #endregion

        #region converting IDs and back again
        private static long MakeVoxelID(int x, int y, int z, int level, params VoxelRoleTypes[] levels)
        {
            var shift = 4 * (4 - level);
            x = x >> shift;
            y = y >> shift;
            z = z >> shift;
            x = x << (44 + shift); //can't you combine with the above? no. The shift is doing both division
            y = y << (24 + shift); // and remainder. What I mean to say is that e.g. 7>>2<<2 = 4
            z = z << (4 + shift);
            //    x0   x1    x2   x3    x4   y0   y1    y2   y3    y4    z0   z1    z2   z3    z4  flags
            // ||----|----||----|----||----|----||----|----||----|----||----|----||----|----||----|----|
            // 64   60    56    52   48    44   40    36   32    28   24    20   16    12    8    4
            return x + y + z + SetRoleFlags(levels);
        }

        internal static byte SetRoleFlags(VoxelRoleTypes[] levels)
        {
            if (levels == null || !levels.Any()) return 0; //no role is specified
            if (levels[0] == VoxelRoleTypes.Empty) return 1; //the rest of the levels would also be empty
            if (levels[0] == VoxelRoleTypes.Full) return 2;  // the rest of the levels would also be full
            if (levels[0] == VoxelRoleTypes.Partial && levels.Length == 1) return 3;
            // level 0 is partial but the smaller voxels could be full, empty of partial. 
            // they are not specified if the length is only one. If the length is more
            // than 1, then go to next level
            if (levels[1] == VoxelRoleTypes.Empty) return 4; //the rest are empty
            if (levels[1] == VoxelRoleTypes.Full) return 5;  // the rest are full
            if (levels[1] == VoxelRoleTypes.Partial && levels.Length == 2) return 6;
            if (levels[2] == VoxelRoleTypes.Empty) return 7; //the rest are empty
            if (levels[2] == VoxelRoleTypes.Full) return 8;  // the rest are full
            if (levels[2] == VoxelRoleTypes.Partial && levels.Length == 3) return 9;
            if (levels[3] == VoxelRoleTypes.Empty) return 10; //the rest are empty
            if (levels[3] == VoxelRoleTypes.Full) return 11;  // the rest are full
            if (levels[3] == VoxelRoleTypes.Partial && levels.Length == 4) return 12;
            if (levels[3] == VoxelRoleTypes.Empty) return 13;
            if (levels[4] == VoxelRoleTypes.Full) return 14;
            return 15;
        }
        internal static VoxelRoleTypes[] GetRoleFlags(byte flags)
        {
            if (flags == 0) return new VoxelRoleTypes[0]; //no role is specified
            if (flags == 1) return new[] { VoxelRoleTypes.Empty }; // could add a bunch more empties. is this necessary?
            if (flags == 2) return new[] { VoxelRoleTypes.Full };
            if (flags == 3) return new[] { VoxelRoleTypes.Partial };
            if (flags == 4) return new[] { VoxelRoleTypes.Partial, VoxelRoleTypes.Empty }; //the rest are empty
            if (flags == 5) return new[] { VoxelRoleTypes.Partial, VoxelRoleTypes.Full };  // the rest are full
            if (flags == 6) return new[] { VoxelRoleTypes.Partial, VoxelRoleTypes.Partial };
            if (flags == 7) return new[] { VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Empty };
            if (flags == 8) return new[] { VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Full };
            if (flags == 9) return new[] { VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial };
            if (flags == 10) return new[] { VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Empty };
            if (flags == 11) return new[] { VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Full };
            if (flags == 12) return new[] { VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial };
            if (flags == 13) return new[] { VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Empty };
            if (flags == 14) return new[] { VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Full };
            return new[] { VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial };
        }

        internal static int[] GetCoordinatesFromID(long ID, int level)
        {
            return new[]
            {
                GetCoordinateFromID(ID, 0, level), GetCoordinateFromID(ID, 1, level), GetCoordinateFromID(ID, 2, level)
            };
        }
        internal static int GetCoordinateFromID(long id, int dimension, int level)
        {
            if (dimension == 0) //x starts at 44 and goes to the end,64
                return (int)(id >> (44 + 4 * (4 - level)));
            if (dimension == 1) // y starts at 24 and goes to 44, so the x amount from (44 to 64 needs to be subtracted off
            {
                var yShift = 24 + 4 * (4 - level);
                return (int)((id >> yShift) - (id >> 44 << yShift));
            }
            var zShift = 4 * (5 - level); //this is like y,but note the simplification (the 5) in this equation
            return (int)((id >> zShift) - (id >> 24 << zShift));

        }

        internal static long MakeCoordinateZero(long id, int dimension)
        {
            switch (dimension)
            {
                case 0: return id & -16777201;
                case 1: return id & -17592169267201;
                case 2: return id & 17592186044415;
            }
            throw new ArgumentOutOfRangeException("dimension must be 0, 1, or 2");
        }
        internal static long ChangeCoordinate(long id, int newValue, int dimension)
        {
            newValue = newValue << (4 + 20 * (2 - dimension));
            return newValue + MakeCoordinateZero(id, dimension);
        }

        internal static long GetContainingVoxel(long id, int level)
        {
            switch (level)
            {
                case 0: return (id & -1152905011916701696) + SetRoleFlags(new[] { VoxelRoleTypes.Partial });
                case 1: return (id & -72040070554648576) + SetRoleFlags(new[] { VoxelRoleTypes.Partial, VoxelRoleTypes.Partial });
                case 2: return (id & -4486011719520256) + SetRoleFlags(new[] { VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial });
                case 3: return (id & -263883042324736) + SetRoleFlags(new[] { VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial });
            }
            throw new ArgumentOutOfRangeException("containing level must be 0, 1, 2, or 3");
        }
        #endregion


        /// <summary>
        /// Is the double currently at an integer value?
        /// </summary>
        /// <param name="d">The d.</param>
        /// <returns></returns>
        private static bool atIntegerValue(double d)
        {
            return Math.Ceiling(d) == d;
        }
    }

}