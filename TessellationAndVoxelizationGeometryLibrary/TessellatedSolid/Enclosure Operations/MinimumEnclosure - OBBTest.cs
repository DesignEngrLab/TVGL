// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 02-15-2015
// ***********************************************************************
// <copyright file="MinimumBoundingBox.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using TVGL.Numerics;
using TVGL.Enclosure_Operations;

namespace TVGL
{
    /// <summary>
    ///     The MinimumEnclosure class includes static functions for defining smallest enclosures for a
    ///     tessellated solid. For example: convex hull, minimum bounding box, or minimum bounding sphere.
    /// </summary>
    public static partial class MinimumEnclosure
    {
        /// <summary>
        ///     Finds the minimum bounding box oriented along a particular Direction.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="times"></param>
        /// <param name="volumes"></param>
        /// <returns>BoundingBox.</returns>
        //private
        public static BoundingBox OrientedBoundingBox_Test(TessellatedSolid ts, out List<double> times,
            out List<double> volumes) //, out List<List<Vector2>> volumeData2)
        {
            var vertices = ts.ConvexHull.Vertices.Any() ? ts.ConvexHull.Vertices : ts.Vertices;
            times = new List<double>();
            volumes = new List<double>();
            //var flats = ListFunctions.Flats(ts.Faces.ToList());
            var now = DateTime.Now;
            Message.output("Beginning OBB Test", 2);
            var boundingBox1 = OrientedBoundingBox(vertices);
            times.Add((DateTime.Now - now).TotalMilliseconds);
            volumes.Add(boundingBox1.Volume);
            //Message.output("Time Elapsed for PCA Approach = " ,4);
            //Message.output("Volume for PCA Approach= " + boundingBox1.Volume,4);
            now = DateTime.Now;
            Message.output("Beginning OBB Test", 2);

            var boundingBox12 = Find_via_PCA_ApproachNR(vertices);
            times.Add((DateTime.Now - now).TotalMilliseconds);
            volumes.Add(boundingBox12.Volume);
            //Message.output("Time Elapsed for PCA Approach = " ,4 );
            //Message.output("Volume for PCA Approach= " + boundingBox1.Volume);
            now = DateTime.Now;
            var boundingBox2 = Find_via_ChanTan_AABB_Approach(vertices);
            times.Add((DateTime.Now - now).TotalMilliseconds);
            volumes.Add(boundingBox2.Volume);
            Message.output("Time Elapsed for ChanTan Approach = " + (DateTime.Now - now), 4);
            Message.output("Volume for ChanTan Approach = " + boundingBox2.Volume, 4);
            //now = DateTime.Now;
            //Message.output("Beginning OBB Test");
            //var boundingBox1 = Find_via_MC_ApproachOne(ts, out volumeData1);
            //Message.output("Time Elapsed for MC Approach One = " + (DateTime.Now - now),4);
            //now = DateTime.Now;
            //var boundingBox2 = Find_via_BM_ApproachTwo(ts, out volumeData2);
            //Message.output("Time Elapsed for BM Approach Two = " + (DateTime.Now - now),4);

            return boundingBox2;
        }

        private static BoundingBox Find_via_ChanTan_AABB_Approach(IList<Vertex> convexHullVertices)
        {
            return Find_via_ChanTan_AABB_Approach(convexHullVertices, new BoundingBox
            {
                Directions = new[] { Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ }
                Volume = double.PositiveInfinity
            });
        }

        private class BoundingBoxData
        {
            private BoundingBoxData()
            {
            }

            internal BoundingBoxData(Vector3 startDir, Vector3 yDir, Edge rotatorEdge, Vector3 rotatorVector,
                TVGLConvexHull convexHull)
            {
                Direction = startDir;
                PosYDir = yDir;
                RotatorEdge = rotatorEdge;
                RotatorVector = rotatorVector;
                OrthGaussSphereArcs = new List<GaussSphereArc>();
                // make arrays of the dotproducts with start and end directions (x-values) to help subsequent
                // foreach loop which will look up faces multiple times.
                var startingDots = new double[convexHull.Faces.Length];
                for (var i = 0; i < convexHull.Faces.Length; i++)
                {
                    var face = convexHull.Faces[i];
                    face.IndexInList = i;
                    startingDots[i] = face.Normal.Dot(startDir);
                }
                foreach (var edge in convexHull.Edges)
                {
                    var ownedX = startingDots[edge.OwnedFace.IndexInList];
                    var otherX = startingDots[edge.OtherFace.IndexInList];
                    if (otherX*ownedX <= 0)
                    {
                        var ownedY = edge.OwnedFace.Normal.Dot(yDir);
                        var otherY = edge.OtherFace.Normal.Dot(yDir);
                        //if ((ownedX < 0 && ownedY > 0) || (ownedX > 0 && ownedY < 0))
                        //    OrthGaussSphereArcs.Add(new GaussSphereArc(edge, edge.OwnedFace));
                        //else if ((otherX < 0 && otherY > 0) || (otherX > 0 && otherY < 0))
                        //    OrthGaussSphereArcs.Add(new GaussSphereArc(edge, edge.OtherFace));
                        if ((ownedX <= 0 && ownedY > 0) || (ownedX >= 0 && ownedY < 0))
                            OrthGaussSphereArcs.Add(new GaussSphereArc(edge, edge.OwnedFace));
                        else if ((otherX <= 0 && otherY > 0) || (otherX >= 0 && otherY < 0))
                            OrthGaussSphereArcs.Add(new GaussSphereArc(edge, edge.OtherFace));
                    }
                }
                OrthVertices =
                    OrthGaussSphereArcs.SelectMany(arc => new[] {arc.Edge.From, arc.Edge.To}).Distinct().ToList();
                var maxDistance = double.NegativeInfinity;
                foreach (var v in convexHull.Vertices)
                {
                    var distance = rotatorEdge.From.Coordinates.Subtract(v.Coordinates).Dot(startDir);
                    if (distance > maxDistance)
                    {
                        maxDistance = distance;
                        BackVertex = v;
                    }
                }
            }

            public BoundingBox Box { get; set; }
            public Vector3 Direction { get; set; }
            public double Angle { get; set; }
            public List<Vertex> OrthVertices { get; private set; }
            public List<GaussSphereArc> OrthGaussSphereArcs { get; private set; }
            public Vertex BackVertex { get; set; }
            public Edge BackEdge { get; set; }
            public Vector2 PosYDir { get; set; }
            public Vector2 RotatorVector { get; private set; }
            public Edge RotatorEdge { get; private set; }

            public BoundingBoxData Copy()
            {
                return new BoundingBoxData
                {
                    Angle = Angle,
                    BackVertex = BackVertex,
                    BackEdge = BackEdge,
                    Box = new BoundingBox
                    {
                        CornerVertices = Box.CornerVertices != null ? (Vertex[]) Box.CornerVertices.Clone() : null,
                        Center = Box.Center != null ? new Vertex(Box.Center.Coordinates) : null,
                        Dimensions = Box.Dimensions != null ? (Vector3) Box.Dimensions.Clone() : Vector3.Null,
                        Directions = Box.Directions != null ? (Vector3[]) Box.Directions.Clone() : Vector3.Null,
                        PointsOnFaces = Box.PointsOnFaces != null ? (List<Vertex>[]) Box.PointsOnFaces.Clone() : null,
                        Volume = Box.Volume
                    },
                    Direction = (Vector2) Direction.Clone(),
                    OrthGaussSphereArcs = new List<GaussSphereArc>(OrthGaussSphereArcs),
                    OrthVertices = new List<Vertex>(OrthVertices),
                    PosYDir = (Vector2) PosYDir.Clone(),
                    RotatorVector = (Vector2) RotatorVector.Clone(),
                    RotatorEdge = RotatorEdge
                };
            }
        }



        #region MC ApproachOne

        /// <summary>
        ///     The MC_ApproachOne rotates around each edge of the convex hull between the owned and
        ///     other faces. In this way, it guarantees a much more optimal solution than the flat
        ///     with face algorithm, but is, therefore, slower.
        /// </summary>
        /// <timeDomain>
        ///     Since the computation cost for each Bounding Box is linear O(n),
        ///     and the approximate worse case number of normals considered is n*PI/maxDeltaAngle,
        ///     Lower Bound O(n^2). Upper Bound O(n^(2)*PI/maxDeltaAngle). [ex.  upper bound is O(36*n^2) when MaxDeltaAngle = 5
        ///     degrees.]
        /// </timeDomain>
        /// <accuracy>
        ///     Garantees the optimial orientation is within maxDeltaAngle error.
        /// </accuracy>
        private static BoundingBox OrientedBoundingBox(TVGLConvexHull convexHull)
        {
            var minBox = new BoundingBox {Volume = double.PositiveInfinity};
            foreach (var rotateEdge in convexHull.Edges)
            {
                #region Initialize variables

                //Initialize variables
                //rotatorVector is basically the edge in question - the vector that is being rotated about
                var rotatorVector = rotateEdge.Vector.Normalize();
                // startDir is the starting Direction - based on the OtherFace
                var startDir = rotateEdge.OtherFace.Normal;
                // endDir is the OwnedFace final Direction - we go from Other to Owned since in order to be about
                // the positive Direction of the rotatorVector
                var endDir = rotateEdge.OwnedFace.Normal;
                // posYDir is the vector for the positive y-Direction. Well, this is a simplification of the 
                //gauss sphere to a 2D circle. The Direction (such as startDir) represents the x-axis and this
                //, which is the orthogonal is the y Direction
                var origPosYDir = rotatorVector.Cross(startDir).Normalize();
                var totalAngle = Math.PI - rotateEdge.InternalAngle;
                var thisBoxData = new BoundingBoxData(startDir, origPosYDir, rotateEdge, rotatorVector, convexHull);

                #endregion

                FindOBBAlongDirection(thisBoxData);
                if (thisBoxData.Box.Volume < minBox.Volume) minBox = thisBoxData.Box;
                var angle = 0.0;
                var deltaAngleToBackChange = 0.0;
                var deltaAngleOrthSet = 0.0;
                BoundingBoxData backChangeBox = null;
                BoundingBoxData sideChangeBox = null;
                do
                {
                    if (deltaAngleToBackChange <= 0)
                    {
                        backChangeBox = thisBoxData.Copy();
                        deltaAngleToBackChange = UpdateBackAngle(backChangeBox);
                    }
                    if (deltaAngleOrthSet <= 0)
                    {
                        sideChangeBox = thisBoxData.Copy();
                        deltaAngleOrthSet = UpdateOrthAngle(sideChangeBox);
                    }
                    BoundingBoxData nextBoxData;
                    if (deltaAngleOrthSet < deltaAngleToBackChange)
                    {
                        deltaAngleToBackChange -= deltaAngleOrthSet;
                        angle += deltaAngleOrthSet;
                        deltaAngleOrthSet = 0;
                        nextBoxData = sideChangeBox;
                    }
                    else if (deltaAngleToBackChange < deltaAngleOrthSet)
                    {
                        deltaAngleOrthSet -= deltaAngleToBackChange;
                        angle += deltaAngleToBackChange;
                        deltaAngleToBackChange = 0;
                        nextBoxData = backChangeBox;
                    }
                    else // if they are equal to each other
                    {
                        angle += deltaAngleToBackChange;
                        deltaAngleOrthSet = deltaAngleToBackChange = 0;
                        nextBoxData = backChangeBox;
                    }
                    if (angle > totalAngle)
                    {
                        // nextBoxData = new BoundingBoxData(endDir, rotatorVector.Cross(endDir).Normalize(), rotateEdge, rotatorVector, convexHull);
                        nextBoxData.Angle = totalAngle;
                        nextBoxData.Direction = endDir;
                    }
                    else
                    {
                        nextBoxData.Angle = angle;
                        nextBoxData.Direction = UpdateDirection(startDir, rotatorVector, origPosYDir, angle);
                    }
                    nextBoxData.PosYDir = nextBoxData.RotatorVector.Cross(nextBoxData.Direction).Normalize();

                    /****************/
                    FindOBBAlongDirection(nextBoxData);
                    /****************/
                    if (DifferentMembershipInExtrema(thisBoxData, nextBoxData))
                    {
                        var lowerBox = thisBoxData;
                        var upperBox = nextBoxData;
                        var midBox = thisBoxData.Copy();
                        while (!lowerBox.Angle.IsPracticallySame(upperBox.Angle, Constants.OBBAngleTolerance))
                        {
                            midBox.Direction = (lowerBox.Direction + upperBox.Direction).Divide(2).Normalize();
                            midBox.Angle = (lowerBox.Angle + upperBox.Angle)/2.0;
                            FindOBBAlongDirection(midBox);
                            if (midBox.Box.Volume > lowerBox.Box.Volume && midBox.Box.Volume > upperBox.Box.Volume)
                                break;
                            if (!DifferentMembershipInExtrema(lowerBox, midBox))
                                lowerBox = midBox;
                            else if (!DifferentMembershipInExtrema(upperBox, midBox))
                                upperBox = midBox;
                            else throw new Exception("new midbox is different from BOTH neighbors!");
                        }
                        if (thisBoxData.Box.Volume < minBox.Volume) minBox = midBox.Box;
                    }
                    thisBoxData = nextBoxData;
                    if (thisBoxData.Box.Volume < minBox.Volume) minBox = thisBoxData.Box;
                } while (angle < totalAngle);
            }
            return minBox;
        }

        private static double UpdateOrthAngle(BoundingBoxData boxData)
        {
            GaussSphereArc arcToRemove = null;
            var minSlope = double.PositiveInfinity;
            boxData.PosYDir = boxData.RotatorVector.Cross(boxData.Direction).Normalize();
            foreach (var arc in boxData.OrthGaussSphereArcs)
            {
                var x = boxData.Direction.Dot(arc.ToFace.Normal);
                var y = boxData.PosYDir.Dot(arc.ToFace.Normal);
                if (y == 0.0) continue;
                var tempSlope = -x/y;
                if (!(tempSlope < minSlope)) continue;
                minSlope = tempSlope;
                arcToRemove = arc;
            }
            if (minSlope < 0) return double.PositiveInfinity;
            var edgesAtJunction = new List<Edge>(arcToRemove.ToFace.Edges);
            for (var i = boxData.OrthGaussSphereArcs.Count - 1; i >= 0; i--)
            {
                var index = edgesAtJunction.FindIndex(boxData.OrthGaussSphereArcs[i].Edge);
                if (index >= 0)
                {
                    boxData.OrthGaussSphereArcs.RemoveAt(i);
                    boxData.OrthVertices.Remove(edgesAtJunction[index].From);
                    boxData.OrthVertices.Remove(edgesAtJunction[index].To);
                    edgesAtJunction.RemoveAt(index);
                }
            }
            foreach (var edge in edgesAtJunction)
            {
                if (!boxData.OrthVertices.Contains(edge.From)) boxData.OrthVertices.Add(edge.From);
                if (!boxData.OrthVertices.Contains(edge.To)) boxData.OrthVertices.Add(edge.To);
                boxData.OrthGaussSphereArcs.Add(new GaussSphereArc(edge, edge.OwnedFace == arcToRemove.ToFace
                    ? edge.OtherFace
                    : edge.OwnedFace));
            }
            return Math.Atan(minSlope);
        }

        private static double UpdateBackAngle(BoundingBoxData boxData)
        {
            Edge nextEdge = null;
            var yDir = boxData.RotatorVector.Cross(boxData.Direction);
            var minSlope = double.PositiveInfinity;
            foreach (var edge in boxData.BackVertex.Edges)
            {
                var otherVertex = edge.OtherVertex(boxData.BackVertex);
                var vector = otherVertex.Coordinates.Subtract(boxData.BackVertex.Coordinates);
                var y = yDir.Dot(vector);
                if (y < 0)
                {
                    // the x-value is boxData.Direction.Dot(vector) and it's positive for all edges since it's the back vertex
                    var slope = -boxData.Direction.Dot(vector)/y;
                    if (slope < minSlope)
                    {
                        minSlope = slope;
                        nextEdge = edge;
                    }
                }
            }
            if (minSlope < 0) return double.PositiveInfinity;
            boxData.BackVertex = nextEdge.OtherVertex(boxData.BackVertex);
            boxData.BackEdge = nextEdge;
            return Math.Atan(minSlope);
        }


        private static bool DifferentMembershipInExtrema(BoundingBoxData boxDataA, BoundingBoxData boxDataB)
        {
            var boxASides = boxDataA.Box.PointsOnFaces.Skip(2);
            var boxBSides = boxDataB.Box.PointsOnFaces.Skip(2).ToList();
            foreach (var boxASide in boxASides)
            {
                if (!boxBSides.Any(boxBSide => boxASide.Intersect(boxBSide).Any()))
                    return true;
            }
            return false;
        }


        private static Vector2 UpdateDirection(Vector2 startDir, Vector2 rotator, Vector2 posYDir, double angle)
        {
            var a = new double[3, 3];
            a.SetRow(0, rotator);
            a.SetRow(1, startDir);
            a.SetRow(2, posYDir);
            var b = new[] {0.0, Math.Cos(angle), Math.Cos(angle + Math.PI/2)};
            return EqualityExtensions.solve(a, b);
        }

        #endregion
    }
}