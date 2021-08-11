using ClipperLib2Beta;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TVGL.Numerics;

namespace ClipperLib2Beta
{
	public class ClipperConstants
    {
		private const double floating_point_tolerance = 1E-15;           //floating point tolerance for equality
		private const double default_min_edge_len = 0.2;  //minimum edge length for stripping duplicates
		private const double sqrt_two = 1.4142135623731;
		private const double one_degree_as_radians = 0.01745329252;
	}

	//Path: a simple data structure to represent a series of vertices, whether
	//open (poly-line) or closed (polygon). A path may be simple or complex (self
	//intersecting). For simple polygons, path orientation (whether clockwise or
	//counter-clockwise) is generally used to differentiate outer paths from inner
	//paths (holes). For complex polygons (and also for overlapping polygons),
	//explicit 'filling rules' (see below) are used to indicate regions that are
	//inside (filled) and regions that are outside (unfilled) a specific polygon.
	public interface Path<T> : IList<T>
	{
		public int size();
		public bool empty();
		public void reserve(int size);
		//public void push_back(Point<T> point);
		public void pop_back();
		public void clear();
		public void Resize(int n);
		public void Rotate(PointD center, double angle_rad);
	}
	public struct PathD : Path<PointD>, IList<PointD>
	{
		public List<PointD> data { get; set; }
		public PointD this[int i] { get => data[i]; set => data[i] = value; }
		public int Count => data.Count;
		public bool IsReadOnly => false;
		public void Add(PointD p) => data.Add(p);
		public void Clear() => data.Clear();
		public bool Contains(PointD p) => data.Contains(p);
		public void CopyTo(PointD[] array, int arrayIndex) => data.CopyTo(array, arrayIndex);
		public IEnumerator<PointD> GetEnumerator() => data.GetEnumerator();
		public int IndexOf(PointD p) => data.IndexOf(p);
		public void Insert(int i, PointD p) => data.Insert(i, p);
		public bool Remove(PointD p) => data.Remove(p);
		public void RemoveAt(int i) => data.RemoveAt(i);
		public void Resize(int n) => data = new List<PointD>(n);
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		public void Rotate(PointD center, double angle_rad)
		{
			double cos_a = Math.Cos(angle_rad);
			double sin_a = Math.Sin(angle_rad);
			foreach (var point in data)
				point.Rotate(center, sin_a, cos_a);
		}
		public int size() => data.Count();
		public bool empty() => data.Count() == 0;
		public void reserve(int size) => data.Capacity = size;
		public void push_back(PointD point) => data.Add(point);
		public void pop_back() => data.RemoveAt(data.Count() - 1);
		public void clear() => data.Clear();

		public void Append(PathD extra)
		{
			if (extra.size() > 0)
				data.AddRange(extra.data);
		}

		public double Area()
		{
			double area = 0.0;
			var len = data.Count() - 1;
			if (len < 2) return area;
			var j = len;
			for (var i = 0; i <= len; ++i)
			{
				double d = (double)(data[j].x + data[i].x);
				area += d * (data[j].y - data[i].y);
				j = i;
			}
			return -area * 0.5;
		}

		public RectD Bounds()
		{
			var bounds = new RectD(double.MaxValue, double.MaxValue, double.MinValue, double.MinValue);
			foreach (var point in data)
			{
				if (point.x < bounds.left) bounds.left = point.x;
				if (point.x > bounds.right) bounds.right = point.x;
				if (point.y < bounds.top) bounds.top = point.y;
				if (point.y > bounds.bottom) bounds.bottom = point.y;
			}
			return (bounds.left >= bounds.right) ? new RectD() : bounds;
		}

		public void Offset(double dx, double dy)
		{
			if (dx == 0 && dy == 0) return;
			var offset = new List<PointD>(size());
			foreach (var point in data)
			{
				offset.Add(new PointD(point.x + dx, point.y + dy));
			}
			data = offset;
		}

		public bool Orientation() => Area() >= 0;

		public void Reverse() => data.Reverse();

		public void Scale(double sx, double sy)
		{
			if (sx == 0) sx = 1;
			if (sy == 0) sy = 1;
			if (sx == 1 && sy == 1) return;

			var scaled = new List<PointD>(size());
			foreach (var point in data)
			{
				scaled.Add(new PointD(point.x * sx, point.y * sy));
			}
			data = scaled;

			StripDuplicates();
		}

		public void StripDuplicates(bool is_closed_path = false, long min_length = 0)
		{
			throw new NotImplementedException();
		}

		public void AppendPointsScale(Path<PointI> other, double scale)
		{
			throw new NotImplementedException();
		}
    }

	public struct PathI : Path<PointI>, IList<PointI>
	{
		public List<PointI> data { get; set; }
		public PointI this[int i] { get => data[i]; set => data[i] = value; }
		public int Count => data.Count;
		public bool IsReadOnly => false;
		public void Add(PointI p) => data.Add(p);
		public void Clear() => data.Clear();
		public bool Contains(PointI p) => data.Contains(p);
		public void CopyTo(PointI[] array, int arrayIndex) => data.CopyTo(array, arrayIndex);
		public IEnumerator<PointI> GetEnumerator() => data.GetEnumerator();
		public int IndexOf(PointI p) => data.IndexOf(p);
		public void Insert(int i, PointI p) => data.Insert(i, p);
		public bool Remove(PointI p) => data.Remove(p);
		public void RemoveAt(int i) => data.RemoveAt(i);
		public void Resize(int n) => data.Resize(n);
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		public void Rotate(PointD center, double angle_rad)
		{
			double cos_a = Math.Cos(angle_rad);
			double sin_a = Math.Sin(angle_rad);
			foreach (var point in data)
				point.Rotate(center, sin_a, cos_a);
		}
		public int size() => data.Count();
		public bool empty() => data.Count() == 0;
		public void reserve(int size) => data.Capacity = size;
		public void push_back(PointI point) => data.Add(point);
		public void pop_back() => data.RemoveAt(data.Count() - 1);
		public void clear() => data.Clear();

		public void Append(PathI extra)
		{
			if (extra.size() > 0)
				data.AddRange(extra.data);
		}

		public double Area()
		{
			double area = 0.0;
			var len = data.Count() - 1;
			if (len < 2) return area;
			var j = len;
			for (var i = 0; i <= len; ++i)
			{
				double d = (double)(data[j].x + data[i].x);
				area += d * (data[j].y - data[i].y);
				j = i;
			}
			return -area * 0.5;
		}

		public RectI Bounds()
		{
			var bounds = new RectI(long.MaxValue, long.MaxValue, long.MinValue, long.MinValue);
			foreach (var point in data)
			{
				if (point.x < bounds.left) bounds.left = point.x;
				if (point.x > bounds.right) bounds.right = point.x;
				if (point.y < bounds.top) bounds.top = point.y;
				if (point.y > bounds.bottom) bounds.bottom = point.y;
			}
			return (bounds.left >= bounds.right) ? new RectI() : bounds;
		}

		public void Offset(long dx, long dy)
		{
			if (dx == 0 && dy == 0) return;
			var offset = new List<PointI>(size());
			foreach (var point in data)
			{
				offset.Add(new PointI(point.x + dx, point.y + dy));
			}
			data = offset;
		}

		public bool Orientation() => Area() >= 0;

		public void Reverse() => data.Reverse();

		public void Scale(double sx, double sy)
		{
			if (sx == 0) sx = 1;
			if (sy == 0) sy = 1;
			if (sx == 1 && sy == 1) return;

			var scaled = new List<PointI>(size());
			foreach (var point in data)
			{
				scaled.Add(new PointI((long)Math.Round(point.x * sx), (long)Math.Round(point.y * sy)));
			}
			data = scaled;

			StripDuplicates();
		}

		public void StripDuplicates(bool is_closed_path = false, long min_length = 0)
		{
			throw new NotImplementedException();
		}

        public void AppendPointsScale(Path<PointI> other, double scale)
        {
            throw new NotImplementedException();
        }

        //PathI(Path<PointI> other, double scale)
        //{
        //	if (scale == 0) scale = 1;
        //	if (scale == 1)
        //	{
        //		Append(other);
        //	}
        //	else
        //	{
        //		AppendPointsScale(other, scale);
        //	}
        //}

        //		public Path(Path<T> other, double scale)
        //		{
        //			if (scale == 0) scale = 1;
        //			AppendPointsScale(other, scale);
        //		}		

        //template < typename T2, typename =
        //  typename std::enable_if < !std::is_same < T, T2 >::value,T >::type >
        //	   void Assign(const Path<T2> & other, double scale){
        //	if (&other == reinterpret_cast<Path<T2>*>(this))
        //		throw ClipperLibException("Can't assign self to self in Path<T>::Assign.");
        //	data.clear();
        //	if (scale == 0) scale = 1;
        //	AppendPointsScale(other, scale);
        //}

        //	public void Assign(Path<T> other, double scale)
        //	{
        //		if (other == (Path<T>)(this))
        //			throw ClipperLibException("Can't assign self to self in Path<T>::Assign.");
        //		data.Clear();
        //		if (scale == 0) scale = 1;
        //		if (scale == 1)
        //		{
        //			Append(other);
        //		}
        //		else
        //		{
        //			AppendPointsScale(other, scale);
        //		}
        //	}

        //	friend inline Path<T> &operator<<(Path<T> &path, const Point<T> &point) 
        //{
        //	path.data.push_back(point);
        //	return path;
        //}
        //friend std::ostream &operator<<(std::ostream &os, const Path<T> &path)
        //{
        //	if (path.data.empty())
        //		return os;

        //	Size last = path.size() - 1;

        //	for (Size i = 0; i < last; ++i)
        //		os << "(" << path[i].x << "," << path[i].y << "), ";

        //	os << "(" << path[last].x << "," << path[last].y << ")\n";

        //	return os;
        //}
    }

	public interface Point<T>
	{
		public T x { get; set; }
		public T y { get; set; }
		public void Rotate(PointD center, double angle_rad);
		public void Rotate(PointD center, double sin_a, double cos_a);
		public bool NearEqual(Point<T> p, double min_dist_sqrd);
	}

	public struct PointD : Point<double>
	{
		public PointD(double nx, double ny) { x = nx; y = ny; }
		public double x { get; set; }
		public double y { get; set; }

		public void Rotate(PointD center, double angle_rad)
		{
			double tmp_x = x - center.x;
			double tmp_y = y - center.y;
			double cos_a = Math.Cos(angle_rad);
			double sin_a = Math.Sin(angle_rad);
			x = tmp_x * cos_a - tmp_y * sin_a + center.x;
			y = tmp_x * sin_a - tmp_y * cos_a + center.y;
		}

		public void Rotate(PointD center, double sin_a, double cos_a)
		{
			double tmp_x = x - center.x;
			double tmp_y = y - center.y;
			x = tmp_x * cos_a - tmp_y * sin_a + center.x;
			y = tmp_x * sin_a - tmp_y * cos_a + center.y;
		}

		[Intrinsic]
		public bool NearEqual(Point<double> p, double min_dist_sqrd)
		{
			return (x - p.x) * (x - p.x) + (y - p.y) * (y - p.y) < min_dist_sqrd;
		}

		[Intrinsic]
		public static bool operator ==(PointD a, PointD b) => a.x == b.x && a.y == b.y;

		[Intrinsic]
		public static bool operator !=(PointD a, PointD b) => !(a == b);

		[Intrinsic]
		public static PointD operator -(PointD a, PointD b) => new PointD(a.x - b.x, a.y - b.y);

		[Intrinsic]
		public static PointD operator +(PointD a, PointD b) => new PointD(a.x + b.x, a.y + b.y);

		[Intrinsic]
		public static PointD operator *(PointD a, double factor) => new PointD(a.x * factor, a.y * factor);

		[Intrinsic]
		public static bool operator <(PointD a, PointD b) => (a.x == b.x) ? (a.y < b.y) : (a.x < b.x);

		[Intrinsic]
		public static bool operator >(PointD a, PointD b) => (a.x == b.x) ? (a.y > b.y) : (a.x > b.x);

		public override bool Equals(object obj)
		{
			throw new NotImplementedException();
		}

		public override int GetHashCode()
		{
			throw new NotImplementedException();
		}
	}

	public struct PointI : Point<long>
	{
		public PointI(long nx, long ny) { x = nx; y = ny; }
		public long x { get; set; }
		public long y { get; set; }

		public void Rotate(PointD center, double angle_rad)
		{
			double tmp_x = x - center.x;
			double tmp_y = y - center.y;
			double cos_a = Math.Cos(angle_rad);
			double sin_a = Math.Sin(angle_rad);
			x = (long)Math.Round(tmp_x * cos_a - tmp_y * sin_a + center.x);
			y = (long)Math.Round(tmp_x * sin_a - tmp_y * cos_a + center.y);
		}

		public void Rotate(PointD center, double sin_a, double cos_a)
		{
			double tmp_x = x - center.x;
			double tmp_y = y - center.y;
			x = (long)Math.Round(tmp_x * cos_a - tmp_y * sin_a + center.x);
			y = (long)Math.Round(tmp_x * sin_a - tmp_y * cos_a + center.y);
		}

		[Intrinsic]
		public bool NearEqual(Point<long> p, double min_dist_sqrd)
		{
			return (x - p.x) * (x - p.x) + (y - p.y) * (y - p.y) < min_dist_sqrd;
		}

		[Intrinsic]
		public static bool operator ==(PointI a, PointI b) => a.x == b.x && a.y == b.y;

		[Intrinsic]
		public static bool operator !=(PointI a, PointI b) => !(a == b);

		[Intrinsic]
		public static PointI operator -(PointI a, PointI b) => new PointI(a.x - b.x, a.y - b.y);

		[Intrinsic]
		public static PointI operator +(PointI a, PointI b) => new PointI(a.x + b.x, a.y + b.y);

		[Intrinsic]
        public static bool operator <(PointI a, PointI b) => (a.x == b.x) ? (a.y < b.y) : (a.x < b.x);

		[Intrinsic]
		public static bool operator >(PointI a, PointI b) => (a.x == b.x) ? (a.y > b.y) : (a.x > b.x);

        public override bool Equals(object obj)
        {
            throw new NotImplementedException();
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

        //public static operator<<(std::ostream &os, const Point<T> &point)
        //{
        //	os << "(" << point.x << "," << point.y << ")";
        //	return os;
        //}
    }

	#region Rectangle
	public interface Rect<T>
	{
		public T left { get; set; }
		public T top { get; set; }
		public T right { get; set; }
		public T bottom { get; set; }
		public T Width();
		public T Height();
		public void Width(T width);
		public void Height(T height);
		public bool IsEmpty();
		public void Inflate(T dx, T dy);
		public void Offset(T dx, T dy);
		public void Intersect(Rect<T> rect);
		public void Union(Rect<T> rect);
		public void Rotate(double angle_rad);
		public void Scale(T scale);
	}

	public struct RectD : Rect<double>
	{	
		public double left { get; set; }
		public double top { get; set; }
		public double right { get; set; }
		public double bottom { get; set; }

		public RectD(double l, double t, double r, double b)
		{
			left = l; top = t;
			right = r; bottom = b;
		}

		public RectD(RectD r)
		{
			left = r.left; top = r.top;
			right = r.right; bottom = r.bottom;
		}

		public double Width() => right - left;
		public double Height() => bottom - top;
		public void Width(double width) => right = left + width;
		public void Height(double height) => bottom = top + height;
		public bool IsEmpty() => bottom <= top || right <= left;

		public void Inflate(double dx, double dy)
		{
			left -= dx;
			right += dx;
			top -= dy;
			bottom += dy;
		}

		public void Offset(double dx, double dy)
		{
			left += dx;
			right += dx;
			top += dy;
			bottom += dy;
		}

		public void Intersect(Rect<double> rect)
		{
			if (IsEmpty())
				return;
			else if (rect.IsEmpty())
			{
				this = new RectD();
			}
			else
			{
				left = Math.Max(rect.left, left);
				right = Math.Min(rect.right, right);
				top = Math.Max(rect.top, top);
				bottom = Math.Min(rect.bottom, bottom);
				if (IsEmpty())
					this = new RectD();
			}
		}

		public void Union(Rect<double> rect)
		{
			if (rect.IsEmpty())
				return;
			else if (IsEmpty())
			{
				this = (RectD)rect;
				return;
			}
			left = Math.Min(rect.left, left);
			right = Math.Max(rect.right, right);
			top = Math.Min(rect.top, top);
			bottom = Math.Max(rect.bottom, bottom);
		}

		public void Scale(double scale)
		{
			left *= scale;
			top *= scale;
			right *= scale;
			bottom *= scale;
		}

		[Intrinsic]
		public void Rotate(double angle_rad)
		{
			var cp = new PointD() { x = (right + left) / 2, y = (bottom + top) / 2 };
			var pts = new PathD();
			pts.Resize(4);
			pts[0] = new PointD(left, top);
			pts[1] = new PointD(right, top);
			pts[2] = new PointD(right, bottom);
			pts[3] = new PointD(left, bottom);

			pts.Rotate(cp, angle_rad);

			right = long.MinValue;
			left = long.MaxValue;
			top = long.MinValue;
			bottom = long.MaxValue;
			foreach (var point in pts)//Do X and Y in the same enumeration
			{
				if (point.x > right) right = point.x;
				if (point.x < left) left = point.x;
				if (point.y > top) top = point.y;
				if (point.y < bottom) bottom = point.y;
			}
		}
	}

	public struct RectI : Rect<long>
	{
		public long left { get; set; }
		public long top { get; set; }
		public long right { get; set; }
		public long bottom { get; set; }

		public RectI(long l, long t, long r, long b)
		{
			left = l; top = t;
			right = r; bottom = b;
		}

		public RectI(RectI r)
		{
			left = r.left; top = r.top;
			right = r.right; bottom = r.bottom;
		}

		public long Width() => right - left;
		public long Height() => bottom - top;
		public void Width(long width) => right = left + width;
		public void Height(long height) => bottom = top + height;
		public bool IsEmpty() => bottom <= top || right <= left;

		public void Inflate(long dx, long dy)
		{
			left -= dx;
			right += dx;
			top -= dy;
			bottom += dy;
		}

		public void Offset(long dx, long dy)
		{
			left += dx;
			right += dx;
			top += dy;
			bottom += dy;
		}

		public void Intersect(Rect<long> rect)
		{
			if (IsEmpty())
				return;
			else if (rect.IsEmpty())
			{
				this = new RectI();
			}
			else
			{
				left = Math.Max(rect.left, left);
				right = Math.Min(rect.right, right);
				top = Math.Max(rect.top, top);
				bottom = Math.Min(rect.bottom, bottom);
				if (IsEmpty())
					this = new RectI();
			}
		}

		public void Union(Rect<long> rect)
		{
			if (rect.IsEmpty())
				return;
			else if (IsEmpty())
			{
				this = (RectI)rect;
				return;
			}
			left = Math.Min(rect.left, left);
			right = Math.Max(rect.right, right);
			top = Math.Min(rect.top, top);
			bottom = Math.Max(rect.bottom, bottom);
		}

		public void Scale(long scale)
		{
			left *= scale;
			top *= scale;
			right *= scale;
			bottom *= scale;
		}

		[Intrinsic]
		public void Rotate(double angle_rad)
		{
			var cp = new PointD() { x = (right + left) / 2, y = (bottom + top) / 2 };
			var pts = new PathI();
			pts.Resize(4);
			pts[0] = new PointI(left, top);
			pts[1] = new PointI(right, top);
			pts[2] = new PointI(right, bottom);
			pts[3] = new PointI(left, bottom);

			pts.Rotate(cp, angle_rad);

			right = long.MinValue;
			left = long.MaxValue;
			top = long.MinValue;
			bottom = long.MaxValue;
			foreach (var point in pts)//Do X and Y in the same enumeration
			{
				if (point.x > right) right = point.x;
				if (point.x < left) left = point.x;
				if (point.y > top) top = point.y;
				if (point.y < bottom) bottom = point.y;
			}
		}
	}
	#endregion

	//friend std::ostream &operator<<(std::ostream &os, const Rect<T> &rect)
	//{
	//	os << "("
	//	   << rect.left << "," << rect.top << "," << rect.right << "," << rect.bottom
	//	   << ")";
	//	return os;
	//}


	// ClipperLibException ---------------------------------------------------------

	//class ClipperLibException : public std::exception
	//{
	//	public:
	//		explicit ClipperLibException(const char* description) :
	//			m_descr(description) { }
	//		virtual const char *what() const throw() { return m_descr.c_str(); }

	//	private:
	//		std::string m_descr;
	//};

	public static class ListExtra
	{
		public static void Resize<T>(this List<T> list, int sz, T c)
		{
			int cur = list.Count;
			if (sz < cur)
				list.RemoveRange(sz, cur - sz);
			else if (sz > cur)
			{
				if (sz > list.Capacity)//this bit is purely an optimisation, to avoid multiple automatic capacity changes.
					list.Capacity = sz;
				list.AddRange(Enumerable.Repeat(c, sz - cur));
			}
		}
		public static void Resize<T>(this List<T> list, int sz) where T : new()
		{
			Resize(list, sz, new T());
		}
	}


	//------------------------------------------------------------------------------
	// Specialization functions for Paths
	//------------------------------------------------------------------------------

	//template<>
	//inline void PathsI::Assign(const PathsI &other, double scale) {
	//	using namespace std;
	//	data.clear();
	//	data.resize(other.data.size());
	//	typename vector<PathI>::iterator it1;
	//typename vector<PathI>::const_iterator it2;
	//	for (it1 = data.begin(), it2 = other.data.begin(); it1 != data.end(); ++it1, ++it2)
	//		it1->Assign(*it2, scale);
	//}
	////------------------------------------------------------------------------------

	//template<>
	//inline void PathsD::Assign(const PathsI &other, double scale) {
	//	using namespace std;
	//	data.clear();
	//	data.resize(other.data.size());
	//	typename vector<PathD>::iterator it1;
	//typename vector<PathI>::const_iterator it2;
	//	for (it1 = data.begin(), it2 = other.data.begin(); it1 != data.end(); ++it1, ++it2)
	//		it1->Assign(*it2, scale);
	//}
	////------------------------------------------------------------------------------
	//template<>
	//inline void PathsI::Assign(const PathsD &other, double scale) {
	//	using namespace std;
	//	data.clear();
	//	data.resize(other.data.size());
	//	typename vector<PathI>::iterator it1;
	//typename vector<PathD>::const_iterator it2;
	//	for (it1 = data.begin(), it2 = other.data.begin(); it1 != data.end(); ++it1, ++it2)
	//		it1->Assign(*it2, scale);
	//}
	////------------------------------------------------------------------------------

	//template<>
	//inline void PathsD::Assign(const PathsD &other, double scale) {
	//	using namespace std;
	//	data.clear();
	//	data.resize(other.data.size());
	//	typename vector<PathD>::iterator it1;
	//typename vector<PathD>::const_iterator it2;
	//	for (it1 = data.begin(), it2 = other.data.begin(); it1 != data.end(); ++it1, ++it2)
	//		it1->Assign(*it2, scale);
	//}
	////------------------------------------------------------------------------------

	//template < typename T >
	//  void clipperlib::Paths < T >::Assign(const PathsI & other, double scale){ }
	////------------------------------------------------------------------------------

	//template < typename T >
	//  void clipperlib::Paths < T >::Assign(const PathsD & other, double scale){ }
	////------------------------------------------------------------------------------

	//template<>
	//inline PathsI::Paths(const PathsI &other, double scale) {
	//	Assign(other, scale);
	//}
	////------------------------------------------------------------------------------

	//template<>
	//inline PathsD::Paths(const PathsI &other, double scale) {
	//	Assign(other, scale);
	//}
	////------------------------------------------------------------------------------

	//template<>
	//inline PathsI::Paths(const PathsD &other, double scale) {
	//	Assign(other, scale);
	//}
	////------------------------------------------------------------------------------

	//template<>
	//inline PathsD::Paths(const PathsD &other, double scale) {
	//	Assign(other, scale);
	//}
	////------------------------------------------------------------------------------
	public interface Paths<T>
    {
		public int size();
		public void resize(int size);
		public void reserve(int size);
		public void push_back(Path<T> paths);
		public void clear();
		public void Append(List<Path<T>> extra);
		public Rect<T> Bounds();
		public void offset(T dx, T dy);
		public void Reverse();
		public void Rotate(Point<T> center, double angle_rad);
		public void Scale(double scale_x, double scale_y);
		public void StripDuplicates(bool is_closed_path, T min_length);

				//	void Offset(T dx, T dy)
				//{
				//	if (dx == 0 && dy == 0) return;
				//	for (auto & path : data)
				//			for (auto & point : path.data)
				//{
				//	point.x += dx;
				//	point.y += dy;
				//}
				//	}
				//	void Reverse()
				//{
				//	for (auto & path : data)
				//			path.Reverse();
				//	}
				//	void Rotate(const PointD &center, double angle_rad)
				//{
				//	double cos_a = cos(angle_rad);
				//	double sin_a = sin(angle_rad);

		//	for (auto & path : data)
		//			for (auto & point : path.data)
		//	point.Rotate(center, sin_a, cos_a);
		//	}
		//	void Scale(double scale_x, double scale_y)
		//{
		//	for (auto & path : data)
		//			path.Scale(scale_x, scale_y);
		//	}
		//	void StripDuplicates(bool is_closed_path, T min_length)
		//{
		//	for (auto & path : data)
		//			path.StripDuplicates(is_closed_path, min_length);
		//	}

		//	template < typename T2 >
		//	  void AppendPointsScale(const Paths<T2>& other, double scale) {
		//	size_t other_size = other.size();
		//	data.resize(other_size);
		//	for (size_t i = 0; i < other_size; ++i)
		//		data[i].AppendPointsScale(other[i], scale);

		//}

		//friend inline Paths<T> &operator<<(Paths<T> &paths, const Path<T> &path) {
		//	paths.data.push_back(path);
		//	return paths;
		//}

		//friend std::ostream &operator<<(std::ostream &os, const Paths<T> &paths)
		//{
		//	for (Size i = 0; i < paths.size(); ++i)
		//		os << paths[i];
		//	os << "\n";
		//	return os;
		//}
	}

    public struct PathsI : Paths<PathI>
    {
		public List<PathI> data;
		public PathI this[int i] { get => data[i]; set => data[i] = value; }
		public int size() => data.Sum(p => p.Count);
		public void resize(int size) => data.Resize(size);
		public void reserve(int size) => data.Capacity = size;
		public void push_back(PathI paths) => data.Add(paths);
		public void clear() => data.Clear();
        public void Append(Paths<PathI> extra)
		{
			if (extra.size() > 0)
				data.AddRange(((PathsI)extra).data);
		}
		public RectI Bounds()
        {
			var bounds = new RectI(long.MaxValue, long.MaxValue, long.MinValue, long.MinValue);
			foreach(var path in data)
            {
				foreach (var point in path)
				{
					if (point.x < bounds.left) bounds.left = point.x;
					if (point.x > bounds.right) bounds.right = point.x;
					if (point.y < bounds.top) bounds.top = point.y;
					if (point.y > bounds.bottom) bounds.bottom = point.y;
				}
			}
			return (bounds.left >= bounds.right) ? new RectI() : bounds;
		}

        public void offset(PointI dx, PointI dy)
        {
            throw new NotImplementedException();
        }

        public void Reverse()
        {
            throw new NotImplementedException();
        }

        public void Rotate(Point<PointI> center, double angle_rad)
        {
            throw new NotImplementedException();
        }

        public void Scale(double scale_x, double scale_y)
        {
            throw new NotImplementedException();
        }

        public void StripDuplicates(bool is_closed_path, PointI min_length)
        {
            throw new NotImplementedException();
        }
    }

    public struct PathsD : Paths<PathD>
	{
		public List<PathD> data;
		public PathD this[int i] { get => data[i]; set => data[i] = value; }
		public int size() => data.Sum(p => p.Count);
		public void resize(int size) => data.Resize(size);
		public void reserve(int size) => data.Capacity = size;
		public void push_back(PathD paths) => data.Add(paths);
		public void clear() => data.Clear();
		public void Append(List<PathD> extra)
		{
			if (extra.Count > 0)
				data.AddRange(extra);
		}
		public RectD Bounds()
		{
			var bounds = new RectD(double.MaxValue, double.MaxValue, double.MinValue, double.MinValue);
			foreach (var path in data)
			{
				foreach (var point in path)
				{
					if (point.x < bounds.left) bounds.left = point.x;
					if (point.x > bounds.right) bounds.right = point.x;
					if (point.y < bounds.top) bounds.top = point.y;
					if (point.y > bounds.bottom) bounds.bottom = point.y;
				}
			}
			return (bounds.left >= bounds.right) ? new RectD() : bounds;
		}

        public void push_back(Path<PointD> paths)
        {
            throw new NotImplementedException();
        }

        public void Append(Paths<PointD> extra)
        {
            throw new NotImplementedException();
        }

        RectI Paths<PointD>.Bounds()
        {
            throw new NotImplementedException();
        }

        public void offset(PointD dx, PointD dy)
        {
            throw new NotImplementedException();
        }

        public void Reverse()
        {
            throw new NotImplementedException();
        }

        public void Rotate(Point<PointD> center, double angle_rad)
        {
            throw new NotImplementedException();
        }

        public void Scale(double scale_x, double scale_y)
        {
            throw new NotImplementedException();
        }

        public void StripDuplicates(bool is_closed_path, PointD min_length)
        {
            throw new NotImplementedException();
        }
    }

	//// PathsArray ------------------------------------------------------------------
	//public abstract class PathsArray<T>
	//{
 //       public List<Paths<T>> data;
	//	public Paths<T> this[int i] { get => data[i]; set => data[i] = value; }
	//	public int Size => data.Count;
	//	public void resize(int size) => data.Resize(size);
	//	public void reserve(int size) => data.Capacity = size;
	//	public void push_back(Path<T> paths) => data.Add(paths);
	//	public void clear() => data.Clear();
	//}

	//public class PathsIArray : PathsArray
	//{

	//}

	//public class PathsDArray : PathsArray
	//{

	//}


	//std::vector< data;

	
	//Paths<T> &operator[](Size idx) { return data[idx]; }
	//const Paths<T> &operator[](Size idx) const { return data[idx]; }

	//	Rect<T> Bounds() const{
	//		const T _MAX = std::numeric_limits<T>::max();
	//const T _MIN = std::numeric_limits < T >::lowest(); //-_MAX;

	//Rect<T> bounds(_MAX, _MAX, _MIN, _MIN);

	//for (const auto &paths : data) {
	//	for (const auto &path : paths.data) {
	//		for (const auto &point : path.data) {
	//			if (point.x < bounds.left) bounds.left = point.x;
	//			if (point.x > bounds.right) bounds.right = point.x;
	//			if (point.y < bounds.top) bounds.top = point.y;
	//			if (point.y > bounds.bottom) bounds.bottom = point.y;
	//		}
	//	}
	//}

	//if (bounds.left >= bounds.right)
	//	return Rect<T>();
	//else
	//	return bounds;
	//	}
	//};

	//using PathsArrayI = PathsArray<int64_t>;
	//using PathsArrayD = PathsArray<double>;


	////Rect function Rotate needs declaration of path first
	//template < typename T >
	// inline void Rect<T>::Rotate(double angle_rad)
	//{
	//	using UsedT = typename std::conditional<std::numeric_limits<T>::is_integer, double, T>::type;
	//	Point<UsedT> cp;
	//	cp.x = static_cast<UsedT>((right + left) / 2);
	//	cp.y = static_cast<UsedT>((bottom + top) / 2);

	//	Path<UsedT> pts;
	//	pts.resize(4);
	//	pts[0] = Point<UsedT>(static_cast<UsedT>(left), static_cast<UsedT>(top));
	//	pts[1] = Point<UsedT>(static_cast<UsedT>(right), static_cast<UsedT>(top));
	//	pts[2] = Point<UsedT>(static_cast<UsedT>(right), static_cast<UsedT>(bottom));
	//	pts[3] = Point<UsedT>(static_cast<UsedT>(left), static_cast<UsedT>(bottom));

	//	pts.Rotate(cp, angle_rad);

	//	const auto resultx = std::minmax_element(begin(pts.data), end(pts.data),[](Point < UsedT > p1, Point < UsedT > p2) { return p1.x < p2.x; });
	//	const auto resulty = std::minmax_element(begin(pts.data), end(pts.data),[](Point < UsedT > p1, Point < UsedT > p2) { return p1.y < p2.y; });

	//	if (std::numeric_limits < T >::is_integer)
	//	{
	//		left = static_cast<T>(std::floor(resultx.first->x));
	//		right = static_cast<T>(std::ceil(resultx.second->x));
	//		top = static_cast<T>(std::floor(resulty.first->y));
	//		bottom = static_cast<T>(std::ceil(resulty.second->y));
	//	}
	//	else
	//	{
	//		left = static_cast<T>(resultx.first->x);
	//		right = static_cast<T>(resultx.second->x);
	//		top = static_cast<T>(resulty.first->y);
	//		bottom = static_cast<T>(resulty.second->y);
	//	}
	//}

	public static class ClipperMath
	{
		public static long CrossProduct(PointI pt1, PointI pt2, PointI pt3) => (pt2.x - pt1.x) * (pt3.y - pt2.y) - (pt2.y - pt1.y) * (pt3.x - pt2.x);
		public static double CrossProduct(PointD pt1, PointD pt2, PointD pt3) => (pt2.x - pt1.x) * (pt3.y - pt2.y) - (pt2.y - pt1.y) * (pt3.x - pt2.x);
		public static double DistanceSqr(PointI pt1, PointI pt2) => Math.Pow(pt1.x - pt2.x, 2.0) + Math.Pow(pt1.y - pt2.y, 2.0);
		public static double DistanceSqr(PointD pt1, PointD pt2) => Math.Pow(pt1.x - pt2.x, 2.0) + Math.Pow(pt1.y - pt2.y, 2.0);

		public static double DistanceFromLineSqrd(PointD pt, PointD ln1, PointD ln2)
		{
			//perpendicular distance of point (x³,y³) = (Ax³ + By³ + C)/Sqrt(A² + B²)
			//see http://en.wikipedia.org/wiki/Perpendicular_distance
			double A = ln1.y - ln2.y;
			double B = ln2.x - ln1.x;
			double C = A * ln1.x + B * ln1.y;
			C = A * pt.x + B * pt.y - C;
			return (C * C) / (A * A + B * B);
		}

		public static double DistanceFromLineSqrd(PointI pt, PointI ln1, PointI ln2)
		{
			//perpendicular distance of point (x³,y³) = (Ax³ + By³ + C)/Sqrt(A² + B²)
			//see http://en.wikipedia.org/wiki/Perpendicular_distance
			double A = ln1.y - ln2.y;
			double B = ln2.x - ln1.x;
			double C = A * ln1.x + B * ln1.y;
			C = A * pt.x + B * pt.y - C;
			return (C * C) / (A * A + B * B);
		}

		public static bool NearCollinear(PointI pt1, PointI pt2, PointI pt3, double sin_sqrd_min_angle_rads)
		{
			double cp = Math.Abs(CrossProduct(pt1, pt2, pt3));
			return (cp * cp) / (DistanceSqr(pt1, pt2) * DistanceSqr(pt2, pt3)) < sin_sqrd_min_angle_rads;
		}

		public static bool NearCollinear(PointD pt1, PointD pt2, PointD pt3, double sin_sqrd_min_angle_rads)
		{
			double cp = Math.Abs(CrossProduct(pt1, pt2, pt3));
			return (cp * cp) / (DistanceSqr(pt1, pt2) * DistanceSqr(pt2, pt3)) < sin_sqrd_min_angle_rads;
		}

		//public static void CleanPathWithSinAngleRads(PathI path, bool is_closed, double min_length, double sin_min_angle_in_radians)
		//{
		//	if (path.size() < 2) return;
		//	//clean up insignificant edges
		//	double distSqrd = min_length * min_length;
		//	for (it = path.data.begin() + 1; it != path.data.end();)
		//	{
		//		if (NearEqual(it - 1, it, distSqrd))
		//			it = path.data.erase(it);
		//		else
		//			++it;
		//	}
		//	var len = path.size();
		//	if (is_closed && NearEqual(path[0], path[len - 1], distSqrd)) path.pop_back();

		//	if (path.size() < 3) return;
		//	double sin_sqrd_min_angle = Math.Sin(sin_min_angle_in_radians);
		//	sin_sqrd_min_angle *= sin_sqrd_min_angle;

		//	//clean up near colinear edges
		//	for (it = path.data.begin() + 2; it != path.data.end(); ++it)
		//		if (NearCollinear((it - 2), (it - 1), it, sin_sqrd_min_angle))
		//			it = path.data.erase(it - 1);

		//	len = path.size();
		//	if (len > 2 && is_closed &&
		//		NearCollinear(path[len - 2], path[len - 1], path[0], sin_sqrd_min_angle))
		//		path.pop_back();
		//}

		//public static void CleanPath(PathI path, bool is_closed, double min_length, double min_angle_in_radians)
		//{
		//	CleanPathWithSinAngleRads(path, is_closed, min_length, Math.Sin(min_angle_in_radians));
		//}

		//public static void CleanPaths(PathsI paths, bool is_closed, double min_length, double min_angle_in_radians)
		//{
		//	double sine = Math.Sin(min_angle_in_radians);
		//	foreach(var path in paths) 
		//		CleanPathWithSinAngleRads(path, is_closed, min_length, sine);
		//}
	}

	//PipResult PointInPolygon(PointI pt, PathI path);
}  

