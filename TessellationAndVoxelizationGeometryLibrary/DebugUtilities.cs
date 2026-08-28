using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    public static class DebugUtilities
    {
        /// <summary>Displays surface groups and, optionally, their borders.</summary>
        /// <param name="ts">The solid to display.</param>
        /// <param name="features">The surface groups to display.</param>
        /// <param name="showBorders">Whether to display group borders.</param>
        /// <param name="lineThickness">The border line thickness.</param>
        /// <param name="color">The optional border color.</param>
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
                OutputServices.Presenter3D.ShowAndHang([lines], [false], [lineThickness], [color], ts);
            }
            else
                OutputServices.Presenter3D.ShowAndHang(ts);
        }

        /// <summary>Displays primitives, assigning colors by type or from a random palette.</summary>
        /// <param name="ts">The solid to display.</param>
        /// <param name="showBorders">Whether to display primitive borders.</param>
        /// <param name="randomColors">Whether to assign random colors to primitives.</param>
        /// <param name="primitives">The primitives to display, or the solid's primitives when omitted.</param>
        /// <param name="lineThickness">The border line thickness.</param>
        /// <param name="color">The optional border color.</param>
        public static void ShowPrimitives(this TessellatedSolid ts, bool showBorders = true,
            bool randomColors = false, IEnumerable<PrimitiveSurface> primitives = null, double lineThickness = 1,
            Color color = null)
        {
            PaintSurfaces(ts, primitives, randomColors);
            if (showBorders)
                ts.ShowWireFrame(false, null, primitives, lineThickness, color);
            else
                OutputServices.Presenter3D.ShowAndHang(ts);
        }

        private static readonly Color white = new Color(KnownColors.White);
        /// <summary>Displays the solid and selected wire-frame borders without recoloring its primitives.</summary>
        /// <param name="ts">The solid to display.</param>
        /// <param name="resetColor">Whether to reset the solid's colors before displaying it.</param>
        /// <param name="borders">The borders to display, or the available borders when omitted.</param>
        /// <param name="primitives">The primitives whose borders should be displayed.</param>
        /// <param name="lineThickness">The wire-frame line thickness.</param>
        /// <param name="color">The optional wire-frame color.</param>
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
            OutputServices.Presenter3D.ShowAndHang([lines], [false], [lineThickness], colors, ts);
        }

        /// <summary>Builds line paths representing the selected solid wire frame.</summary>
        /// <param name="ts">The solid whose wire frame is requested.</param>
        /// <param name="borders">The borders to use, or available primitive borders when omitted.</param>
        /// <param name="primitives">The primitives whose outer edges should be used.</param>
        /// <returns>The wire-frame line paths.</returns>
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

        /// <summary>Assigns colors to primitive surfaces based on type or a random palette.</summary>
        /// <param name="ts">The solid whose primitives are colored.</param>
        /// <param name="primitives">The primitives to color, or the solid's primitives when omitted.</param>
        /// <param name="randomColors">Whether to assign colors from a random palette.</param>
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
                    KnownColors primitiveColor = primitiveSurface switch
                    {
                        Cylinder => KnownColors.Red,  //like a coke can
                        Cone => KnownColors.DarkOrange, // like an ice cream cone
                        Sphere => KnownColors.Yellow, // like the sun
                        Plane => KnownColors.Green, // like a field (see Minecraft)
                        Torus => KnownColors.HotPink, // like a donut (see Homer Simpson)
                        Prismatic => KnownColors.Indigo, // last color in a rainbow (prismatic -> think prism)
                        UnknownRegion => KnownColors.SlateGray,
                        _ => KnownColors.Gray
                    };
                    primitiveSurface.SetColor(new Color(primitiveColor));
                }
            }
        }

        /// <summary>Displays a primitive surface with illustrative dimensions and axes.</summary>
        /// <param name="solid">The solid containing the primitive.</param>
        /// <param name="bestPrimitiveSurface">The primitive surface to illustrate.</param>
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
                OutputServices.Presenter3D.ShowAndHang(new List<Vector3> { bottom, top }, false, solids: solid);
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
                OutputServices.Presenter3D.ShowAndHang([ cone.Apex, bottom, edgePoint ], false, solids: solid);
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
                OutputServices.Presenter3D.ShowAndHang(torusPoints, false, solids: solid);
            }
            else if (bestPrimitiveSurface is Sphere)
            {
                var sphere = (Sphere)bestPrimitiveSurface;
                var spherePoints = new List<Vector3> {
                                sphere.Center + sphere.Radius * Vector3.UnitX, sphere.Center - sphere.Radius * Vector3.UnitX, sphere.Center,
                                sphere.Center + sphere.Radius * Vector3.UnitY, sphere.Center - sphere.Radius * Vector3.UnitY, sphere.Center,
                                sphere.Center + sphere.Radius * Vector3.UnitZ, sphere.Center - sphere.Radius * Vector3.UnitZ};
                OutputServices.Presenter3D.ShowAndHang(spherePoints, false, solids: solid);

            }
            //For all other surface types, just show the colored primitive
            else
            {
                OutputServices.Presenter3D.ShowAndHang(solid);
            }
        }

        /// <summary>
        /// Get the bitmap of a region of the grid. Start coordinates must be lower than stop coordinates.
        /// </summary>
        /// <param name="grid">The voxelized height grid to render.</param>
        /// <param name="blackPixels">Optional grid indices to render as black pixels.</param>
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
            OutputServices.Presenter2D.ShowAndHang(bitmap, "ZBuffer");
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
                return new Color(v, t, p);
            else if (hi == 1)
                return new Color(q, v, p);
            else if (hi == 2)
                return new Color(p, v, t);
            else if (hi == 3)
                return new Color(p, q, v);
            else if (hi == 4)
                return new Color(t, p, v);
            else
                return new Color(v, p, q);
        }

        /// <summary>
        /// Draws the 12 edges of the bounding box
        /// </summary>
        /// <param name="boundingBox"></param>
        /// <returns></returns>
        public static Vector3[] GetPlotEdges(this BoundingBox boundingBox)
        {
            var corners = boundingBox.Corners;
            //[0] = ---, [1] = +-- , [2] = ++- , [3] = -+-, [4] = --+ , [5] = +-+, [6] = +++, [7] = -++

            return [ corners[0], corners[1], corners[2] , corners[3] , corners[0],
                     // first draw the bottom face edges (z = min), then go up to the top ones (the transition from
                     //0 to 4. This completes 9 of the 12 edges on the box
                     corners[4],corners[5], corners[6],corners[7],corners[4],
                     // now cut back/double-over to finish making the "stilts" between the z planes
                     corners[5], corners[1],
                     corners[2], corners[6],
                     corners[7], corners[3]
                   ];
        }
    }
}
