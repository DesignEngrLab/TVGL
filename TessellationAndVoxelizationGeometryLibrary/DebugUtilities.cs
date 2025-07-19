using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    public static class DebugUtilities
    {
        /// <summary>
        /// Shows and colors the machining primitives of a solid or the given machining primitives.
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="resetColor"></param>
        /// <param name="showBorders"></param>
        /// <param name="randomColors"></param>
        /// <param name="primitives"></param>
        /// <param name="lineThickness"></param>
        /// <param name="color"></param>
        public static void ShowSurfaceGroups(this TessellatedSolid ts, IEnumerable<SurfaceGroup> features,
            bool showBorders = true, double lineThickness = 1, Color color = null)
        {
            if (showBorders)
            {
                var lines = new List<IEnumerable<Vector3>>();
                foreach (var feature in features)
                    foreach (var border in feature.Borders)
                        lines.Add(border.GetCoordinates());
                color = color ?? white;
                Global.Presenter3D.ShowAndHang([lines], [false], [lineThickness], [color], ts);
            }
            else
                Global.Presenter3D.ShowAndHang(ts);
        }

        /// <summary>
        /// Shows and colors the primitives of a solid or the given primitives. For each primitive, 
        /// it sets the colors to match the primitive types or sets random colors. If showBorders = true, 
        /// it will show the border loops of the given primitives.
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="resetColor"></param>
        /// <param name="showBorders"></param>
        /// <param name="randomColors"></param>
        /// <param name="primitives"></param>
        /// <param name="lineThickness"></param>
        /// <param name="color"></param>
        public static void ShowPrimitives(this TessellatedSolid ts, bool showBorders = true,
            bool randomColors = false, IEnumerable<PrimitiveSurface> primitives = null, double lineThickness = 1,
            Color color = null)
        {
            PaintSurfaces(ts, primitives, randomColors);
            if (showBorders)
                ts.ShowWireFrame(false, null, primitives, lineThickness, color);
            else
                Global.Presenter3D.ShowAndHang(ts);
        }

        /// <summary>
        /// Shows the solid and borders. If primitives are given, it will only show the borders for those.
        /// This function can be used instead of ShowPrimitives if you don't want to change the color
        /// of the primitives OR you want the solid to be the default color.
        /// </summary>
        private static readonly Color white = new Color(KnownColors.White);
        public static void ShowWireFrame(this TessellatedSolid ts, bool resetColor,
            IEnumerable<BorderLoop> borders = null, IEnumerable<PrimitiveSurface> primitives = null,
            double lineThickness = 1, Color color = null)
        {
            if (resetColor)
            {
                ts.HasUniformColor = false;
                ts.ResetDefaultColor();
            }
            var lines = ts.GetWireFrame(borders, primitives);
            var colors = color == null ? new Color[] { white } : new Color[] { color };
            Global.Presenter3D.ShowAndHang([lines], [false], [lineThickness], colors, ts);
        }

        public static List<IEnumerable<Vector3>> GetWireFrame(this TessellatedSolid ts, IEnumerable<BorderLoop> borders = null,
            IEnumerable<PrimitiveSurface> primitives = null)
        {
            var lines = new List<IEnumerable<Vector3>>();
            if (primitives == null) primitives = ts.Primitives;
            if (primitives == null || !primitives.Any()) return lines;
            //Use borders if they have been set. Otherwise, use the outer edges.
            var bordersHaveBeenSet = primitives.Any(p => p.Borders != null);
            if (borders != null)
                foreach (var border in borders)
                    lines.Add(border.GetCoordinates());
            else if (bordersHaveBeenSet)
                foreach (var prim in primitives)
                    foreach (var border in prim.Borders)
                        lines.Add(border.GetCoordinates());
            else
                foreach (var prim in primitives)
                    foreach (var edge in prim.OuterEdges)
                        lines.Add(new Vector3[] { edge.From.Coordinates, edge.To.Coordinates });
            return lines;
        }

        public static void PaintSurfaces(this TessellatedSolid ts, IEnumerable<PrimitiveSurface> primitives = null, bool randomColors = false)
        {
            ts.HasUniformColor = false;
            ts.ResetDefaultColor();
            if (primitives == null) primitives = ts.Primitives;
            if (primitives == null || !primitives.Any()) return;

            if (randomColors)
            {
                //Iterating through the color palette does a better job assigning different colors than the TVGL Color Enumerator.
                var colors = Color.GetRandomColors().GetEnumerator();
                foreach (var primitiveSurface in primitives)
                {
                    colors.MoveNext();
                    primitiveSurface.SetColor(colors.Current);
                }
            }
            else
            {
                foreach (var primitiveSurface in primitives)
                {
                    if (primitiveSurface is Cylinder)
                        foreach (var f in primitiveSurface.Faces)
                            f.Color = new Color(KnownColors.Red);

                    if (primitiveSurface is Cone)
                        foreach (var f in primitiveSurface.Faces)
                            f.Color = new Color(KnownColors.DarkOrange);
                    else if (primitiveSurface is Sphere)
                        foreach (var f in primitiveSurface.Faces)
                            f.Color = new Color(KnownColors.Yellow);
                    else if (primitiveSurface is Plane)
                        foreach (var f in primitiveSurface.Faces)
                            f.Color = new Color(KnownColors.Green);
                    else if (primitiveSurface is Torus)
                        foreach (var f in primitiveSurface.Faces)
                            f.Color = new Color(KnownColors.HotPink);
                    else if (primitiveSurface is Prismatic)
                        foreach (var f in primitiveSurface.Faces)
                            f.Color = new Color(KnownColors.Purple);
                    else if (primitiveSurface is UnknownRegion)
                        foreach (var f in primitiveSurface.Faces)
                            f.Color = new Color(KnownColors.Brown);
                    else
                        foreach (var f in primitiveSurface.Faces)
                            f.Color = new Color(KnownColors.Blue);
                }
            }
        }

        public static void ShowPrimitiveWithIllustrativeParameters(this TessellatedSolid solid, PrimitiveSurface bestPrimitiveSurface)
        {
            solid.PaintSurfaces(new List<PrimitiveSurface> { bestPrimitiveSurface });
            if (bestPrimitiveSurface is Cylinder cylinder)
            {
                var heights = MinimumEnclosure.GetDistanceToExtremeVertex(cylinder.Vertices, cylinder.Axis, out _, out _);
                var centerTValue = (heights.Item1 + heights.Item2) / 2;
                var halfheight = 1.25 * Math.Abs((heights.Item1 - heights.Item2) / 2);
                var anchorTValue = cylinder.Anchor.Dot(cylinder.Axis);

                var height = MinimumEnclosure.GetLengthAndExtremeVertex(cylinder.Vertices, cylinder.Axis, out _, out _);
                var bottom = cylinder.Anchor + cylinder.Axis * ((centerTValue - anchorTValue) - halfheight);
                var top = cylinder.Anchor + cylinder.Axis * ((centerTValue - anchorTValue) + halfheight);
                Global.Presenter3D.ShowAndHang(new List<Vector3> { bottom, top }, false, solids: solid);
            }
            else if (bestPrimitiveSurface is Cone cone)
            {
                var (minheight, maxheight) = MinimumEnclosure.GetDistanceToExtremeVertex(cone.Vertices, cone.Axis, out _, out _);
                var distanceToApex = cone.Apex.Dot(cone.Axis);
                double height;
                Vector3 bottom;
                if (distanceToApex > maxheight)
                {
                    height = distanceToApex - minheight;
                    bottom = cone.Apex - height * cone.Axis;
                }
                else
                {
                    height = maxheight - distanceToApex;
                    bottom = cone.Apex + height * cone.Axis;
                }
                var toEdge = height * cone.Aperture * cone.Axis.GetPerpendicularDirection();
                var edgePoint = bottom + toEdge;

                //Get the circle at the base of the cone.
                var d = bottom.Dot(cone.Axis);
                var transfrom = cone.Axis.TransformToXYPlane(out var backTransform);
                var conePlane = bottom.ConvertTo2DCoordinates(cone.Axis, out var backTransform2);
                var pointsOnPlane = cone.Vertices.ProjectTo2DCoordinates(transfrom).ToArray();
                var circle = MinimumEnclosure.MinimumCircle(pointsOnPlane);
                var circlePoints = circle.CreatePath(36);
                var circle3D = circlePoints.ConvertTo3DLocations(cone.Axis, d).ToList();
                var centerLine = new List<Vector3> { cone.Apex, bottom, edgePoint };
                Global.Presenter3D.ShowAndHang(new List<Vector3> { cone.Apex, bottom, edgePoint }, false, solids: solid);
            }
            else if (bestPrimitiveSurface is Torus torus)
            {
                var d1 = torus.Axis.GetPerpendicularDirection();
                var d2 = (d1.Cross(torus.Axis)).Normalize();
                var torusPoints = new List<Vector3>
                    {
                        torus.Center,
                        torus.Center+d1*(torus.MajorRadius-torus.MinorRadius),
                        torus.Center+d2*(torus.MajorRadius-torus.MinorRadius),
                        torus.Center-d1*(torus.MajorRadius-torus.MinorRadius),
                        torus.Center-d2*(torus.MajorRadius-torus.MinorRadius),
                        torus.Center+d1*(torus.MajorRadius-torus.MinorRadius),
                        torus.Center+d1*(torus.MajorRadius+torus.MinorRadius),
                        torus.Center+d2*(torus.MajorRadius+torus.MinorRadius),
                        torus.Center-d1*(torus.MajorRadius+torus.MinorRadius),
                        torus.Center-d2*(torus.MajorRadius+torus.MinorRadius),
                        torus.Center+d1*(torus.MajorRadius+torus.MinorRadius)
                    };
                Global.Presenter3D.ShowAndHang(torusPoints, false, solids: solid);
            }
            else if (bestPrimitiveSurface is Sphere)
            {
                var sphere = (Sphere)bestPrimitiveSurface;
                var spherePoints = new List<Vector3> {
                                sphere.Center + sphere.Radius * Vector3.UnitX, sphere.Center - sphere.Radius * Vector3.UnitX, sphere.Center,
                                sphere.Center + sphere.Radius * Vector3.UnitY, sphere.Center - sphere.Radius * Vector3.UnitY, sphere.Center,
                                sphere.Center + sphere.Radius * Vector3.UnitZ, sphere.Center - sphere.Radius * Vector3.UnitZ};
                Global.Presenter3D.ShowAndHang(spherePoints, false, solids: solid);

            }
            //For all other surface types, just show the colored primitive
            else
            {
                Global.Presenter3D.ShowAndHang(solid);
            }
        }

        /// <summary>
        /// Get the bitmap of a region of the grid. Start coordinates must be lower than stop coordinates.
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="startX"></param>
        /// <param name="startY"></param>
        /// <param name="stopX"></param>
        /// <param name="stopY"></param>
        /// <param name="black"></param>
        /// <param name=""></param>
        public static void SaveBitmap(ZBuffer grid, HashSet<int> blackPixels = null)
        {
            var ZRange = grid.VertexZHeights.Max() - grid.VertexZHeights.Min();
            var bitmap = new double[grid.XCount, grid.YCount];
            for (var xIndex = 0; xIndex < grid.XCount; xIndex++)
            {
                for (var yIndex = 0; yIndex < grid.YCount; yIndex++)
                {
                    var index = grid.GetIndex(xIndex, yIndex);
                    var z = grid.Values[index].Item2;
                    if (blackPixels != null && blackPixels.Contains(index))
                    {
                        bitmap[xIndex, yIndex] = 0.0;
                    }
                    else
                    {
                        bitmap[xIndex, yIndex] = z * 360 / ZRange;
                    }
                }
            }
            Global.Presenter2D.ShowAndHang(bitmap, "ZBuffer");
        }

        /// <summary>
        /// https://stackoverflow.com/questions/1335426/is-there-a-built-in-c-net-system-api-for-hsv-to-rgb
        /// The ranges are 0 - 360 for hue, and 0 - 1 for saturation or value.
        /// </summary>
        /// <param name="hue"></param>
        /// <param name="saturation"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Color ColorFromHSV(double hue, double saturation = 1, double value = 1)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            if (hi == 0)
                return new Color(255, v, t, p);
            else if (hi == 1)
                return new Color(255, q, v, p);
            else if (hi == 2)
                return new Color(255, p, v, t);
            else if (hi == 3)
                return new Color(255, p, q, v);
            else if (hi == 4)
                return new Color(255, t, p, v);
            else
                return new Color(255, v, p, q);
        }
    }
}
