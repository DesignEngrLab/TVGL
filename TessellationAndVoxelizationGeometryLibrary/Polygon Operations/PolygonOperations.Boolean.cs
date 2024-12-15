// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="PolygonOperations.Boolean.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;


namespace TVGL
{
    /// <summary>
    /// A set of general operation for points and paths
    /// </summary>
    public static partial class PolygonOperations
    {
        /// <summary>
        /// The area simplification fraction
        /// </summary>
        const double areaSimplificationFraction = 1e-5;

        /// <summary>
        /// The sw
        /// </summary>
        static Stopwatch sw = new Stopwatch();
        /// <summary>
        /// The time files
        /// </summary>
        const string timeFiles = "times.csv";
        /// <summary>
        /// Compares the specified TVGL result.
        /// </summary>
        /// <param name="tvglResult">The TVGL result.</param>
        /// <param name="clipperResult">The clipper result.</param>
        /// <param name="operationString">The operation string.</param>
        /// <param name="clipTime">The clip time.</param>
        /// <param name="tvglTime">The TVGL time.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private static bool Compare(List<Polygon> tvglResult, List<Polygon> clipperResult, string operationString, TimeSpan clipTime, TimeSpan tvglTime)
        {
            lock (sw)
            {
                if (!File.Exists(timeFiles))
                {
                    using (var newFs = File.Create(timeFiles)) { }

                }
                using (var fs = File.Open(timeFiles, FileMode.Append))
                {
                    byte[] info = new UTF8Encoding(true).GetBytes(operationString + "," + clipTime.Ticks.ToString() + "," + tvglTime.Ticks.ToString() +
                        "," + clipperResult.Sum(p => p.Vertices.Count) + "," + tvglResult.Sum(p => p.Vertices.Count) + "\n");
                    fs.Write(info, 0, info.Length);
                }

                var tolerance = 0.2;
                var clipperMinX = clipperResult == null || !clipperResult.Any() ? 0 : clipperResult.Min(p => p.MinXIP);
                var clipperMinY = clipperResult == null || !clipperResult.Any() ? 0 : clipperResult.Min(p => p.MinYIP);
                var clipperMaxX = clipperResult == null || !clipperResult.Any() ? 0 : clipperResult.Max(p => p.MaxXIP);
                var clipperMaxY = clipperResult == null || !clipperResult.Any() ? 0 : clipperResult.Max(p => p.MaxYIP);
                var tvglMinX = tvglResult == null || !tvglResult.Any() ? 0 : tvglResult.Min(p => p.MinXIP);
                var tvglMinY = tvglResult == null || !tvglResult.Any() ? 0 : tvglResult.Min(p => p.MinYIP);
                var tvglMaxX = tvglResult == null || !tvglResult.Any() ? 0 : tvglResult.Max(p => p.MaxXIP);
                var tvglMaxY = tvglResult == null || !tvglResult.Any() ? 0 : tvglResult.Max(p => p.MaxYIP);
                var extremaTolerance = tolerance * (new[] { clipperMaxX - clipperMinX, clipperMaxY - clipperMinY, tvglMaxX - tvglMinX, tvglMaxY - tvglMinY }).Min();
                var numPolygonsTVGL = tvglResult == null || !tvglResult.Any() ? 0 : tvglResult.Sum(poly => poly.AllPolygons.Count());
                var numPolygonsClipper = clipperResult == null || !clipperResult.Any() ? 0 : clipperResult.Sum(poly => poly.AllPolygons.Count());
                var vertsTVGL = tvglResult == null || !tvglResult.Any() ? 0 : tvglResult.Sum(poly => poly.AllPolygons.Sum(innerpoly => innerpoly.Vertices.Count));
                var vertsClipper = clipperResult == null || !clipperResult.Any() ? 0 : clipperResult.Sum(poly => poly.AllPolygons.Sum(innerpoly => innerpoly.Vertices.Count));
                var areaTVGL = tvglResult == null || !tvglResult.Any() ? 0 : tvglResult.Sum(p => p.Area);
                var areaClipper = clipperResult == null || !clipperResult.Any() ? 0 : clipperResult.Sum(p => p.Area);
                var perimeterTVGL = tvglResult == null || !tvglResult.Any() ? 0 : tvglResult.Sum(p => p.Perimeter);
                var perimeterClipper = clipperResult == null || !clipperResult.Any() ? 0 : clipperResult.Sum(p => p.Perimeter);
                if (
                    //((double)Math.Abs(numPolygonsTVGL - numPolygonsClipper) / (numPolygonsTVGL + numPolygonsClipper + tolerance)).IsNegligible(tolerance) &&
                    //((double)Math.Abs(vertsTVGL - vertsClipper) / (vertsTVGL + vertsClipper + tolerance)).IsNegligible(tolerance) &&
                    (Math.Abs(areaTVGL - areaClipper) / (areaTVGL + areaClipper + tolerance)).IsNegligible(tolerance)  //&&
                    || (areaTVGL < 0.15 && areaClipper < 0.15)
                    //(Math.Abs(perimeterTVGL - perimeterClipper) / Math.Abs(perimeterTVGL + perimeterClipper + tolerance)).IsNegligible(tolerance) &&
                    //tvglMinX.IsPracticallySame(clipperMinX, extremaTolerance) &&
                    //tvglMinY.IsPracticallySame(clipperMinY, extremaTolerance) &&
                    //tvglMaxX.IsPracticallySame(clipperMaxX, extremaTolerance) &&
                    //tvglMaxY.IsPracticallySame(clipperMaxY, extremaTolerance)
                    )
                {
                    Message.output("***** " + operationString + " matches", 4);
                    Message.output("clipper time = " + clipTime + "; tvgl time = " + tvglTime, 4);
                    return false;
                }
                else
                {
                    //if (numPolygonsClipper == 0) return false;
                    Message.output(operationString + " does not match", 2);
                    Message.output("clipper time = " + clipTime + "; tvgl time = " + tvglTime, 2);
                    //if (numPolygonsTVGL == numPolygonsClipper)
                    //    Message.output("+++ both have {0} polygon(s)", numPolygonsTVGL, numPolygonsClipper);
                    //else 
                    Message.output("    --- polygons: TVGL=" + numPolygonsTVGL + "  : Clipper={1} " + numPolygonsClipper, 2);
                    //if (vertsTVGL == vertsClipper)
                    //   Message.output("+++ both have {0} vertices(s)", vertsTVGL);
                    //else
                    Message.output("    --- verts: TVGL= "+vertsTVGL+"  : Clipper={1} "+ vertsClipper, 2);

                    //if (areaTVGL.IsPracticallySame(areaClipper, tolerance))
                    //   Message.output("+++ both have area of {0}", areaTVGL);
                    //else
                    Message.output("    --- polygons: TVGL=" + areaTVGL + "  : Clipper={1} " + areaClipper, 2);
                    //if (perimeterTVGL.IsPracticallySame(perimeterClipper, tolerance))
                    //    Message.output("+++ both have perimeter of {0}", perimeterTVGL);
                    //else
                    Message.output("    --- polygons: TVGL=" + perimeterTVGL + "  : Clipper={1} " + perimeterClipper, 2);
                    if (perimeterClipper - perimeterTVGL > 0 && Math.Round(perimeterClipper - perimeterTVGL) % 2 == 0)
                        Message.output("<><><><><><><> clipper is connecting separate poly's :", (int)(perimeterClipper - perimeterTVGL) / 2);
                    return true;
                }
            }
        }

        // while the main executing methods are provided in this file (all of which can be invoked as Extensions), the code that perform the new polygon creation
        // is provided in the following four non-Static classes. These are non-static because they all inherit from the BooleanBase class. Each of these only needs
        // to be instantiated once as no data is stored in the class objects. So, this is a sort of singleton model but it's too bad we can have static classes inherit from
        // other static classes.
        /// <summary>
        /// The polygon union
        /// </summary>
        private static PolygonUnion polygonUnion;
        /// <summary>
        /// The polygon intersection
        /// </summary>
        private static PolygonIntersection polygonIntersection;
        /// <summary>
        /// The polygon remove intersections
        /// </summary>
        private static PolygonRemoveIntersections polygonRemoveIntersections;

        #region Union Public Methods

        /// <summary>
        /// Returns the list of polygons that exist in either A OR B.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> Union(this Polygon polygonA, Polygon polygonB,
            PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles)
        {
            if (areaSimplificationFraction > 0)
            {
                polygonA = polygonA.SimplifyFast();
                //polygonA = polygonA.SimplifyByAreaChangeToNewPolygon(areaSimplificationFraction);
                if (polygonB != null)
                {
                    polygonB = polygonB?.SimplifyFast();
                    //polygonB = polygonB?.SimplifyByAreaChangeToNewPolygon(areaSimplificationFraction);
                }
            }
#if CLIPPER
            return BooleanViaClipper(ClipperLib.PolyFillType.Positive, ClipperLib.ClipType.Union, new[] { polygonA },
                  new[] { polygonB });
#elif !COMPARE
            var relationship = GetPolygonInteraction(polygonA, polygonB);
            return Union(polygonA, polygonB, relationship, outputAsCollectionType);
#else
            sw.Restart();
            var pClipper = BooleanViaClipper(ClipperLib.PolyFillType.pftPositive, ClipperLib.ClipType.ctUnion, new[] { polygonA },
                  new[] { polygonB });
            sw.Stop();
            var clipTime = sw.Elapsed;
            sw.Restart();
            var relationship = GetPolygonInteraction(polygonA, polygonB);
            var pTVGL = Union(polygonA, polygonB, relationship, outputAsCollectionType);
            sw.Stop();

            var tvglTime = sw.Elapsed;
            if (Compare(pTVGL, pClipper, "Union", clipTime, tvglTime))
            {
#if PRESENT
                Presenter.ShowAndHang(new[] { polygonA, polygonB });
                Presenter.ShowAndHang(pClipper);
                Presenter.ShowAndHang(pTVGL);
#else
                var fileNameStart = "unionFail" + DateTime.Now.ToOADate().ToString();
                TVGL.IO.Save(polygonA, fileNameStart + ".A.json");
                TVGL.IO.Save(polygonB, fileNameStart + ".B.json");
#endif
            }
            return pClipper;
#endif
        }


        /// <summary>
        /// Returns the list of polygons that exist in either A OR B.By providing the intersections
        /// between the two polygons, the operation will be performed with less time and memory.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <param name="polygonInteraction">The polygon relationship.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="tolerance">The minimum allowable area.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        /// <exception cref="System.ArgumentException">A negative polygon (i.e. hole) is provided to Union which results in infinite shape. - polygonA</exception>
        /// <exception cref="System.ArgumentException">A negative polygon (i.e. hole) is provided to Union which results in infinite shape. - polygonB</exception>
        public static List<Polygon> Union(this Polygon polygonA, Polygon polygonB, PolygonInteractionRecord polygonInteraction,
            PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles, double tolerance = double.NaN)
        {
            if (!polygonA.IsPositive) throw new ArgumentException("A negative polygon (i.e. hole) is provided to Union which results in infinite shape.", nameof(polygonA));
            if (!polygonB.IsPositive) throw new ArgumentException("A negative polygon (i.e. hole) is provided to Union which results in infinite shape.", nameof(polygonB));
            if (!polygonInteraction.CoincidentEdges && (polygonInteraction.Relationship == ABRelationships.Separated ||
                polygonInteraction.Relationship == ABRelationships.AIsInsideHoleOfB ||
                polygonInteraction.Relationship == ABRelationships.BIsInsideHoleOfA))
                return new List<Polygon> { polygonA.Copy(true, false), polygonB.Copy(true, false) };
            if (polygonInteraction.Relationship == ABRelationships.BInsideA ||
               polygonInteraction.Relationship == ABRelationships.Equal)
                return new List<Polygon> { polygonA.Copy(true, false) };
            if (polygonInteraction.Relationship == ABRelationships.AInsideB)
                return new List<Polygon> { polygonB.Copy(true, false) };
            polygonUnion ??= new PolygonUnion();
            return polygonUnion.Run(polygonA, polygonB, polygonInteraction, outputAsCollectionType, tolerance);
        }

        /// <summary>
        /// Returns the list of polygons that are the subshapes of ANY of the provided polygons. Notice this is called UnionPolygons here to distinguish
        /// it from the LINQ function Union, which is also a valid extension for any IEnumerable collection.
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> UnionPolygons(this IEnumerable<Polygon> polygons, PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles)
        {
            if (areaSimplificationFraction > 0)
                polygons = polygons.Select(p => SimplifyFast(p));
            //polygons = polygons.SimplifyByAreaChangeToNewPolygons(areaSimplificationFraction);
#if CLIPPER
            return BooleanViaClipper(ClipperLib.PolyFillType.Positive, ClipperLib.ClipType.Union, polygons);
#elif !COMPARE
            var polygonList = polygons.ToList();
            for (int i = polygonList.Count - 1; i > 0; i--)
            {
                for (int j = i - 1; j >= 0; j--)
                {
                    var interaction = GetPolygonInteraction(polygonList[i], polygonList[j]);
                    if (interaction.Relationship == ABRelationships.BInsideA
                        || interaction.Relationship == ABRelationships.Equal)
                    {  // remove polygon B
                        polygonList.RemoveAt(j);
                        i--;
                    }
                    else if (interaction.Relationship == ABRelationships.AInsideB)
                    {                            // remove polygon A
                        polygonList.RemoveAt(i);
                        break; // to stop the inner loop
                    }
                    else if (interaction.CoincidentEdges || interaction.Relationship == ABRelationships.Intersection)
                    {
                        //if (i == 1 && j == 0)
                        //Presenter.ShowAndHang(new[] { polygonList[i], polygonList[j] });
                        var newPolygons = Union(polygonList[i], polygonList[j], interaction, outputAsCollectionType);
                        //if (i == 1 && j == 0)
                        //Presenter.ShowAndHang(newPolygons);
                        polygonList.RemoveAt(i);
                        polygonList.RemoveAt(j);
                        polygonList.AddRange(newPolygons);
                        i = polygonList.Count; // to restart the outer loop
                        break; // to stop the inner loop
                    }
                }
            }
            return polygonList;
#else
            sw.Restart();
            var pClipper = BooleanViaClipper(ClipperLib.PolyFillType.pftPositive, ClipperLib.ClipType.ctUnion, polygons);
            sw.Stop();
            var clipTime = sw.Elapsed;

            sw.Restart();
            var polygonList = polygons.ToList();
            for (int i = polygonList.Count - 1; i > 0; i--)
            {
                for (int j = i - 1; j >= 0; j--)
                {
                    var interaction = GetPolygonInteraction(polygonList[i], polygonList[j]);
                    if (interaction.Relationship == PolygonRelationship.BInsideA
                        || interaction.Relationship == PolygonRelationship.Equal)
                    {  // remove polygon B
                        polygonList.RemoveAt(j);
                        i--;
                    }
                    else if (interaction.Relationship == PolygonRelationship.AInsideB)
                    {                            // remove polygon A
                        polygonList.RemoveAt(i);
                        break; // to stop the inner loop
                    }
                    else if (interaction.CoincidentEdges || interaction.Relationship == PolygonRelationship.Intersection)
                    {
                        //if (i == 1 && j == 0)
                        //Presenter.ShowAndHang(new[] { polygonList[i], polygonList[j] });
                        var newPolygons = Union(polygonList[i], polygonList[j], interaction, outputAsCollectionType);
                        //if (i == 1 && j == 0)
                        //Presenter.ShowAndHang(newPolygons);
                        polygonList.RemoveAt(i);
                        polygonList.RemoveAt(j);
                        polygonList.AddRange(newPolygons);
                        i = polygonList.Count; // to restart the outer loop
                        break; // to stop the inner loop
                    }
                }
            }
            sw.Stop();
            var tvglTime = sw.Elapsed;
            if (Compare(polygonList, pClipper, "UnionLists", clipTime, tvglTime))
            {
#if PRESENT
                Presenter.ShowAndHang(polygons);
                Presenter.ShowAndHang(pClipper);
                Presenter.ShowAndHang(polygonList);
#else
                var fileNameStart = "unionFail" + DateTime.Now.ToOADate().ToString();
                int i = 0;
                foreach (var poly in polygons)
                    TVGL.IO.Save(poly, fileNameStart + "." + (i++).ToString() + ".json");
#endif
            }
            return pClipper;
#endif
        }

        /// <summary>
        /// Returns the list of polygons that are the subshapes of the two collections of polygons. Notice this is called UnionPolygons
        /// here to distinguish it from the LINQ function Union, which is also a valid extension for any IEnumerable collection.
        /// </summary>
        /// <param name="polygonsA">The polygons a.</param>
        /// <param name="polygonsB">The polygons b.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> UnionPolygons(this IEnumerable<Polygon> polygonsA, IEnumerable<Polygon> polygonsB, PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles,
            double tolerance = double.NaN)
        {
            if (areaSimplificationFraction > 0)
            {
                polygonsA = polygonsA.Select(p => SimplifyFast(p));
                //polygonsA = polygonsA.SimplifyByAreaChangeToNewPolygons(areaSimplificationFraction);
                if (polygonsB != null)
                    polygonsB = polygonsB?.Select(p => SimplifyFast(p));
                //polygonsB = polygonsB?.SimplifyByAreaChangeToNewPolygons(areaSimplificationFraction);
            }
#if CLIPPER
            return BooleanViaClipper(ClipperLib.PolyFillType.Positive, ClipperLib.ClipType.Union, polygonsA, polygonsB);
#elif !COMPARE
            if (polygonsB is null)
                return UnionPolygons(polygonsA, outputAsCollectionType);
            var unionedPolygons = polygonsA.ToList();
            var polygonBList = polygonsB.ToList();
            for (int i = unionedPolygons.Count - 1; i >= 0; i--)
            {
                for (int j = polygonBList.Count - 1; j >= 0; j--)
                {
                    var interaction = GetPolygonInteraction(unionedPolygons[i], polygonBList[j]);
                    if (interaction.Relationship == ABRelationships.BInsideA
                        || interaction.Relationship == ABRelationships.Equal)
                    {  // remove polygon B
                        polygonBList.RemoveAt(j);
                    }
                    else if (interaction.Relationship == ABRelationships.AInsideB)
                    {                            // remove polygon A
                        unionedPolygons[i] = polygonBList[j];
                        polygonBList.RemoveAt(j);
                        break; // to stop the inner loop
                    }
                    else if (interaction.CoincidentEdges || interaction.Relationship == ABRelationships.Intersection)
                    {
                        var newPolygons = Union(unionedPolygons[i], polygonBList[j], interaction, outputAsCollectionType, tolerance);
                        unionedPolygons.RemoveAt(i);
                        polygonBList.RemoveAt(j);
                        unionedPolygons.AddRange(newPolygons);
                        i = unionedPolygons.Count; // to restart the outer loop
                        break; // to stop the inner loop
                    }
                }
            }
            return UnionPolygons(unionedPolygons.Where(p => p.IsPositive), outputAsCollectionType);
#else
            if (polygonsB is null)
                return UnionPolygons(polygonsA, outputAsCollectionType);
            var unionedPolygons = polygonsA.ToList();
            var polygonBList = polygonsB.ToList();
            sw.Restart();
            var pClipper = BooleanViaClipper(ClipperLib.PolyFillType.pftPositive, ClipperLib.ClipType.ctUnion, polygonsA, polygonsB);
            sw.Stop();
            var clipTime = sw.Elapsed;

            sw.Restart();
            List<Polygon> pTVGL = null;
            for (int i = unionedPolygons.Count - 1; i >= 0; i--)
            {
                for (int j = polygonBList.Count - 1; j >= 0; j--)
                {
                    var interaction = GetPolygonInteraction(unionedPolygons[i], polygonBList[j]);
                    if (interaction.Relationship == PolygonRelationship.BInsideA
                        || interaction.Relationship == PolygonRelationship.Equal)
                    {  // remove polygon B
                        polygonBList.RemoveAt(j);
                    }
                    else if (interaction.Relationship == PolygonRelationship.AInsideB)
                    {                            // remove polygon A
                        unionedPolygons[i] = polygonBList[j];
                        polygonBList.RemoveAt(j);
                        break; // to stop the inner loop
                    }
                    else if (interaction.CoincidentEdges || interaction.Relationship == PolygonRelationship.Intersection)
                    {
                        //if (i == 1 && j == 0)
                        //Presenter.ShowAndHang(new[] { polygonList[i], polygonList[j] });
                        var newPolygons = Union(unionedPolygons[i], polygonBList[j], interaction, outputAsCollectionType, tolerance);
                        //if (i == 1 && j == 0)
                        //Presenter.ShowAndHang(newPolygons);
                        unionedPolygons.RemoveAt(i);
                        polygonBList.RemoveAt(j);
                        unionedPolygons.AddRange(newPolygons);
                        i = unionedPolygons.Count; // to restart the outer loop
                        break; // to stop the inner loop
                    }
                }
            }
            pTVGL = UnionPolygons(unionedPolygons.Where(p => p.IsPositive), outputAsCollectionType);
            sw.Stop();
            var tvglTime = sw.Elapsed;
            if (Compare(pTVGL, pClipper, "UnionTwoLists", clipTime, tvglTime))
            {
#if PRESENT
                var all = polygonsA.ToList();
                all.AddRange(polygonsB);
                Presenter.ShowAndHang(all);
                Presenter.ShowAndHang(pClipper);
                Presenter.ShowAndHang(unionedPolygons);
#else
                var fileNameStart = "unionFail" + DateTime.Now.ToOADate().ToString();
                int i = 0;
                foreach (var poly in polygonsA)
                    TVGL.IO.Save(poly, fileNameStart + "." + (i++).ToString() + "A.json");
                i = 0;
                foreach (var poly in polygonsB)
                    TVGL.IO.Save(poly, fileNameStart + "." + (i++).ToString() + "B.json");
#endif
            }
            return pClipper;
#endif
        }

        #endregion Union Public Methods

        #region Intersect Public Methods

        /// <summary>
        /// Returns the list of polygons that result from the subshapes common to both A and B.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> Intersect(this Polygon polygonA, Polygon polygonB,
            PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles, double tolerance = double.NaN)
        {
            if (areaSimplificationFraction > 0)
            {
                polygonA = polygonA.SimplifyFast();
                //polygonA = polygonA.SimplifyByAreaChangeToNewPolygon(areaSimplificationFraction);
                if (polygonB != null)
                {
                    //If not null
                    polygonB = polygonB?.SimplifyFast();
                    //polygonB = polygonB?.SimplifyByAreaChangeToNewPolygon(areaSimplificationFraction);
                }
            }
#if CLIPPER
            return BooleanViaClipper(ClipperLib.PolyFillType.Positive, ClipperLib.ClipType.Intersection, new[] { polygonA },
                    new[] { polygonB });
#elif !COMPARE
            var relationship = GetPolygonInteraction(polygonA, polygonB);
            return Intersect(polygonA, polygonB, relationship, outputAsCollectionType, tolerance);
#else
            sw.Restart();
            var pClipper = BooleanViaClipper(ClipperLib.PolyFillType.pftPositive, ClipperLib.ClipType.ctIntersection, new[] { polygonA },
                    new[] { polygonB });
            sw.Stop();
            var clipTime = sw.Elapsed;
            sw.Restart();
            var relationship = GetPolygonInteraction(polygonA, polygonB);
            var pTVGL = Intersect(polygonA, polygonB, relationship, outputAsCollectionType, tolerance);
            sw.Stop();
            var tvglTime = sw.Elapsed;
            if (Compare(pTVGL, pClipper, "Intersect", clipTime, tvglTime))
            {
#if PRESENT
                Presenter.ShowAndHang(new[] { polygonA, polygonB });
                Presenter.ShowAndHang(pClipper);
                Presenter.ShowAndHang(pTVGL);
#else
                var fileNameStart = "intersectFail" + DateTime.Now.ToOADate().ToString();
                TVGL.IO.Save(polygonA, fileNameStart + "." + "A.json");
                TVGL.IO.Save(polygonB, fileNameStart + "." + "B.json");
#endif
            }
            return pClipper;
#endif
        }

        /// <summary>
        /// Returns the list of polygons that result from the subshapes common to both A and B. By providing the intersections
        /// between the two polygons, the operation will be performed with less time and memory.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <param name="interaction">The interaction.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="tolerance">The minimum allowable area.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> Intersect(this Polygon polygonA, Polygon polygonB, PolygonInteractionRecord interaction,
            PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles, double tolerance = double.NaN)
        {
            if (interaction.IntersectionWillBeEmpty())
                return new List<Polygon>();
            else
            {
                polygonIntersection ??= new PolygonIntersection();
                return polygonIntersection.Run(polygonA, polygonB, interaction, outputAsCollectionType, tolerance);
            }
        }

        /// <summary>
        /// Returns the list of polygons that are the sub-shapes of the two collections of polygons. Notice this is called IntersectPolygons here
        /// to distinguish it from the LINQ function Intersect, which is also a valid extension for any IEnumerable collection.
        /// Notice also that any overlap between the polygons in A or the polygons in B are ignored. Finally, all inputs must be positive.
        /// </summary>
        /// <param name="polygonsA">The polygons a.</param>
        /// <param name="polygonsB">The polygons b.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> IntersectPolygons(this IEnumerable<Polygon> polygonsA, IEnumerable<Polygon> polygonsB, PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles)
        {
            if (areaSimplificationFraction > 0)
            {
                polygonsA = polygonsA.Select(p => SimplifyFast(p));
                //polygonsA = polygonsA.SimplifyByAreaChangeToNewPolygons(areaSimplificationFraction);
                if (polygonsB != null)
                    polygonsB = polygonsB.Select(p => SimplifyFast(p));
                //polygonsB = polygonsB?.SimplifyByAreaChangeToNewPolygons(areaSimplificationFraction);
            }
#if CLIPPER
            return BooleanViaClipper(ClipperLib.PolyFillType.Positive, ClipperLib.ClipType.Intersection, polygonsA, polygonsB);
#elif !COMPARE
            if (polygonsB is null)
                return UnionPolygons(polygonsA, outputAsCollectionType);

            var result = polygonsA.ToList();
            foreach (var polygon in polygonsB)
            {
                if (!result.Any()) break;
                result = result.SelectMany(r => r.Intersect(polygon)).ToList();
            }
            return result;
#else
            if (polygonsB is null)
                return UnionPolygons(polygonsA, outputAsCollectionType);
            var polygonAList = new List<Polygon>(polygonsA);
            var polygonBList = polygonsB.ToList();
            sw.Restart();
            var pClipper = BooleanViaClipper(ClipperLib.PolyFillType.pftPositive, ClipperLib.ClipType.ctIntersection, polygonsA, polygonsB);
            sw.Stop();
            var clipTime = sw.Elapsed;
            sw.Restart();

            foreach (var polyB in polygonBList)
            {
                for (int i = polygonAList.Count - 1; i >= 0; i--)
                {
                    var newPolygons = polygonAList[i].Intersect(polyB, outputAsCollectionType);
                    polygonAList.RemoveAt(i);
                    foreach (var newPoly in newPolygons)
                        polygonAList.Insert(i, newPoly);
                }
            }
            //return polygonAList;
            sw.Stop();
            var tvglTime = sw.Elapsed;
            if (Compare(polygonAList, pClipper, "IntersectTwoList", clipTime, tvglTime))
            {
#if PRESENT
                var all = polygonsA.ToList();
                all.AddRange(polygonsB);
                Presenter.ShowAndHang(all);
                Presenter.ShowAndHang(pClipper);
                Presenter.ShowAndHang(polygonAList);
#else
                var fileNameStart = "intersectFail" + DateTime.Now.ToOADate().ToString();
                int i = 0;
                foreach (var poly in polygonsA)
                    TVGL.IO.Save(poly, fileNameStart + "." + (i++).ToString() + "A.json");
                i = 0;
                foreach (var poly in polygonsA)
                    TVGL.IO.Save(poly, fileNameStart + "." + (i++).ToString() + "B.json");
#endif
            }
            return pClipper;
#endif
        }


        /// <summary>
        /// Returns the list of polygons that are the sub-shapes of ALL of the provided polygons. Notice this is called IntersectPolygons here
        /// to distinguish it from the LINQ function Intersect, which is also a valid extension for any IEnumerable collection.
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> IntersectPolygons(this IEnumerable<Polygon> polygons, PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles)
        {
            if (areaSimplificationFraction > 0)
                polygons = polygons.Select(p => SimplifyFast(p));
            //polygons = polygons.SimplifyByAreaChangeToNewPolygons(areaSimplificationFraction);
#if CLIPPER
            return BooleanViaClipper(ClipperLib.PolyFillType.Positive, ClipperLib.ClipType.Intersection, polygons);
#elif !COMPARE
            var result = new List<Polygon>();
            if (!polygons.Any()) return result;
            else result.Add(polygons.First());
            foreach (var polygon in polygons.Skip(1))
            {
                if (!result.Any()) break;
                result = result.SelectMany(r => r.Intersect(polygon)).ToList();
            }
            return result;
#else
            var polygonList = polygons.ToList();
            sw.Restart();
            var pClipper = BooleanViaClipper(ClipperLib.PolyFillType.pftPositive, ClipperLib.ClipType.ctIntersection, polygons);
            sw.Stop();
            var clipTime = sw.Elapsed;
            sw.Restart();
            var result = new List<Polygon>();
            if (!polygons.Any()) return result;
            else result.Add(polygons.First());
            foreach (var polygon in polygons.Skip(1))
            {
                if (!result.Any()) break;
                result = result.SelectMany(r => r.Intersect(polygon)).ToList();
            }
            return result;
            sw.Stop();
            var tvglTime = sw.Elapsed;
            if (Compare(polygonList, pClipper, "IntersectList", clipTime, tvglTime))
            {
#if PRESENT
                Presenter.ShowAndHang(polygons);
                Presenter.ShowAndHang(pClipper);
                Presenter.ShowAndHang(polygonList);
#else
                var fileNameStart = "intersectFail" + DateTime.Now.ToOADate().ToString();
                int i = 0;
                foreach (var poly in polygons)
                    TVGL.IO.Save(poly, fileNameStart + "." + (i++).ToString() + ".json");
#endif
            }
            return pClipper;
#endif
        }

        #endregion Intersect Public Methods

        #region Direct Access to Clipper API
        /// <summary>
        /// Booleans the via clipper.
        /// </summary>
        /// <param name="polygonsA">The polygons a.</param>
        /// <param name="polygonsB">The polygons b.</param>
        /// <param name="fillType">Type of the fill.</param>
        /// <param name="clipType">Type of the clip.</param>
        /// <returns>List&lt;Polygon&gt;.</returns>
        internal static List<Polygon> BooleanViaClipper(IEnumerable<Polygon> polygonsA, IEnumerable<Polygon> polygonsB, ClipperLib.PolyFillType fillType,
            ClipperLib.ClipType clipType)
        {
            return BooleanViaClipper(fillType, clipType, polygonsA, polygonsB);
        }

        /// <summary>
        /// Booleans the via clipper.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <param name="fillType">Type of the fill.</param>
        /// <param name="clipType">Type of the clip.</param>
        /// <returns>List&lt;Polygon&gt;.</returns>
        internal static List<Polygon> BooleanViaClipper(Polygon polygonA, Polygon polygonB, ClipperLib.PolyFillType fillType, ClipperLib.ClipType clipType)
        {
            return BooleanViaClipper(fillType, clipType, new[] { polygonA }, new[] { polygonB });
        }
        #endregion

        #region Subtract Public Methods

        /// <summary>
        /// Returns the list of polygons that result from A-B (subtracting polygon B from polygon A).
        /// </summary>
        /// <param name="minuend">The polygon a.</param>
        /// <param name="subtrahend">The polygon b.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> Subtract(this Polygon minuend, Polygon subtrahend,
                    PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles, double tolerance = double.NaN)
        {
            if (!subtrahend.Path.Any() || subtrahend.Area.IsNegligible())
                return [minuend];
            if (areaSimplificationFraction > 0)
            {
                minuend = minuend.SimplifyFast();
                //minuend = minuend.SimplifyByAreaChangeToNewPolygon(areaSimplificationFraction);
                if (subtrahend != null)
                    subtrahend = subtrahend?.SimplifyFast();
                //    subtrahend = subtrahend?.SimplifyByAreaChangeToNewPolygon(areaSimplificationFraction);
            }
#if CLIPPER
            return BooleanViaClipper(ClipperLib.PolyFillType.Positive, ClipperLib.ClipType.Difference, new[] { minuend },
                            new[] { subtrahend });
#elif !COMPARE
                    var polygonBInverted = subtrahend.Copy(true, true);
                    var relationship = GetPolygonInteraction(minuend, polygonBInverted);
                    return Intersect(minuend, polygonBInverted, relationship, outputAsCollectionType, tolerance);
#else
                    sw.Restart();
                    var pClipper = BooleanViaClipper(ClipperLib.PolyFillType.pftPositive, ClipperLib.ClipType.ctDifference, new[] { minuend },
                        new[] { subtrahend });
                    sw.Stop();
                    var clipTime = sw.Elapsed;
                    sw.Restart();
                    var polygonBInverted = subtrahend.Copy(true, true);
                    var relationship = GetPolygonInteraction(minuend, polygonBInverted);
                    var pTVGL = Intersect(minuend, polygonBInverted, relationship, outputAsCollectionType, tolerance);

                    sw.Stop();
                    var tvglTime = sw.Elapsed;
                    if (Compare(pTVGL, pClipper, "Subtract", clipTime, tvglTime))
                    {
#if PRESENT

                        Presenter.ShowAndHang(new[] { minuend, subtrahend });
                        Presenter.ShowAndHang(pClipper);
                        Presenter.ShowAndHang(pTVGL);
#else
                        var fileNameStart = "subtractFail" + DateTime.Now.ToOADate().ToString();
                        TVGL.IO.Save(minuend, fileNameStart + "." + "min.json");
                        TVGL.IO.Save(subtrahend, fileNameStart + "." + "sub.json");
#endif
                    }
                    return pClipper;
#endif
        }

        /// <summary>
        /// Returns the list of polygons that result from A-B (subtracting polygon B from polygon A). By providing the intersections
        /// between the two polygons, the operation will be performed with less time and memory.
        /// </summary>
        /// <param name="minuend">The polygon a.</param>
        /// <param name="subtrahend">The polygon b.</param>
        /// <param name="interaction">The polygon relationship.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        /// <exception cref="ArgumentException">The minuend is already a negative polygon (i.e. hole). Consider another operation"
        /// +" to accomplish this function, like Intersect. - polygonA</exception>
        public static List<Polygon> Subtract(this Polygon minuend, Polygon subtrahend, PolygonInteractionRecord interaction,
                    PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles, double tolerance = double.NaN)
        {
            interaction = interaction.InvertPolygonInRecord(subtrahend, out var invertedPolygonB);
            return Intersect(minuend, invertedPolygonB, interaction, outputAsCollectionType, tolerance);
        }


        /// <summary>
        /// Returns the list of polygons that are the sub-shapes of the minuends and that are not part of the subtrahends.
        /// Notice also that any overlap between the polygons in A or the polygons in B are ignored. Finally, all inputs must be positive.
        /// </summary>
        /// <param name="minuends">The polygons a.</param>
        /// <param name="subtrahends">The polygons b.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> Subtract(this IEnumerable<Polygon> minuends, IEnumerable<Polygon> subtrahends,
                    PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles, double tolerance = double.NaN)
        {
            if (areaSimplificationFraction > 0)
            {
                minuends = minuends.Select(p => SimplifyFast(p));
                //minuends = minuends.SimplifyByAreaChangeToNewPolygons(areaSimplificationFraction);
                if (subtrahends != null)
                    subtrahends = subtrahends.Select(p => SimplifyFast(p));
                //subtrahends = subtrahends.SimplifyByAreaChangeToNewPolygons(areaSimplificationFraction);
            }
#if CLIPPER
            return BooleanViaClipper(ClipperLib.PolyFillType.Positive, ClipperLib.ClipType.Difference, minuends, subtrahends);
#elif !COMPARE
                    var minuendsList = minuends.ToList();
        foreach (var polyB in subtrahends)
                    {
                        for (int i = minuendsList.Count - 1; i >= 0; i--)
                        {
                            var newPolygons = minuendsList[i].Subtract(polyB, outputAsCollectionType, tolerance);
                            minuendsList.RemoveAt(i);
                            foreach (var newPoly in newPolygons)
                                minuendsList.Insert(i, newPoly);
                        }
                    }
                    return minuendsList;
#else
                    var minuendsList = minuends.ToList();
                    sw.Restart();
                    var pClipper = BooleanViaClipper(ClipperLib.PolyFillType.pftPositive, ClipperLib.ClipType.ctDifference, minuends, subtrahends);
                    sw.Stop();
                    var clipTime = sw.Elapsed;
                    sw.Restart();

                    foreach (var polyB in subtrahends)
                    {
                        for (int i = minuendsList.Count - 1; i >= 0; i--)
                        {
                            var newPolygons = minuendsList[i].Subtract(polyB, outputAsCollectionType, tolerance);
                            minuendsList.RemoveAt(i);
                            foreach (var newPoly in newPolygons)
                                minuendsList.Insert(i, newPoly);
                        }
                    }
                    sw.Stop();
                    var tvglTime = sw.Elapsed;
                    if (Compare(minuendsList, pClipper, "SubtractLists", clipTime, tvglTime))
                    {
#if PRESENT
                        var all = minuends.ToList();
                        all.AddRange(subtrahends);
                        Presenter.ShowAndHang(all);
                        Presenter.ShowAndHang(pClipper);
                        Presenter.ShowAndHang(minuendsList);
#else
                        var fileNameStart = "subtractFail" + DateTime.Now.ToOADate().ToString();
                        int i = 0;
                        foreach (var poly in minuends)
                            TVGL.IO.Save(poly, fileNameStart + "." + (i++).ToString() + "min.json");
                        i = 0;
                        foreach (var poly in subtrahends)
                            TVGL.IO.Save(poly, fileNameStart + "." + (i++).ToString() + "sub.json");
#endif
                    }
                    return pClipper;
#endif
        }


        #endregion Subtract Public Methods

        #region Exclusive-OR Public Methods

        /// <summary>
        /// Returns the list of polygons that are the Exclusive-OR of the two input polygons. Exclusive-OR are the regions where one polgyon
        /// resides but not both.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> ExclusiveOr(this Polygon polygonA, Polygon polygonB,
            PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles)
        {
            if (areaSimplificationFraction > 0)
            {
                polygonA = polygonA.SimplifyFast();
                //polygonA = polygonA.SimplifyByAreaChangeToNewPolygon(areaSimplificationFraction);
                if (polygonB != null)
                    polygonB = polygonB.SimplifyFast();
                //polygonB = polygonB?.SimplifyByAreaChangeToNewPolygon(areaSimplificationFraction);
            }
#if CLIPPER
            return BooleanViaClipper(ClipperLib.PolyFillType.Positive, ClipperLib.ClipType.Xor, new[] { polygonA }, new[] { polygonB });
#elif !COMPARE
            var relationship = GetPolygonInteraction(polygonA, polygonB);
            return ExclusiveOr(polygonA, polygonB, relationship, outputAsCollectionType);
#else
            sw.Restart();
            var pClipper = BooleanViaClipper(ClipperLib.PolyFillType.pftPositive, ClipperLib.ClipType.ctXor, new[] { polygonA }, new[] { polygonB });
            sw.Stop();
            var clipTime = sw.Elapsed;
            sw.Restart();
            var relationship = GetPolygonInteraction(polygonA, polygonB);
            var pTVGL = ExclusiveOr(polygonA, polygonB, relationship, outputAsCollectionType);

            sw.Stop();
            var tvglTime = sw.Elapsed;
            if (Compare(pTVGL, pClipper, "XOR", clipTime, tvglTime))
            {
#if PRESENT

                Presenter.ShowAndHang(new[] { polygonA, polygonB });
                Presenter.ShowAndHang(pClipper);
                Presenter.ShowAndHang(pTVGL);
#else
                var fileNameStart = "xorFail" + DateTime.Now.ToOADate().ToString();
                TVGL.IO.Save(polygonA, fileNameStart + "." + "A.json");
                TVGL.IO.Save(polygonB, fileNameStart + "." + "B.json");
#endif
            }
            return pClipper;
#endif
        }

        /// <summary>
        /// Returns the list of polygons that are the Exclusive-OR of the two input polygons. Exclusive-OR are the regions where one polgyon
        /// resides but not both. By providing the intersections between the two polygons, the operation will be performed with less time and memory.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <param name="interactionRecord">The interaction record.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> ExclusiveOr(this Polygon polygonA, Polygon polygonB, PolygonInteractionRecord interactionRecord,
       PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles, double tolerance = double.NaN)
        {
            if (interactionRecord.IntersectionWillBeEmpty())
                return new List<Polygon> { polygonA.Copy(true, false), polygonB.Copy(true, false) };
            else if (interactionRecord.Relationship == ABRelationships.BInsideA &&
                !interactionRecord.CoincidentEdges && !interactionRecord.CoincidentVertices)
            {
                var polygonACopy1 = polygonA.Copy(true, false);
                polygonACopy1.AddInnerPolygon(polygonB.Copy(true, true));
                return new List<Polygon> { polygonACopy1 };
            }
            else if (interactionRecord.Relationship == ABRelationships.AInsideB &&
                !interactionRecord.CoincidentEdges && !interactionRecord.CoincidentVertices)
            {
                var polygonBCopy2 = polygonB.Copy(true, false);
                polygonBCopy2.AddInnerPolygon(polygonA.Copy(true, true));
                return new List<Polygon> { polygonBCopy2 };
            }
            else
            {
                var result = polygonA.Subtract(polygonB, interactionRecord, outputAsCollectionType, tolerance);
                result.AddRange(polygonB.Subtract(polygonA, interactionRecord, outputAsCollectionType, tolerance));
                return result;
            }
        }

        #endregion Exclusive-OR Public Methods

        #region RemoveSelfIntersections Public Method

        /// <summary>
        /// Removes the self intersections.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="resultType">Type of the result.</param>
        /// <param name="knownWrongPoints">The known wrong points.</param>
        /// <param name="maxNumberOfPolygons">The maximum number of polygons.</param>
        /// <returns>List&lt;Polygon&gt;.</returns>
        public static List<Polygon> RemoveSelfIntersections(this Polygon polygon, ResultType resultType,
            List<bool> knownWrongPoints = null, int maxNumberOfPolygons = int.MaxValue)
        {
            var intersections = polygon.GetSelfIntersections().Where(intersect => intersect.Relationship != SegmentRelationship.NoOverlap).ToList();
            if (intersections.Count == 0)
                return new List<Polygon> { polygon };
            polygonRemoveIntersections ??= new PolygonRemoveIntersections();
            return polygonRemoveIntersections.Run(polygon, intersections, resultType, knownWrongPoints, maxNumberOfPolygons);
        }

        #endregion RemoveSelfIntersections Public Method
    }
}