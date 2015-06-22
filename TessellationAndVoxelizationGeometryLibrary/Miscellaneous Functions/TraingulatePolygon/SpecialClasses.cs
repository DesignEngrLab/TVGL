using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using MIConvexHull;

namespace TVGL.Miscellaneous_Functions.TraingulatePolygon
{
    #region Node Class
    /// <summary>
    /// Node class used in Triangulate Polygon
    /// Inherets position from point class
    /// </summary>
    public class Node : Point
    {
        #region Properties
        
        public int LoopID { get; private set; }

        /// <summary>
        /// Gets the line that starts at this node.
        /// </summary>
        public Line StartLine { get; set; }

        /// <summary>
        /// Gets the line that ends at this node.
        /// </summary>
        public Line EndLine { get;  set; }

        /// <summary>
        /// Gets the type of  node.
        /// </summary>
        public int Type { get; private set; }

        #endregion
        
        #region Constructor

        /// <summary>
        /// Create a new node from a given point
        /// </summary>
        /// <param name="point"></param>
        public Node(IVertex currentPoint, int nodeType, int loopID)
            : base(new Point(currentPoint))
        {
            LoopID = loopID;
            Type = nodeType;
        }

        #endregion

    }
    #endregion

    #region Trapezoid Class
    /// <summary>
    /// Trapezoid Class
    /// </summary>
    public class Trapezoid
    {
        /// <summary>
        /// Gets the TopNode. Set is through constructor.
        /// </summary>
        public Node TopNode { get; private set; }

        /// <summary>
        /// Gets the BottomNode. Set is through constructor.
        /// </summary>
        public Node BottomNode { get; private set; }

        /// <summary>
        /// Gets the left vertical line of the trapezoid. Set is through constructor.
        /// </summary>
        public Line LeftLine { get; private set; }

        /// <summary>
        /// Gets the right vertical line of the trapezoid. Set is through constructor.
        /// </summary>
        public Line RightLine { get; private set; }

        /// <summary>
        /// Constructs a new trapezoid based on two nodes and two vertical lines.
        /// </summary>
        /// <param name="topNode"></param>
        /// <param name="bottomNode"></param>
        /// <param name="leftLine"></param>
        /// <param name="rightLine"></param>
        public Trapezoid(Node topNode, Node bottomNode, Line leftLine, Line rightLine)
        {
            TopNode = topNode;
            BottomNode = bottomNode;
            LeftLine = leftLine;
            RightLine = rightLine;
        }
    }
    #endregion

    #region Partial Trapezoid Class
    /// <summary>
    /// Partial Trapezoid Class. Used to hold information to create Trapezoids.
    /// </summary>
    public class PartialTrapezoid
    {
        /// <summary>
        /// Gets the TopNode. Set is through constructor.
        /// </summary>
        public Node TopNode { get; private set; }

        /// <summary>
        /// Gets the left vertical line of the trapezoid. Set is through constructor.
        /// </summary>
        public Line LeftLine { get; private set; }

        /// <summary>
        /// Gets the right vertical line of the trapezoid. Set is through constructor.
        /// </summary>
        public Line RightLine { get; private set; }

        /// <summary>
        /// Constructs a partial trapezoid
        /// </summary>
        /// <param name="topNode"></param>
        /// <param name="leftLine"></param>
        /// <param name="rightLine"></param>
        public PartialTrapezoid(Node topNode, Line leftLine, Line rightLine)
        {
            TopNode = topNode;
            LeftLine = leftLine;
            RightLine = rightLine;
        }

        /// <summary>
        /// Checks whether the partial trapezoid contains the two lines.
        /// </summary>
        /// <param name="line1"></param>
        /// <param name="line2"></param>
        /// <returns></returns>
        public bool Contains(Line line1, Line line2)
        {
            if (LeftLine != line1 && LeftLine != line2) return false;
            return RightLine == line1 || RightLine == line2;
        }
    }
    #endregion

    #region Intercept Class
    /// <summary>
    /// Intercept class used for monotone polygon creation
    /// </summary>
    public class Intercept
    {
        #region Properties
        /// <summary>
        /// Gets the X. Set is through the constructor.
        /// </summary>
        public double X { get; private set; }

        /// <summary>
        /// Gets the Y. Set is through the constructor.
        /// </summary>
        public double Y { get; private set; }

        /// <summary>
        /// Gets the reference node. Set is through the constructor.
        /// </summary>
        public Node RefNode { get; private set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Constructs new Intercept based on position and pointing to a reference node
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="node"></param>
        public Intercept(double x, double y, Node node)
        {
            X = x;
            Y = y;
            RefNode = node;
        }
        #endregion
    }
    #endregion

    #region MonotonePolygon class

    /// <summary>
    /// Monotone Polygon, which consists of two ordered chains
    /// The chains start and end at the same nodes
    /// </summary>
    public class MonotonePolygon2
    {
        public MonotonePolygon2(List<Node> leftChain, List<Node> rightChain)
        {
            
        }
    }

    //public class MonotoneChain
    //{
        /// <summary>
        /// Gets or sets monotone chain
        /// </summary>
     ///   public List<Node> Chain { get; set; } 
    //}
    
    public class MonotonePolygon
    {
        /// <summary>
        /// Gets Monochain1. Set is through the constructor.
        /// </summary>
        public List<Node> MonoChain1 { get; private set; }

        /// <summary>
        /// Gets Monochain2. Set is through the constructor.
        /// </summary>
        public List<Node> MonoChain2 { get; private set; }

        /// <summary>
        /// Constructs a MonotonePolygon based on a list of nodes.
        /// </summary>
        /// <param name="nodes"></param>
        public MonotonePolygon(List<Node> nodes)
        {
            #region Get the start and stop nodes
            var maxY = double.NegativeInfinity;
            var minY = double.PositiveInfinity;
            var maxX = double.NegativeInfinity;
            var minX = double.PositiveInfinity;
            Node startNode = null;
            Node stopNode = null;
            foreach (var node in nodes)
            {
                if (node.Y > maxY)
                {
                    maxY = node.Y;
                    minX = node.X;
                    startNode = node;
                }
                else if (Math.Abs(node.Y - maxY) < 0.00001 && node.X < minX)
                {
                    maxY = node.Y;
                    minX = node.X;
                    startNode = node;
                }
                if (node.Y < minY)
                {
                    minY = node.Y;
                    maxX = node.X;
                    stopNode = node;
                }
                else if (Math.Abs(node.Y - minY) < 0.00001 && node.X > maxX)
                {
                    minY = node.Y;
                    maxX = node.X;
                    stopNode = node;
                }
            }
#endregion 

            //Create the Two Monotone Chains
            var i = nodes.IndexOf(startNode);
            var j = nodes.IndexOf(stopNode);
            if (i > j)
            {
                MonoChain1 = nodes.GetRange(i, nodes.Count - 1);
                MonoChain1.AddRange(nodes.GetRange(0,j));
                MonoChain2 = nodes.GetRange(j, i);
            }
            else if (i < j)
            {
                MonoChain2 = nodes.GetRange(j, nodes.Count - 1);
                MonoChain2.AddRange(nodes.GetRange(0,i));
                MonoChain2.Reverse();
                MonoChain1 = nodes.GetRange(i,j);
                MonoChain1.Reverse();
            }
        }
    }
    #endregion

    #region Line Class
    /// <summary>
    /// Line
    /// </summary>
    public class Line
    {

        /// <summary>
        /// Gets the Node which the line is pointing to. Set is through the constructor.
        /// </summary>
        public Node ToNode { get; private set; }

        /// <summary>
        /// Gets the Node which the line is pointing away from. Set is through the constructor.
        /// </summary>
        public Node FromNode { get; private set; }

        private readonly double m;
        private readonly double b;
        
        /// <summary>
        /// Sets to and from nodes as well as slope and intercept of line.
        /// </summary>
        /// <param name="fromNode"></param>
        /// <param name="toNode"></param>
        public Line(Node fromNode, Node toNode)
        {
            FromNode = fromNode;
            ToNode = toNode;
                        
            //Solve for y = mx + b
            m = (ToNode.Y - FromNode.Y) / (ToNode.X - FromNode.X);
            b = ToNode.Y - m * ToNode.X;
        }

        /// <summary>
        /// Gets X intercept given Y
        /// </summary>
        /// <param name="y"></param>
        /// <returns></returns>
        public double Xintercept(double y)
        {
            return (y - b)/m;
        }

        /// <summary>
        ///   Gets Y intercept given X
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public double Yintercept(double x)
        {
            return (x - b)/m;
        }

    }
    #endregion
}
