// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 04-18-2016
//
// Last Modified By : Design Engineering Lab
// Last Modified On : 05-26-2016
// ***********************************************************************
// <copyright file="SpecialClasses.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System.Collections.Generic;
using StarMathLib;

namespace TVGL
{
    /// <summary>
    ///     Enum NodeType
    /// </summary>
    internal enum NodeType
    {
        /// <summary>
        ///     The downward reflex
        /// </summary>
        DownwardReflex,
        UpwardReflex,
        Peak,
        Root,
        Left,
        Right,

        /// <summary>
        ///     The duplicate
        /// </summary>
        Duplicate
    }

    #region Node Class

    /// <summary>
    ///     Node class used in Triangulate Polygon
    ///     Inherets position from point class
    /// </summary>
    internal class Node
    {
        #region Properties

        /// <summary>
        ///     Gets the loop ID that this node belongs to.
        /// </summary>
        /// <value>The loop identifier.</value>
        internal int LoopID { get; set; }

        /// <summary>
        ///     Gets or sets the x.
        /// </summary>
        /// <value>The x.</value>
        public double X { get; }

        /// <summary>
        ///     Gets or sets the y.
        /// </summary>
        /// <value>The y.</value>
        public double Y { get; }

        /// <summary>
        ///     Gets or sets the z coordinate. If one is using Point in a 2D capacity, it can be ignored.
        /// </summary>
        /// <value>The z.</value>
        public double Z { get; private set; }

        /// <summary>
        ///     Gets the line that starts at this node.
        /// </summary>
        /// <value>The start line.</value>
        internal NodeLine StartLine { get; set; }

        /// <summary>
        ///     Gets the line that ends at this node.
        /// </summary>
        /// <value>The end line.</value>
        internal NodeLine EndLine { get; set; }

        /// <summary>
        ///     Gets the type of  node.
        /// </summary>
        /// <value>The type.</value>
        internal NodeType Type { get; set; }

        /// <summary>
        ///     Gets the base class, Point of this node.
        /// </summary>
        /// <value>The point.</value>
        internal Point Point { get; private set; }

        /// <summary>
        ///     Gets the base class, Point of this node.
        /// </summary>
        /// <value><c>true</c> if this instance is right chain; otherwise, <c>false</c>.</value>
        internal bool IsRightChain { get; set; }

        /// <summary>
        ///     Gets the base class, Point of this node.
        /// </summary>
        /// <value><c>true</c> if this instance is left chain; otherwise, <c>false</c>.</value>
        internal bool IsLeftChain { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        ///     Create a new node from a given point
        /// </summary>
        /// <param name="currentPoint">The current point.</param>
        /// <param name="nodeType">Type of the node.</param>
        /// <param name="loopID">The loop identifier.</param>
        internal Node(Point currentPoint, NodeType nodeType, int loopID)
        {
            LoopID = loopID;
            Type = nodeType;
            Point = currentPoint;
            X = currentPoint.X;
            Y = currentPoint.Y;
            Z = currentPoint.Z;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Node" /> class.
        /// </summary>
        /// <param name="currentPoint">The current point.</param>
        /// <param name="loopID">The loop identifier.</param>
        internal Node(Point currentPoint, int loopID)
        {
            LoopID = loopID;
            Point = currentPoint;
            X = currentPoint.X;
            Y = currentPoint.Y;
            Z = currentPoint.Z;
        }

        #endregion
    }

    #endregion

    #region Trapezoid Class

    /// <summary>
    ///     Trapezoid Class
    /// </summary>
    internal class Trapezoid
    {
        #region Constructor

        /// <summary>
        ///     Constructs a new trapezoid based on two nodes and two vertical lines.
        /// </summary>
        /// <param name="topNode">The top node.</param>
        /// <param name="bottomNode">The bottom node.</param>
        /// <param name="leftLine">The left line.</param>
        /// <param name="rightLine">The right line.</param>
        internal Trapezoid(Node topNode, Node bottomNode, NodeLine leftLine, NodeLine rightLine)
        {
            TopNode = topNode;
            BottomNode = bottomNode;
            LeftLine = leftLine;
            RightLine = rightLine;
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the TopNode. Set is through constructor.
        /// </summary>
        /// <value>The top node.</value>
        internal Node TopNode { get; private set; }

        /// <summary>
        ///     Gets the BottomNode. Set is through constructor.
        /// </summary>
        /// <value>The bottom node.</value>
        internal Node BottomNode { get; private set; }

        /// <summary>
        ///     Gets the left vertical line of the trapezoid. Set is through constructor.
        /// </summary>
        /// <value>The left line.</value>
        internal NodeLine LeftLine { get; private set; }

        /// <summary>
        ///     Gets the right vertical line of the trapezoid. Set is through constructor.
        /// </summary>
        /// <value>The right line.</value>
        internal NodeLine RightLine { get; private set; }

        #endregion
    }

    #endregion

    #region Partial Trapezoid Class

    /// <summary>
    ///     Partial Trapezoid Class. Used to hold information to create Trapezoids.
    /// </summary>
    internal class PartialTrapezoid
    {
        /// <summary>
        ///     Constructs a partial trapezoid
        /// </summary>
        /// <param name="topNode">The top node.</param>
        /// <param name="leftLine">The left line.</param>
        /// <param name="rightLine">The right line.</param>
        internal PartialTrapezoid(Node topNode, NodeLine leftLine, NodeLine rightLine)
        {
            TopNode = topNode;
            LeftLine = leftLine;
            RightLine = rightLine;
        }

        /// <summary>
        ///     Gets the TopNode. Set is through constructor.
        /// </summary>
        /// <value>The top node.</value>
        internal Node TopNode { get; private set; }

        /// <summary>
        ///     Gets the left vertical line of the trapezoid. Set is through constructor.
        /// </summary>
        /// <value>The left line.</value>
        internal NodeLine LeftLine { get; }

        /// <summary>
        ///     Gets the right vertical line of the trapezoid. Set is through constructor.
        /// </summary>
        /// <value>The right line.</value>
        internal NodeLine RightLine { get; }

        /// <summary>
        ///     Checks whether the partial trapezoid contains the two lines.
        /// </summary>
        /// <param name="line1">The line1.</param>
        /// <param name="line2">The line2.</param>
        /// <returns><c>true</c> if [contains] [the specified line1]; otherwise, <c>false</c>.</returns>
        internal bool Contains(NodeLine line1, NodeLine line2)
        {
            if (LeftLine != line1 && LeftLine != line2) return false;
            return RightLine == line1 || RightLine == line2;
        }
    }

    #endregion

    #region MonotonePolygon class

    /// <summary>
    ///     Monotone Polygon, which consists of two ordered chains
    ///     The chains start and end at the same nodes
    /// </summary>
    internal class MonotonePolygon
    {
        #region Constructor

        /// <summary>
        ///     Constructs a MonotonePolygon based on a list of nodes.
        /// </summary>
        /// <param name="leftChain">The left chain.</param>
        /// <param name="rightChain">The right chain.</param>
        /// <param name="sortedNodes">The sorted nodes.</param>
        internal MonotonePolygon(List<Node> leftChain, List<Node> rightChain, List<Node> sortedNodes)
        {
            LeftChain = leftChain;
            RightChain = rightChain;
            SortedNodes = sortedNodes;
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets Monochain1. Set is through the constructor.
        /// </summary>
        /// <value>The left chain.</value>
        internal List<Node> LeftChain { get; private set; }

        /// <summary>
        ///     Gets Monochain2. Set is through the constructor.
        /// </summary>
        /// <value>The right chain.</value>
        internal List<Node> RightChain { get; private set; }

        /// <summary>
        ///     Gets Monochain2. Set is through the constructor.
        /// </summary>
        /// <value>The sorted nodes.</value>
        internal List<Node> SortedNodes { get; private set; }

        #endregion
    }

    #endregion

    #region NodeLine Class

    /// <summary>
    ///     NodeLine
    /// </summary>
    internal class NodeLine
    {
        /// <summary>
        ///     Sets to and from nodes as well as slope and intercept of line.
        /// </summary>
        /// <param name="fromNode">From node.</param>
        /// <param name="toNode">To node.</param>
        internal NodeLine(Node fromNode, Node toNode)
        {
            FromNode = fromNode;
            ToNode = toNode;

            //Solve for slope and y intercept. 
            if (ToNode.X.IsPracticallySame(FromNode.X)) //If vertical line, set slope = inf.
            {
                m = double.PositiveInfinity;
                b = double.PositiveInfinity;
            }

            else if (ToNode.Y.IsPracticallySame(FromNode.Y)) //If horizontal line, set slope = 0.
            {
                m = 0.0;
                b = ToNode.Y;
            }
            else //Else y = mx + b
            {
                m = (ToNode.Y - FromNode.Y)/(ToNode.X - FromNode.X);
                b = ToNode.Y - m*ToNode.X;
            }
        }

        /// <summary>
        ///     Gets the Node which the line is pointing to. Set is through the constructor.
        /// </summary>
        /// <value>To node.</value>
        internal Node ToNode { get; private set; }

        /// <summary>
        ///     Gets the Node which the line is pointing away from. Set is through the constructor.
        /// </summary>
        /// <value>From node.</value>
        internal Node FromNode { get; private set; }

        // ReSharper disable once InconsistentNaming
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
        /// <summary>
        ///     Gets the m.
        /// </summary>
        /// <value>The m.</value>
        internal double m { get; private set; }

        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Gets the b.
        /// </summary>
        /// <value>The b.</value>
        internal double b { get; private set; }

        /// <summary>
        ///     Gets X intercept given Y
        /// </summary>
        /// <param name="y">The y.</param>
        /// <returns>System.Double.</returns>
        internal double Xintercept(double y)
        {
            //If basically a vertical line, return an x value on that line (e.g., ToNode.X)
            if (m >= double.PositiveInfinity)
            {
                return FromNode.X;
            }

            //If a flat line give either positive or negative infinity depending on the direction of the line.
            if (m.IsNegligible())
            {
                if (ToNode.X - FromNode.X > 0)
                {
                    return double.PositiveInfinity;
                }
                return double.NegativeInfinity;
            }
            return (y - b)/m;
        }

        /// <summary>
        ///     Reverses this instance.
        /// </summary>
        internal void Reverse()
        {
            var tempNode = FromNode;
            FromNode = ToNode;
            ToNode = tempNode;
        }
    }

    #endregion
}