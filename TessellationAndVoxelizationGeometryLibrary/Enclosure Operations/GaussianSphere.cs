using System;
using System.Collections.Generic;
using System.Linq;
using StarMathLib;

namespace TVGL.Enclosure_Operations
{
    internal class GaussSphereArc
    {
        internal readonly Edge Edge;
        internal readonly PolygonalFace ToFace;

        internal GaussSphereArc(Edge edge, PolygonalFace toFace)
        {
            Edge = edge;
            ToFace = toFace;
        }
    }
    /// <summary>
    /// Gaussian Sphere for a polyhedron
    /// </summary>
    /// NOTE: Using spherical coordinates from mathematics (r, θ, φ), since it follows the right hand rule.
    /// Where r is the radial distance (r = 1 for the unit circle), θ is the azimuthal angle (XY and θ equal to or between 0 and 360), 
    /// and φ is the polar angle (From Z axis and φ is equal to or between 0 and 180). 
    public struct GaussianSphere
    {
        /// <summary>
        /// The volume of the bounding box.
        /// </summary>
        internal List<Node> Nodes;

        /// <summary>
        /// The Directions are the three unit vectors that describe the orientation of the box.
        /// </summary>
        internal List<Arc> Arcs;

        internal List<Edge> ReferenceEdges;

        /// <summary>
        /// Initializes a new instance of the <see cref="GaussianSphere"/> class.
        /// </summary>
        internal GaussianSphere(TessellatedSolid ts)
        {
            Nodes = new List<Node>();
            Arcs = new List<Arc>();
            ReferenceEdges = new List<Edge>();
            var referenceIndices = new List<int[]>();
            foreach (var triangle in ts.ConvexHull.Faces)
            {
                var sameIndex = Nodes.FindIndex(p => Math.Abs(p.Vector[0] - triangle.Normal[0]) < 0.0001 &&
                    Math.Abs(p.Vector[1] - triangle.Normal[1]) < 0.0001 &&
                    Math.Abs(p.Vector[2] - triangle.Normal[2]) < 0.0001);
                if (sameIndex >= 0) //If the normal already exists, 
                {
                    var node = Nodes[sameIndex];
                    node.ReferenceFaces.Add(triangle);
                    foreach (var edge in triangle.Edges)
                    {
                        if (node.ReferenceEdges.Contains(edge)) node.ReferenceEdges.Remove(edge);
                        else node.ReferenceEdges.Add(edge);
                    }
                }
                else //the normal does not exist yet, create a new node.
                {
                    var node = new Node(triangle);
                    Nodes.Add(node);
                }
            }
            for (var i = 0; i < Nodes.Count; i++)
            {
                var node = Nodes[i];
                foreach (var edge in node.ReferenceEdges)
                {
                    //Add reference vertices 
                    node.ReferenceVertices.Add(edge.To);

                    //Save info to create an arc for every edge that is owned by this face
                    //Since an edge is owned by only one face, each edge will
                    //only be represented by reference indices once.
                    //Also, note that only edges on the border of the face were stored in
                    //node reference faces.
                    if (node.ReferenceFaces.Any(triangle => edge.OwnedFace == triangle))
                    {
                        var otherNormal = edge.OtherFace.Normal;
                        var j = Nodes.FindIndex(p => Math.Abs(p.Vector[0] - otherNormal[0]) < 0.0001 &&
                            Math.Abs(p.Vector[1] - otherNormal[1]) < 0.0001 &&
                            Math.Abs(p.Vector[2] - otherNormal[2]) < 0.0001);
                        var referenceIndex = new[] { i, j };
                        ReferenceEdges.Add(edge);
                        referenceIndices.Add(referenceIndex);
                    }
                }
            }

            //Now that all the nodes are created, and they have the same indices as the faces
            //We can create arcs that point to nodes, and add that reference back to the nodes.
            for (var i = 0; i < referenceIndices.Count; i++)
            {
                var edge = ReferenceEdges[i];
                var referenceIndex = referenceIndices[i];
                var arc = new Arc(Nodes[referenceIndex[0]], Nodes[referenceIndex[1]], edge);
                Nodes[referenceIndex[0]].AddArcReference(arc);
                Arcs.Add(arc);
            }
        }
    }

    internal class Node
    {
        internal double[] Vector;
        internal double X;
        internal double Y;
        internal double Z;
        internal double Theta;
        internal double Phi;
        internal List<Arc> Arcs;
        internal List<PolygonalFace> ReferenceFaces;
        internal List<Edge> ReferenceEdges;
        internal List<Vertex> ReferenceVertices;
        internal Node(PolygonalFace triangle)
        {
            ReferenceFaces = new List<PolygonalFace> { triangle };
            ReferenceEdges = triangle.Edges.ToList();
            ReferenceVertices = new List<Vertex>(); //Create a null list, to build up later.
            Vector = triangle.Normal; //Set unit normal as location on sphere
            X = triangle.Normal[0];
            Y = triangle.Normal[1];
            Z = triangle.Normal[2];
            Arcs = new List<Arc>();

            //bound azimuthal angle (theta) to 0 <= θ <= 360
            Theta = 0.0;
            if (triangle.Normal[0] < 0) //If both negative or just x, add 180 (Q2 and Q3)
            {
                Theta = Math.Atan(triangle.Normal[1] / triangle.Normal[0]) + Math.PI;
            }
            else if (triangle.Normal[1] < 0) //If only y is negative, add 360 (Q4)
            {
                Theta = Math.Atan(triangle.Normal[1] / triangle.Normal[0]) + 2 * Math.PI;
            }
            else //Everything is positive (Q1).
            {
                Theta = Math.Atan(triangle.Normal[1] / triangle.Normal[0]);
            }

            //Calculate polar angle.  Note that Acos is bounded 0 <= φ <= 180. 
            //Aslo, note that r = 1, so this calculation is simpler that usual.
            Phi = Math.Acos(triangle.Normal[2]);
        }

        internal void AddArcReference(Arc arc)
        {
            Arcs.Add(arc);
        }
    }

    internal class Arc
    {
        internal double ArcLength;
        internal List<Node> Nodes;
        internal List<Vertex> ReferenceVertices;
        internal Edge ReferenceEdge;
        internal double[] Direction;
        internal Arc(Node node1, Node node2, Edge edge)
        {
            Nodes = new List<Node> { node1, node2 };
            ReferenceEdge = edge;
            ReferenceVertices = new List<Vertex> { edge.To, edge.From };
            //Calculate arc length. Base on the following answer, where r = 1 for our unit circle.
            //http://math.stackexchange.com/questions/231221/great-arc-distance-between-two-points-on-a-unit-sphere
            //Note that the arc length must be the smaller of the two directions around the sphere. Acos will take care of this.
            ArcLength = Math.Acos(node1.Vector.dotProduct(node2.Vector));
            if (double.IsNaN(ArcLength)) ArcLength = 0.0;

            //Set the direction of the arc (θ, φ), based on the azimuthal angle and the polar angle respectively.
            //Direction based on node1 to node2. 
            var azimuthal = node2.Theta - node1.Theta;
            if (azimuthal > Math.PI) azimuthal = azimuthal - 2 * Math.PI;
            if (azimuthal <= -Math.PI) azimuthal = azimuthal + 2 * Math.PI;
            var polar = node2.Phi - node1.Phi;
            Direction = new[] { azimuthal, polar };
        }

        internal bool Intersect(Arc arc1, Arc arc2, out Vertex intersection)
        {
            intersection = null;
            //Create two planes given arc1 and arc2
            var norm1 = arc1.Nodes[0].Vector.crossProduct(arc1.Nodes[1].Vector); //unit normal
            var norm2 = arc2.Nodes[0].Vector.crossProduct(arc2.Nodes[1].Vector);
            //Check whether the planes are the same. 
            if (Math.Abs(norm1[0] - norm2[0]) < 0.0001 && Math.Abs(norm1[1] - norm2[1]) < 0.0001
                && Math.Abs(norm1[2] - norm2[2]) < 0.0001)
                return true; //All points intersect
            if (Math.Abs(norm1[0] + norm2[0]) < 0.0001 && Math.Abs(norm1[1] + norm2[1]) < 0.0001
                && Math.Abs(norm1[2] + norm2[2]) < 0.0001)
                return true; //All points intersect
            //if (norm1[0].IsPracticallySame(norm2[0]) && norm1[1].IsPracticallySame(norm2[1]) &&
            //   norm1[2].IsPracticallySame(norm2[2])) return true; 
            //Check whether the planes are the same, but built with opposite normals.
            //if (norm1[0].IsPracticallySame(-norm2[0]) && norm1[1].IsPracticallySame(-norm2[1]) &&
            //    norm1[2].IsPracticallySame(-norm2[2])) return true; //All points intersect
            //Find points of intersection between two planes
            var position1 = norm1.crossProduct(norm2).normalize();
            var position2 = new[] { -position1[0], -position1[1], -position1[2] };
            var vertices = new[] { new Vertex(position1), new Vertex(position2), };
            //Check to see if the intersections are on the arcs
            for (var i = 0; i < 2; i++)
            {
                var l1 = arc1.ArcLength;
                var l2 = Math.Acos(arc1.Nodes[0].Vector.dotProduct(vertices[i].Position));
                var l3 = Math.Acos(arc1.Nodes[1].Vector.dotProduct(vertices[i].Position));
                var total1 = l1 - l2 - l3;
                l1 = arc2.ArcLength;
                l2 = Math.Acos(arc2.Nodes[0].Vector.dotProduct(vertices[i].Position));
                l3 = Math.Acos(arc2.Nodes[1].Vector.dotProduct(vertices[i].Position));
                var total2 = l1 - l2 - l3;
                if (!total1.IsNegligible() || !total2.IsNegligible()) continue;
                intersection = vertices[i];
                return true;
            }
            return false;
        }

        //Find the furthest node along the change in rotation.
        //Rotation is given in polar coordinates with (delta_azimuthal, delta_polar)
        //θ is the azimuthal angle (in XY plane, measured CCW from positive X axis && 0 <= θ <= 360), 
        //and φ is the polar angle (From positive Z axis && 0 <= φ <= 180).
        internal Node NextNodeAlongRotation(double[] rotation)
        {
            //If dot product is positive, it matches the arc's direction which was based on node1 to node2.
            var nextNode = rotation.dotProduct(Direction) >= 0 ? Nodes[0] : Nodes[1];
            return nextNode;
        }

        //Given the used node and an arc, get the other node
        internal Node NextNode(Node node)
        {
            var nextNode = Nodes[0] == node ? Nodes[1] : Nodes[0];
            return nextNode;
        }
    }

    /// <summary>
    /// Great Circle based on a gaussian sphere. 
    /// </summary>
    public class GreatCircleAlongArc
    {
        internal List<Arc> ArcList;
        internal List<Intersection> Intersections;
        internal List<Vertex> ReferenceVertices;
        internal double[] Normal { get; set; }
        /// <summary>
        /// The volume of the bounding box. 
        /// Note that antipodal points would result in an infinite number of great circles, but can
        /// be ignored since we are assuming the thickness of this solid is greater than 0.
        /// </summary>
        internal GreatCircleAlongArc(GaussianSphere gaussianSphere, double[] vector1, double[] vector2, Arc referenceArc)
        {
            var antiPoint1 = new[] { -referenceArc.Nodes[0].X, -referenceArc.Nodes[0].Y, -referenceArc.Nodes[0].Z };
            //var antiPoint2 = new[] { -referenceArc.Nodes[1].X, -referenceArc.Nodes[1].Y, -referenceArc.Nodes[1].Z };
            ArcList = new List<Arc>();
            Intersections = new List<Intersection>();
            var tempIntersections = new List<Intersection>();
            ReferenceVertices = new List<Vertex>();
            Normal = vector1.crossProduct(vector2);
            foreach (var arc in gaussianSphere.Arcs)
            {
                if (arc == referenceArc) continue;
                var segmentBool = false;
                //Create two planes given arc and the great circle
                var norm2 = arc.Nodes[0].Vector.crossProduct(arc.Nodes[1].Vector).normalize();
                //Check whether the planes are the same. 
                if (Math.Abs(Normal[0] - norm2[0]) < 0.0001 && Math.Abs(Normal[1] - norm2[1]) < 0.0001
                    && Math.Abs(Normal[2] - norm2[2]) < 0.0001)
                    segmentBool = true; //All points intersect
                if (Math.Abs(Normal[0] + norm2[0]) < 0.0001 && Math.Abs(Normal[1] + norm2[1]) < 0.0001
                    && Math.Abs(Normal[2] + norm2[2]) < 0.0001)
                    segmentBool = true; //All points intersect
                //if (Normal[0].IsPracticallySame(norm2[0]) && Normal[1].IsPracticallySame(norm2[1]) &&
                //    Normal[2].IsPracticallySame(norm2[2])) segmentBool = true; //All points intersect
                //Check whether the planes are the same, but built with opposite normals.
                //if (Normal[0].IsPracticallySame(-norm2[0]) && Normal[1].IsPracticallySame(-norm2[1]) &&
                //    Normal[2].IsPracticallySame(-norm2[2])) segmentBool = true; //All points intersect
                //Set the intersection vertices 
                double[][] vertices;
                if (segmentBool)
                {
                    vertices = new[] { arc.Nodes[0].Vector, arc.Nodes[1].Vector };
                }
                else
                {
                    //Find points of intersection between two planes
                    var position1 = Normal.crossProduct(norm2).normalize();
                    var position2 = new[] { -position1[0], -position1[1], -position1[2] };
                    vertices = new[] { position1, position2 };
                }


                for (var i = 0; i < 2; i++)
                {
                    double l1;
                    double l2;
                    double l3;
                    //If not on the same plane, check to see if intersection is on arc.
                    if (!segmentBool)
                    {
                        l1 = arc.ArcLength;
                        l2 = ArcLength(arc.Nodes[0].Vector, vertices[i]);
                        l3 = ArcLength(arc.Nodes[1].Vector, vertices[i]);

                        //Check to see if the intersection is on the arc. We already know it is on the great circle.
                        //It is ok if the vertex == the node.Vector.
                        var total = l1 - l2 - l3;
                        if ((l2.IsNegligible() && l3.IsNegligible()) || Math.Abs(total) > 0.00001) continue; //Go to next.
                    }
                    var intersectionVertex = new Vertex(vertices[i]);

                    //Find distance from the reference arc's antipodal point 1 in direction of the reference arc.
                    l1 = referenceArc.ArcLength;
                    l2 = Math.PI - referenceArc.ArcLength;
                    l3 = ArcLength(referenceArc.Nodes[0].Vector, vertices[i]);
                    var l4 = ArcLength(referenceArc.Nodes[1].Vector, vertices[i]);
                    var l5 = ArcLength(antiPoint1, vertices[i]);
                    Intersection intersection;

                    //Case 1: Inbetween point1 and point2
                    var total1 = l1 - l3 - l4;
                    //Case 2: Inbetween point2 and antiPoint1
                    var total2 = l2 - l4 - l5;
                    if (Math.Abs(total1) < 0.00001 || Math.Abs(total2) < 0.00001)
                    {
                        intersection = new Intersection(intersectionVertex, l3, arc);
                        tempIntersections.Add(intersection);
                        continue;
                    }
                    //Case 3: Inbetween antiPoint1 and antiPoint2 
                    //Case 4: Inbetween antiPoint2 and Point1
                    intersection = new Intersection(intersectionVertex, 2 * Math.PI - l3, arc);
                    tempIntersections.Add(intersection);

                    //Only one intersection is possible per arc, since the case of multiple intersections is captured above.
                    break;
                }
            }
            var tempIntersections2 = tempIntersections.OrderBy(intersection => intersection.SphericalDistance).ToList();
            //Remove duplicates
            for (var i = 0; i < tempIntersections2.Count - 1; i++)
            {
                var intersection1 = tempIntersections2[i];
                var intersection2 = tempIntersections2[i + 1];
                if (Math.Abs(intersection1.SphericalDistance - intersection2.SphericalDistance) > 0.00001)
                {
                    Intersections.Add(intersection1);

                }
                //Add the last intersection to the list if it was no the same as the current
                if (i == tempIntersections2.Count - 2)
                {
                    Intersections.Add(intersection2);
                }
            }
        }

        internal double ArcLength(double[] double1, double[] double2)
        {
            var arcLength = Math.Acos(double1.dotProduct(double2));
            if (double.IsNaN(arcLength)) arcLength = 0.0;
            return arcLength;
        }
    }


    /// <summary>
    /// Great Circle based on a gaussian sphere. 
    /// </summary>
    public class GreatCircleOrthogonalToArc
    {
        internal List<Arc> ArcList;
        internal List<Intersection> Intersections;
        internal List<Vertex> ReferenceVertices;
        internal double[] Normal { get; set; }
        /// <summary>
        /// The volume of the bounding box. 
        /// Note that antipodal points would result in an infinite number of great circles, but can
        /// be ignored since we are assuming the thickness of this solid is greater than 0.
        /// </summary>
        internal GreatCircleOrthogonalToArc(GaussianSphere gaussianSphere, double[] vector1, double[] vector2, Arc referenceArc)
        {
            ArcList = new List<Arc>();
            Intersections = new List<Intersection>();
            var tempIntersections = new List<Intersection>();
            ReferenceVertices = new List<Vertex>();
            Normal = vector1.crossProduct(vector2);
            foreach (var arc in gaussianSphere.Arcs)
            {
                var segmentBool = false;
                //Create two planes given arc and the great circle
                var norm2 = arc.Nodes[0].Vector.crossProduct(arc.Nodes[1].Vector);
                //Check whether the planes are the same. 
                if (Math.Abs(Normal[0] - norm2[0]) < 0.0001 && Math.Abs(Normal[1] - norm2[1]) < 0.0001
                    && Math.Abs(Normal[2] - norm2[2]) < 0.0001)
                    segmentBool = true; //All points intersect
                if (Math.Abs(Normal[0] + norm2[0]) < 0.0001 && Math.Abs(Normal[1] + norm2[1]) < 0.0001
                    && Math.Abs(Normal[2] + norm2[2]) < 0.0001)
                    segmentBool = true; //All points intersect
                //if (Normal[0].IsPracticallySame(norm2[0]) && Normal[1].IsPracticallySame(norm2[1]) &&
                //    Normal[2].IsPracticallySame(norm2[2])) segmentBool = true; //All points intersect
                //Check whether the planes are the same, but built with opposite normals.
                //if (Normal[0].IsPracticallySame(-norm2[0]) && Normal[1].IsPracticallySame(-norm2[1]) &&
                //    Normal[2].IsPracticallySame(-norm2[2])) segmentBool = true; //All points intersect
                if (segmentBool)
                {
                    //The arc should be on the arc list.
                    ArcList.Add(arc);
                }

                //Find points of intersection between two planes
                var position1 = Normal.crossProduct(norm2).normalize();
                var position2 = new[] { -position1[0], -position1[1], -position1[2] };
                var vertices = new[] { position1, position2 };
                //Check to see if the intersection is on the arc. We already know it is on the great circle.
                for (var i = 0; i < 2; i++)
                {
                    var l1 = arc.ArcLength;
                    var l2 = ArcLength(arc.Nodes[0].Vector, vertices[i]);
                    var l3 = ArcLength(arc.Nodes[1].Vector, vertices[i]);
                    var total = l1 - l2 - l3;
                    if (Math.Abs(total) > 0.00001) continue;
                    var node = arc.NextNodeAlongRotation(referenceArc.Direction);

                    //todo: NEED spherical distance along rotation. Below is an attempt
                    //Subtract the reference arc direction vector from the node and determine where it intersects the great circle
                    var theta = referenceArc.Direction[0] - node.Theta;
                    var phi = referenceArc.Direction[1] - node.Phi;
                    var x = Math.Cos(theta) * Math.Sin(phi);
                    var y = Math.Sin(theta) * Math.Sin(phi);
                    var z = Math.Cos(phi);
                    var point = new[] { x, y, z };
                    //Create two planes given the great circle and this new temporary arc
                    var tempNorm = point.crossProduct(node.Vector);
                    //Find points of intersection between two planes
                    var position3 = Normal.crossProduct(tempNorm).normalize();
                    var position4 = new[] { -position3[0], -position3[1], -position3[2] };
                    var vertices2 = new[] { position3, position4 };
                    for (var j = 0; j < 2; j++)
                    {
                        var tempL1 = ArcLength(node.Vector, point);
                        var tempL2 = ArcLength(node.Vector, vertices2[j]);
                        var tempL3 = ArcLength(point, vertices2[j]);
                        var tempTotal = tempL1 - tempL2 - tempL3;
                        if (Math.Abs(tempTotal) > 0.00001) continue;
                        var intersection = new Intersection(node, tempL3, arc);
                        tempIntersections.Add(intersection);
                        ArcList.Add(arc);
                        break;
                    }
                    break;
                }
            }
            //Sort intersections
            var tempIntersections2 = tempIntersections.OrderBy(intersection => intersection.SphericalDistance).ToList();
            //Remove duplicates
            for (var i = 0; i < tempIntersections2.Count - 1; i++)
            {
                var intersection1 = tempIntersections2[i];
                var intersection2 = tempIntersections2[i + 1];
                if (intersection1.Node != intersection2.Node)
                {
                    Intersections.Add(intersection1);
                    //Add the last intersection to the list if it was no the same as the current
                    if (i == tempIntersections2.Count - 2)
                    {
                        Intersections.Add(intersection2);
                    }
                }
            }
            //Add the reference vertices to a list 
            foreach (var arc in ArcList)
            {
                foreach (var referenceVertex in arc.ReferenceVertices)
                {
                    if (!ReferenceVertices.Contains(referenceVertex)) ReferenceVertices.Add(referenceVertex);
                }
            }
        }

        internal double ArcLength(double[] a, double[] b)
        {
            var arcLength = Math.Acos(a.dotProduct(b));
            if (double.IsNaN(arcLength)) arcLength = 0.0;
            return arcLength;
        }
    }

    /// <summary>
    /// Intersection Class retains information about the type of arc intersection
    /// </summary>
    internal class Intersection
    {
        internal Node Node;
        internal double SphericalDistance;
        internal Vertex Vertex;
        internal Arc ReferenceArc;

        internal Intersection(Node node, double sphericalDistance, Arc referenceArc)
        {
            Node = node;
            SphericalDistance = sphericalDistance;
            ReferenceArc = referenceArc;
            Vertex = null;
        }
        internal Intersection(Vertex vertex, double sphericalDistance, Arc referenceArc)
        {
            Node = null;
            SphericalDistance = sphericalDistance;
            ReferenceArc = referenceArc;
            Vertex = vertex;
        }
    }
}

