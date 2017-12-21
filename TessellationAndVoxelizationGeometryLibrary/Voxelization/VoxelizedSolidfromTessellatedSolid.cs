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
    public partial class VoxelizedSolid
    {
        public TessellatedSolid OriginalTessellatedSolid;

        /// <summary>
        /// Initializes a new instance of the <see cref="VoxelizedSolid" /> class.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="voxelDiscretization">The voxel discretization.</param>
        /// <param name="onlyDefineBoundary">if set to <c>true</c> [only define boundary].</param>
        /// <param name="bounds">The bounds.</param>
        public VoxelizedSolid(TessellatedSolid ts, VoxelDiscretization voxelDiscretization, bool onlyDefineBoundary = false,
            double[][] bounds = null) : base(ts.Units, ts.Name, "", ts.Comments, ts.Language)
        {
            OriginalTessellatedSolid = ts;
            Discretization = voxelDiscretization;
            #region Setting Up Parameters 

            double longestSide;
            Bounds = new double[2][];
            if (bounds != null)
            {
                Bounds[0] = (double[])bounds[0].Clone();
                Bounds[1] = (double[])bounds[1].Clone();
                dimensions = new double[3];
                for (int i = 0; i < 3; i++)
                    dimensions[i] = Bounds[1][i] - Bounds[0][i];
                longestSide = dimensions.Max();
                longestDimensionIndex = dimensions.FindIndex(d => d == longestSide);
            }
            else
            {  // add a small buffer only if no bounds are provided.
                dimensions = new double[3];
                for (int i = 0; i < 3; i++)
                    dimensions[i] = ts.Bounds[1][i] - ts.Bounds[0][i];
                longestSide = dimensions.Max();
                longestDimensionIndex = dimensions.FindIndex(d => d == longestSide);
                var delta = longestSide * Constants.fractionOfWhiteSpaceAroundFinestVoxelFactor;
                Bounds[0] = ts.Bounds[0].subtract(new[] { delta, delta, delta });
                Bounds[1] = ts.Bounds[1].add(new[] { delta, delta, delta });
            }
            longestSide = Bounds[1][longestDimensionIndex] - Bounds[0][longestDimensionIndex];
            VoxelSideLengths = new[] { longestSide / 16, longestSide / 256, longestSide / 4096, longestSide / 65536, longestSide / 1048576 };
            #endregion

            voxelDictionaryLevel0 = new Dictionary<long, Voxel_Level0_Class>(new VoxelComparerCoarse());
            voxelDictionaryLevel1 = new Dictionary<long, Voxel_Level1_Class>(new VoxelComparerCoarse());
            transformedCoordinates = new double[ts.NumberOfVertices][];
            //Parallel.For(0, ts.NumberOfVertices, i =>
            for (int i = 0; i < ts.NumberOfVertices; i++)
            {
                var vertex = ts.Vertices[i];
                var coordinates = vertex.Position.subtract(Offset).divide(VoxelSideLengths[1]);
                // even if the request was for ExtraCoarse voxels (only 16 on a side), we investigate
                // these at the 256 or level-1 discretization. The reason for this is that our algorithm for
                // defining partial and full voxels by first looking at the vertices, edges, and faces and then
                // filling up inward is often fooled when there are too many of the tessellation objects
                // in one voxel.
                transformedCoordinates[i] = coordinates;
                makeVoxelForVertexLevel0And1(vertex, coordinates);
            }  //);
            makeVoxelsForFacesAndEdges(ts);
            if (!onlyDefineBoundary)
                makeVoxelsInInterior();
            if (discretizationLevel > 1)
            {  //here's where the higher level voxels are defined.
                //todo
                Parallel.For(0, ts.NumberOfVertices, i =>
                //for (int i = 0; i < ts.NumberOfVertices; i++)
                {
                    var vertex = ts.Vertices[i];
                    var coordinates = vertex.Position.subtract(Offset).divide(VoxelSideLengths[1]);
                    // even if the request was for ExtraCoarse voxels (only 16 on a side), we investigate
                    // these at the 256 or level-1 discretization. The reason for this is that our algorithm for
                    // defining partial and full voxels by first looking at the vertices, edges, and faces and then
                    // filling up inward is often fooled when there are too many of the tessellation objects
                    // in one voxel.
                    transformedCoordinates[i] = coordinates;
                    makeVoxelForVertexLevel0And1(vertex, coordinates);
                });
            }
            UpdateProperties();
        }

        #region Making Voxels for Levels 0 and 1

        private void makeVoxelForVertexLevel0And1(Vertex vertex, double[] coordinates)
        {
            byte x, y, z;
            if (coordinates.Any(atIntegerValue))
            {
                var edgeVectors = vertex.Edges.Select(e => e.To == vertex ? e.Vector : e.Vector.multiply(-1));
                if (edgeVectors.All(ev => ev[0] >= 0))
                    x = (byte)(coordinates[0] - 1);
                else x = (byte)Math.Floor(coordinates[0]);
                if (edgeVectors.All(ev => ev[1] >= 0))
                    y = (byte)(coordinates[1] - 1);
                else y = (byte)Math.Floor(coordinates[1]);
                if (edgeVectors.All(ev => ev[2] >= 0))
                    z = (byte)(coordinates[2] - 1);
                else z = (byte)Math.Floor(coordinates[2]);
            }
            else
            {
                //Gets the integer coordinates, rounded down for the point.
                x = (byte)Math.Floor(coordinates[0]);
                y = (byte)Math.Floor(coordinates[1]);
                z = (byte)Math.Floor(coordinates[2]);
            }
            if (discretizationLevel == 0)
                MakeAndStorePartialVoxelLevel0(x, y, z, vertex);
            else
                MakeAndStorePartialVoxelLevel0And1(x, y, z, vertex);
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
            var faceAVoxel = face.A.Voxels.First(v => v.Level == discretizationLevel);
            var faceBVoxel = face.B.Voxels.First(v => v.Level == discretizationLevel);
            var faceCVoxel = face.C.Voxels.First(v => v.Level == discretizationLevel);
            // The first simple case is that all vertices are within the same voxel. 
            if (faceAVoxel.Equals(faceBVoxel) && faceAVoxel.Equals(faceCVoxel))
            {
                Add(faceAVoxel, face);
                foreach (var edge in face.Edges)
                    Add(faceAVoxel, edge);
                return true;
            }
            // the second, third, and fourth simple cases are if the triangle
            // fits within a line of voxels.
            // this condition checks that all voxels have same x & y values (hence aligned in z-direction)
            if (faceAVoxel.CoordinateIndices[0] == faceBVoxel.CoordinateIndices[0] &&
                faceAVoxel.CoordinateIndices[0] == faceCVoxel.CoordinateIndices[0] &&
                faceAVoxel.CoordinateIndices[1] == faceBVoxel.CoordinateIndices[1] &&
                faceAVoxel.CoordinateIndices[1] == faceCVoxel.CoordinateIndices[1])
            {
                makeVoxelsForFaceInCardinalLine(face, 2);
                return true;
            }
            // this condition checks that all voxels have same x & z values (hence aligned in y-direction)
            if (faceAVoxel.CoordinateIndices[0] == faceBVoxel.CoordinateIndices[0] &&
                faceAVoxel.CoordinateIndices[0] == faceCVoxel.CoordinateIndices[0] &&
                faceAVoxel.CoordinateIndices[2] == faceBVoxel.CoordinateIndices[2] &&
                faceAVoxel.CoordinateIndices[2] == faceCVoxel.CoordinateIndices[2])
            {
                makeVoxelsForFaceInCardinalLine(face, 1);
                return true;
            }
            // this condition checks that all voxels have same y & z values (hence aligned in x-direction)
            if (faceAVoxel.CoordinateIndices[1] == faceBVoxel.CoordinateIndices[1] &&
                faceAVoxel.CoordinateIndices[1] == faceCVoxel.CoordinateIndices[1] &&
                faceAVoxel.CoordinateIndices[2] == faceBVoxel.CoordinateIndices[2] &&
                faceAVoxel.CoordinateIndices[2] == faceCVoxel.CoordinateIndices[2])
            {
                makeVoxelsForFaceInCardinalLine(face, 0);
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
        private void makeVoxelsForFaceInCardinalLine(PolygonalFace face, int dim)
        {
            var coordA = face.A.Voxels.First(v => v.Level == discretizationLevel).CoordinateIndices[dim];
            var coordB = face.B.Voxels.First(v => v.Level == discretizationLevel).CoordinateIndices[dim];
            var coordC = face.C.Voxels.First(v => v.Level == discretizationLevel).CoordinateIndices[dim];
            int minCoord = coordA;
            int maxCoord = coordA;
            if (coordB < minCoord) minCoord = coordB;
            else if (coordB > maxCoord) maxCoord = coordB;
            if (coordC < minCoord) minCoord = coordC;
            else if (coordC > maxCoord) maxCoord = coordC;
            var coordinates = (byte[])face.A.Voxels.First(v => v.Level == discretizationLevel).CoordinateIndices.Clone();
            for (var i = minCoord; i <= maxCoord; i++)
            {
                // set up voxels for the face
                coordinates[dim] = (byte)i;
                if (discretizationLevel == 0)
                    MakeAndStorePartialVoxelLevel0(coordinates[0], coordinates[1], coordinates[2], face);
                else
                    MakeAndStorePartialVoxelLevel0And1(coordinates[0], coordinates[1], coordinates[2], face);
            }
            foreach (var faceEdge in face.Edges)
            {
                // cycle over the edges to link them to the voxels
                int fromIndex = faceEdge.From.Voxels.First(v => v.Level == discretizationLevel).CoordinateIndices[dim];
                int toIndex = faceEdge.To.Voxels.First(v => v.Level == discretizationLevel).CoordinateIndices[dim];
                var step = Math.Sign(toIndex - fromIndex);
                if (step == 0) continue;
                for (var i = fromIndex; i != toIndex; i += step)
                {
                    coordinates[dim] = (byte)i;
                    if (discretizationLevel == 0)
                        MakeAndStorePartialVoxelLevel0(coordinates[0], coordinates[1], coordinates[2], faceEdge);
                    else
                        MakeAndStorePartialVoxelLevel0And1(coordinates[0], coordinates[1], coordinates[2], faceEdge);
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
            var v = atIntegerValue(startV) && (vRange < 0 || (vRange == 0 && insideIsLowerV))
                ? (int)(startV - 1)
                : (int)Math.Floor(startV);
            // likewise for v. if you are starting an integer value and you're going in the negative directive, OR if you're at an integer
            // value and happen to be vertical line then decrement v to the next lower value
            var ijk = new byte[3];
            ijk[sweepDim] = (byte)(sweepValue - 1);
            while (increment * u < increment * endU)
            {
                ijk[uDim] = (byte)u;
                ijk[vDim] = (byte)v;
                if (discretizationLevel == 0)
                    MakeAndStorePartialVoxelLevel0(ijk[0], ijk[1], ijk[2], tsObject);
                else
                    MakeAndStorePartialVoxelLevel0And1(ijk[0], ijk[1], ijk[2], tsObject);
                // now move to the next increment, of course, you may not use it if the while condition is not met
                u += increment;
                var vDouble = vRange * (u - startU) / uRange + startV;
                v = atIntegerValue(vDouble) && (vRange < 0 || (vRange == 0 && insideIsLowerV))
                    ? (int)(vDouble - 1)
                    : (int)Math.Floor(vDouble);
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
        private bool findWhereLineCrossesPlane(double[] startPoint, double[] endPoint, int sweepDim,
            double valueSweepDim, out double valueD1, out double valueD2)
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
            var ids = voxelDictionaryLevel1.Keys.Select(vxID => MakeCoordinateZero(vxID, sweepDim))
                .Distinct()
                .AsParallel();
            var dict = ids.ToDictionary(id => id, id => new Tuple<SortedSet<Voxel_Level1_Class>, 
                SortedSet<Voxel_Level1_Class>, SortedSet<Voxel_Level1_Class>, SortedSet<Voxel_Level1_Class>>(
                new SortedSet<Voxel_Level1_Class>(new SortByVoxelIndex(sweepDim + 1)), // why the plus one? see the comparator. it is usually to line up with the
                new SortedSet<Voxel_Level1_Class>(new SortByVoxelIndex(sweepDim + 1)),
                new SortedSet<Voxel_Level1_Class>(new SortByVoxelIndex(sweepDim + 1)),
                new SortedSet<Voxel_Level1_Class>(new SortByVoxelIndex(sweepDim + 1))));  //VoxelDirection enumerator, and since there is no negative 0, we start at 1 (x=1).
            var all = new List<Voxel_Level1_Class>();
            Parallel.ForEach(voxelDictionaryLevel1, voxelKeyValuePair =>
            //foreach (var voxelKeyValuePair in voxelDictionaryLevel1)
            {
                var voxel = voxelKeyValuePair.Value;
                var id = MakeCoordinateZero(voxelKeyValuePair.Key, sweepDim);

                var sortedSets = dict[id];
                var negativeFaceVoxels = sortedSets.Item1;
                var positiveFaceVoxels = sortedSets.Item2;
                //var bothFaceVoxels = sortedSets.Item3;
                var allVoxels = sortedSets.Item4;
                var faces = voxel.Faces;
                if (faces.All(f => f.Normal[sweepDim] > 0))
                    lock (positiveFaceVoxels) positiveFaceVoxels.Add(voxel);
                if (faces.All(f => f.Normal[sweepDim] < 0))
                    lock (negativeFaceVoxels) negativeFaceVoxels.Add(voxel);
                //else
                //    lock (bothFaceVoxels) bothFaceVoxels.Add(voxel);
                lock (allVoxels) allVoxels.Add(voxel);
            });
            // Parallel.ForEach(dict.Values.Where(v => v.Item1.Any() && v.Item2.Any()), v =>
            var allFaces = new HashSet<PolygonalFace>();
            foreach (var voxel in voxelDictionaryLevel1.Values)
            {
                foreach (var face in voxel.Faces)
                {
                    allFaces.Add(face);
                }
            }
            var count = 0;
            foreach (var face in OriginalTessellatedSolid.Faces)
            {
                if (!allFaces.Contains(face)) count++;
            }
            if(count > 0) throw new Exception("Voxels do not capture all the faces.");
            foreach (var v in dict.Values.Where(v => v.Item4.Any()))
                MakeInteriorVoxelsAlongLine(v.Item1, v.Item2, v.Item4, sweepDim);
        }

        //Sort partial voxels along a given direction and then consider rows along that direction 
        //End that line at every partial voxel, regardless of face orientation. This voxel may start a new 
        //line if it has faces in both directions.
        //If the next partial voxel is adjacent to the current voxel, go to the next voxel.
        //Start a new line anytime there is a voxel with all its faces pointed away from the search direction
        //OR if it contains faces pointing both ways and the next voxel is fully inside the solid.
        //To Determine if a voxel is fully inside the solid, use the normal of the closest
        //face cast back from the voxel in question. 
        private void MakeInteriorVoxelsAlongLine(SortedSet<Voxel_Level1_Class> sortedNegatives,
            SortedSet<Voxel_Level1_Class> sortedPositives, SortedSet<Voxel_Level1_Class> all, int sweepDim)
        {
            var direction = new [] { 0.0, 0.0, 0.0 };
            direction[sweepDim] = 1;
            const int voxelLevel = 1;
            var coords = (byte[])all.First().CoordinateIndices.Clone();
            var negativeFaceVoxels = new HashSet<Voxel_Level1_Class>(sortedNegatives);
            var positiveFaceVoxels = new HashSet<Voxel_Level1_Class>(sortedPositives);
            var sortedVoxelsInRow = new Queue<Voxel_Level1_Class>(all);
            var priorVoxels = new List<Voxel_Level1_Class>();
            var lineStartIndex = int.MinValue;
            var startIndex = sortedVoxelsInRow.First().CoordinateIndices[sweepDim];
            var finalIndex = sortedVoxelsInRow.Last().CoordinateIndices[sweepDim];
            var faceVoxelIntersectionDict = new Dictionary<int, Tuple<int, double, bool>>(); //Key = faceIndex, Value = <coordinateIndex[sweepDim], intersection distance, isInside>
            while (sortedVoxelsInRow.Any())
            {
                var currentVoxel = sortedVoxelsInRow.Dequeue();
                var currentIndex = currentVoxel.CoordinateIndices[sweepDim];

                //End that line at every partial voxel, regardless of face orientation. This voxel may start a new 
                //line if it has faces in both directions.
                if (lineStartIndex != int.MinValue)
                {
                    //Construct the line
                    for (var i = (int)lineStartIndex + 1; i < currentIndex; i++)
                    {
                        coords[sweepDim] = (byte)i;
                        if (discretizationLevel == 0)
                            MakeAndStoreFullVoxelLevel0(coords[0], coords[1], coords[2]);
                        MakeAndStoreFullVoxelLevel0And1(coords[0], coords[1], coords[2]);
                    }
                    lineStartIndex = int.MinValue;
                }

                //Start a new line anytime there is a voxel with any faces pointed away from the search direction
                //and the next voxel is fully inside the solid (not a partial). 
                priorVoxels.Add(currentVoxel);
                //If it is the last voxel, then it cannot start a new line 
                if (finalIndex == currentIndex) break;
                //If it is positive, then it must be outside. Do not make a new line.
                if (positiveFaceVoxels.Contains(currentVoxel)) continue;  
                //If the next partial voxel is adjacent, then the current voxel cannot start a new line.
                if (sortedVoxelsInRow.Peek().CoordinateIndices[sweepDim] - currentIndex == 1) continue;
                //If the current voxel is negative, then it must be inside. Start a new line.
                if (negativeFaceVoxels.Contains(currentVoxel))
                {
                    lineStartIndex = currentIndex;
                    continue;
                }

                //Else, the current voxel contains both positive and negative faces. 
                //We need to determine if the next voxel is fully inside the solid or empty
                //Check the reversed prior voxels, looking for the closest face that intersects
                //a ray casted from the next voxel
                var isInside = false;
                //var validIntersectionFound = false;
                //var lastVoxelToConsider = false;
                var realCoordinate = new []
                {
                    //ToDo: If Offset is changed to Transform, then the direction will need to be set differently
                    currentVoxel.BottomCoordinate[0],//+ 0.5*VoxelSideLengths[voxelLevel]
                    currentVoxel.BottomCoordinate[1],//+ 0.5*VoxelSideLengths[voxelLevel]
                    currentVoxel.BottomCoordinate[2] //+ 0.5*VoxelSideLengths[voxelLevel]
                };
                realCoordinate[sweepDim] += VoxelSideLengths[voxelLevel];
                var facesToConsider = priorVoxels.SelectMany(v => v.Faces).Distinct().ToList();
                var minDistance = double.MaxValue;
                var intersectionPoints = new Dictionary<double, double[]>();
                foreach (var face in facesToConsider)
                {
                    var dot = face.Normal[sweepDim];
                    if (dot == 0) continue; //Use a different face because this face is inconclusive.

                    double signedDistance;
                    var intersectionPoint = MiscFunctions.PointOnTriangleFromLine(face, realCoordinate, direction.multiply(-1),
                        out signedDistance,
                        true);

                    if (intersectionPoint == null) continue;
                    var duplicateFound = false;
                    foreach (var key in intersectionPoints.Keys)
                    {
                        if (key.IsNegligible(signedDistance))
                        {
                            //Duplicate found, such as when intersecting an edge or vertex. Ignore.
                            duplicateFound = true;
                        }
                    }
                    if (!duplicateFound) intersectionPoints.Add(signedDistance, intersectionPoint);
                    Debug.WriteLine(signedDistance);

                    if (signedDistance < minDistance)
                    {
                        //Debug.WriteLine(signedDistance);
                        minDistance = signedDistance;
                        isInside = dot < 0;
                    }
                }
                Debug.WriteLine(isInside);
                if (isInside) lineStartIndex = currentIndex;

                //    for (var i = priorVoxels.Count - 1; i >= 0 ; i--)
                //    {
                //        var priorVoxel = priorVoxels[i];

                //        //If we get to the first item in this list [0] and it is in the positive or negative hashes,
                //        //then we don't need to do any geomtry.
                //        if (i == 0)
                //        {
                //            if (negativeFaceVoxels.Contains(priorVoxel))
                //            {
                //                isInside = true;
                //                validIntersectionFound = true;
                //            }
                //            else if (positiveFaceVoxels.Contains(priorVoxel))
                //            {
                //                isInside = false;
                //                validIntersectionFound = true;
                //            }
                //        }
                //        if (validIntersectionFound) break;

                //        //This section determines if a ray from the next voxel, cast along the reversed search direction, 
                //        //intersects any of the voxels faces. If it does intersect a face(s), choose the closest intersection's 
                //        //face to determine isInside based on the normal of that face.
                //        //Once we get to a voxel that contains a valid intersection, then we can stop (lastVoxelToConsider = true). 
                //        //This IS the partial voxel with the closest intersection to the voxel in question. 
                //        //Note that we still want to consider all new faces that this voxel adds to the hash consideredFaces,
                //        //since it may contain another intersection.
                //        var minDistance = double.MaxValue;
                //        var priorVoxelIndex = priorVoxel.CoordinateIndices[sweepDim];
                //        foreach (var face in priorVoxel.Faces)
                //        {
                //            //We only need to calculate the intersection for each face once.
                //            if (faceVoxelIntersectionDict.ContainsKey(face.IndexInList))
                //            {
                //                var intersectionInfo = faceVoxelIntersectionDict[face.IndexInList];

                //                //If this voxel is the intersection voxel for this face, then this is the last voxel to consider.
                //                //Otherwise, continue. 
                //                if (intersectionInfo.Item1 != priorVoxelIndex) continue;
                //                lastVoxelToConsider = true;

                //                //We have already calculated the intersection for this face in a previous partial along this row.
                //                if (intersectionInfo.Item2 < minDistance)
                //                {
                //                    minDistance = intersectionInfo.Item2;
                //                    isInside = intersectionInfo.Item3;
                //                    validIntersectionFound = true;
                //                } 
                //            }
                //            else
                //            {
                //                var dot = face.Normal[sweepDim];
                //                if (dot == 0) continue; //Use a different face because this face is inconclusive.


                //                double signedDistance;
                //                var intersectionPoint = MiscFunctions.PointOnTriangleFromLine(face, realCoordinate, direction,
                //                    out signedDistance,
                //                    true);

                //                if (intersectionPoint == null) continue;
                //                //var distanceToOrigin = -face.Normal.dotProduct(face.Vertices[0].Position);
                //                //var intersectionPoint2 = MiscFunctions.PointOnPlaneFromRay(face.Normal, distanceToOrigin,
                //                //    realCoordinate, direction.multiply(-1));

                //                //ToDo: If Offset is changed to Transform, then this distance will need to be set differently
                //                var val = (intersectionPoint.subtract(Offset)[sweepDim] / VoxelSideLengths[voxelLevel]);
                //                var intersectionIndex = (int) val;
                //                if(intersectionIndex < startIndex) throw new Exception("Error in implementation.");
                //                faceVoxelIntersectionDict.Add(face.IndexInList, new Tuple<int, double, bool>(intersectionIndex, signedDistance, dot < 0));
                //                if (signedDistance < minDistance)
                //                {
                //                    minDistance = signedDistance;
                //                    isInside = dot < 0;
                //                    validIntersectionFound = true;
                //                }
                //                //Check if it intersected in this voxel. If it did, this is the last voxel we need to check. 
                //                //Otherwise we need to iterate until we go past the voxel that contains the intersection.
                //                if (intersectionIndex == priorVoxelIndex)
                //                {
                //                    lastVoxelToConsider = true;
                //                }
                //            }

                //        }
                //        if (lastVoxelToConsider)  break;
                //    }
                //    if (isInside) lineStartIndex = currentVoxel.CoordinateIndices[sweepDim];
                //    if (!validIntersectionFound)
                //    {
                //        //It may not have a valid intersection => empty
                //        //throw new Exception();
                //    }
            }
        }




        private void MakeAndStoreFullVoxelLevel0And1(byte x, byte y, byte z)
        {
            bool level1AlreadyMade = false;
            var voxIDLevel0 = MakeVoxelID0(x, y, z);
            var voxIDLevel1 = MakeVoxelID1(x, y, z);
            lock (voxelDictionaryLevel1)
            {
                if (voxelDictionaryLevel1.ContainsKey(voxIDLevel1))
                    level1AlreadyMade = true;
                else
                    voxelDictionaryLevel1.Add(voxIDLevel1, new Voxel_Level1_Class(x, y, z, VoxelRoleTypes.Full, VoxelSideLengths, Offset));
            }
            if (level1AlreadyMade)
            {
                //actually don't need to do anything here as partial is a stronger conviction than full
                // and we shouldn't change that thus there really isn't anything to do
                //  voxelDictionaryLevel0[voxIDLevel0].VoxelRole = VoxelRoleTypes.Full;
            }
            else
            {
                Voxel_Level0_Class voxelLevel0;
                lock (voxelDictionaryLevel0)
                {
                    if (voxelDictionaryLevel0.ContainsKey(voxIDLevel0))
                        voxelLevel0 = voxelDictionaryLevel0[voxIDLevel0];
                    else
                    {
                        voxelLevel0 = new Voxel_Level0_Class(x, y, z, VoxelRoleTypes.Partial, VoxelSideLengths, Offset);
                        voxelDictionaryLevel0.Add(voxIDLevel0, voxelLevel0);
                    }
                }
                if (!voxelLevel0.NextLevelVoxels.Contains(voxIDLevel1))
                {
                    if (voxelLevel0.NextLevelVoxels.Count == 4095
                        && voxelLevel0.NextLevelVoxels.All(v => voxelDictionaryLevel1[v].Role == VoxelRoleTypes.Full))
                        MakeVoxelFull(voxelLevel0);
                    else lock (voxelLevel0.NextLevelVoxels) voxelLevel0.NextLevelVoxels.Add(voxIDLevel1);
                }
            }
        }
        #endregion

        #region Make and Store Voxel
        private void MakeAndStoreFullVoxelLevel0(byte x, byte y, byte z)
        {
            var voxIDLevel0 = MakeVoxelID0(x, y, z);
            lock (voxelDictionaryLevel0)
            {
                if (!voxelDictionaryLevel0.ContainsKey(voxIDLevel0))
                    voxelDictionaryLevel0.Add(voxIDLevel0, new Voxel_Level0_Class(x, y, z, VoxelRoleTypes.Partial, VoxelSideLengths, Offset));
            }
        }

        #endregion


        private void MakeAndStorePartialVoxelLevel0(byte x, byte y, byte z, TessellationBaseClass tsObject)
        {
            Voxel_Level0_Class voxelLevel0;
            var voxIDLevel0 = MakeVoxelID0(x, y, z);
            lock (voxelDictionaryLevel0)
            {
                if (voxelDictionaryLevel0.ContainsKey(voxIDLevel0))
                    voxelLevel0 = voxelDictionaryLevel0[voxIDLevel0];
                else
                {
                    voxelLevel0 = new Voxel_Level0_Class(x, y, z, VoxelRoleTypes.Partial, VoxelSideLengths, Offset);
                    voxelDictionaryLevel0.Add(voxIDLevel0, voxelLevel0);
                }
            }

            Add(voxelLevel0, tsObject);
        }

        private void MakeAndStorePartialVoxelLevel0And1(byte x, byte y, byte z, TessellationBaseClass tsObject)
        {
            bool level1AlreadyMade = false;
            Voxel_Level0_Class voxelLevel0;
            Voxel_Level1_Class voxelLevel1;
            var voxIDLevel0 = MakeVoxelID0(x, y, z);
            var voxIDLevel1 = MakeVoxelID1(x, y, z);
            lock (voxelDictionaryLevel1)
            {
                if (voxelDictionaryLevel1.ContainsKey(voxIDLevel1))
                {
                    voxelLevel1 = voxelDictionaryLevel1[voxIDLevel1];
                    level1AlreadyMade = true;
                }
                else
                {
                    voxelLevel1 = new Voxel_Level1_Class(x, y, z, VoxelRoleTypes.Partial, VoxelSideLengths, Offset);
                    voxelDictionaryLevel1.Add(voxIDLevel1, voxelLevel1);
                }
            }
            if (level1AlreadyMade)
                voxelLevel0 = voxelDictionaryLevel0[voxIDLevel0];
            else
            {
                lock (voxelDictionaryLevel0)
                {
                    if (voxelDictionaryLevel0.ContainsKey(voxIDLevel0))
                        voxelLevel0 = voxelDictionaryLevel0[voxIDLevel0];
                    else
                    {
                        voxelLevel0 = new Voxel_Level0_Class(x, y, z, VoxelRoleTypes.Partial, VoxelSideLengths, Offset);
                        voxelDictionaryLevel0.Add(voxIDLevel0, voxelLevel0);
                    }
                }
                if (!voxelLevel0.NextLevelVoxels.Contains(voxIDLevel1))
                {
                    if (voxelLevel0.NextLevelVoxels.Count == 4095) MakeVoxelFull(voxelLevel0);
                    else lock (voxelLevel0.NextLevelVoxels) voxelLevel0.NextLevelVoxels.Add(voxIDLevel1);
                }
            }
            Add(voxelLevel0, tsObject);
            Add(voxelLevel1, tsObject);
        }

        #endregion
    }
}