using Priority_Queue;
using System;
using System.Collections.Generic;
using System.Linq;
using TVGL.Numerics;
using static ClipperLib2Beta.ClipperMath;

/*******************************************************************************
* Author    :  Angus Johnson                                                   *
* Version   :  10.0 (beta)                                                     *
* Date      :  21 November 2020                                                *
* Website   :  http://www.angusj.com                                           *
* Copyright :  Angus Johnson 2010-2020                                         *
* Purpose   :  Polygon 'clipping' (boolean operations on polygons)             *
* License   :  http://www.boost.org/LICENSE_1_0.txt                            *
*******************************************************************************/

namespace ClipperLib2Beta
{
	using OutRecList = List<OutRec>;
	using IntersectList = List<IntersectNode>;
	using ScanlinePriorityQueue = SortedSet<long>;
	using MinimaList = List<LocalMinima>;
	using VertexList = List<Vertex>;

	//------------------------------------------------------------------------------
	// Miscellaneous structures etc.
	//------------------------------------------------------------------------------
	[Flags]
	internal enum VertexFlags { vfNone = 0, vfOpenStart = 1, vfOpenEnd = 2, vfLocalMax = 4, vfLocalMin = 8 }
	internal enum OutRecState { Undefined, Open, Outer, OuterCheck, Inner, InnerCheck };

	public enum ClipType { None, Intersection, Union, Difference, Xor };
	public enum PathType { Subject, Clip };
	//By far the most widely used winding rules for polygon filling are
	//EvenOdd & NonZero (GDI, GDI+, XLib, OpenGL, Cairo, AGG, Quartz, SVG, Gr32)
	//Others rules include Positive, Negative and ABS_GTR_EQ_TWO (only in OpenGL)
	//see http://glprogramming.com/red/chapter11.html
	public enum FillRule { EvenOdd, NonZero, Positive, Negative };

	public enum PipResult { Inside, Outside, OnEdge };

	//Vertex must be a class to avoid circular references introduced as struct objects.
	internal class Vertex
	{
		internal PointI pt;
		internal Vertex next;
		internal Vertex prev;
		internal VertexFlags flags = VertexFlags.vfNone;
	}

	//Every closed path (or polygon) is made up of a series of vertices forming
	//edges that alternate between going up (relative to the Y-axis) and going
	//down. Edges consecutively going up or consecutively going down are called
	//'bounds' (or sides if they're simple polygons). 'Local Minima' refer to
	//vertices where descending bounds become ascending ones.

	internal class LocalMinima
	{
		internal Vertex vertex;
		internal PathType polytype = PathType.Subject;
		internal bool is_open = false;
	}

	public class OutPt
	{
		internal PointI pt;
		internal OutPt next;
		internal OutPt prev;
		internal OutRec outrec;  //used in descendant classes
	};

	//OutRec: contains a path in the clipping solution. Edges in the AEL will
	//have OutRec pointers assigned when they form part of the clipping solution.
	public class OutRec
	{
		internal int idx;
		internal OutRec owner;
		internal Active front_e;
		internal Active back_e;
		internal OutPt pts;
		internal PolyTreeI PolyTree;
		internal OutRecState state;
	};

	//Active: an edge in the AEL that may or may not be 'hot' (part of the clip solution).
	internal class Active
	{
		internal OutPt op;  //used in descendant classes
		internal PointI bot;
		internal PointI top;
		internal long curr_x = 0;  //current (updated at every new scanline)
		internal double dx = 0.0;
		internal int wind_dx = 1;  //1 or -1 depending on winding direction
		internal int wind_cnt = 0;
		internal int wind_cnt2 = 0;  //winding count of the opposite polytype
		public OutRec outrec;
		//AEL: 'active edge list' (Vatti's AET - active edge table)
		//     a linked list of all edges (from left to right) that are present
		//     (or 'active') within the current scanbeam (a horizontal 'beam' that
		//     sweeps from bottom to top over the paths in the clipping operation).
		internal Active prev_in_ael;
		internal Active next_in_ael;
		//SEL: 'sorted edge list' (Vatti's ST - sorted table)
		//     linked list used when sorting edges into their new positions at the
		//     top of scanbeams, but also (re)used to process horizontals.
		internal Active prev_in_sel;
		internal Active next_in_sel;
		internal Active jump;
		internal Vertex vertex_top;
		internal LocalMinima local_min;  //the bottom of an edge 'bound' (also Vatti)
	};

	public class ScanLine
	{
		internal ScanLine(long y)
        {
			Y = y;
        }
		internal long Y;
		internal ScanLine next;
	};

	public class IntersectNode
	{
		internal PointI pt;
		internal Active edge1;
		internal Active edge2;
	};

	//------------------------------------------------------------------------------
	// Clipper 
	//------------------------------------------------------------------------------
	public abstract class Clipper
	{
		internal long botY { get; set; } = 0;
		internal double _scale { get; set; } = 1.0;
		internal bool has_open_paths_ { get; set; }
		internal bool minimasSorted { get; set; }
		internal ClipType cliptype { get; set; } = ClipType.None;
		internal FillRule fillrule { get; set; } = FillRule.EvenOdd;
		internal Active actives { get; set; }
		internal Active sel { get; set; }
		internal MinimaList minimas { get; set; }
		internal IEnumerator<LocalMinima> currentLocalMin { get; set; }
		internal OutRecList outRecs { get; set; }
		internal IntersectList intersections { get; set; }
		internal VertexList vertices { get; set; }
		internal ScanlinePriorityQueue scanlines { get; set; }
		internal const double DefaultScale = 100;

		//------------------------------------------------------------------------------
		// miscellaneous functions ...
		//------------------------------------------------------------------------------

		[Intrinsic]
		private static bool IsOdd(int val) => val % 2 != 0;

		[Intrinsic]
		private void SetCheckFlag(OutRec outrec)
		{
			if (outrec.state == OutRecState.Inner)
				outrec.state = OutRecState.InnerCheck;
			else if (outrec.state == OutRecState.Outer)
				outrec.state = OutRecState.OuterCheck;
		}

		[Intrinsic]
		private void UnsetCheckFlag(OutRec outrec)
		{
			if (outrec.state == OutRecState.InnerCheck)
				outrec.state = OutRecState.Inner;
			else if (outrec.state == OutRecState.OuterCheck)
				outrec.state = OutRecState.Outer;
		}

		[Intrinsic]
		private bool IsHotEdge(Active edge) => edge.outrec != null;

		[Intrinsic]
		private bool IsOpen(Active edge) => edge.local_min.is_open;

		[Intrinsic]
		private bool IsOpen(OutRec outrec) => outrec.state == OutRecState.Open;

		[Intrinsic]
		private Active GetPrevHotEdge(Active edge)
		{
			Active prev = edge.prev_in_ael;
			while (prev != null && (IsOpen(prev) || !IsHotEdge(prev)))
				prev = prev.prev_in_ael;
			return prev;
		}

		[Intrinsic]
		private bool IsOuter(OutRec outrec) => outrec.state == OutRecState.Outer || outrec.state == OutRecState.OuterCheck;

		[Intrinsic]
		private void SetAsOuter(OutRec outrec) => outrec.state = OutRecState.Outer;

		[Intrinsic]
		private bool IsInner(OutRec outrec) => outrec.state == OutRecState.Inner || outrec.state == OutRecState.InnerCheck;

		[Intrinsic]
		private void SetAsInner(OutRec outrec) => outrec.state = OutRecState.Inner;

		//the front edge will be the LEFT edge when it's an OUTER polygon
		//so that outer polygons will be orientated clockwise
		[Intrinsic]
		private bool IsFront(Active e) => e.outrec.state == OutRecState.Open ? e.wind_dx > 0 : e == e.outrec.front_e;

		[Intrinsic]
		private bool IsInvalidPath(OutPt op) => op == null || op.next == op;

		/*******************************************************************************
		  *  Dx:                             0(90deg)                                    *
		  *                                  |                                           *
		  *               +inf (180deg) <--- o --. -inf (0deg)                          *
		  *******************************************************************************/
		[Intrinsic]
		private double GetDx(PointI pt1, PointI pt2)
		{
			var dy = (double)(pt2.y - pt1.y);
			if (dy != 0)
				return (pt2.x - pt1.x) / dy;
			else if (pt2.x > pt1.x)
				return double.MinValue;
			else
				return double.MaxValue;
		}

		[Intrinsic]
		private long TopX(Active e, long currentY)
		{
			if ((currentY == e.top.y) || (e.top.x == e.bot.x))
				return e.top.x;
			else
				return e.bot.x + (long)Math.Round(e.dx * (currentY - e.bot.y));
		}

		[Intrinsic]
		private long TopX(PointI pt1, PointI pt2, long y)
		{
			if (y == pt1.y)
				return pt1.x;
			else if (y == pt2.y)
				return pt2.x;
			else if ((pt1.y == pt2.y) || (pt1.x == pt2.x))
				return pt2.x;
			else
			{
				double dx = GetDx(pt1, pt2);
				return pt1.x + (long)Math.Round(dx * (y - pt1.y));
			}
		}

		[Intrinsic]
		private bool IsHorizontal(Active edge) => edge.top.y == edge.bot.y;

		[Intrinsic]
		private bool IsHeadingRightHorz(Active edge) => edge.dx == double.MinValue;

		[Intrinsic]
		private bool IsHeadingLeftHorz(Active edge) => edge.dx == double.MaxValue;

		[Intrinsic]
		private void SwapActives(Active e1, Active e2)
		{
			Active e = e1;
			e1 = e2;
			e2 = e;
		}

		[Intrinsic]
		private PathType GetPolyType(Active edge) => edge.local_min.polytype;


		[Intrinsic]
		private bool IsSamePolyType(Active e1, Active e2) => e1.local_min.polytype == e2.local_min.polytype;

		private PointI GetIntersectPoint(Active e1, Active e2)
		{
			double b1, b2;
			if (e1.dx == e2.dx) return e1.top;

			if (e1.dx == 0)
			{
				if (IsHorizontal(e2)) return new PointI(e1.bot.x, e2.bot.y);
				b2 = e2.bot.y - (e2.bot.x / e2.dx);
				return new PointI(e1.bot.x, (long)Math.Round(e1.bot.x / e2.dx + b2));
			}
			else if (e2.dx == 0)
			{
				if (IsHorizontal(e1)) return new PointI(e2.bot.x, e1.bot.y);
				b1 = e1.bot.y - (e1.bot.x / e1.dx);
				return new PointI(e2.bot.x, (long)Math.Round(e2.bot.x / e1.dx + b1));
			}
			else
			{
				b1 = e1.bot.x - e1.bot.y * e1.dx;
				b2 = e2.bot.x - e2.bot.y * e2.dx;
				double q = (b2 - b1) / (e1.dx - e2.dx);
				return Math.Abs(e1.dx) < Math.Abs(e2.dx) ?
					new PointI((long)Math.Round(e1.dx * q + b1), (long)Math.Round(q)) :
					new PointI((long)Math.Round(e2.dx * q + b2), (long)Math.Round(q));
			}
		}

		[Intrinsic]
		private void SetDx(Active e) => e.dx = GetDx(e.bot, e.top);

		[Intrinsic]
		private bool IsLeftBound(Active e) => e.wind_dx > 0;

		[Intrinsic]
		private Vertex NextVertex(Active e) => IsLeftBound(e) ? e.vertex_top.next : e.vertex_top.prev;

		[Intrinsic]
		private Vertex NextVertex(Vertex op, bool going_forward) => going_forward ? op.next : op.prev;

		[Intrinsic]
		private Vertex PrevVertex(Vertex op, bool going_forward) => going_forward ? op.prev : op.next;

		[Intrinsic]
		private bool IsClockwise(Vertex vertex) => CrossProduct(vertex.prev.pt, vertex.pt, vertex.next.pt) >= 0;

		[Intrinsic]
		private bool IsClockwise(OutPt op) => CrossProduct(op.prev.pt, op.pt, op.next.pt) >= 0;

		[Intrinsic]
		private bool IsMaxima(Active e)
		{
			return (e.vertex_top.flags & VertexFlags.vfLocalMax) != VertexFlags.vfNone;
		}

		private Active GetMaximaPair(Active e)
		{
			Active e2;
			if (IsHorizontal(e))
			{
				//we can't be sure whether the MaximaPair is on the left or right, so ...
				e2 = e.prev_in_ael;
				while (e2 != null && e2.curr_x >= e.top.x)
				{
					if (e2.vertex_top == e.vertex_top) return e2;  //Found!
					e2 = e2.prev_in_ael;
				}
				e2 = e.next_in_ael;
				while (e2 != null && (TopX(e2, e.top.y) <= e.top.x))
				{
					if (e2.vertex_top == e.vertex_top) return e2;  //Found!
					e2 = e2.next_in_ael;
				}
				return null;
			}
			else
			{
				e2 = e.next_in_ael;
				while (e2 != null)
				{
					if (e2.vertex_top == e.vertex_top) return e2;  //Found!
					e2 = e2.next_in_ael;
				}
				return null;
			}
		}

		internal int PointCount(OutPt op)
		{
			if (op == null) return 0;
			OutPt p = op;
			int cnt = 0;
			do
			{
				cnt++;
				p = p.next;
			} while (p != op);
			return cnt;
		}

		internal PathI BuildPath(OutPt op)
		{
			var path = new PathI();
			int opCnt = PointCount(op);
			if (opCnt < 2) return path;

			path.Resize(opCnt);

			for (int i = 0; i < opCnt; ++i)
			{
				path[i] = op.pt;
				op = op.next;
			}
			return path;
		}

		[Intrinsic]
		private void DisposeOutPt(OutPt pp)
		{
			//OutPt *pp_next =
			pp.prev.next = pp.next;
			pp.next.prev = pp.prev;
		}

		[Intrinsic]
		private void DisposeOutPts(OutPt op)
		{
			if (op == null) return;
			op.prev.next = null;
			while (op != null)
			{
				OutPt tmpPp = op;
				op = op.next;
			}
		}

		[Intrinsic]
		private void SetSides(OutRec outrec, Active start_edge, Active end_edge)
		{
			outrec.front_e = start_edge;
			outrec.back_e = end_edge;
		}

		private void SwapOutrecs(Active e1, Active e2)
		{
			OutRec or1 = e1.outrec;
			OutRec or2 = e2.outrec;
			if (or1 == or2)
			{
				Active e = or1.front_e;
				or1.front_e = or1.back_e;
				or1.back_e = e;
				return;
			}
			if (or1 != null)
			{
				if (e1 == or1.front_e)
					or1.front_e = e2;
				else
					or1.back_e = e2;
			}
			if (or2 != null)
			{
				if (e2 == or2.front_e)
					or2.front_e = e1;
				else
					or2.back_e = e1;
			}
			e1.outrec = or2;
			e2.outrec = or1;
		}

		private double Area(OutPt op)
		{
			double area = 0.0;
			OutPt op2 = op;
			if (op2 != null)
			{
				do
				{
					var d = (double)(op2.prev.pt.x + op2.pt.x);
					area = area + d * (op2.prev.pt.y - op2.pt.y);
					op2 = op2.next;
				} while (op2 != op);
			}
			return area * -0.5;  //positive areas are clockwise
		}

		private void ReverseOutPts(OutPt op)
		{
			if (op == null) return;

			OutPt op1 = op;
			OutPt op2;

			do
			{
				op2 = op1.next;
				op1.next = op1.prev;
				op1.prev = op2;
				op1 = op2;
			} while (op1 != op);
		}
		private bool RecheckInnerOuter(Active e)
		{
			double area = Area(e.outrec.pts);
			bool result = area != 0.0;
			if (!result) return result;  //returns false when area == 0

			bool was_outer = IsOuter(e.outrec);
			bool is_outer = true;

			Active e2 = e.prev_in_ael;
			while (e2 != null)
			{
				if (IsHotEdge(e2) && !IsOpen(e2)) is_outer = !is_outer;
				e2 = e2.prev_in_ael;
			}

			if (is_outer != was_outer)
			{
				if (is_outer)
					SetAsOuter(e.outrec);
				else
					SetAsInner(e.outrec);
			}

			e2 = GetPrevHotEdge(e);
			if (is_outer)
			{
				if (e2 != null && IsInner(e2.outrec))
					e.outrec.owner = e2.outrec;
				else
					e.outrec.owner = null;
			}
			else
			{
				if (e2 == null)
					SetAsOuter(e.outrec);
				else if (IsInner(e2.outrec))
					e.outrec.owner = e2.outrec.owner;
				else
					e.outrec.owner = e2.outrec;
			}

			if ((area > 0.0) != is_outer) ReverseOutPts(e.outrec.pts);
			UnsetCheckFlag(e.outrec);

			return result;
		}


		[Intrinsic]
		private void SwapSides(OutRec outrec)
		{
			Active e2 = outrec.front_e;
			outrec.front_e = outrec.back_e;
			outrec.back_e = e2;
			outrec.pts = outrec.pts.next;
		}

		private bool FixSides(Active e)
		{
			bool fix = !RecheckInnerOuter(e) || ((IsOuter(e.outrec)) != IsFront(e));
			if (fix) SwapSides(e.outrec);
			return fix;
		}

		private void SetOwnerAndInnerOuterState(Active e)
		{
			Active e2;
			OutRec outrec = e.outrec;

			if (IsOpen(e))
			{
				outrec.owner = null;
				outrec.state = OutRecState.Open;
				return;
			}
			//set owner ...
			if (IsHeadingLeftHorz(e))
			{
				e2 = e.next_in_ael;  //ie assess state from opposite direction
				while (e2 != null && (!IsHotEdge(e2) || IsOpen(e2)))
					e2 = e2.next_in_ael;
				if (e2 == null)
					outrec.owner = null;
				else if ((e2.outrec.state == OutRecState.Outer) == (e2.outrec.front_e == e2))
					outrec.owner = e2.outrec.owner;
				else
					outrec.owner = e2.outrec;
			}
			else
			{
				e2 = GetPrevHotEdge(e);
				while (e2 != null && (!IsHotEdge(e2) || IsOpen(e2)))
					e2 = e2.prev_in_ael;
				if (e2 == null)
					outrec.owner = null;
				else if (IsOuter(e2.outrec) == (e2.outrec.back_e == e2))
					outrec.owner = e2.outrec.owner;
				else
					outrec.owner = e2.outrec;
			}
			//set inner/outer ...
			if (outrec.owner != null || IsInner(outrec.owner))
				outrec.state = OutRecState.Outer;
			else
				outrec.state = OutRecState.Inner;
		}

		internal bool EdgesAdjacentInAEL(IntersectNode inode)
		{
			return (inode.edge1.next_in_ael == inode.edge2) || (inode.edge1.prev_in_ael == inode.edge2);
		}

		//------------------------------------------------------------------------------
		// Clipper methods ...
		//------------------------------------------------------------------------------

		public Clipper()
		{
			Clear();
		}

		internal void CleanUp()
		{
			while (actives != null) DeleteFromAEL(actives);
			scanlines = new ScanlinePriorityQueue();  //resets priority_queue
			DisposeIntersectNodes();
			DisposeAllOutRecs();
		}

		private void Clear()
		{
			CleanUp();
			DisposeVerticesAndLocalMinima();
			currentLocalMin = minimas.GetEnumerator();
			minimasSorted = false;
			has_open_paths_ = false;
		}

		private void Reset()
		{
			if (!minimasSorted)
			{
				minimas = minimas.OrderBy(p => p.vertex.pt.y).ToList();
				minimasSorted = true;
			}
			foreach (var minima in minimas)
				InsertScanline(minima.vertex.pt.y);

			currentLocalMin = minimas.GetEnumerator();
			actives = null;
			sel = null;
		}

		[Intrinsic]
		private void InsertScanline(long y) => scanlines.Add(y);

		private bool PopScanline(out long y)
		{
			y = 0;
			if (!scanlines.Any()) return false;
			y = scanlines.Max;
			scanlines.Remove(y);
			while (scanlines.Any() && y == scanlines.Max)
				scanlines.Remove(y);  // Pop duplicates.
			return true;
		}

		private bool PopLocalMinima(long y, out LocalMinima local_minima)
		{
			local_minima = null;
			if (currentLocalMin.Current == minimas.Last() || currentLocalMin.Current.vertex.pt.y != y) return false;
			local_minima = currentLocalMin.Current;
			currentLocalMin.MoveNext();
			return true;
		}

		private void DisposeAllOutRecs()
		{
			outRecs.Clear();
		}

		private void DisposeVerticesAndLocalMinima()
		{
			minimas.Clear();
			vertices.Clear();
		}

		private void AddLocMin(Vertex vert, PathType polytype, bool is_open)
		{
			//make sure the vertex is added only once ...
			if ((VertexFlags.vfLocalMin & vert.flags) != VertexFlags.vfNone) return;
			vert.flags = (vert.flags | VertexFlags.vfLocalMin);

			LocalMinima lm = new LocalMinima();
			lm.vertex = vert;
			lm.polytype = polytype;
			lm.is_open = is_open;
			minimas.Add(lm);
		}

		private void AddPathToVertexList(PathI path, PathType polytype, bool is_open)
		{
			var path_len = (int)path.Count;
			if (!is_open)
			{
				while (path_len > 1 && (path[(int)(path_len - 1)] == path[0])) --path_len;
				if (path_len < 2) return;
			}
			else if (path_len == 0) return;

			Vertex[] vertices = new Vertex[path_len];

			int highI = path_len - 1;
			vertices[0].pt = path[0];
			vertices[0].flags = VertexFlags.vfNone;
			bool going_up, going_up0;
			if (is_open)
			{
				int i = 1;
				while (i < path_len && path[i].y == path[0].y) ++i;
				going_up = path[i].y <= path[0].y;
				if (going_up)
				{
					vertices[0].flags = VertexFlags.vfOpenStart;
					AddLocMin(vertices[0], polytype, true);
				}
				else
					vertices[0].flags = VertexFlags.vfOpenStart | VertexFlags.vfLocalMax;
			}
			else if (path[0].y == path[highI].y)
			{
				int i = highI - 1;
				while (i > 0 && path[i].y == path[0].y) --i;
				if (i == 0) return; //ie a flat closed path
				going_up = path[0].y < path[i].y;       //ie direction leading up to path[0]
			}
			else
				going_up = path[0].y < path[highI].y;   //ie direction leading up to path[0]

			going_up0 = going_up;

			//nb: polygon orientation is determined later (see InsertLocalMinimaIntoAEL).
			int j = 0; //vertices[j]
			for (int i = 1; i < path_len; ++i)
			{
				if (path[i] == vertices[j].pt) continue;  //ie skips duplicates
				vertices[i].pt = path[i];

				vertices[i].flags = VertexFlags.vfNone;
				vertices[j].next = vertices[i];
				vertices[i].prev = vertices[j];
				if (path[i].y > path[j].y && going_up)
				{
					vertices[j].flags = vertices[j].flags | VertexFlags.vfLocalMax;
					going_up = false;
				}
				else if (path[i].y < path[j].y && !going_up)
				{
					going_up = true;
					AddLocMin(vertices[j], polytype, is_open);
				}
				j = i;
			}
			//close the double-linked loop
			vertices[highI].next = vertices[0];
			vertices[0].prev = vertices[highI];

			if (is_open)
			{
				vertices[highI].flags = vertices[highI].flags | VertexFlags.vfOpenEnd;
				if (going_up)
					vertices[highI].flags = vertices[highI].flags | VertexFlags.vfLocalMax;
				else
					AddLocMin(vertices[highI], polytype, is_open);
			}
			else if (going_up != going_up0)
			{
				if (going_up0) AddLocMin(vertices[highI], polytype, is_open);
				else vertices[highI].flags = vertices[highI].flags | VertexFlags.vfLocalMax;
			}

			this.vertices.AddRange(vertices);
		}

		public void AddPath(PathI path, PathType polytype, bool is_open)
		{
			if (is_open)
			{
				if (polytype == PathType.Clip)
					throw new Exception("AddPath: Only subject paths may be open.");
				has_open_paths_ = true;
			}
			minimasSorted = false;
			AddPathToVertexList(path, polytype, is_open);
		}

		public void AddPaths(PathsI paths, PathType polytype, bool is_open)
		{
			foreach (var path in paths.data)
				AddPath(path, polytype, is_open);
		}

		private bool IsContributingClosed(Active e)
		{
			switch (fillrule)
			{
				case FillRule.NonZero:
					if (Math.Abs(e.wind_cnt) != 1) return false;
					break;
				case FillRule.Positive:
					if (e.wind_cnt != 1) return false;
					break;
				case FillRule.Negative:
					if (e.wind_cnt != -1) return false;
					break;
				default:
					break;  // delphi2cpp translation note: no warnings
			}
			switch (cliptype)
			{
				case ClipType.Intersection:
					switch (fillrule)
					{
						case FillRule.EvenOdd:
						case FillRule.NonZero: return e.wind_cnt2 != 0;
						case FillRule.Positive: return e.wind_cnt2 > 0;
						case FillRule.Negative: return e.wind_cnt2 < 0;
					}
					break;
				case ClipType.Union:
					switch (fillrule)
					{
						case FillRule.EvenOdd:
						case FillRule.NonZero: return e.wind_cnt2 == 0;
						case FillRule.Positive: return e.wind_cnt2 <= 0;
						case FillRule.Negative: return e.wind_cnt2 >= 0;
					}
					break;
				case ClipType.Difference:
					if (GetPolyType(e) == PathType.Subject)
						switch (fillrule)
						{
							case FillRule.EvenOdd:
							case FillRule.NonZero: return e.wind_cnt2 == 0;
							case FillRule.Positive: return e.wind_cnt2 <= 0;
							case FillRule.Negative: return e.wind_cnt2 >= 0;
						}
					else
						switch (fillrule)
						{
							case FillRule.EvenOdd:
							case FillRule.NonZero: return e.wind_cnt2 != 0;
							case FillRule.Positive: return e.wind_cnt2 > 0;
							case FillRule.Negative: return e.wind_cnt2 < 0;
						}
					break;
				case ClipType.Xor:
					return true;  //XOr is always contributing unless open
				default:
					return false;  // delphi2cpp translation note: no warnings
			}
			return false;  //we should never get here
		}

		private bool IsContributingOpen(Active e)
		{
			switch (cliptype)
			{
				case ClipType.Intersection: return e.wind_cnt2 != 0;
				case ClipType.Union: return (e.wind_cnt == 0) && (e.wind_cnt2 == 0);
				case ClipType.Difference: return e.wind_cnt2 == 0;
				case ClipType.Xor: return (e.wind_cnt != 0) != (e.wind_cnt2 != 0);
				default:
					return false;  // delphi2cpp translation note: no warnings
			}
			return false;  //stops compiler error
		}

		private void SetWindCountForClosedPathEdge(Active e)
		{
			//Wind counts refer to polygon regions not edges, so here an edge's WindCnt
			//indicates the higher of the wind counts for the two regions touching the
			//edge. (nb: Adjacent regions can only ever have their wind counts differ by
			//one. Also, open paths have no meaningful wind directions or counts.)

			Active e2 = e.prev_in_ael;
			//find the nearest closed path edge of the same PolyType in AEL (heading left)
			PathType pt = GetPolyType(e);
			while (e2 != null && (GetPolyType(e2) != pt || IsOpen(e2))) e2 = e2.prev_in_ael;

			if (e2 == null)
			{
				e.wind_cnt = e.wind_dx;
				e2 = actives;
			}
			else if (fillrule == FillRule.EvenOdd)
			{
				e.wind_cnt = e.wind_dx;
				e.wind_cnt2 = e2.wind_cnt2;
				e2 = e2.next_in_ael;
			}
			else
			{
				//NonZero, positive, or negative filling here ...
				//if e's WindCnt is in the SAME direction as its WindDx, then polygon
				//filling will be on the right of 'e'.
				//nb: neither e2.WindCnt nor e2.WindDx should ever be 0.
				if (e2.wind_cnt * e2.wind_dx < 0)
				{
					//opposite directions so 'e' is outside 'e2' ...
					if (Math.Abs(e2.wind_cnt) > 1)
					{
						//outside prev poly but still inside another.
						if (e2.wind_dx * e.wind_dx < 0)
							//reversing direction so use the same WC
							e.wind_cnt = e2.wind_cnt;
						else
							//otherwise keep 'reducing' the WC by 1 (ie towards 0) ...
							e.wind_cnt = e2.wind_cnt + e.wind_dx;
					}
					else
						//now outside all polys of same polytype so set own WC ...
						e.wind_cnt = (IsOpen(e) ? 1 : e.wind_dx);
				}
				else
				{
					//'e' must be inside 'e2'
					if (e2.wind_dx * e.wind_dx < 0)
						//reversing direction so use the same WC
						e.wind_cnt = e2.wind_cnt;
					else
						//otherwise keep 'increasing' the WC by 1 (ie away from 0) ...
						e.wind_cnt = e2.wind_cnt + e.wind_dx;
				}
				e.wind_cnt2 = e2.wind_cnt2;
				e2 = e2.next_in_ael;  //ie get ready to calc WindCnt2
			}

			//update wind_cnt2 ...
			if (fillrule == FillRule.EvenOdd)
				while (e2 != e)
				{
					if (GetPolyType(e2) != pt && !IsOpen(e2))
						e.wind_cnt2 = (e.wind_cnt2 == 0 ? 1 : 0);
					e2 = e2.next_in_ael;
				}
			else
				while (e2 != e)
				{
					if (GetPolyType(e2) != pt && !IsOpen(e2))
						e.wind_cnt2 += e2.wind_dx;
					e2 = e2.next_in_ael;
				}
		}

		private void SetWindCountForOpenPathEdge(Active e)
		{
			Active e2 = actives;
			if (fillrule == FillRule.EvenOdd)
			{
				int cnt1 = 0, cnt2 = 0;
				while (e2 != e)
				{
					if (GetPolyType(e2) == PathType.Clip)
						cnt2++;
					else if (!IsOpen(e2))
						cnt1++;
					e2 = e2.next_in_ael;
				}
				e.wind_cnt = (IsOdd(cnt1) ? 1 : 0);
				e.wind_cnt2 = (IsOdd(cnt2) ? 1 : 0);
			}
			else
			{
				while (e2 != e)
				{
					if (GetPolyType(e2) == PathType.Clip)
						e.wind_cnt2 += e2.wind_dx;
					else if (!IsOpen(e2))
						e.wind_cnt += e2.wind_dx;
					e2 = e2.next_in_ael;
				}
			}
		}

		private bool IsValidAelOrder(Active a1, Active a2)
		{
			bool is_valid;
			PointI pt1;
			PointI pt2;
			Vertex op1;
			Vertex op2;
			long x;

			if (a2.curr_x != a1.curr_x)
			{
				is_valid = a2.curr_x > a1.curr_x;
				return is_valid;
			}

			pt1 = a1.bot;
			pt2 = a2.bot;
			op1 = a1.vertex_top;
			op2 = a2.vertex_top;

			while (true)
			{
				if (op1.pt.y >= op2.pt.y)
				{
					x = TopX(pt2, op2.pt, op1.pt.y) - op1.pt.x;
					is_valid = x > 0;
					if (x != 0) return is_valid;
					if (op2.pt.y == op1.pt.y)
					{
						pt2 = op2.pt;
						op2 = NextVertex(op2, IsLeftBound(a2));
					}
					pt1 = op1.pt;
					op1 = NextVertex(op1, IsLeftBound(a1));
				}
				else
				{
					x = op2.pt.x - TopX(pt1, op1.pt, op2.pt.y);
					is_valid = x > 0;
					if (x != 0) return is_valid;
					pt2 = op2.pt;
					op2 = NextVertex(op2, IsLeftBound(a2));
				}
				if (op1.pt.y > pt1.y)
				{
					is_valid = (a1.wind_dx > 0) != IsClockwise(PrevVertex(op1, a1.wind_dx > 0));
					return is_valid;
				}
				else if (op2.pt.y > pt2.y)
				{
					is_valid = (a2.wind_dx > 0) == IsClockwise(PrevVertex(op2, a2.wind_dx > 0));
					return is_valid;
				}
			}
			is_valid = true;
			return is_valid;
		}

		private void InsertLeftEdge(Active e)
		{
			Active e2;

			if (actives == null)
			{
				e.prev_in_ael = null;
				e.next_in_ael = null;
				actives = e;
			}
			else if (IsValidAelOrder(e, actives))
			{
				e.prev_in_ael = null;
				e.next_in_ael = actives;
				actives.prev_in_ael = e;
				actives = e;
			}
			else
			{
				e2 = actives;
				while (e2.next_in_ael != null && IsValidAelOrder(e2.next_in_ael, e))
					e2 = e2.next_in_ael;
				e.next_in_ael = e2.next_in_ael;
				if (e2.next_in_ael != null) e2.next_in_ael.prev_in_ael = e;
				e.prev_in_ael = e2;
				e2.next_in_ael = e;
			}
		}

		private void InsertRightEdge(Active e, Active e2)
		{
			e2.next_in_ael = e.next_in_ael;
			if (e.next_in_ael != null) e.next_in_ael.prev_in_ael = e2;
			e2.prev_in_ael = e;
			e.next_in_ael = e2;
		}

		private void InsertLocalMinimaIntoAEL(long bot_y)
		{
			Active left_bound, right_bound;
			//Add any local minima (if any) at BotY ...
			//nb: horizontal local minima edges should contain locMin.vertex.prev

			while (PopLocalMinima(bot_y, out LocalMinima local_minima))
			{
				if ((local_minima.vertex.flags & VertexFlags.vfOpenStart) != VertexFlags.vfNone)
				{
					left_bound = null;
				}
				else
				{
					left_bound = new Active();
					left_bound.bot = local_minima.vertex.pt;
					left_bound.curr_x = left_bound.bot.x;
					left_bound.vertex_top = local_minima.vertex.prev;  //ie descending
					left_bound.top = left_bound.vertex_top.pt;
					left_bound.wind_dx = -1;
					left_bound.outrec = null;
					left_bound.local_min = local_minima;
					SetDx(left_bound);
				}

				if ((local_minima.vertex.flags & VertexFlags.vfOpenEnd) != VertexFlags.vfNone)
				{
					right_bound = null;
				}
				else
				{
					right_bound = new Active();
					right_bound.bot = local_minima.vertex.pt;
					right_bound.curr_x = right_bound.bot.x;
					right_bound.vertex_top = local_minima.vertex.next;  //ie ascending
					right_bound.top = right_bound.vertex_top.pt;
					right_bound.wind_dx = 1;
					right_bound.outrec = null;
					right_bound.local_min = local_minima;
					SetDx(right_bound);
				}

				//Currently LeftB is just the descending bound and RightB is the ascending.
				//Now if the LeftB isn't on the left of RightB then we need swap them.
				if (left_bound != null && right_bound != null)
				{
					if (IsHorizontal(left_bound))
					{
						if (IsHeadingRightHorz(left_bound)) SwapActives(left_bound, right_bound);
					}
					else if (IsHorizontal(right_bound))
					{
						if (IsHeadingLeftHorz(right_bound)) SwapActives(left_bound, right_bound);
					}
					else if (left_bound.dx < right_bound.dx)
						SwapActives(left_bound, right_bound);
				}
				else if (left_bound == null)
				{
					left_bound = right_bound;
					right_bound = null;
				}

				bool contributing;
				InsertLeftEdge(left_bound);  ///////
											 //todo: further validation of position in AEL ???

				if (IsOpen(left_bound))
				{
					SetWindCountForOpenPathEdge(left_bound);
					contributing = IsContributingOpen(left_bound);
				}
				else
				{
					SetWindCountForClosedPathEdge(left_bound);
					contributing = IsContributingClosed(left_bound);
				}

				if (right_bound != null)
				{
					right_bound.wind_cnt = left_bound.wind_cnt;
					right_bound.wind_cnt2 = left_bound.wind_cnt2;
					InsertRightEdge(left_bound, right_bound);  ///////
					if (contributing)
						AddLocalMinPoly(left_bound, right_bound, left_bound.bot, true);
					if (IsHorizontal(right_bound))
						PushHorz(right_bound);
					else
						InsertScanline(right_bound.top.y);
				}
				else if (contributing)
					StartOpenPath(left_bound, left_bound.bot);

				if (IsHorizontal(left_bound))
					PushHorz(left_bound);
				else
					InsertScanline(left_bound.top.y);
			}  //while (PopLocalMinima())
		}

		[Intrinsic]
		private void PushHorz(Active e)
		{
			e.next_in_sel = sel != null ? sel : null;
			sel = e;
		}

		[Intrinsic]
		private bool PopHorz(out Active e)
		{
			e = sel;
			if (e == null) return false;
			sel = sel.next_in_sel;
			return true;
		}

		/*OutRec *Clipper.GetOwner(const Active *e) {
			if (IsHorizontal(*e) && e.top.x < e.bot.x) {
				e = e.next_in_ael;
				while (e && (!IsHotEdge(*e) || IsOpen(*e)))
					e = e.next_in_ael;
				if (!e) return null;
				return ((e.outrec.state == OutRecState.Outer) == (e.outrec.front_e == e)) ?
							   e.outrec.owner :
							   e.outrec;
			} else {
				e = e.prev_in_ael;
				while (e && (!IsHotEdge(*e) || IsOpen(*e)))
					e = e.prev_in_ael;
				if (!e) return null;
				return ((e.outrec.state == OutRecState.Outer) == (e.outrec.back_e == e)) ?
							   e.outrec.owner :
							   e.outrec;
			}
		}*/
		//------------------------------------------------------------------------------

		private void AddLocalMinPoly(Active e1, Active e2, PointI pt, bool is_new = false, bool orientation_check_required = false)
		{
			OutRec outrec = CreateOutRec();
			outrec.idx = (int)outRecs.Count;
			outRecs.Add(outrec);
			outrec.pts = null;
			outrec.PolyTree = null;

			e1.outrec = outrec;
			SetOwnerAndInnerOuterState(e1);
			//flag when orientation needs to be rechecked later ...
			if (orientation_check_required) SetCheckFlag(outrec);
			e2.outrec = outrec;

			if (!IsOpen(e1))
			{
				//Setting the owner and inner/outer states (above) is an essential
				//precursor to setting edge 'sides' (ie left and right sides of output
				//polygons) and hence the orientation of output paths ...
				if (IsOuter(outrec) == is_new)
					SetSides(outrec, e1, e2);
				else
					SetSides(outrec, e2, e1);
			}
			OutPt op = CreateOutPt();
			outrec.pts = op;
			op.pt = pt;
			op.prev = op;
			op.next = op;

			//nb: currently e1.NextInAEL == e2 but this could change immediately on return
		}

		private void AddLocalMaxPoly(Active e1, Active e2, PointI pt)
		{
			if (!IsOpen(e1) && (IsFront(e1) == IsFront(e2)))
				if (!FixSides(e1)) FixSides(e2);

			OutPt op = AddOutPt(e1, pt);
			// AddOutPt(e2, pt); //this may no longer be necessary

			if (e1.outrec == e2.outrec)
			{
				if (e1.outrec.state == OutRecState.OuterCheck || e1.outrec.state == OutRecState.InnerCheck)
					RecheckInnerOuter(e1);

				//nb: IsClockwise() is generally faster than Area() but will occasionally
				//give false positives when there are tiny self-intersections at the top...
				if (IsOuter(e1.outrec))
				{
					if (!IsClockwise(op) && (Area(op) < 0.0))
						ReverseOutPts(e1.outrec.pts);
				}
				else
				{
					if (IsClockwise(op) && (Area(op) > 0.0))
						ReverseOutPts(e1.outrec.pts);
				}
				e1.outrec.front_e = null;
				e1.outrec.back_e = null;
				e1.outrec = null;
				e2.outrec = null;
			}
			//and to preserve the winding orientation of outrec ...
			else if (e1.outrec.idx < e2.outrec.idx)
				JoinOutrecPaths(e1, e2);
			else
				JoinOutrecPaths(e2, e1);
		}

		private void JoinOutrecPaths(Active e1, Active e2)
		{
			if (IsFront(e1) == IsFront(e2))
			{
				//one or other 'side' must be wrong ...
				if (IsOpen(e1))
					SwapSides(e2.outrec);
				else if (!FixSides(e1) && !FixSides(e2))
					throw new Exception("Error in JoinOutrecPaths()");
				if (e1.outrec.owner == e2.outrec) e1.outrec.owner = e2.outrec.owner;
			}

			//join E2 outrec path onto E1 outrec path and then delete E2 outrec path
			//pointers. (nb: Only very rarely do the joining ends share the same coords.)
			OutPt p1_st = e1.outrec.pts;
			OutPt p2_st = e2.outrec.pts;
			OutPt p1_end = p1_st.next;
			OutPt p2_end = p2_st.next;
			if (IsFront(e1))
			{
				p2_end.prev = p1_st;
				p1_st.next = p2_end;
				p2_st.next = p1_end;
				p1_end.prev = p2_st;
				e1.outrec.pts = p2_st;
				if (IsOpen(e1))
				{
					e1.outrec.pts = p2_st;
				}
				else
				{
					e1.outrec.front_e = e2.outrec.front_e;
					e1.outrec.front_e.outrec = e1.outrec;
				}
				//strip duplicates ...
				if ((p2_end != p2_st) && (p2_end.pt == p2_end.prev.pt))
					DisposeOutPt(p2_end);
			}
			else
			{
				p1_end.prev = p2_st;
				p2_st.next = p1_end;
				p1_st.next = p2_end;
				p2_end.prev = p1_st;
				if (IsOpen(e1))
				{
					e1.outrec.pts = p1_st;
				}
				else
				{
					e1.outrec.back_e = e2.outrec.back_e;
					e1.outrec.back_e.outrec = e1.outrec;
				}
				//strip duplicates ...
				if ((p1_end != p1_st) && (p1_end.pt == p1_end.prev.pt))
					DisposeOutPt(p1_end);
			}

			if ((e1.outrec.pts.pt == e1.outrec.pts.prev.pt) && !IsInvalidPath(e1.outrec.pts))
				DisposeOutPt(e1.outrec.pts.prev);

			//after joining, the e2.OutRec must contains no vertices ...
			e2.outrec.front_e = null;
			e2.outrec.back_e = null;
			e2.outrec.pts = null;
			e2.outrec.owner = e1.outrec;  //this may be redundant

			//and e1 and e2 are maxima and are about to be dropped from the Actives list.
			e1.outrec = null;
			e2.outrec = null;
		}

		//this is a virtual method as descendant classes may need
		//to produce descendant classes of OutPt ...
		private OutPt CreateOutPt() => new OutPt();

		//this is a virtual method as descendant classes may need
		//to produce descendant classes of OutRec ...

		private OutRec CreateOutRec() => new OutRec();

		private OutPt AddOutPt(Active e, PointI pt)
		{
			OutPt new_op = null;

			//Outrec.OutPts: a circular doubly-linked-list of POutPt where ...
			//op_front[.Prev]* ~~~> op_back & op_back == op_front.Next
			OutRec outrec = e.outrec;
			bool to_front = IsFront(e);
			OutPt op_front = outrec.pts;
			OutPt op_back = op_front.next;

			if (to_front && (pt == op_front.pt))
				new_op = op_front;
			else if (!to_front && (pt == op_back.pt))
				new_op = op_back;
			else
			{
				new_op = CreateOutPt();
				new_op.pt = pt;
				op_back.prev = new_op;
				new_op.prev = op_front;
				new_op.next = op_back;
				op_front.next = new_op;
				if (to_front) outrec.pts = new_op;
			}
			return new_op;
		}

		private void StartOpenPath(Active e, PointI pt)
		{
			OutRec outrec = CreateOutRec();
			outrec.idx = outRecs.Count;
			outRecs.Add(outrec);
			outrec.owner = null;
			outrec.state = OutRecState.Open;
			outrec.pts = null;
			outrec.PolyTree = null;
			outrec.back_e = null;
			outrec.front_e = null;
			e.outrec = outrec;

			OutPt op = CreateOutPt();
			op.pt = pt;
			op.next = op;
			op.prev = op;
			outrec.pts = op;
		}

		private void UpdateEdgeIntoAEL(Active e)
		{
			e.bot = e.top;
			e.vertex_top = NextVertex(e);
			e.top = e.vertex_top.pt;
			e.curr_x = e.bot.x;
			SetDx(e);
			if (!IsHorizontal(e)) InsertScanline(e.top.y);
		}

		private void IntersectEdges(Active e1, Active e2, PointI pt, bool orientation_check_required = false)
		{
			//MANAGE OPEN PATH INTERSECTIONS SEPARATELY ...
			if (has_open_paths_ && (IsOpen(e1) || IsOpen(e2)))
			{
				if (IsOpen(e1) && IsOpen(e2)) return;
				Active edge_o, edge_c;
				if (IsOpen(e1))
				{
					edge_o = e1;
					edge_c = e2;
				}
				else
				{
					edge_o = e2;
					edge_c = e1;
				}

				switch (cliptype)
				{
					case ClipType.Intersection:
					case ClipType.Difference:
						if (IsSamePolyType(edge_o, edge_c) || (Math.Abs(edge_c.wind_cnt) != 1)) return;
						break;
					case ClipType.Union:
						if (IsHotEdge(edge_o) != ((Math.Abs(edge_c.wind_cnt) != 1) ||
							(IsHotEdge(edge_o) != (edge_c.wind_cnt != 0)))) return;  //just works!
						break;
					case ClipType.Xor:
						if (Math.Abs(edge_c.wind_cnt) != 1) return;
						break;
					default:
						throw new Exception("Error in IntersectEdges - ClipType is unknown!");
				}
				//toggle contribution ...
				if (IsHotEdge(edge_o))
				{
					AddOutPt(edge_o, pt);
					edge_o.outrec = null;
				}
				else
					StartOpenPath(edge_o, pt);
				return;
			}

			//UPDATE WINDING COUNTS...
			int old_e1_windcnt, old_e2_windcnt;
			if (e1.local_min.polytype == e2.local_min.polytype)
			{
				if (fillrule == FillRule.EvenOdd)
				{
					old_e1_windcnt = e1.wind_cnt;
					e1.wind_cnt = e2.wind_cnt;
					e2.wind_cnt = old_e1_windcnt;
				}
				else
				{
					if (e1.wind_cnt + e2.wind_dx == 0)
						e1.wind_cnt = -e1.wind_cnt;
					else
						e1.wind_cnt += e2.wind_dx;
					if (e2.wind_cnt - e1.wind_dx == 0)
						e2.wind_cnt = -e2.wind_cnt;
					else
						e2.wind_cnt -= e1.wind_dx;
				}
			}
			else
			{
				if (fillrule != FillRule.EvenOdd)
					e1.wind_cnt2 += e2.wind_dx;
				else
					e1.wind_cnt2 = (e1.wind_cnt2 == 0 ? 1 : 0);
				if (fillrule != FillRule.EvenOdd)
					e2.wind_cnt2 -= e1.wind_dx;
				else
					e2.wind_cnt2 = (e2.wind_cnt2 == 0 ? 1 : 0);
			}

			switch (fillrule)
			{
				case FillRule.Positive:
					old_e1_windcnt = e1.wind_cnt;
					old_e2_windcnt = e2.wind_cnt;
					break;
				case FillRule.Negative:
					old_e1_windcnt = -e1.wind_cnt;
					old_e2_windcnt = -e2.wind_cnt;
					break;
				default:
					old_e1_windcnt = Math.Abs(e1.wind_cnt);
					old_e2_windcnt = Math.Abs(e2.wind_cnt);
					break;
			}

			bool e1_windcnt_in_01 = old_e1_windcnt == 0 || old_e1_windcnt == 1;
			bool e2_windcnt_in_01 = old_e2_windcnt == 0 || old_e2_windcnt == 1;

			if ((!IsHotEdge(e1) && !e1_windcnt_in_01) || (!IsHotEdge(e2) && !e2_windcnt_in_01))
			{
				return;
			}
			//NOW PROCESS THE INTERSECTION ...

			//if both edges are 'hot' ...
			if (IsHotEdge(e1) && IsHotEdge(e2))
			{
				if ((old_e1_windcnt != 0 && old_e1_windcnt != 1) || (old_e2_windcnt != 0 && old_e2_windcnt != 1) ||
						(e1.local_min.polytype != e2.local_min.polytype && cliptype != ClipType.Xor))
				{
					AddLocalMaxPoly(e1, e2, pt);
				}
				else if (IsFront(e1) || (e1.outrec == e2.outrec))
				{
					AddLocalMaxPoly(e1, e2, pt);
					AddLocalMinPoly(e1, e2, pt);
				}
				else
				{
					//right & left bounds touching, NOT maxima & minima ...
					AddOutPt(e1, pt);
					AddOutPt(e2, pt);
					SwapOutrecs(e1, e2);
				}
			}
			//if one or other edge is 'hot' ...
			else if (IsHotEdge(e1))
			{
				AddOutPt(e1, pt);
				SwapOutrecs(e1, e2);
			}
			else if (IsHotEdge(e2))
			{
				AddOutPt(e2, pt);
				SwapOutrecs(e1, e2);
			}
			else
			{  //neither edge is 'hot'
				long e1Wc2, e2Wc2;
				switch (fillrule)
				{
					case FillRule.Positive:
						e1Wc2 = e1.wind_cnt2;
						e2Wc2 = e2.wind_cnt2;
						break;
					case FillRule.Negative:
						e1Wc2 = -e1.wind_cnt2;
						e2Wc2 = -e2.wind_cnt2;
						break;
					default:
						e1Wc2 = Math.Abs(e1.wind_cnt2);
						e2Wc2 = Math.Abs(e2.wind_cnt2);
						break;
				}

				if (!IsSamePolyType(e1, e2))
				{
					AddLocalMinPoly(e1, e2, pt, false, orientation_check_required);
				}
				else if (old_e1_windcnt == 1 && old_e2_windcnt == 1)
					switch (cliptype)
					{
						case ClipType.Intersection:
							if (e1Wc2 > 0 && e2Wc2 > 0)
								AddLocalMinPoly(e1, e2, pt, false, orientation_check_required);
							break;
						case ClipType.Union:
							if (e1Wc2 <= 0 && e2Wc2 <= 0)
								AddLocalMinPoly(e1, e2, pt, false, orientation_check_required);
							break;
						case ClipType.Difference:
							if (((GetPolyType(e1) == PathType.Clip) && (e1Wc2 > 0) && (e2Wc2 > 0)) ||
									((GetPolyType(e1) == PathType.Subject) && (e1Wc2 <= 0) && (e2Wc2 <= 0)))
							{
								AddLocalMinPoly(e1, e2, pt, false, orientation_check_required);
							}
							break;
						case ClipType.Xor:
							AddLocalMinPoly(e1, e2, pt, false, orientation_check_required);
							break;
						default:
							break;  // delphi2cpp translation note: no warnings
					}
			}
		}

		private void DeleteFromAEL(Active e)
		{
			Active prev = e.prev_in_ael;
			Active next = e.next_in_ael;
			if (prev == null && next == null && (e != actives)) return;  //already deleted
			if (prev != null)
				prev.next_in_ael = next;
			else
				actives = next;
			if (next != null) next.prev_in_ael = prev;
		}

		[Intrinsic]
		private void AdjustCurrXAndCopyToSEL(long top_y)
		{
			Active e = actives;
			sel = e;
			while (e != null)
			{
				e.prev_in_sel = e.prev_in_ael;
				e.next_in_sel = e.next_in_ael;
				e.curr_x = TopX(e, top_y);
				//nb: don't update e.curr.Y yet (see AddNewIntersectNode)
				e = e.next_in_ael;
			}
		}

		internal void ExecuteInternal(ClipType ct, FillRule ft)
		{
			//if (ct == ClipType.None) return;
			fillrule = ft;
			cliptype = ct;
			Reset();
			if (!PopScanline(out long y)) return;

			while (true)
			{
				InsertLocalMinimaIntoAEL(y);
				while (PopHorz(out Active e)) DoHorizontal(e);
				botY = y;  //bot_y_ == bottom of scanbeam
				if (!PopScanline(out y)) break;  //y new top of scanbeam
				DoIntersections(y);
				DoTopOfScanbeam(y);
			}
		}

		public void DoIntersections(long top_y)
		{
			if (BuildIntersectList(top_y))
			{
				ProcessIntersectList();
				DisposeIntersectNodes();
			}
		}

		[Intrinsic]
		private void DisposeIntersectNodes()
		{
			intersections.Clear();
		}

		private void AddNewIntersectNode(Active e1, Active e2, long top_y)
		{
			PointI pt = GetIntersectPoint(e1, e2);

			//rounding errors can occasionally place the calculated intersection
			//point either below or above the scanbeam, so check and correct ...
			if (pt.y > botY)
			{
				//e.curr.y is still the bottom of scanbeam
				pt.y = botY;
				//use the more vertical of the 2 edges to derive pt.x ...
				if (Math.Abs(e1.dx) < Math.Abs(e2.dx))
					pt.x = TopX(e1, botY);
				else
					pt.x = TopX(e2, botY);
			}
			else if (pt.y < top_y)
			{
				//top_y is at the top of the scanbeam
				pt.y = top_y;
				if (e1.top.y == top_y)
					pt.x = e1.top.x;
				else if (e2.top.y == top_y)
					pt.x = e2.top.x;
				else if (Math.Abs(e1.dx) < Math.Abs(e2.dx))
					pt.x = e1.curr_x;
				else
					pt.x = e2.curr_x;
			}

			IntersectNode node = new IntersectNode();
			node.edge1 = e1;
			node.edge2 = e2;
			node.pt = pt;
			intersections.Add(node);
		}

		private bool BuildIntersectList(long top_y)
		{
			if (actives == null || actives.next_in_ael == null) return false;

			//Calculate edge positions at the top of the current scanbeam, and from this
			//we will determine the intersections required to reach these new positions.
			AdjustCurrXAndCopyToSEL(top_y);

			//Track every edge intersection between the bottom and top of each scanbeam,
			//using a stable merge sort to ensure edges are adjacent when intersecting.
			//Re merge sorts see https://stackoverflow.com/a/46319131/359538
			int jump_size = 1;
			while (true)
			{
				Active first = sel, second = null, base_e = null, prev_base = null;
				//sort successive larger jump counts of nodes ...
				while (first != null)
				{
					if (jump_size == 1)
					{
						second = first.next_in_sel;
						if (second == null)
						{
							first.jump = null;
							break;
						}
						first.jump = second.next_in_sel;
					}
					else
					{
						second = first.jump;
						if (second == null)
						{
							first.jump = null;
							break;
						}
						first.jump = second.jump;
					}

					//now sort first and second groups ...
					Active tmp = null;
					base_e = first;
					int left_cnt = jump_size, right_cnt = jump_size;
					while (left_cnt > 0 && right_cnt > 0)
					{
						if (second.curr_x < first.curr_x)
						{
							tmp = second.prev_in_sel;

							//create intersect 'node' events for each time 'second' needs to
							//move left, ie intersecting with its prior edge ...
							for (int i = 0; i < left_cnt; ++i)
							{
								//create a new intersect node.
								//nb: 'tmp' will always be assigned 
								AddNewIntersectNode(tmp, second, top_y);
								tmp = tmp.prev_in_sel;
							}
							//now move the out of place 'second' to it's new position in SEL ...
							if (first == base_e)
							{
								if (prev_base != null) prev_base.jump = second;
								base_e = second;
								base_e.jump = first.jump;
								if (first.prev_in_sel == null) sel = second;
							}
							tmp = second.next_in_sel;

							//first remove 'second' from list ...
							Active prev_e = second.prev_in_sel;
							Active next_e = second.next_in_sel;
							prev_e.next_in_sel = next_e;
							if (next_e != null) next_e.prev_in_sel = prev_e;
							//and then reinsert 'second' into list just before 'first' ...
							prev_e = first.prev_in_sel;
							if (prev_e != null) prev_e.next_in_sel = second;
							first.prev_in_sel = second;
							second.prev_in_sel = prev_e;
							second.next_in_sel = first;

							second = tmp;
							if (second == null) break;
							--right_cnt;
						}
						else
						{
							first = first.next_in_sel;
							--left_cnt;
						}
					}
					first = base_e.jump;
					prev_base = base_e;
				}
				if (sel.jump == null) //this is safe because 'sel' is always assigned
					break;
				else
					jump_size <<= 1;
			}
			return intersections.Count > 0;
		}

		private void ProcessIntersectList()
		{
			//We now have a list of intersections required so that edges will be
			//correctly positioned at the top of the scanbeam. However, it's important
			//that edge intersections are processed from the bottom up, but it's also
			//crucial that intersections only occur between adjacent edges.

			//First we do a quicksort so intersections proceed in a bottom up order ...
			intersections = intersections.OrderBy(p => p.pt.y).ThenBy(p => p.pt.x).ToList();
			//Now as we process these intersections, we must sometimes adjust the order
			//to ensure that intersecting edges are always adjacent ...
			for (int i = 0; i < intersections.Count; ++i)
			{
				if (!EdgesAdjacentInAEL(intersections[i]))
				{
					int j = i + 1;
					while (j < intersections.Count && !EdgesAdjacentInAEL(intersections[j])) j++;
					if (j < intersections.Count)
					{
						var temp = intersections[i];
						intersections[i] = intersections[j];
						intersections[j] = temp;
					}
				}

				//Occasionally a non-minima intersection is processed before its own
				//minima. This causes problems with orientation so we need to flag it ...
				IntersectNode node = intersections[i];
				bool flagged = (i < intersections.Count - 1) &&
					(intersections[i + 1].pt.y > node.pt.y);
				IntersectEdges(node.edge1, node.edge2, node.pt, flagged);
				SwapPositionsInAEL(node.edge1, node.edge2);
			}
		}
		private void SwapPositionsInAEL(Active e1, Active e2)
		{
			//preconditon: e1 must be immediately to the left of e2
			Active next = e2.next_in_ael;
			if (next != null) next.prev_in_ael = e1;
			Active prev = e1.prev_in_ael;
			if (prev != null) prev.next_in_ael = e2;
			e2.prev_in_ael = prev;
			e2.next_in_ael = e1;
			e1.prev_in_ael = e2;
			e1.next_in_ael = next;
			if (e2.prev_in_ael == null) actives = e2;
		}

		private bool ResetHorzDirection(Active horz, Active max_pair, out long horz_left, out long horz_right)
		{
			if (horz.bot.x == horz.top.x)
			{
				//the horizontal edge is going nowhere ...
				horz_left = horz.curr_x;
				horz_right = horz.curr_x;
				Active e = horz.next_in_ael;
				while (e != null && e != max_pair) e = e.next_in_ael;
				return e != null;
			}
			else if (horz.curr_x < horz.top.x)
			{
				horz_left = horz.curr_x;
				horz_right = horz.top.x;
				return true;
			}
			else
			{
				horz_left = horz.top.x;
				horz_right = horz.curr_x;
				return false;  //right to left
			}
		}

		/*******************************************************************************
		* Notes: Horizontal edges (HEs) at scanline intersections (ie at the top or    *
		* bottom of a scanbeam) are processed as if layered.The order in which HEs     *
		* are processed doesn't matter. HEs intersect with the bottom vertices of      *
		* other HEs[#] and with non-horizontal edges [*]. Once these intersections     *
		* are completed, intermediate HEs are 'promoted' to the next edge in their     *
		* bounds, and they in turn may be intersected[%] by other HEs.                 *
		*                                                                              *
		* eg: 3 horizontals at a scanline:    /   |                     /           /  *
		*              |                     /    |     (HE3)o ========%========== o   *
		*              o ======= o(HE2)     /     |         /         /                *
		*          o ============#=========*======*========#=========o (HE1)           *
		*         /              |        /       |       /                            *
		*******************************************************************************/
		private void DoHorizontal(Active horz)
		{
			PointI pt;
			//with closed paths, simplify consecutive horizontals into a 'single' edge ...
			if (!IsOpen(horz))
			{
				pt = horz.bot;
				while (!IsMaxima(horz) && NextVertex(horz).pt.y == pt.y)
					UpdateEdgeIntoAEL(horz);
				horz.bot = pt;
				horz.curr_x = pt.x;
				//update Dx in case of direction change ...
				if (horz.bot.x < horz.top.x)
					horz.dx = double.MinValue;
				else
					horz.dx = double.MaxValue;
			}

			Active max_pair = null;
			if (IsMaxima(horz) && (!IsOpen(horz) ||
				((horz.vertex_top.flags & (VertexFlags.vfOpenStart | VertexFlags.vfOpenEnd)) == VertexFlags.vfNone)))
				max_pair = GetMaximaPair(horz);

			bool is_left_to_right = ResetHorzDirection(horz, max_pair, out long horz_left, out long horz_right);
			if (IsHotEdge(horz)) AddOutPt(horz, new PointI(horz.curr_x, horz.bot.y));

			while (true)
			{  //loops through consec. horizontal edges (if open)
				Active e;
				bool isMax = IsMaxima(horz);
				if (is_left_to_right)
					e = horz.next_in_ael;
				else
					e = horz.prev_in_ael;
				while (e != null)
				{
					//break if we've gone past the } of the horizontal ...
					if ((is_left_to_right && (e.curr_x > horz_right)) ||
							(!is_left_to_right && (e.curr_x < horz_left))) break;
					//or if we've got to the } of an intermediate horizontal edge ...
					if (e.curr_x == horz.top.x && !isMax && !IsHorizontal(e))
					{
						pt = NextVertex(horz).pt;
						if ((is_left_to_right && (TopX(e, pt.y) >= pt.x)) ||
								(!is_left_to_right && (TopX(e, pt.y) <= pt.x))) break;
					}

					if (e == max_pair)
					{
						if (IsHotEdge(horz))
						{
							if (is_left_to_right)
								AddLocalMaxPoly(horz, e, horz.top);
							else
								AddLocalMaxPoly(e, horz, horz.top);
						}
						DeleteFromAEL(e);
						DeleteFromAEL(horz);
						return;
					}

					pt = new PointI(e.curr_x, horz.bot.y);
					if (is_left_to_right)
					{
						IntersectEdges(horz, e, pt);
						SwapPositionsInAEL(horz, e);
						e = horz.next_in_ael;
					}
					else
					{
						IntersectEdges(e, horz, pt);
						SwapPositionsInAEL(e, horz);
						e = horz.prev_in_ael;
					}
				}

				//check if we've finished with (consecutive) horizontals ...
				if (isMax || NextVertex(horz).pt.y != horz.top.y) break;

				//still more horizontals in bound to process ...
				UpdateEdgeIntoAEL(horz);
				is_left_to_right = ResetHorzDirection(horz, max_pair, out horz_left, out horz_right);

				if (IsOpen(horz))
				{
					if (IsMaxima(horz)) max_pair = GetMaximaPair(horz);
					if (IsHotEdge(horz)) AddOutPt(horz, horz.bot);
				}
			}

			if (IsHotEdge(horz)) AddOutPt(horz, horz.top);
			if (!IsOpen(horz))
				UpdateEdgeIntoAEL(horz);  //this is the } of an intermediate horiz.
			else if (!IsMaxima(horz))
				UpdateEdgeIntoAEL(horz);
			else if (max_pair == null)  //ie open at top
				DeleteFromAEL(horz);
			else if (IsHotEdge(horz))
				AddLocalMaxPoly(horz, max_pair, horz.top);
			else
			{
				DeleteFromAEL(max_pair);
				DeleteFromAEL(horz);
			}
		}

		private void DoTopOfScanbeam(long y)
		{
			sel = null;  // sel_ is reused to flag horizontals (see PushHorz below)
			Active e = actives;
			while (e != null)
			{
				//nb: 'e' will never be horizontal here
				if (e.top.y == y)
				{
					//the following helps to avoid micro self-intersections
					//with negligible impact on performance ...
					e.curr_x = e.top.x;
					if (e.prev_in_ael != null && (e.prev_in_ael.curr_x == e.curr_x) &&
							(e.prev_in_ael.bot.y != y) && IsHotEdge(e.prev_in_ael))
						AddOutPt(e.prev_in_ael, e.top);
					if (e.next_in_ael != null && (e.next_in_ael.curr_x == e.curr_x) &&
							(e.next_in_ael.top.y != y) && IsHotEdge(e.next_in_ael))
						AddOutPt(e.next_in_ael, e.top);

					if (IsMaxima(e))
					{
						e = DoMaxima(e);  //TOP OF BOUND (MAXIMA)
						continue;
					}
					else
					{
						//INTERMEDIATE VERTEX ...
						UpdateEdgeIntoAEL(e);
						if (IsHotEdge(e)) AddOutPt(e, e.bot);
						if (IsHorizontal(e))
							PushHorz(e);  //horizontals are processed later
					}
				}
				e = e.next_in_ael;
			}
		}

		private Active DoMaxima(Active e)
		{
			Active next_e, prev_e, max_pair;
			prev_e = e.prev_in_ael;
			next_e = e.next_in_ael;
			if (IsOpen(e) &&
				((e.vertex_top.flags & (VertexFlags.vfOpenStart | VertexFlags.vfOpenEnd)) != VertexFlags.vfNone))
			{
				if (IsHotEdge(e)) AddOutPt(e, e.top);
				if (!IsHorizontal(e))
				{
					if (IsHotEdge(e)) e.outrec = null;
					DeleteFromAEL(e);
				}
				return next_e;
			}
			else
			{
				max_pair = GetMaximaPair(e);
				if (max_pair == null) return next_e;  //eMaxPair is horizontal
			}

			//only non-horizontal maxima here.
			//process any edges between maxima pair ...
			while (next_e != max_pair)
			{
				IntersectEdges(e, next_e, e.top);
				SwapPositionsInAEL(e, next_e);
				next_e = e.next_in_ael;
			}

			if (IsOpen(e))
			{
				if (IsHotEdge(e))
				{
					if (max_pair != null)
						AddLocalMaxPoly(e, max_pair, e.top);
					else
						AddOutPt(e, e.top);
				}
				if (max_pair != null) DeleteFromAEL(max_pair);
				DeleteFromAEL(e);
				return prev_e != null ? prev_e.next_in_ael : actives;
			}
			//here E.next_in_ael == ENext == EMaxPair ...
			if (IsHotEdge(e))
				AddLocalMaxPoly(e, max_pair, e.top);

			DeleteFromAEL(e);
			DeleteFromAEL(max_pair);
			return prev_e != null ? prev_e.next_in_ael : actives;
		}
	}

    #region ClipperI
    public class ClipperI : Clipper
    {
		public ClipperI(int scale = 1)
        {
			_scale = scale == 0 ? 1 : Math.Abs(scale);
		}
        public bool Execute(ClipType clipType, FillRule ft, PathsI solution_closed)
		{
			bool executed = true;
			solution_closed.clear();
			try
			{
				ExecuteInternal(clipType, ft);
				BuildResultI(solution_closed, new PathsI());
			}
			catch
			{
				executed = false;
			}
			CleanUp();
			return executed;
		}

		public bool Execute(ClipType clipType, FillRule ft, PathsI solution_closed, PathsI solution_open)
		{
			bool executed = true;
			solution_closed.clear();
			solution_open.clear();
			try
			{
				ExecuteInternal(clipType, ft);
				BuildResultI(solution_closed, solution_open);
			}
			catch
			{
				executed = false;
			}
			CleanUp();
			return executed;
		}

		public bool Execute(ClipType clipType, FillRule ft, PolyTreeI solution_closed, PathsI solution_open)
		{
			bool executed = true;
			solution_closed.Clear();
			solution_open.clear();
			try
			{
				ExecuteInternal(clipType, ft);
				BuildResultTreeI(solution_closed, solution_open);
			}
			catch
			{
				executed = false;
			}
			CleanUp();
			return executed;
		}

		internal bool BuildResultI(PathsI solution_closed, PathsI solution_open)
		{
			bool built = false;
			try
			{
				solution_closed.resize(0);
				solution_closed.reserve(outRecs.Count);
				if (solution_open.data == null)
				{
					solution_open.data = new List<PathI>(outRecs.Count);
				}

				double inv_scale = 1 / _scale;

				foreach (var outrec in outRecs)
				{
					if (outrec.pts == null) continue;
					OutPt op = outrec.pts.next;
					int cnt = PointCount(op);

					bool is_open = (outrec.state == OutRecState.Open);
					if (is_open)
					{
						if (cnt < 2 || solution_open.data == null) continue;
					}
					else
					{
						//fixup for duplicate start and end points ...
						if (op.pt == outrec.pts.pt) cnt--;
						if (cnt < 3) continue;
					}

					PathI p = new PathI(cnt);
					if (_scale != 1.0)
					{
						for (int i = 0; i < cnt; i++)
						{
							p.Add(new PointI((long)Math.Round(op.pt.x * inv_scale),
								(long)Math.Round(op.pt.y * inv_scale)));
							op = op.next;
						}
					}
					else
					{
						for (int i = 0; i < cnt; i++)
						{
							p.Add(op.pt);
							op = op.next;
						}
					}

					if (is_open)
						solution_open.Add(p);
					else
						solution_closed.Add(p);
				}
				built = true;
			}
			catch
			{
				built = false;
			}
			return built;
		}

		private bool BuildResultTreeI(PolyTreeI pt, PathsI solution_open)
		{
			bool built = false;

			try
			{
				pt.Clear();
				if (solution_open.data != null)
				{
					solution_open.resize(0);
					solution_open.reserve(outRecs.Count);
				}

				double inv_scale = 1 / _scale;

				foreach (var outrec in outRecs)
				{
					if (outrec.pts == null) continue;
					OutPt op = outrec.pts.next;
					int cnt = PointCount(op);
					//fixup for duplicate start and } points ...
					if (op.pt == outrec.pts.pt) cnt--;

					bool is_open = outrec.state == OutRecState.Open;
					if (cnt < 2 || (!is_open && cnt == 2) || (is_open && solution_open.data == null))
						continue;

					var p = new PathI(cnt);
					if (_scale != 1.0)
					{
						for (int i = 0; i < cnt; i++)
						{
							p.Add(new PointI((long)Math.Round(op.pt.x * inv_scale),
								(long)Math.Round(op.pt.y * inv_scale)));
							op = op.next;
						}
					}
					else
					{
						for (int i = 0; i < cnt; i++)
						{
							p.Add(op.pt);
							op = op.next;
						}
					}

					if (is_open)
						solution_open.Add(p);
					else if (outrec.owner != null && outrec.owner.PolyTree != null)
					{
						outrec.PolyTree = new PolyTreeI(outrec.owner.PolyTree, p);
					}
					else
					{
						outrec.PolyTree = new PolyTreeI(pt, p);
					}
				}
				built = true;
			}
			catch
			{
				built = false;
			}
			return built;
		}

		private RectI GetBounds()
		{
			if (vertices.Count == 0) return new RectI(0, 0, 0, 0);
			RectI bounds = new RectI(int.MaxValue, int.MaxValue, int.MinValue, int.MinValue);
			foreach (var it in vertices)
			{
				Vertex v = it, v2 = v;
				do
				{
					if (v2.pt.x < bounds.left) bounds.left = v2.pt.x;
					if (v2.pt.x > bounds.right) bounds.right = v2.pt.x;
					if (v2.pt.y < bounds.top) bounds.top = v2.pt.y;
					if (v2.pt.y > bounds.bottom) bounds.bottom = v2.pt.y;
					v2 = v2.next;
				} while (v2 != v);
			}
			if (_scale != 1.0)
			{
				double inv_scale = 1 / _scale;
				bounds.left = (long)Math.Round(bounds.left * inv_scale);
				bounds.top = (long)Math.Round(bounds.top * inv_scale);
				bounds.right = (long)Math.Round(bounds.right * inv_scale);
				bounds.bottom = (long)Math.Round(bounds.bottom * inv_scale);
			}
			return bounds;
		}
	}
	#endregion

	#region ClipperD 
	public class ClipperD : Clipper
    {
		public ClipperD(double scale = DefaultScale)
		{
			_scale = scale == 0 ? 1 : Math.Abs(scale);
		}
		

		public void AddPath(PathD path, PathType poly_type, bool is_open)
		{
			PathI p = new PathI(path, _scale);
			AddPath(p, poly_type, is_open);
		}

		public void AddPaths(PathsD paths, PathType poly_type, bool is_open)
		{
			foreach (var path in paths.data)
				AddPath(path, poly_type, is_open);
		}

		public bool BuildResultD(PathsD solution_closed, PathsD solution_open = default)
		{
			bool built = false;
			try
			{
				solution_closed.resize(0);
				solution_closed.reserve(outRecs.Count);
				if (solution_open.data != null)
				{
					solution_open.resize(0);
					solution_open.reserve(outRecs.Count);
				}

				double inv_scale = 1 / _scale;

				foreach (var outrec in outRecs) 
				{
					if (outrec.pts == null) continue;
					OutPt op = outrec.pts.next;
					int cnt = PointCount(op);

					bool is_open = outrec.state == OutRecState.Open;

					if (is_open)
					{
						if (cnt < 2 || solution_open.data == null) continue;
					}
					else
					{
						//fixup for duplicate start and end points ...
						if (op.pt == outrec.pts.pt) cnt--;
						if (cnt < 3) continue;
					}

					var p = new PathD(cnt);
					for (int i = 0; i < cnt; i++)
					{
						p.Add(new PointD(op.pt.x * inv_scale, op.pt.y * inv_scale));
						op = op.next;
					}

					if (is_open)
						solution_open.Add(p);
					else
						solution_closed.Add(p);
				}
				built = true;
			}
			catch
			{
				built = false;
			}
			return built;
		}

		private bool BuildResultTreeD(PolyTreeD pt, PathsD solution_open)
		{
            bool built;
            try
			{
				pt.Clear();
				if (solution_open.data != null)
				{
					solution_open.resize(0);
					solution_open.reserve(outRecs.Count);
				}

				double inv_scale = 1 / _scale;

				foreach (var outrec in outRecs)
				{
					if (outrec.pts == null) continue;
					OutPt op = outrec.pts.next;
					int cnt = PointCount(op);
					//fixup for duplicate start and } points ...
					if (op.pt == outrec.pts.pt) cnt--;

					bool is_open = (outrec.state == OutRecState.Open);
					if (cnt < 2 || (!is_open && cnt == 2) || (is_open && solution_open.data == null))
						continue;

					var p = new PathD(cnt);
					for (int i = 0; i < cnt; i++)
					{
						p.Add(new PointD((double)op.pt.x * inv_scale,
							(double)op.pt.y * inv_scale));
						op = op.next;
					}
					if (is_open)
						solution_open.Add(p);
					else if (outrec.owner != null && outrec.owner.PolyTree != null)
					{
						outrec.PolyTree = new PolyTreeI(outrec.owner.PolyTree, new PathI(p, _scale));
					}
					else
						outrec.PolyTree = new PolyTreeI(new PolyTreeD(pt, p));
				}
				built = true;
			}
			catch
			{
				built = false;
			}
			return built;
		}

		public bool Execute(ClipType clipType, FillRule ft, PathsD solution_closed)
		{
			bool executed = true;
			solution_closed.clear();
			try
			{
				ExecuteInternal(clipType, ft);
				BuildResultD(solution_closed);
			}
			catch
			{
				executed = false;
			}
			CleanUp();
			return executed;
		}

		public bool Execute(ClipType clipType, FillRule ft, PathsD solution_closed, PathsD solution_open)
		{
			bool executed = true;
			solution_closed.clear();
			solution_open.clear();
			try
			{
				ExecuteInternal(clipType, ft);
				BuildResultD(solution_closed, solution_open);
			}
			catch
			{
				executed = false;
			}
			CleanUp();
			return executed;
		}

		public bool Execute(ClipType clipType, FillRule ft, PolyTreeD solution_closed, PathsD solution_open) 
		{
			bool executed = true;
			solution_closed.Clear();
			solution_open.clear();
			try
			{
				ExecuteInternal(clipType, ft);
				BuildResultTreeD(solution_closed, solution_open);
			}
			catch
			{
				executed = false;
			}
			CleanUp();
			return executed;
		}
	}
    #endregion
}
