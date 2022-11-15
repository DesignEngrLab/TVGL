/*******************************************************************************
*                                                                              *
* Author    :  Angus Johnson                                                   *
* Version   :  10.0 (alpha)                                                    *
* Date      :  26 September 2017                                               *
* Website   :  http://www.angusj.com                                           *
* Copyright :  Angus Johnson 2010-2017                                         *
*                                                                              *
* License:                                                                     *
* Use, modification & distribution is subject to Boost Software License Ver 1. *
* http://www.boost.org/LICENSE_1_0.txt                                         *
*                                                                              *
*******************************************************************************/

using System;
using System.Collections.Generic;

namespace ClipperLib2
{

    using Path = List<Point64>;
    using Paths = List<List<Point64>>;

    public enum JoinType { Square, Round, Miter };
    public enum EndType { Polygon, OpenJoined, OpenButt, OpenSquare, OpenRound };

    public struct PointD
    {
        public double X;
        public double Y;

        public PointD(double x = 0, double y = 0)
        {
            this.X = x; this.Y = y;
        }
        public PointD(PointD dp)
        {
            this.X = dp.X; this.Y = dp.Y;
        }
        public PointD(Point64 ip)
        {
            this.X = ip.X; this.Y = ip.Y;
        }
    } //PointD

    internal class PathNode
    {
        internal Path path;
        internal JoinType joinType;
        internal EndType endType;
        internal int lowestIdx;

        public PathNode(Path p, JoinType jt, EndType et)
        {
            joinType = jt;
            endType = et;

            int lenP = p.Count;
            if (et == EndType.Polygon || et == EndType.OpenJoined)
                while (lenP > 1 && p[lenP - 1] == p[0]) lenP--;
            else if (lenP == 2 && p[1] == p[0])
                lenP = 1;
            if (lenP == 0) return;

            if (lenP < 3 && (et == EndType.Polygon || et == EndType.OpenJoined))
            {
                if (jt == JoinType.Round) endType = EndType.OpenRound;
                else endType = EndType.OpenSquare;
            }

            path = new Path(lenP);
            path.Add(p[0]);

            Point64 lastIp = p[0];
            lowestIdx = 0;
            for (int i = 1; i < lenP; i++)
            {
                if (lastIp == p[i]) continue;
                path.Add(p[i]);
                lastIp = p[i];
                if (et != EndType.Polygon) continue;
                if (p[i].Y >= path[lowestIdx].Y &&
                  (p[i].Y > path[lowestIdx].Y || p[i].X < path[lowestIdx].X))
                    lowestIdx = i;
            }
            if (endType == EndType.Polygon && path.Count < 3) path = null;
        }
    } //PathNode


    public class ClipperOffset
    {
        private double delta, sinA, sin, cos;
        //nb: miterLim below is a temp field that differs from the MiterLimit property
        private double miterLim, stepsPerRad;
        private Paths solution;
        private Path pathIn, pathOut;
        private List<PointD> norms = new List<PointD>();
        private List<PathNode> nodes = new List<PathNode>();
        private int lowestIdx;
        public double ArcTolerance { get; set; }
        public double MiterLimit { get; set; }
        private Point64 PointZero = new Point64(0, 0);

        private const double TwoPi = Math.PI * 2;
        private const double DefaultArcFrac = 0.02;
        private const double Tolerance = 1.0E-15;

        //------------------------------------------------------------------------------

        internal static long Round(double value)
        {
            return value < 0 ? (long)(value - 0.5) : (long)(value + 0.5);
        }
        //------------------------------------------------------------------------------

        public static double Area(Path p)
        {
            int cnt = (int)p.Count;
            if (cnt < 3) return 0;
            double a = 0;
            for (int i = 0, j = cnt - 1; i < cnt; ++i)
            {
                a += ((double)p[j].X + p[i].X) * ((double)p[j].Y - p[i].Y);
                j = i;
            }
            return -a * 0.5;
        }
        //------------------------------------------------------------------------------

        public ClipperOffset(double MiterLimit = 2.0, double ArcTolerance = 0)
        {
            this.MiterLimit = MiterLimit;
            this.ArcTolerance = ArcTolerance;
        }
        //------------------------------------------------------------------------------

        public void Clear() { nodes.Clear(); norms.Clear(); solution.Clear(); }
        //------------------------------------------------------------------------------

        public void AddPath(Path p, JoinType jt, EndType et)
        {
            PathNode pn = new PathNode(p, jt, et);
            if (pn.path == null) pn = null;
            else nodes.Add(pn);
        }
        //------------------------------------------------------------------------------

        public void AddPaths(Paths paths, JoinType jt, EndType et)
        {
            foreach (Path p in paths) AddPath(p, jt, et);
        }
        //------------------------------------------------------------------------------

        private void GetLowestPolygonIdx()
        {
            lowestIdx = -1;
            Point64 ip1 = PointZero, ip2;
            for (int i = 0; i < nodes.Count; i++)
            {
                PathNode node = nodes[i];
                if (node.endType != EndType.Polygon) continue;
                if (lowestIdx < 0)
                {
                    ip1 = node.path[node.lowestIdx];
                    lowestIdx = i;
                }
                else
                {
                    ip2 = node.path[node.lowestIdx];
                    if (ip2.Y >= ip1.Y && (ip2.Y > ip1.Y || ip2.X < ip1.X))
                    {
                        lowestIdx = i;
                        ip1 = ip2;
                    }
                }
            }
        }
        //------------------------------------------------------------------------------

        internal static PointD GetUnitNormal(Point64 pt1, Point64 pt2)
        {
            double dx = (pt2.X - pt1.X);
            double dy = (pt2.Y - pt1.Y);
            if ((dx == 0) && (dy == 0)) return new PointD();

            double f = 1 * 1.0 / Math.Sqrt(dx * dx + dy * dy);
            dx *= f;
            dy *= f;

            return new PointD(dy, -dx);
        }
        //------------------------------------------------------------------------------

        void OffsetPoint(int j, ref int k, JoinType jointype)
        {
            //A: angle between adjoining paths on left side (left WRT winding direction).
            //A == 0 deg (or A == 360 deg): collinear edges heading in same direction
            //A == 180 deg: collinear edges heading in opposite directions (ie a 'spike')
            //sin(A) < 0: convex on left.
            //cos(A) > 0: angles on both left and right sides > 90 degrees

            //cross product ...
            sinA = (norms[k].X * norms[j].Y - norms[j].X * norms[k].Y);

            if (Math.Abs(sinA * delta) < 1.0) //angle is approaching 180 or 360 deg.
            {
                //dot product ...
                double cosA = (norms[k].X * norms[j].X + norms[j].Y * norms[k].Y);
                if (cosA > 0) //given condition above the angle is approaching 360 deg.
                {
                    //with angles approaching 360 deg collinear (whether concave or convex),
                    //offsetting with two or more vertices (that would be so close together)
                    //occasionally causes tiny self-intersections due to rounding.
                    //So we offset with just a single vertex here ...
                    pathOut.Add(new Point64(Round(pathIn[j].X + norms[k].X * delta),
                      Round(pathIn[j].Y + norms[k].Y * delta)));
                    return;
                }
            }
            else if (sinA > 1.0) sinA = 1.0;
            else if (sinA < -1.0) sinA = -1.0;

            if (sinA * delta < 0) //ie a concave offset
            {
                pathOut.Add(new Point64(Round(pathIn[j].X + norms[k].X * delta),
                  Round(pathIn[j].Y + norms[k].Y * delta)));
                pathOut.Add(pathIn[j]);
                pathOut.Add(new Point64(Round(pathIn[j].X + norms[j].X * delta),
                  Round(pathIn[j].Y + norms[j].Y * delta)));
            }
            else
            {
                //convex offsets here ...
                switch (jointype)
                {
                    case JoinType.Miter:
                        double cosA = (norms[j].X * norms[k].X + norms[j].Y * norms[k].Y);
                        //see offset_triginometry3.svg
                        if (1 + cosA < miterLim) DoSquare(j, k);
                        else DoMiter(j, k, 1 + cosA);
                        break;
                    case JoinType.Square:
                        cosA = (norms[j].X * norms[k].X + norms[j].Y * norms[k].Y);
                        if (cosA >= 0) DoMiter(j, k, 1 + cosA); //angles >= 90 deg. don't need squaring
                        else DoSquare(j, k);
                        break;
                    case JoinType.Round:
                        DoRound(j, k);
                        break;
                }
            }
            k = j;
        }
        //------------------------------------------------------------------------------

        internal void DoSquare(int j, int k)
        {
            //Two vertices, one using the prior offset's (k) normal one the current (j).
            //Do a 'normal' offset (by delta) and then another by 'de-normaling' the
            //normal hence parallel to the direction of the respective edges.
            if (delta > 0)
            {
                pathOut.Add(new Point64(
                  Round(pathIn[j].X + delta * (norms[k].X - norms[k].Y)),
                  Round(pathIn[j].Y + delta * (norms[k].Y + norms[k].X))));
                pathOut.Add(new Point64(
                  Round(pathIn[j].X + delta * (norms[j].X + norms[j].Y)),
                  Round(pathIn[j].Y + delta * (norms[j].Y - norms[j].X))));
            }
            else
            {
                pathOut.Add(new Point64(
                  Round(pathIn[j].X + delta * (norms[k].X + norms[k].Y)),
                  Round(pathIn[j].Y + delta * (norms[k].Y - norms[k].X))));
                pathOut.Add(new Point64(
                  Round(pathIn[j].X + delta * (norms[j].X - norms[j].Y)),
                  Round(pathIn[j].Y + delta * (norms[j].Y + norms[j].X))));
            }
        }
        //------------------------------------------------------------------------------

        internal void DoMiter(int j, int k, double cosAplus1)
        {
            //see offset_triginometry4.svg
            double q = delta / cosAplus1; //0 < cosAplus1 <= 2
            pathOut.Add(new Point64(Round(pathIn[j].X + (norms[k].X + norms[j].X) * q),
              Round(pathIn[j].Y + (norms[k].Y + norms[j].Y) * q)));
        }
        //------------------------------------------------------------------------------

        internal void DoRound(int j, int k)
        {
            double a = Math.Atan2(sinA,
            norms[k].X * norms[j].X + norms[k].Y * norms[j].Y);
            int steps = Math.Max((int)Round(stepsPerRad * Math.Abs(a)), 1);

            double X = norms[k].X, Y = norms[k].Y, X2;
            for (int i = 0; i < steps; ++i)
            {
                pathOut.Add(new Point64(
                  Round(pathIn[j].X + X * delta),
                  Round(pathIn[j].Y + Y * delta)));
                X2 = X;
                X = X * cos - sin * Y;
                Y = X2 * sin + Y * cos;
            }
            pathOut.Add(new Point64(
            Round(pathIn[j].X + norms[j].X * delta),
            Round(pathIn[j].Y + norms[j].Y * delta)));
        }
        //------------------------------------------------------------------------------
        private void DoOffset(double d)
        {
            solution = null;
            delta = d;
            double absDelta = Math.Abs(d);

            //if a Zero offset, then just copy CLOSED polygons to FSolution and return ...
            if (absDelta < Tolerance)
            {
                solution = new Paths(nodes.Count);
                foreach (PathNode node in nodes)
                    if (node.endType == EndType.Polygon) solution.Add(node.path);
                return;
            }

            //MiterLimit: see offset_triginometry3.svg in the documentation folder ...
            if (MiterLimit > 2)
                miterLim = 2 / (MiterLimit * MiterLimit);
            else
                miterLim = 0.5;

            double arcTol;
            if (ArcTolerance < DefaultArcFrac)
                arcTol = absDelta * DefaultArcFrac;
            else
                arcTol = ArcTolerance;

            //see offset_triginometry2.svg in the documentation folder ...
            double steps = Math.PI / Math.Acos(1 - arcTol / absDelta);  //steps per 360 degrees
            if (steps > absDelta * Math.PI) steps = absDelta * Math.PI; //ie excessive precision check

            sin = Math.Sin(TwoPi / steps);
            cos = Math.Cos(TwoPi / steps);
            if (d < 0) sin = -sin;
            stepsPerRad = steps / TwoPi;

            solution = new Paths(nodes.Count * 2);
            foreach (PathNode node in nodes)
            {
                pathIn = node.path;
                pathOut = new Path();
                int pathInCnt = pathIn.Count;

                //if a single vertex then build circle or a square ...
                if (pathInCnt == 1)
                {
                    if (node.joinType == JoinType.Round)
                    {
                        double X = 1.0, Y = 0.0;
                        for (int j = 1; j <= steps; j++)
                        {
                            pathOut.Add(new Point64(
                              Round(pathIn[0].X + X * delta),
                              Round(pathIn[0].Y + Y * delta)));
                            double X2 = X;
                            X = X * cos - sin * Y;
                            Y = X2 * sin + Y * cos;
                        }
                    }
                    else
                    {
                        double X = -1.0, Y = -1.0;
                        for (int j = 0; j < 4; ++j)
                        {
                            pathOut.Add(new Point64(
                              Round(pathIn[0].X + X * delta),
                              Round(pathIn[0].Y + Y * delta)));
                            if (X < 0) X = 1;
                            else if (Y < 0) Y = 1;
                            else X = -1;
                        }
                    }
                    solution.Add(pathOut);
                    continue;
                } //end of single vertex offsetting

                //build norms ...
                norms.Clear();
                norms.Capacity = pathInCnt;
                for (int j = 0; j < pathInCnt - 1; j++)
                    norms.Add(GetUnitNormal(pathIn[j], pathIn[j + 1]));
                if (node.endType == EndType.OpenJoined || node.endType == EndType.Polygon)
                    norms.Add(GetUnitNormal(pathIn[pathInCnt - 1], pathIn[0]));
                else
                    norms.Add(new PointD(norms[pathInCnt - 2]));

                if (node.endType == EndType.Polygon)
                {
                    int k = pathInCnt - 1;
                    for (int j = 0; j < pathInCnt; j++)
                        OffsetPoint(j, ref k, node.joinType);
                    solution.Add(pathOut);
                }
                else if (node.endType == EndType.OpenJoined)
                {
                    int k = pathInCnt - 1;
                    for (int j = 0; j < pathInCnt; j++)
                        OffsetPoint(j, ref k, node.joinType);
                    solution.Add(pathOut);
                    pathOut = new Path();
                    //re-build norms ...
                    PointD n = norms[pathInCnt - 1];
                    for (int j = pathInCnt - 1; j > 0; j--)
                        norms[j] = new PointD(-norms[j - 1].X, -norms[j - 1].Y);
                    norms[0] = new PointD(-n.X, -n.Y);
                    k = 0;
                    for (int j = pathInCnt - 1; j >= 0; j--)
                        OffsetPoint(j, ref k, node.joinType);
                    solution.Add(pathOut);
                }
                else
                {
                    int k = 0;
                    for (int j = 1; j < pathInCnt - 1; j++)
                        OffsetPoint(j, ref k, node.joinType);

                    Point64 pt1;
                    if (node.endType == EndType.OpenButt)
                    {
                        int j = pathInCnt - 1;
                        pt1 = new Point64((long)Round(pathIn[j].X + norms[j].X *
                          delta), (long)Round(pathIn[j].Y + norms[j].Y * delta));
                        pathOut.Add(pt1);
                        pt1 = new Point64((long)Round(pathIn[j].X - norms[j].X *
                          delta), (long)Round(pathIn[j].Y - norms[j].Y * delta));
                        pathOut.Add(pt1);
                    }
                    else
                    {
                        int j = pathInCnt - 1;
                        k = pathInCnt - 2;
                        sinA = 0;
                        norms[j] = new PointD(-norms[j].X, -norms[j].Y);
                        if (node.endType == EndType.OpenSquare)
                            DoSquare(j, k);
                        else
                            DoRound(j, k);
                    }

                    //reverse norms ...
                    for (int j = pathInCnt - 1; j > 0; j--)
                        norms[j] = new PointD(-norms[j - 1].X, -norms[j - 1].Y);
                    norms[0] = new PointD(-norms[1].X, -norms[1].Y);

                    k = pathInCnt - 1;
                    for (int j = k - 1; j > 0; --j) OffsetPoint(j, ref k, node.joinType);

                    if (node.endType == EndType.OpenButt)
                    {
                        pt1 = new Point64((long)Round(pathIn[0].X - norms[0].X * delta),
                          (long)Round(pathIn[0].Y - norms[0].Y * delta));
                        pathOut.Add(pt1);
                        pt1 = new Point64((long)Round(pathIn[0].X + norms[0].X * delta),
                          (long)Round(pathIn[0].Y + norms[0].Y * delta));
                        pathOut.Add(pt1);
                    }
                    else
                    {
                        k = 1;
                        sinA = 0;
                        if (node.endType == EndType.OpenSquare)
                            DoSquare(0, 1);
                        else
                            DoRound(0, 1);
                    }
                    solution.Add(pathOut);
                }
            }
        }
        //------------------------------------------------------------------------------

        public void Execute(ref Paths sol, double delta)
        {
            sol.Clear();
            if (nodes.Count == 0) return;

            GetLowestPolygonIdx();
            bool negate = (lowestIdx >= 0 && Area(nodes[lowestIdx].path) < 0);
            //if polygon orientations are reversed, then 'negate' ...
            if (negate) this.delta = -delta;
            else this.delta = delta;
            DoOffset(this.delta);

            //now clean up 'corners' ...
            Clipper clpr = new Clipper();
            clpr.AddPaths(solution, PathType.Subject);
            if (negate)
                clpr.Execute(ClipType.Union, sol, FillRule.Negative);
            else
                clpr.Execute(ClipType.Union, sol, FillRule.Positive);
        }
        //------------------------------------------------------------------------------

        public static Paths OffsetPaths(Paths pp, double delta, JoinType jt, EndType et)
        {
            Paths result = new Paths();
            ClipperOffset co = new ClipperOffset();
            co.AddPaths(pp, jt, et);
            co.Execute(ref result, delta);
            return result;
        }
        //------------------------------------------------------------------------------

    } //ClipperOffset

} //namespace
