using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="GaussianSphere"/> class.
        /// </summary>
        internal GaussianSphere(TessellatedSolid ts)
        {
            Nodes = new List<Node>();
            Arcs = new List<Arc>();
            var i = 0;
            var referenceIndices = new List<int[]>();
            
            foreach (var triangle in ts.ConvexHullFaces)
            {
                //Create the node for this polygonal face
                var node = new Node(triangle);
                
                Nodes.Add(node);

                //Save info to create an arc for every edge that is owned by this face
                //Since an edge is owned by only one face, each edge will
                //only be represented by reference indices once.
                foreach (var edge in triangle.Edges)
                {
                    if (edge.OwnedFace == triangle)
                    {
                        var j = ts.ConvexHullFaces.FindIndex(edge.OtherFace);
                        var referenceIndex = new int[] {i, j};
                        referenceIndices.Add(referenceIndex);
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
        internal PolygonalFace ReferenceFace;
        internal Node(PolygonalFace triangle)
        {
            ReferenceFace = triangle;
            Vector = triangle.Normal; //Set unit normal as location on sphere
            X = triangle.Normal[0];
            X = triangle.Normal[1];
            X = triangle.Normal[2];
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
        private double m;
        private double b;
        internal Arc(Node node1, Node node2)
        {
            Nodes = new List<Node>{node1,node2};
            //Calculate arc length. Base on the following answer, where r = 1 for our unit circle.
            //http://math.stackexchange.com/questions/231221/great-arc-distance-between-two-points-on-a-unit-sphere
            ArcLength = Math.Acos(node1.Vector.dotProduct(node2.Vector));

            //Set slope and intercept to use for intersections
            if (node1.Phi - node2.Phi == 0) //if rise = 0, Horizontal Line (slope = 0).
            {
                m = 0;
                b = node1.Phi;
            }
            else if (node1.Theta - node2.Theta == 0) // if run = 0, Vertical Line (slope = infinity)
            {
                m = double.PositiveInfinity;
                b = double.PositiveInfinity;
            }
            else
            {
              m = (node1.Phi - node2.Phi)/(node1.Theta - node2.Theta);
              b = node1.Phi - m * node1.Phi;
            }     
        }

        internal bool Intersect(Arc arc1, Arc arc2, out double[] intersection, out double[][] segment)
        {
            intersection = null; 
            segment = null;

            //Arc intersection is identical to cartisian intersection, 
            //where the theta and phi angles represent x and y repsectively.
            if (arc1.m - arc2.m == 0 && arc2.b - arc1.b == 0) //The two arcs are on same great circle.
            {
                //todo: determine if arcs overlap and by how much (start and end point)
                return true;
            }
             if (arc1.m - arc2.m == 0) //The two arcs are parallel, but never equal
            {
                return false;
            }
 
            var x = (arc2.b - arc1.b)/(arc1.m - arc2.m);
            var y = arc1.m*x + arc1.b;
            //Intersection is theta(X) and phi(Y).
            intersection = new double[] {x, y};
            return true;
            
        }
    }


    /// <summary>
    /// Gaussian Sphere for a polyhedron
    /// </summary>
    /// NOTE: Using spherical coordinates from mathematics (r, θ, φ), since it follows the right hand rule.
    /// Where r is the radial distance (r = 1 for the unit circle), θ is the azimuthal angle (XY && 0 <= θ <= 360), and φ is the polar angle (From Z axis && 0 <= φ <= 180). 
    public struct GreatCircle
    {
        /// <summary>
        /// The volume of the bounding box.
        /// </summary>
        internal GreatCircle(GaussianSphere gaussianSphere, double[] direction)
        {

        }
    }

    public struct Intersection
    {
        internal Node Node;
        internal GreatCircle GC;
        internal Vertex Vertex;

        internal Intersection(Node node, GreatCircle gc)
        {
            Node = node;
            GC = gc;
            Vertex = null;
        }
        internal Intersection(Vertex vertex, GreatCircle gc)
        {
            Node = null;
            GC = gc;
            Vertex = vertex;
        }
    }
    
}
