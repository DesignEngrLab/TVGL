using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using TVGL;

namespace PolygonImportExport
{
    public static class SVG
    {
        private const string SvgNs = "http://www.w3.org/2000/svg";

        /// <param name="curvePrecision">Number of line segments used to approximate each curve entity (arc, circle, ellipse).</param>
        /// <param name="positiveYIsUp">When true (default), negates all Y values to convert from SVG's Y-down
        /// coordinate frame into a standard right-handed frame where positive Y points up.</param>
        public static List<Polygon> Open(string filePath, int curvePrecision = 30, bool positiveYIsUp = true)
        {
            var result = new List<Polygon>();
            var doc = XDocument.Load(filePath);
            XNamespace ns = SvgNs;

            // Collect all relevant elements anywhere in the document tree
            var allElements = doc.Descendants();

            // path elements — primary shape carrier in SVG
            foreach (var el in allElements.Where(e => e.Name.LocalName == "path"))
            {
                var d = (string)el.Attribute("d");
                if (string.IsNullOrWhiteSpace(d)) continue;
                var transform = ParseTransform(el);
                foreach (var poly in ParsePathData(d, transform, curvePrecision))
                    result.Add(poly);
            }

            // polygon / polyline elements
            foreach (var el in allElements.Where(e => e.Name.LocalName is "polygon" or "polyline"))
            {
                var points = ParsePointsList((string)el.Attribute("points"));
                if (points.Count < 2) continue;
                var transform = ParseTransform(el);
                points = ApplyTransform(points, transform);
                bool isClosed = el.Name.LocalName == "polygon";
                if (points.Count >= (isClosed ? 3 : 2))
                    result.Add(new Polygon(points, isClosed: isClosed));
            }

            // rect elements
            foreach (var el in allElements.Where(e => e.Name.LocalName == "rect"))
            {
                var pts = RectToPoints(el);
                if (pts == null) continue;
                var transform = ParseTransform(el);
                pts = ApplyTransform(pts, transform);
                result.Add(new Polygon(pts));
            }

            // circle elements
            foreach (var el in allElements.Where(e => e.Name.LocalName == "circle"))
            {
                if (!TryParseDouble((string)el.Attribute("cx"), out double cx) ||
                    !TryParseDouble((string)el.Attribute("cy"), out double cy) ||
                    !TryParseDouble((string)el.Attribute("r"), out double r) || r <= 0) continue;
                var pts = GenerateCirclePoints(cx, cy, r, curvePrecision);
                var transform = ParseTransform(el);
                pts = ApplyTransform(pts, transform);
                result.Add(new Polygon(pts));
            }

            // ellipse elements
            foreach (var el in allElements.Where(e => e.Name.LocalName == "ellipse"))
            {
                if (!TryParseDouble((string)(el.Attribute("cx") ?? el.Attribute("cx")), out double cx))
                    cx = 0;
                if (!TryParseDouble((string)(el.Attribute("cy") ?? el.Attribute("cy")), out double cy))
                    cy = 0;
                if (!TryParseDouble((string)el.Attribute("rx"), out double rx) ||
                    !TryParseDouble((string)el.Attribute("ry"), out double ry) ||
                    rx <= 0 || ry <= 0) continue;
                var pts = GenerateEllipsePoints(cx, cy, rx, ry, curvePrecision);
                var transform = ParseTransform(el);
                pts = ApplyTransform(pts, transform);
                result.Add(new Polygon(pts));
            }

            if (positiveYIsUp)
            {
                for (int idx = 0; idx < result.Count; idx++)
                {
                    var p = result[idx];
                    var flipped = p.Path.Select(v => new Vector2(v.X, -v.Y)).ToList();
                    result[idx] = new Polygon(flipped.AsEnumerable().Reverse(), isClosed: p.IsClosed);
                }
            }

            return result.CreateShallowPolygonTrees(true);
        }

        /// <param name="positiveYIsUp">When true (default), negates all Y values to convert from a standard
        /// right-handed frame (positive Y up) into SVG's Y-down coordinate frame.</param>
        public static bool Save(string filePath, IEnumerable<Polygon> polygons, bool positiveYIsUp = true)
        {
            try
            {
                double ySign = positiveYIsUp ? -1.0 : 1.0;
                var allPolys = polygons.SelectMany(p => p.AllPolygons).ToList();

                // Compute viewBox from bounding box of all vertices (in SVG-space Y)
                var allPts = allPolys.SelectMany(p => p.Path).ToList();
                double minX = allPts.Min(p => p.X);
                double minY = allPts.Min(p => ySign * p.Y);
                double maxX = allPts.Max(p => p.X);
                double maxY = allPts.Max(p => ySign * p.Y);
                double w = maxX - minX;
                double h = maxY - minY;

                var svgEl = new XElement(XName.Get("svg", SvgNs),
                    new XAttribute("xmlns", SvgNs),
                    new XAttribute("width", F(w)),
                    new XAttribute("height", F(h)),
                    new XAttribute("viewBox", $"{F(minX)} {F(minY)} {F(w)} {F(h)}"));

                foreach (var poly in allPolys)
                {
                    if (positiveYIsUp) poly.Path.Reverse();
                    var d = BuildPathData(poly.Path, poly.IsClosed, ySign);
                    svgEl.Add(new XElement(XName.Get("path", SvgNs),
                        new XAttribute("d", d),
                        new XAttribute("fill", poly.IsClosed ? "none" : "none"),
                        new XAttribute("stroke", "black")));
                }

                var xdoc = new XDocument(new XDeclaration("1.0", "utf-8", null), svgEl);
                xdoc.Save(filePath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ── Save helpers ─────────────────────────────────────────────────────────

        private static string BuildPathData(IEnumerable<Vector2> pts, bool closed, double ySign)
        {
            var sb = new StringBuilder();
            bool first = true;
            foreach (var p in pts)
            {
                sb.Append(first ? 'M' : 'L');
                sb.Append(F(p.X));
                sb.Append(',');
                sb.Append(F(ySign * p.Y));
                sb.Append(' ');
                first = false;
            }
            if (closed) sb.Append('Z');
            return sb.ToString().TrimEnd();
        }

        private static string F(double v) =>
            v.ToString("G6", CultureInfo.InvariantCulture);

        // ── Open helpers ─────────────────────────────────────────────────────────

        private static List<Polygon> ParsePathData(string d, double[,] transform, int precision)
        {
            var result = new List<Polygon>();
            var current = new List<Vector2>();
            double cx = 0, cy = 0; // current pen position
            double mx = 0, my = 0; // last move-to
            // Track previous bezier control points for the smooth (S/s/T/t) variants.
            // Per SVG spec: the implied first control of a smooth bezier is the reflection
            // of the previous bezier's last control through the current point — but only if
            // the previous command was the matching bezier family; otherwise use current point.
            double prevC2x = 0, prevC2y = 0;
            double prevQ1x = 0, prevQ1y = 0;
            bool hasPrevCubicCtrl = false;
            bool hasPrevQuadCtrl = false;

            var tokens = TokenizePathData(d);
            int i = 0;
            char cmd = 'M';
            while (i < tokens.Count)
            {
                var t = tokens[i];
                if (t.Length == 1 && char.IsLetter(t[0]))
                {
                    cmd = t[0];
                    i++;
                    if (cmd != 'z' && cmd != 'Z')
                        continue;
                }

                switch (cmd)
                {
                    case 'M':
                        {
                            if (current.Count >= 2) result.Add(new Polygon(ApplyTransform(current, transform), isClosed: false));
                            current = new List<Vector2>();
                            if (!TryParseCoordPair(tokens, ref i, out double x, out double y)) break;
                            cx = mx = x; cy = my = y;
                            current.Add(new Vector2(cx, cy));
                            cmd = 'L'; // implicit lineto after first M
                            break;
                        }
                    case 'm':
                        {
                            if (current.Count >= 2) result.Add(new Polygon(ApplyTransform(current, transform), isClosed: false));
                            current = new List<Vector2>();
                            if (!TryParseCoordPair(tokens, ref i, out double dx, out double dy)) break;
                            cx = mx = cx + dx; cy = my = cy + dy;
                            current.Add(new Vector2(cx, cy));
                            cmd = 'l';
                            break;
                        }
                    case 'L':
                        {
                            if (!TryParseCoordPair(tokens, ref i, out double x, out double y)) break;
                            cx = x; cy = y;
                            current.Add(new Vector2(cx, cy));
                            break;
                        }
                    case 'l':
                        {
                            if (!TryParseCoordPair(tokens, ref i, out double dx, out double dy)) break;
                            cx += dx; cy += dy;
                            current.Add(new Vector2(cx, cy));
                            break;
                        }
                    case 'H':
                        {
                            if (!TryParseDouble(tokens[i++], out double x)) break;
                            cx = x;
                            current.Add(new Vector2(cx, cy));
                            break;
                        }
                    case 'h':
                        {
                            if (!TryParseDouble(tokens[i++], out double dx)) break;
                            cx += dx;
                            current.Add(new Vector2(cx, cy));
                            break;
                        }
                    case 'V':
                        {
                            if (!TryParseDouble(tokens[i++], out double y)) break;
                            cy = y;
                            current.Add(new Vector2(cx, cy));
                            break;
                        }
                    case 'v':
                        {
                            if (!TryParseDouble(tokens[i++], out double dy)) break;
                            cy += dy;
                            current.Add(new Vector2(cx, cy));
                            break;
                        }
                    case 'A':
                    case 'a':
                        {
                            // Arc: rx ry x-rotation large-arc-flag sweep-flag x y
                            if (i + 6 >= tokens.Count) { i = tokens.Count; break; }
                            if (!TryParseDouble(tokens[i++], out double rx)) break;
                            if (!TryParseDouble(tokens[i++], out double ry)) break;
                            if (!TryParseDouble(tokens[i++], out double xrot)) break;
                            if (!TryParseDouble(tokens[i++], out double largeArcFlag)) break;
                            if (!TryParseDouble(tokens[i++], out double sweepFlag)) break;
                            if (!TryParseCoordPair(tokens, ref i, out double ex, out double ey)) break;
                            if (cmd == 'a') { ex += cx; ey += cy; }
                            var arcPts = TessellateArc(cx, cy, rx, ry, xrot * Math.PI / 180.0,
                                largeArcFlag != 0, sweepFlag != 0, ex, ey, precision);
                            current.AddRange(arcPts);
                            cx = ex; cy = ey;
                            break;
                        }
                    case 'C':
                    case 'c':
                        {
                            // Cubic Bezier: x1 y1 x2 y2 x y
                            if (i + 5 >= tokens.Count) { i = tokens.Count; break; }
                            if (!TryParseCoordPair(tokens, ref i, out double x1, out double y1)) break;
                            if (!TryParseCoordPair(tokens, ref i, out double x2, out double y2)) break;
                            if (!TryParseCoordPair(tokens, ref i, out double ex, out double ey)) break;
                            if (cmd == 'c') { x1 += cx; y1 += cy; x2 += cx; y2 += cy; ex += cx; ey += cy; }
                            var bezPts = TessellateCubicBezier(cx, cy, x1, y1, x2, y2, ex, ey, precision);
                            current.AddRange(bezPts);
                            cx = ex; cy = ey;
                            prevC2x = x2; prevC2y = y2;
                            break;
                        }
                    case 'S':
                    case 's':
                        {
                            // Smooth cubic Bezier: x2 y2 x y — first control mirrored from prev cubic's x2/y2.
                            if (i + 3 >= tokens.Count) { i = tokens.Count; break; }
                            if (!TryParseCoordPair(tokens, ref i, out double x2, out double y2)) break;
                            if (!TryParseCoordPair(tokens, ref i, out double ex, out double ey)) break;
                            if (cmd == 's') { x2 += cx; y2 += cy; ex += cx; ey += cy; }
                            double x1 = hasPrevCubicCtrl ? 2 * cx - prevC2x : cx;
                            double y1 = hasPrevCubicCtrl ? 2 * cy - prevC2y : cy;
                            var bezPts = TessellateCubicBezier(cx, cy, x1, y1, x2, y2, ex, ey, precision);
                            current.AddRange(bezPts);
                            cx = ex; cy = ey;
                            prevC2x = x2; prevC2y = y2;
                            break;
                        }
                    case 'Q':
                    case 'q':
                        {
                            // Quadratic Bezier: x1 y1 x y
                            if (i + 3 >= tokens.Count) { i = tokens.Count; break; }
                            if (!TryParseCoordPair(tokens, ref i, out double x1, out double y1)) break;
                            if (!TryParseCoordPair(tokens, ref i, out double ex, out double ey)) break;
                            if (cmd == 'q') { x1 += cx; y1 += cy; ex += cx; ey += cy; }
                            var qPts = TessellateQuadBezier(cx, cy, x1, y1, ex, ey, precision);
                            current.AddRange(qPts);
                            cx = ex; cy = ey;
                            prevQ1x = x1; prevQ1y = y1;
                            break;
                        }
                    case 'T':
                    case 't':
                        {
                            // Smooth quadratic Bezier: x y — control mirrored from prev quad's x1/y1.
                            if (i + 1 >= tokens.Count) { i = tokens.Count; break; }
                            if (!TryParseCoordPair(tokens, ref i, out double ex, out double ey)) break;
                            if (cmd == 't') { ex += cx; ey += cy; }
                            double x1 = hasPrevQuadCtrl ? 2 * cx - prevQ1x : cx;
                            double y1 = hasPrevQuadCtrl ? 2 * cy - prevQ1y : cy;
                            var qPts = TessellateQuadBezier(cx, cy, x1, y1, ex, ey, precision);
                            current.AddRange(qPts);
                            cx = ex; cy = ey;
                            prevQ1x = x1; prevQ1y = y1;
                            break;
                        }
                    case 'Z':
                    case 'z':
                        {
                            if (current.Count >= 3) result.Add(new Polygon(ApplyTransform(current, transform), isClosed: true));
                            current = new List<Vector2>();
                            cx = mx; cy = my;
                            break;
                        }
                    default:
                        i++;
                        break;
                }

                // Smooth-bezier reflection is only valid when the previous command was the
                // matching family (C/c/S/s for cubic, Q/q/T/t for quad).
                hasPrevCubicCtrl = cmd is 'C' or 'c' or 'S' or 's';
                hasPrevQuadCtrl = cmd is 'Q' or 'q' or 'T' or 't';
            }
            if (current.Count >= 2)
                result.Add(new Polygon(ApplyTransform(current, transform), isClosed: false));
            return result;
        }

        private static List<string> TokenizePathData(string d)
        {
            var tokens = new List<string>();
            var sb = new StringBuilder();
            var sawDecimal = false;
            foreach (char c in d)
            {
                if (char.IsLetter(c))
                {
                    if (sb.Length > 0)
                    {
                        tokens.Add(sb.ToString()); sb.Clear();
                        sawDecimal = false;
                    }
                    tokens.Add(c.ToString());
                }
                else if (c == ',' || c == ' ' || c == '\t' || c == '\r' || c == '\n')
                {
                    if (sb.Length > 0)
                    {
                        tokens.Add(sb.ToString()); sb.Clear();
                        sawDecimal = false;
                    }
                }
                else if ((c == '-' || c == '+') && sb.Length > 0 && sb[^1] != 'e' && sb[^1] != 'E')
                {

                    tokens.Add(sb.ToString()); sb.Clear(); sawDecimal = false;
                    sb.Append(c);
                }
                else if (c == '.' && sawDecimal)
                {
                    // SVG compact notation: "13.04.4" means "13.04" then ".4"
                    tokens.Add(sb.ToString()); sb.Clear();
                    sb.Append(c); // sawDecimal stays true — new number starts with '.'
                }
                else
                {
                    if (c == '.') sawDecimal = true;
                    sb.Append(c);
                }
            }

            if (sb.Length > 0) tokens.Add(sb.ToString());
            return tokens;
        }

        private static bool TryParseCoordPair(List<string> tokens, ref int i,
            out double x, out double y)
        {
            x = y = 0;
            if (i + 1 >= tokens.Count) return false;
            if (!TryParseDouble(tokens[i++], out x)) return false;
            if (!TryParseDouble(tokens[i++], out y)) return false;
            return true;
        }

        private static bool TryParseDouble(string s, out double v) =>
            double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v);

        private static List<Vector2> ParsePointsList(string points)
        {
            var result = new List<Vector2>();
            if (string.IsNullOrWhiteSpace(points)) return result;
            var nums = points.Split(new[] { ' ', ',', '\t', '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i + 1 < nums.Length; i += 2)
            {
                if (TryParseDouble(nums[i], out double x) && TryParseDouble(nums[i + 1], out double y))
                    result.Add(new Vector2(x, y));
            }
            return result;
        }

        private static List<Vector2> RectToPoints(XElement el)
        {
            if (!TryParseDouble((string)el.Attribute("x") ?? "0", out double x)) x = 0;
            if (!TryParseDouble((string)el.Attribute("y") ?? "0", out double y)) y = 0;
            if (!TryParseDouble((string)el.Attribute("width"), out double w) || w <= 0) return null;
            if (!TryParseDouble((string)el.Attribute("height"), out double h) || h <= 0) return null;
            return [new Vector2(x, y), new Vector2(x + w, y), new Vector2(x + w, y + h), new Vector2(x, y + h)];
        }

        private static List<Vector2> GenerateCirclePoints(double cx, double cy, double r, int n)
        {
            var pts = new List<Vector2>(n);
            for (int i = 0; i < n; i++)
            {
                double a = 2 * Math.PI * i / n;
                pts.Add(new Vector2(cx + r * Math.Cos(a), cy + r * Math.Sin(a)));
            }
            return pts;
        }

        private static List<Vector2> GenerateEllipsePoints(double cx, double cy,
            double rx, double ry, int n)
        {
            var pts = new List<Vector2>(n);
            for (int i = 0; i < n; i++)
            {
                double a = 2 * Math.PI * i / n;
                pts.Add(new Vector2(cx + rx * Math.Cos(a), cy + ry * Math.Sin(a)));
            }
            return pts;
        }

        // ── Curve tessellation ────────────────────────────────────────────────────

        private static List<Vector2> TessellateCubicBezier(
            double x0, double y0, double x1, double y1,
            double x2, double y2, double x3, double y3, int n)
        {
            var pts = new List<Vector2>(n);
            for (int i = 1; i <= n; i++)
            {
                double t = (double)i / n;
                double u = 1 - t;
                double x = u * u * u * x0 + 3 * u * u * t * x1 + 3 * u * t * t * x2 + t * t * t * x3;
                double y = u * u * u * y0 + 3 * u * u * t * y1 + 3 * u * t * t * y2 + t * t * t * y3;
                pts.Add(new Vector2(x, y));
            }
            return pts;
        }

        private static List<Vector2> TessellateQuadBezier(
            double x0, double y0, double x1, double y1,
            double x2, double y2, int n)
        {
            var pts = new List<Vector2>(n);
            for (int i = 1; i <= n; i++)
            {
                double t = (double)i / n;
                double u = 1 - t;
                double x = u * u * x0 + 2 * u * t * x1 + t * t * x2;
                double y = u * u * y0 + 2 * u * t * y1 + t * t * y2;
                pts.Add(new Vector2(x, y));
            }
            return pts;
        }

        // SVG arc-to-endpoint parameterisation → centre parameterisation → tessellate
        private static List<Vector2> TessellateArc(
            double x1, double y1, double rx, double ry,
            double xRotRad, bool largeArc, bool sweep,
            double x2, double y2, int n)
        {
            var pts = new List<Vector2>();
            if (rx == 0 || ry == 0)
            {
                pts.Add(new Vector2(x2, y2));
                return pts;
            }

            // Endpoint-to-centre conversion (SVG spec §B.2.4)
            double cosA = Math.Cos(xRotRad), sinA = Math.Sin(xRotRad);
            double dx = (x1 - x2) / 2, dy = (y1 - y2) / 2;
            double x1p = cosA * dx + sinA * dy;
            double y1p = -sinA * dx + cosA * dy;

            rx = Math.Abs(rx); ry = Math.Abs(ry);
            double x1pSq = x1p * x1p, y1pSq = y1p * y1p;
            double rxSq = rx * rx, rySq = ry * ry;
            double lambda = x1pSq / rxSq + y1pSq / rySq;
            if (lambda > 1) { double sq = Math.Sqrt(lambda); rx *= sq; ry *= sq; rxSq = rx * rx; rySq = ry * ry; }

            double num = Math.Max(0, rxSq * rySq - rxSq * y1pSq - rySq * x1pSq);
            double den = rxSq * y1pSq + rySq * x1pSq;
            double sq2 = den == 0 ? 0 : Math.Sqrt(num / den);
            if (largeArc == sweep) sq2 = -sq2;

            double cxp = sq2 * rx * y1p / ry;
            double cyp = -sq2 * ry * x1p / rx;
            double cx = cosA * cxp - sinA * cyp + (x1 + x2) / 2;
            double cy = sinA * cxp + cosA * cyp + (y1 + y2) / 2;

            double startAngle = Angle(1, 0, (x1p - cxp) / rx, (y1p - cyp) / ry);
            double dAngle = Angle((x1p - cxp) / rx, (y1p - cyp) / ry,
                                  (-x1p - cxp) / rx, (-y1p - cyp) / ry);
            if (!sweep && dAngle > 0) dAngle -= 2 * Math.PI;
            else if (sweep && dAngle < 0) dAngle += 2 * Math.PI;

            for (int i = 1; i <= n; i++)
            {
                double t = startAngle + dAngle * i / n;
                double px = cosA * rx * Math.Cos(t) - sinA * ry * Math.Sin(t) + cx;
                double py = sinA * rx * Math.Cos(t) + cosA * ry * Math.Sin(t) + cy;
                pts.Add(new Vector2(px, py));
            }
            return pts;
        }

        private static double Angle(double ux, double uy, double vx, double vy)
        {
            double dot = ux * vx + uy * vy;
            double len = Math.Sqrt((ux * ux + uy * uy) * (vx * vx + vy * vy));
            double angle = Math.Acos(Math.Clamp(dot / len, -1, 1));
            if (ux * vy - uy * vx < 0) angle = -angle;
            return angle;
        }

        // ── Transform helpers ─────────────────────────────────────────────────────

        // Returns a 3×3 affine matrix stored as [row,col] (row-major, homogeneous).
        // Identity when no transform attribute is present.
        private static double[,] ParseTransform(XElement el)
        {
            double[,] m = Identity();
            // Walk up ancestors accumulating transforms (outermost applied last = post-multiply)
            var chain = new List<double[,]>();
            for (var node = el; node != null; node = node.Parent)
            {
                var attr = (string)node.Attribute("transform");
                if (!string.IsNullOrWhiteSpace(attr))
                    chain.Add(ParseSingleTransform(attr));
            }
            // Apply in document order (parent first)
            chain.Reverse();
            foreach (var t in chain) m = Multiply(m, t);
            return m;
        }

        private static double[,] ParseSingleTransform(string attr)
        {
            double[,] result = Identity();
            // multiple transforms can be chained: e.g. "translate(10,5) rotate(45)"
            var parts = System.Text.RegularExpressions.Regex.Matches(attr, @"(\w+)\(([^)]*)\)");
            foreach (System.Text.RegularExpressions.Match part in parts)
            {
                string name = part.Groups[1].Value;
                var args = part.Groups[2].Value
                    .Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => { TryParseDouble(s, out double v); return v; })
                    .ToArray();
                double[,] t = name switch
                {
                    "translate" => Translate(args.Length > 0 ? args[0] : 0,
                                             args.Length > 1 ? args[1] : 0),
                    "scale" => Scale(args.Length > 0 ? args[0] : 1,
                                     args.Length > 1 ? args[1] : args[0]),
                    "rotate" => args.Length == 3
                        ? Multiply(Translate(args[1], args[2]),
                            Multiply(Rotate(args[0] * Math.PI / 180.0), Translate(-args[1], -args[2])))
                        : Rotate(args[0] * Math.PI / 180.0),
                    "matrix" when args.Length == 6 => new double[,]
                        { { args[0], args[2], args[4] },
                          { args[1], args[3], args[5] },
                          { 0,       0,       1       } },
                    "skewX" => SkewX(args[0] * Math.PI / 180.0),
                    "skewY" => SkewY(args[0] * Math.PI / 180.0),
                    _ => Identity()
                };
                result = Multiply(result, t);
            }
            return result;
        }

        private static List<Vector2> ApplyTransform(List<Vector2> pts, double[,] m)
        {
            if (IsIdentity(m)) return pts;
            return pts.Select(p =>
                new Vector2(m[0, 0] * p.X + m[0, 1] * p.Y + m[0, 2],
                            m[1, 0] * p.X + m[1, 1] * p.Y + m[1, 2])).ToList();
        }

        private static double[,] Identity() =>
            new double[,] { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } };

        private static bool IsIdentity(double[,] m) =>
            m[0, 0] == 1 && m[0, 1] == 0 && m[0, 2] == 0 &&
            m[1, 0] == 0 && m[1, 1] == 1 && m[1, 2] == 0;

        private static double[,] Multiply(double[,] a, double[,] b) => new double[,]
        {
            { a[0,0]*b[0,0]+a[0,1]*b[1,0]+a[0,2]*b[2,0],
              a[0,0]*b[0,1]+a[0,1]*b[1,1]+a[0,2]*b[2,1],
              a[0,0]*b[0,2]+a[0,1]*b[1,2]+a[0,2]*b[2,2] },
            { a[1,0]*b[0,0]+a[1,1]*b[1,0]+a[1,2]*b[2,0],
              a[1,0]*b[0,1]+a[1,1]*b[1,1]+a[1,2]*b[2,1],
              a[1,0]*b[0,2]+a[1,1]*b[1,2]+a[1,2]*b[2,2] },
            { 0, 0, 1 }
        };

        private static double[,] Translate(double tx, double ty) =>
            new double[,] { { 1, 0, tx }, { 0, 1, ty }, { 0, 0, 1 } };

        private static double[,] Scale(double sx, double sy) =>
            new double[,] { { sx, 0, 0 }, { 0, sy, 0 }, { 0, 0, 1 } };

        private static double[,] Rotate(double rad) =>
            new double[,]
            { { Math.Cos(rad), -Math.Sin(rad), 0 },
              { Math.Sin(rad),  Math.Cos(rad), 0 },
              { 0,              0,             1 } };

        private static double[,] SkewX(double rad) =>
            new double[,] { { 1, Math.Tan(rad), 0 }, { 0, 1, 0 }, { 0, 0, 1 } };

        private static double[,] SkewY(double rad) =>
            new double[,] { { 1, 0, 0 }, { Math.Tan(rad), 1, 0 }, { 0, 0, 1 } };
    }
}
