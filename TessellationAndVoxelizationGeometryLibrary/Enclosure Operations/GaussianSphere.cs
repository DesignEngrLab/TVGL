using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using amf;
using StarMathLib;

namespace TVGL.Enclosure_Operations
{
    /// <summary>
    /// Gaussian Sphere for a polyhedron
    /// </summary>
    /// NOTE: Using spherical coordinates from mathematics (r, θ, φ), since it follows the right hand rule.
    /// Where r is the radial distance (r = 1 for the unit circle), θ is the azimuthal angle (XY && 0 <= θ <= 360), and φ is the polar angle (From Z axis && 0 <= φ <= 180). 
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
            var i = 0;
            var referenceIndices = new List<int[]>();
            foreach (var triangle in ts.ConvexHullFaces)
            {
                //todo: Clean triangles at the source
                var sameIndex = Nodes.FindIndex(p => p.Vector.IsPracticallySame(triangle.Normal));
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

            foreach (var node in Nodes)
            {
                //Save info to create an arc for every edge that is owned by this face
                //Since an edge is owned by only one face, each edge will
                //only be represented by reference indices once.
                foreach (var edge in node.ReferenceEdges)
                {
                    foreach (var triangle in node.ReferenceFaces)
                    {
                        if (edge.OwnedFace == triangle)
                        {
                            var j = ts.ConvexHullFaces.FindIndex(edge.OtherFace);
                            var referenceIndex = new int[] { i, j };
                            referenceIndices.Add(referenceIndex);
                        }
                    }  
                }
                i++;
            }

            //Now that all the nodes are created, and they have the same indices as the faces
            //We can create arcs that point to nodes, and add that reference back to the nodes.
            foreach (var referenceIndex in referenceIndices)
            {
                var arc = new Arc(Nodes[referenceIndex[0]],Nodes[referenceIndex[1]]);
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
        internal Node(PolygonalFace triangle)
        {
            ReferenceFaces = new List<PolygonalFace>{triangle};
            ReferenceEdges = triangle.Edges.ToList();
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
                Theta = Math.Atan(triangle.Normal[1] / triangle.Normal[0]) + 2*Math.PI;
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
        private readonly double m;
        private readonly double b;
        internal Arc(Node node1, Node node2)
        {
            Nodes = new List<Node>{node1,node2};
            ReferenceVertices = new List<Vertex>();
            //Calculate arc length. Base on the following answer, where r = 1 for our unit circle.
            //http://math.stackexchange.com/questions/231221/great-arc-distance-between-two-points-on-a-unit-sphere
            //Note that the arc length must be the smaller of the two directions around the sphere.
            ArcLength = Math.Acos(node1.Vector.dotProduct(node2.Vector));

            //Set slope and intercept to use for intersections
            if (node1.Phi.IsPracticallySame(node2.Phi)) //if rise = 0, Horizontal Line (slope = 0).
            {
                m = 0;
                b = node1.Phi;
            }
            else if (node1.Theta.IsPracticallySame(node2.Theta)) // if run = 0, Vertical Line (slope = infinity)
            {
                m = double.PositiveInfinity;
                b = double.PositiveInfinity;
            }
            else
            {
              m = (node1.Phi - node2.Phi)/(node1.Theta - node2.Theta);
              b = node1.Phi - m * node1.Phi;
            }

            //todo: Fix this:
            //Find reference vertices. There should be 2 for every arc.
            //foreach (var referenceFace in node1.ReferenceFace.Vertices)
            //{
            //    foreach (var referenceVertex2 in node2.ReferenceFace.Vertices)
             //   {
            //        if (referenceVertex1 == referenceVertex2) ReferenceVertices.Add(referenceVertex1);
            //    }
            //}
            if (ReferenceVertices.Count != 2) throw new System.ArgumentException("Incorrect number of reference vertices");
        }

        internal bool Intersect(Arc arc1, Arc arc2, out Vertex intersection)
        {
            intersection = null; 
            
            //Create two planes given arc1 and arc2
            var norm1 = arc1.Nodes[0].Vector.crossProduct(arc1.Nodes[1].Vector); //unit normal
            var norm2 = arc2.Nodes[0].Vector.crossProduct(arc2.Nodes[1].Vector);
            //Check whether the planes are the same. 
            if (norm1[0].IsPracticallySame(norm2[0]) && norm1[1].IsPracticallySame(norm2[1]) &&
                norm1[2].IsPracticallySame(norm2[2])) return true; //All points intersect
            //Check whether the planes are the same, but built with opposite normals.
            if (norm1[0].IsPracticallySame(-norm2[0]) && norm1[1].IsPracticallySame(-norm2[1]) &&
                norm1[2].IsPracticallySame(-norm2[2])) return true; //All points intersect
            //Find points of intersection between two planes
            var position1 = norm1.crossProduct(norm2).normalize(); 
            var position2 = new [] {-position1[0], -position1[1], -position1[2]};
            var vertices = new []{new Vertex(position1) , new Vertex(position2), };
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

        internal Node NextNodeAlongRotation(double[] rotation, Arc arc)
        {
            foreach (var node in arc.Nodes)
            {
                //todo: implement next node function
            }
            Node node1 = null;
            return node1;
        }
    }

    /// <summary>
    /// Great Circle based on a gaussian sphere. 
    /// </summary>
    public class GreatCircle
    {
        private readonly double[] vector1;
        private readonly double[] vector2;
        internal List<Arc> ArcList;
        internal List<Vertex> IntersectionVertices;
        internal List<Vertex> ReferenceVertices;
        internal double[] Normal { get; set; }
        /// <summary>
        /// The volume of the bounding box. 
        /// Note that antipodal points would result in an infinite number of great circles, but can
        /// be ignored since we are assuming the thickness of this solid is greater than 0.
        /// </summary>
        internal GreatCircle(GaussianSphere gaussianSphere, double[] vector1, double[] vector2)
        {
            this.vector1 = vector1;
            this.vector2 = vector2;
            ArcList = new List<Arc>();
            IntersectionVertices = new List<Vertex>();
            ReferenceVertices = new List<Vertex>();
            Normal = vector1.crossProduct(vector2);
            var segmentBool = false;
            foreach (var arc in gaussianSphere.Arcs)
            {
                //Create two planes given arc and the great circle
                var norm2 = arc.Nodes[0].Vector.crossProduct(arc.Nodes[1].Vector);
                //Check whether the planes are the same. 
                if (Normal[0].IsPracticallySame(norm2[0]) && Normal[1].IsPracticallySame(norm2[1]) &&
                    Normal[2].IsPracticallySame(norm2[2])) segmentBool = true; //All points intersect
                //Check whether the planes are the same, but built with opposite normals.
                if (Normal[0].IsPracticallySame(-norm2[0]) && Normal[1].IsPracticallySame(-norm2[1]) &&
                    Normal[2].IsPracticallySame(-norm2[2])) segmentBool = true; //All points intersect
                if (segmentBool)
                {
                    //Both nodes are intersection vertices and the arc should be on the arc list.
                    IntersectionVertices.Add(new Vertex(arc.Nodes[0].Vector));
                    IntersectionVertices.Add(new Vertex(arc.Nodes[1].Vector));
                    ArcList.Add(arc);
                    continue;
                }

                //Find points of intersection between two planes
                var position1 = Normal.crossProduct(norm2).normalize();
                var position2 = new[] { -position1[0], -position1[1], -position1[2] };
                var vertices = new[] { new Vertex(position1), new Vertex(position2), };
                //Check to see if the intersection is on the arc. We already know it is on the great circle.
                for (var i = 0; i < 2; i++)
                {
                    var l1 = arc.ArcLength;
                    var l2 = Math.Acos(arc.Nodes[0].Vector.dotProduct(vertices[i].Position));
                    var l3 = Math.Acos(arc.Nodes[1].Vector.dotProduct(vertices[i].Position));
                    var total = l1 - l2 - l3;
                    if (!total.IsNegligible()) continue;
                    IntersectionVertices.Add(vertices[i]);
                    ArcList.Add(arc);
                    break;
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
    }

    /// <summary>
    /// Intersection Class retains information about the type of arc intersection
    /// </summary>
    internal class Intersection
    {
        internal Node Node;
        internal GreatCircle GreatCircle;
        internal Vertex Vertex;

        internal Intersection(Node node, GreatCircle greatCircle)
        {
            Node = node;
            GreatCircle = greatCircle;
            Vertex = null;
        }
        internal Intersection(Vertex vertex, GreatCircle greatCircle)
        {
            Node = null;
            GreatCircle = greatCircle;
            Vertex = vertex;
        }
    }
    
}
