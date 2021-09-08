// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using TVGL.Numerics;
using ClipperLib;
using ClipperLib2Beta;

namespace TVGL.TwoDimensional
{
    /// <summary>
    /// A set of general operation for points and paths
    /// </summary>
    public static partial class PolygonOperations
    {
        static Stopwatch sw = new Stopwatch();
        const string timeFiles = "times.csv";
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
                var clipperMinX = clipperResult == null || !clipperResult.Any() ? 0 : clipperResult.Min(p => p.MinX);
                var clipperMinY = clipperResult == null || !clipperResult.Any() ? 0 : clipperResult.Min(p => p.MinY);
                var clipperMaxX = clipperResult == null || !clipperResult.Any() ? 0 : clipperResult.Max(p => p.MaxX);
                var clipperMaxY = clipperResult == null || !clipperResult.Any() ? 0 : clipperResult.Max(p => p.MaxY);
                var tvglMinX = tvglResult == null || !tvglResult.Any() ? 0 : tvglResult.Min(p => p.MinX);
                var tvglMinY = tvglResult == null || !tvglResult.Any() ? 0 : tvglResult.Min(p => p.MinY);
                var tvglMaxX = tvglResult == null || !tvglResult.Any() ? 0 : tvglResult.Max(p => p.MaxX);
                var tvglMaxY = tvglResult == null || !tvglResult.Any() ? 0 : tvglResult.Max(p => p.MaxY);
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
                    //Debug.WriteLine("***** " + operationString + " matches");
                    //Debug.WriteLine("clipper time = {0}; tvgl time = {1}", clipTime, tvglTime);
                    return false;
                }
                else
                {
                    //if (numPolygonsClipper == 0) return false;
                    Debug.WriteLine(operationString + " does not match");
                    Debug.WriteLine("clipper time = {0}; tvgl time = {1}", clipTime, tvglTime);
                    //if (numPolygonsTVGL == numPolygonsClipper)
                    //    Debug.WriteLine("+++ both have {0} polygon(s)", numPolygonsTVGL, numPolygonsClipper);
                    //else 
                    Debug.WriteLine("    --- polygons: TVGL={0}  : Clipper={1} ", numPolygonsTVGL, numPolygonsClipper);
                    //if (vertsTVGL == vertsClipper)
                    //    Debug.WriteLine("+++ both have {0} vertices(s)", vertsTVGL);
                    //else
                    Debug.WriteLine("    --- verts: TVGL= {0}  : Clipper={1} ", vertsTVGL, vertsClipper);

                    //if (areaTVGL.IsPracticallySame(areaClipper, tolerance))
                    //    Debug.WriteLine("+++ both have area of {0}", areaTVGL);
                    //else
                    Debug.WriteLine("    --- area: TVGL= {0}  : Clipper={1} ", areaTVGL, areaClipper);
                    //if (perimeterTVGL.IsPracticallySame(perimeterClipper, tolerance))
                    //    Debug.WriteLine("+++ both have perimeter of {0}", perimeterTVGL);
                    //else
                    Debug.WriteLine("    --- perimeter: TVGL={0}  : Clipper={1} ", perimeterTVGL, perimeterClipper);
                    if (perimeterClipper - perimeterTVGL > 0 && Math.Round(perimeterClipper - perimeterTVGL) % 2 == 0)
                        Debug.WriteLine("<><><><><><><> clipper is connecting separate poly's :", (int)(perimeterClipper - perimeterTVGL) / 2);
                    return true;
                }
            }
        }

        // while the main executing methods are provided in this file (all of which can be invoked as Extensions), the code that perform the new polygon creation
        // is provided in the following four non-Static classes. These are non-static because they all inherit from the BooleanBase class. Each of these only needs
        // to be instantiated once as no data is stored in the class objects. So, this is a sort of singleton model but it's too bad we can have static classes inherit from
        // other static classes.
        private static PolygonUnion polygonUnion;
        private static PolygonIntersection polygonIntersection;
        private static PolygonRemoveIntersections polygonRemoveIntersections;

        #region Union Public Methods

        /// <summary>
        /// Returns the list of polygons that exist in either A OR B.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> Union(this Polygon polygonA, Polygon polygonB, PolygonSimplify polygonSimplify = PolygonSimplify.CanSimplifyOriginal,
            PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles)
        {
            polygonA = polygonA.CleanUpForBooleanOperations(polygonSimplify);
            polygonB = polygonB?.CleanUpForBooleanOperations(polygonSimplify);
#if CLIPPER
            return BooleanViaClipper(PolyFillType.Positive, ClipType.Union, new[] { polygonA },
                  new[] { polygonB });
            sw.Restart();
            var pClipper = BooleanViaClipper(PolyFillType.Positive, ClipType.Union, new[] { polygonA },
                  new[] { polygonB });
            sw.Stop();
            var clipTime = sw.Elapsed;

            sw.Restart();
            var pClipper2B = PolygonOperationsV2.BooleanViaClipper(FillRule.Positive, ClipType2.Union, new[] { polygonA },
                new[] { polygonB });
            sw.Stop();
            var clipBTime = sw.Elapsed;
            if (Compare(pClipper, pClipper2B, "Union", clipTime, clipBTime))
            {
                var fileNameStart = "unionFail" + DateTime.Now.ToOADate().ToString();
                IOFunctions.IO.Save(polygonA, fileNameStart + ".A.json");
                IOFunctions.IO.Save(polygonB, fileNameStart + ".B.json");
            }
            return pClipper;
#elif !COMPARE
            return UnionTVGL(polygonA, polygonB, polygonSimplify, outputAsCollectionType);
#else
            sw.Restart();
            var pClipper = BooleanViaClipper(PolyFillType.Positive, ClipType.Union, new[] { polygonA },
                  new[] { polygonB });
            sw.Stop();
            var clipTime = sw.Elapsed;
            sw.Restart();
            var pTVGL = UnionTVGL(polygonA, polygonB, polygonSimplify, outputAsCollectionType);
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
                TVGL.IOFunctions.IO.Save(polygonA, fileNameStart + ".A.json");
                TVGL.IOFunctions.IO.Save(polygonB, fileNameStart + ".B.json");
#endif
            }
            return pClipper;
#endif
        }

        /// <summary>
        /// Returns the list of polygons that exist in either A OR B.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> UnionTVGL(this Polygon polygonA, Polygon polygonB, PolygonSimplify polygonSimplify = PolygonSimplify.CanSimplifyOriginal,
            PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles)
        {
            //polygonA = polygonA.CleanUpForBooleanOperations(polygonSimplify);
            //polygonB = polygonB?.CleanUpForBooleanOperations(polygonSimplify);
            var relationship = GetPolygonInteraction(polygonA, polygonB);
            return UnionTVGL(polygonA, polygonB, relationship, outputAsCollectionType);
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
        /// <exception cref="ArgumentException">A negative polygon (i.e. hole) is provided to Union which results in infinite shape. - polygonA</exception>
        /// <exception cref="ArgumentException">A negative polygon (i.e. hole) is provided to Union which results in infinite shape. - polygonB</exception>
        public static List<Polygon> UnionTVGL(this Polygon polygonA, Polygon polygonB, PolygonInteractionRecord polygonInteraction,
            PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles, double tolerance = double.NaN)
        {
            if (!polygonA.IsPositive) throw new ArgumentException("A negative polygon (i.e. hole) is provided to Union which results in infinite shape.", nameof(polygonA));
            if (!polygonB.IsPositive) throw new ArgumentException("A negative polygon (i.e. hole) is provided to Union which results in infinite shape.", nameof(polygonB));
            if (!polygonInteraction.CoincidentEdges && (polygonInteraction.Relationship == PolygonRelationship.Separated ||
                polygonInteraction.Relationship == PolygonRelationship.AIsInsideHoleOfB ||
                polygonInteraction.Relationship == PolygonRelationship.BIsInsideHoleOfA))
                return new List<Polygon> { polygonA.Copy(true, false), polygonB.Copy(true, false) };
            if (polygonInteraction.Relationship == PolygonRelationship.BInsideA ||
               polygonInteraction.Relationship == PolygonRelationship.Equal)
                return new List<Polygon> { polygonA.Copy(true, false) };
            if (polygonInteraction.Relationship == PolygonRelationship.AInsideB)
                return new List<Polygon> { polygonB.Copy(true, false) };
            polygonUnion ??= new PolygonUnion();
            return polygonUnion.Run(polygonA, polygonB, polygonInteraction, outputAsCollectionType, tolerance).Where(p => p.IsPositive).ToList();
        }

        /// <summary>
        /// Returns the list of polygons that are the subshapes of ANY of the provided polygons. Notice this is called UnionPolygons here to distinguish
        /// it from the LINQ function Union, which is also a valid extension for any IEnumerable collection.
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        private static List<Polygon> UnionPolygonsTVGL(this IEnumerable<Polygon> polygons,
            PolygonSimplify polygonSimplify = PolygonSimplify.CanSimplifyOriginal,
            PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles)
        {
            //polygons = polygons.CleanUpForBooleanOperations(polygonSimplify);
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
                        var newPolygons = UnionTVGL(polygonList[i], polygonList[j], interaction, outputAsCollectionType);
                        //Debug.WriteLine("i = {0}, j = {1}", i, j);
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
        }

        /// <summary>
        /// Returns the list of polygons that are the subshapes of ANY of the provided polygons. Notice this is called UnionPolygons here to distinguish
        /// it from the LINQ function Union, which is also a valid extension for any IEnumerable collection.
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> UnionPolygons(this IEnumerable<Polygon> polygons,
            PolygonSimplify polygonSimplify = PolygonSimplify.CanSimplifyOriginal, PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles)
        {
            polygons = polygons.CleanUpForBooleanOperations(polygonSimplify);
#if CLIPPER
            return BooleanViaClipper(PolyFillType.Positive, ClipType.Union, polygons);
            sw.Restart();
            var pClipper = BooleanViaClipper(PolyFillType.Positive, ClipType.Union, polygons);
            sw.Stop();
            var clipTime = sw.Elapsed;

            sw.Restart();
            var pClipper2B = PolygonOperationsV2.BooleanViaClipper(FillRule.Positive, ClipType2.Union, polygons);
            sw.Stop();
            var clipBTime = sw.Elapsed;
            if (Compare(pClipper, pClipper2B, "Union", clipTime, clipBTime))
            {
            }
            return pClipper;
#elif !COMPARE
            return UnionPolygonsTVGL(polygons, polygonSimplify, outputAsCollectionType);
#else
            sw.Restart();
            var pClipper = BooleanViaClipper(PolyFillType.Positive, ClipType.Union, polygons);
            sw.Stop();
            var clipTime = sw.Elapsed;

            sw.Restart();
            var polygonList = UnionPolygonsTVGL(polygons, polygonSimplify, outputAsCollectionType);
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
                    TVGL.IOFunctions.IO.Save(poly, fileNameStart + "." + (i++).ToString() + ".json");
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
        public static List<Polygon> UnionPolygonsTVGL(this IEnumerable<Polygon> polygonsA, IEnumerable<Polygon> polygonsB,
            PolygonSimplify polygonSimplify = PolygonSimplify.CanSimplifyOriginal,
            PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles, double tolerance = double.NaN)
        {
            var pTVGL = polygonsA.ToList();
            if (polygonsB is null) return pTVGL;
            pTVGL.AddRange(polygonsB);
            return UnionPolygonsTVGL(pTVGL, polygonSimplify, outputAsCollectionType);
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
        public static List<Polygon> UnionPolygons(this IEnumerable<Polygon> polygonsA, IEnumerable<Polygon> polygonsB,
            PolygonSimplify polygonSimplify = PolygonSimplify.CanSimplifyOriginal,
            PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles, double tolerance = double.NaN)
        {
            polygonsA = polygonsA.CleanUpForBooleanOperations(polygonSimplify);
            polygonsB = polygonsB?.CleanUpForBooleanOperations(polygonSimplify);
#if CLIPPER
            return BooleanViaClipper(PolyFillType.Positive, ClipType.Union, polygonsA, polygonsB);
            sw.Restart();
            var pClipper = BooleanViaClipper(PolyFillType.Positive, ClipType.Union, polygonsA, polygonsB);
            sw.Stop();
            var clipTime = sw.Elapsed;

            sw.Restart();
            var pClipper2B = PolygonOperationsV2.BooleanViaClipper(FillRule.Positive, ClipType2.Union, polygonsA, polygonsB);
            sw.Stop();
            var clipBTime = sw.Elapsed;
            if (Compare(pClipper, pClipper2B, "Union", clipTime, clipBTime))
            {
            }
            return pClipper;
#elif !COMPARE
            return UnionPolygonsTVGL(polygonsA, polygonsB, polygonSimplify, outputAsCollectionType, tolerance);
#else
            sw.Restart();
            var pClipper = BooleanViaClipper(PolyFillType.Positive, ClipType.Union, polygonsA, polygonsB);
            sw.Stop();
            var clipTime = sw.Elapsed;

            sw.Restart();
            var pTVGL = UnionPolygonsTVGL(polygonsA, polygonsB, polygonSimplify, outputAsCollectionType,tolerance);
            sw.Stop();
            var tvglTime = sw.Elapsed;
            if (Compare(pTVGL, pClipper, "UnionTwoLists", clipTime, tvglTime))
            {
#if PRESENT
                var all = polygonsA.ToList();
                all.AddRange(polygonsB);
                Presenter.ShowAndHang(all);
                Presenter.ShowAndHang(pClipper);
                Presenter.ShowAndHang(pTVGL);
#else
                var fileNameStart = "unionFail" + DateTime.Now.ToOADate().ToString();
                int i = 0;
                foreach (var poly in polygonsA)
                    TVGL.IOFunctions.IO.Save(poly, fileNameStart + "." + (i++).ToString() + "A.json");
                i = 0;
                foreach (var poly in polygonsB)
                    TVGL.IOFunctions.IO.Save(poly, fileNameStart + "." + (i++).ToString() + "B.json");
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
        /// <param name="minAllowableArea">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        private static List<Polygon> IntersectTVGL(this Polygon polygonA, Polygon polygonB,
            PolygonSimplify polygonSimplify = PolygonSimplify.CanSimplifyOriginal,
            PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles, double minAllowableArea = double.NaN)
        {
            //polygonA = polygonA.CleanUpForBooleanOperations(polygonSimplify);
            //polygonB = polygonB?.CleanUpForBooleanOperations(polygonSimplify);

            var relationship = GetPolygonInteraction(polygonA, polygonB);
            return IntersectTVGL(polygonA, polygonB, relationship, outputAsCollectionType, minAllowableArea);
        }
        /// <summary>
        /// Returns the list of polygons that result from the subshapes common to both A and B.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="minAllowableArea">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> Intersect(this Polygon polygonA, Polygon polygonB,
            PolygonSimplify polygonSimplify = PolygonSimplify.CanSimplifyOriginal,
            PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles, double minAllowableArea = double.NaN)
        {
            polygonA = polygonA.CleanUpForBooleanOperations(polygonSimplify);
            polygonB = polygonB?.CleanUpForBooleanOperations(polygonSimplify);

#if CLIPPER
            return BooleanViaClipper(PolyFillType.Positive, ClipType.Intersection, new[] { polygonA }, new[] { polygonB });

            sw.Restart();
            var pClipper = BooleanViaClipper(PolyFillType.Positive, ClipType.Intersection, new[] { polygonA },
                    new[] { polygonB });
            sw.Stop();
            var clipTime = sw.Elapsed;

            sw.Restart();
            var pClipper2B = PolygonOperationsV2.BooleanViaClipper(FillRule.Positive, ClipType2.Intersection, new[] { polygonA },
                    new[] { polygonB });
            var clipBTime = sw.Elapsed;
            if (Compare(pClipper, pClipper2B, "Intersection", clipTime, clipBTime))
            {
            }
            return pClipper;
#elif !COMPARE
            return IntersectTVGL(polygonA, polygonB, polygonSimplify, outputAsCollectionType, minAllowableArea);
#else
            sw.Restart();
            var pClipper = BooleanViaClipper(PolyFillType.Positive, ClipType.Intersection, new[] { polygonA }, new[] { polygonB });
            sw.Stop();
            var clipTime = sw.Elapsed;
            sw.Restart();
            var pTVGL =IntersectTVGL(polygonA, polygonB, polygonSimplify, outputAsCollectionType, tolerance);
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
                TVGL.IOFunctions.IO.Save(polygonA, fileNameStart + "." + "A.json");
                TVGL.IOFunctions.IO.Save(polygonB, fileNameStart + "." + "B.json");
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
        /// <param name="minAllowableArea">The minimum allowable area.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> IntersectTVGL(this Polygon polygonA, Polygon polygonB, PolygonInteractionRecord interaction,
            PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles, double minAllowableArea = double.NaN)
        {
            if (interaction.IntersectionWillBeEmpty())
                return new List<Polygon>();
            else
            {
                polygonIntersection ??= new PolygonIntersection();
                return polygonIntersection.Run(polygonA, polygonB, interaction, outputAsCollectionType, minAllowableArea);
            }
        }


        /// <summary>
        /// Returns the list of polygons that are the sub-shapes of the two collections of polygons. Notice this is called IntersectPolygons here 
        /// to distinguish it from the LINQ function Intersect, which is also a valid extension for any IEnumerable collection.
        /// Notice also that any overlap between the polygons in A or the polygons in B are ignored. Finally, all inputs must be positive.
        /// 
        /// </summary>
        /// <param name="polygonsA">The polygons a.</param>
        /// <param name="polygonsB">The polygons b.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> IntersectTVGL(this Polygon polygonA, IEnumerable<Polygon> polygonsB,
            PolygonSimplify polygonSimplify = PolygonSimplify.CanSimplifyOriginal,
            PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles, double minAllowableArea = double.NaN)
        {
            //polygonA = polygonA.CleanUpForBooleanOperations(polygonSimplify);
            //polygonsB = polygonsB.CleanUpForBooleanOperations(polygonSimplify).ToList();

            var polygonsBList = polygonsB as IList<Polygon> ?? polygonsB.ToList();
            if (polygonsBList.All(p => p.IsPositive))
                return polygonsBList.SelectMany(p => polygonA.IntersectTVGL(p, PolygonSimplify.DoNotSimplify, outputAsCollectionType)).ToList();
            else if (polygonsBList.All(p => !p.IsPositive))
            {
                //errror here!! fix me!!
                var c = new Polygon();
                foreach (var hole in polygonsBList)
                    c.AddInnerPolygon(hole);
                return polygonA.IntersectTVGL(c, PolygonSimplify.DoNotSimplify, outputAsCollectionType, minAllowableArea);
            }
            else throw new ArgumentException("PolgyonsB is a mix of positive and negative polygons, which are not handled by this Intersect function.");
        }

        /// <summary>
        /// Returns the list of polygons that are the sub-shapes of the two collections of polygons. Notice this is called IntersectPolygons here 
        /// to distinguish it from the LINQ function Intersect, which is also a valid extension for any IEnumerable collection.
        /// Notice also that any overlap between the polygons in A or the polygons in B are ignored. Finally, all inputs must be positive.
        /// 
        /// </summary>
        /// <param name="polygonsA">The polygons a.</param>
        /// <param name="polygonsB">The polygons b.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> Intersect(this Polygon polygonA, IEnumerable<Polygon> polygonsB,
            PolygonSimplify polygonSimplify = PolygonSimplify.CanSimplifyOriginal,
            PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles, double minAllowableArea = double.NaN)
        {
            polygonA = polygonA.CleanUpForBooleanOperations(polygonSimplify);
            polygonsB = polygonsB.CleanUpForBooleanOperations(polygonSimplify).ToList();
#if CLIPPER
            return  BooleanViaClipper(PolyFillType.Positive, ClipType.Intersection, new[] { polygonA }, polygonsB);
            sw.Restart();
            var pClipper =  BooleanViaClipper(PolyFillType.Positive, ClipType.Intersection, new[] { polygonA }, polygonsB);
            sw.Stop();
            var clipTime = sw.Elapsed;

            sw.Restart();
            var pClipper2B = PolygonOperationsV2.BooleanViaClipper(FillRule.Positive, ClipType2.Intersection, polygonsA, polygonsB);
            var clipBTime = sw.Elapsed;
            if (Compare(pClipper, pClipper2B, "Intersection", clipTime, clipBTime))
            {
            }
            return pClipper;
#elif !COMPARE
            return IntersectTVGL(polygonA, polygonsB, polygonSimplify, outputAsCollectionType, minAllowableArea);
#else
            sw.Restart();
            var pClipper = BooleanViaClipper(PolyFillType.Positive, ClipType.Intersection, new[] { polygonA }, polygonsB);
            sw.Stop();
            var clipTime = sw.Elapsed;
            sw.Restart();
            var newPolygons=IntersectTVGL(polygonA, polygonsB, polygonSimplify, outputAsCollectionType,minAllowableArea);
            sw.Stop();
            var tvglTime = sw.Elapsed;
            if (Compare(newPolygons, pClipper, "IntersectTwoList", clipTime, tvglTime))
            {
#if PRESENT
                var all = polygonsBList.ToList();
                all.Add(polygonA);
                Presenter.ShowAndHang(all);
                Presenter.ShowAndHang(pClipper);
                Presenter.ShowAndHang(newPolygons);
#else
                var fileNameStart = "intersectFail" + DateTime.Now.ToOADate().ToString();
                int i = 0;
                    TVGL.IOFunctions.IO.Save(polygonA, fileNameStart + "." + (i).ToString() + "A.json");
                               foreach (var poly in polygonsB)
                    TVGL.IOFunctions.IO.Save(poly, fileNameStart + "." + (i++).ToString() + "B.json");
#endif
            }
            return pClipper;
#endif
        }


        /// <summary>
        /// Returns the list of polygons that are the sub-shapes of the two collections of polygons. Notice this is called IntersectPolygons here 
        /// to distinguish it from the LINQ function Intersect, which is also a valid extension for any IEnumerable collection.
        /// Notice also that any overlap between the polygons in A or the polygons in B are ignored. Finally, all inputs must be positive.
        /// 
        /// </summary>
        /// <param name="polygonsA">The polygons a.</param>
        /// <param name="polygonsB">The polygons b.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> IntersectPolygonsTVGL(this IEnumerable<Polygon> polygonsA, IEnumerable<Polygon> polygonsB,
            PolygonSimplify polygonSimplify = PolygonSimplify.CanSimplifyOriginal,
            PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles, double minAllowableArea = double.NaN)
        {
            //polygonsA = polygonsA.CleanUpForBooleanOperations(polygonSimplify);
            //polygonsB = polygonsB.CleanUpForBooleanOperations(polygonSimplify);

            var polygonAList = new List<Polygon>(polygonsA);
            var newPolygons = new List<Polygon>();
            foreach (var polyB in polygonsB)
                foreach (var polyA in polygonAList)
                    newPolygons.AddRange(polyA.IntersectTVGL(polyB, PolygonSimplify.DoNotSimplify, outputAsCollectionType, minAllowableArea));
            return newPolygons;
        }

        /// <summary>
        /// Returns the list of polygons that are the sub-shapes of the two collections of polygons. Notice this is called IntersectPolygons here 
        /// to distinguish it from the LINQ function Intersect, which is also a valid extension for any IEnumerable collection.
        /// Notice also that any overlap between the polygons in A or the polygons in B are ignored. Finally, all inputs must be positive.
        /// 
        /// </summary>
        /// <param name="polygonsA">The polygons a.</param>
        /// <param name="polygonsB">The polygons b.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> IntersectPolygons(this IEnumerable<Polygon> polygonsA, IEnumerable<Polygon> polygonsB,
            PolygonSimplify polygonSimplify = PolygonSimplify.CanSimplifyOriginal,
            PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles, double minAllowableArea = double.NaN)
        {
            polygonsA = polygonsA.CleanUpForBooleanOperations(polygonSimplify);
            polygonsB = polygonsB.CleanUpForBooleanOperations(polygonSimplify);
#if CLIPPER
            return BooleanViaClipper(PolyFillType.Positive, ClipType.Intersection, polygonsA, polygonsB);
            sw.Restart();
            var pClipper = BooleanViaClipper(PolyFillType.Positive, ClipType.Intersection, polygonsA, polygonsB);
            sw.Stop();
            var clipTime = sw.Elapsed;

            sw.Restart();
            var pClipper2B = PolygonOperationsV2.BooleanViaClipper(FillRule.Positive, ClipType2.Intersection, polygonsA, polygonsB);
            var clipBTime = sw.Elapsed;
            if (Compare(pClipper, pClipper2B, "Intersection", clipTime, clipBTime))
            {
            }
            return pClipper;
#elif !COMPARE
            return IntersectPolygonsTVGL(polygonsA, polygonsB, polygonSimplify, outputAsCollectionType, minAllowableArea);
#else
            if (polygonsB is null)
                return UnionPolygons(polygonsA, outputAsCollectionType);
            sw.Restart();
            var pClipper = BooleanViaClipper(PolyFillType.Positive, ClipType.Intersection, polygonsA, polygonsB);
            sw.Stop();
            var clipTime = sw.Elapsed;
            sw.Restart();
            var newPolygons = IntersectPolygonsTVGL(polygonsA, polygonsB, polygonSimplify, outputAsCollectionType,minAllowableArea);
            sw.Stop();
            var tvglTime = sw.Elapsed;
            if (Compare(newPolygons, pClipper, "IntersectTwoList", clipTime, tvglTime))
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
                    TVGL.IOFunctions.IO.Save(poly, fileNameStart + "." + (i++).ToString() + "A.json");
                i = 0;
                foreach (var poly in polygonsA)
                    TVGL.IOFunctions.IO.Save(poly, fileNameStart + "." + (i++).ToString() + "B.json");
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
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> IntersectPolygonsTVGL(this IEnumerable<Polygon> polygons,
            PolygonSimplify polygonSimplify = PolygonSimplify.CanSimplifyOriginal,
            PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles, double minAllowableArea = double.NaN)
        {
            var polygonList = polygons as IList<Polygon> ?? polygons.ToList();

            //polygonList = polygonList.CleanUpForBooleanOperations(polygonSimplify).ToList();

            var result = new List<Polygon>();
            if (!polygonList.Any()) return result;
            else return polygonList[0].IntersectTVGL(polygonList.Skip(1), PolygonSimplify.DoNotSimplify, outputAsCollectionType, minAllowableArea);
        }

        /// <summary>
        /// Returns the list of polygons that are the sub-shapes of ALL of the provided polygons. Notice this is called IntersectPolygons here 
        /// to distinguish it from the LINQ function Intersect, which is also a valid extension for any IEnumerable collection.
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> IntersectPolygons(this IEnumerable<Polygon> polygons,
            PolygonSimplify polygonSimplify = PolygonSimplify.CanSimplifyOriginal,
            PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles, double minAllowableArea = double.NaN)
        {
            var polygonList = polygons as IList<Polygon> ?? polygons.ToList();
            polygonList = polygonList.CleanUpForBooleanOperations(polygonSimplify).ToList();
#if CLIPPER
            return BooleanViaClipper(PolyFillType.Positive, ClipType.Intersection, polygons);
            sw.Restart();
            var pClipper = BooleanViaClipper(PolyFillType.Positive, ClipType.Intersection, polygons);
            sw.Stop();
            var clipTime = sw.Elapsed;

            sw.Restart();
            var pClipper2B = PolygonOperationsV2.BooleanViaClipper(FillRule.Positive, ClipType2.Intersection, polygons);
            var clipBTime = sw.Elapsed;
            if (Compare(pClipper, pClipper2B, "Intersection", clipTime, clipBTime))
            {
            }
            return pClipper;
#elif !COMPARE
            return IntersectPolygonsTVGL(polygons, polygonSimplify, outputAsCollectionType, minAllowableArea);
#else
            sw.Restart();
            var pClipper = BooleanViaClipper(PolyFillType.Positive, ClipType.Intersection, polygons);
            sw.Stop();
            var clipTime = sw.Elapsed;
            sw.Restart();
            var result = IntersectPolygonsTVGL(polygons, polygonSimplify, outputAsCollectionType);
            sw.Stop();
            var tvglTime = sw.Elapsed;
            if (Compare((List<Polygon>)polygonList, pClipper, "IntersectList", clipTime, tvglTime))
            {
#if PRESENT
                Presenter.ShowAndHang(polygons);
                Presenter.ShowAndHang(pClipper);
                Presenter.ShowAndHang(polygonList);
#else
                var fileNameStart = "intersectFail" + DateTime.Now.ToOADate().ToString();
                int i = 0;
                foreach (var poly in polygons)
                    TVGL.IOFunctions.IO.Save(poly, fileNameStart + "." + (i++).ToString() + ".json");
#endif
            }
            return pClipper;
#endif
        }

        #endregion Intersect Public Methods

        #region Direct Access to Clipper API
        public static List<Polygon> BooleanViaClipper(IEnumerable<Polygon> polygonsA, IEnumerable<Polygon> polygonsB, PolyFillType fillType,
            ClipType clipType)
        {
            return BooleanViaClipper(fillType, clipType, polygonsA, polygonsB);
        }

        public static List<Polygon> BooleanViaClipper(Polygon polygonA, Polygon polygonB, PolyFillType fillType, ClipType clipType)
        {
            return BooleanViaClipper(fillType, clipType, new[] { polygonA }, new[] { polygonB });
        }

        public static List<Polygon> BooleanViaClipper(IEnumerable<Polygon> polygonsA, IEnumerable<Polygon> polygonsB, FillRule fillType,
           ClipType2 clipType)
        {
            return PolygonOperationsV2.BooleanViaClipper(fillType, clipType, polygonsA, polygonsB);
        }

        public static List<Polygon> BooleanViaClipper(Polygon polygonA, Polygon polygonB, FillRule fillType, ClipType2 clipType)
        {
            return PolygonOperationsV2.BooleanViaClipper(fillType, clipType, new[] { polygonA }, new[] { polygonB });
        }
        #endregion

        #region Subtract Public Methods

        /// <summary>
        /// Returns the list of polygons that result from A-B (subtracting polygon B from polygon A).
        /// </summary>
        /// <param name="minuend">The polygon a.</param>
        /// <param name="subtrahend">The polygon b.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="minAllowableArea">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> SubtractTVGL(this Polygon minuend, Polygon subtrahend,
            PolygonSimplify polygonSimplify = PolygonSimplify.CanSimplifyOriginal,
                    PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles, double minAllowableArea = double.NaN)
        {
            var polygonBInverted = subtrahend.Copy(true, true);

            //minuend = minuend.CleanUpForBooleanOperations(polygonSimplify);
            //if (polygonSimplify != PolygonSimplify.DoNotSimplify)
            //    polygonBInverted = polygonBInverted.CleanUpForBooleanOperations(PolygonSimplify.CanSimplifyOriginal);

            return IntersectTVGL(minuend, polygonBInverted, PolygonSimplify.DoNotSimplify, outputAsCollectionType, minAllowableArea);
        }

        /// <summary>
        /// Returns the list of polygons that are the sub-shapes of the two collections of polygons. Notice this is called IntersectPolygons here 
        /// to distinguish it from the LINQ function Intersect, which is also a valid extension for any IEnumerable collection.
        /// Notice also that any overlap between the polygons in A or the polygons in B are ignored. Finally, all inputs must be positive.
        /// 
        /// </summary>
        /// <param name="polygonsA">The polygons a.</param>
        /// <param name="subtrahends">The polygons b.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> SubtractTVGL(this Polygon minuend, IEnumerable<Polygon> subtrahends,
            PolygonSimplify polygonSimplify = PolygonSimplify.CanSimplifyOriginal,
            PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles, double minAllowableArea = double.NaN)
        {
            var polygonBInverted = subtrahends.Select(p => p.Copy(true, true));

            //minuend = minuend.CleanUpForBooleanOperations(polygonSimplify);
            //if (polygonSimplify != PolygonSimplify.DoNotSimplify)
            //    polygonBInverted = polygonBInverted.CleanUpForBooleanOperations(PolygonSimplify.CanSimplifyOriginal);

            return IntersectTVGL(minuend, polygonBInverted, PolygonSimplify.DoNotSimplify, outputAsCollectionType, minAllowableArea);
        }
        /// <summary>
        /// Returns the list of polygons that result from A-B (subtracting polygon B from polygon A).
        /// </summary>
        /// <param name="minuend">The polygon a.</param>
        /// <param name="subtrahend">The polygon b.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="minAllowableArea">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> Subtract(this Polygon minuend, Polygon subtrahend,
            PolygonSimplify polygonSimplify = PolygonSimplify.CanSimplifyOriginal,
                    PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles, double minAllowableArea = double.NaN)
        {
            minuend = minuend.CleanUpForBooleanOperations(polygonSimplify);
            subtrahend = subtrahend?.CleanUpForBooleanOperations(polygonSimplify);
#if CLIPPER
            return BooleanViaClipper(PolyFillType.Positive, ClipType.Difference, new[] { minuend },
            new[] { subtrahend });
            sw.Restart();
            var pClipper = BooleanViaClipper(PolyFillType.Positive, ClipType.Difference, new[] { minuend },
                                    new[] { subtrahend });
            sw.Stop();
            var clipTime = sw.Elapsed;

            sw.Restart();
            var pClipper2B = PolygonOperationsV2.BooleanViaClipper(FillRule.Positive, ClipType2.Difference, new[] { minuend },
                                    new[] { subtrahend });
            var clipBTime = sw.Elapsed;
            if (Compare(pClipper, pClipper2B, "Difference", clipTime, clipBTime))
            {
            }
            return pClipper;
#elif !COMPARE
            return SubtractTVGL(minuend, subtrahend, polygonSimplify, outputAsCollectionType, minAllowableArea);
#else
            sw.Restart();
            var pClipper = BooleanViaClipper(PolyFillType.Positive, ClipType.Difference, new[] { minuend },
                                    new[] { subtrahend });
            sw.Stop();
            var clipTime = sw.Elapsed;
            sw.Restart();
            var pTVGL =  SubtractTVGL(minuend, subtrahend, polygonSimplify, outputAsCollectionType, minAllowableArea);
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
                TVGL.IOFunctions.IO.Save(minuend, fileNameStart + "." + "min.json");
                TVGL.IOFunctions.IO.Save(subtrahend, fileNameStart + "." + "sub.json");
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
        public static List<Polygon> SubtractTVGL(this Polygon minuend, Polygon subtrahend, PolygonInteractionRecord interaction,
            PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles, double tolerance = double.NaN)
        {
            interaction = interaction.InvertPolygonInRecord(subtrahend, out var invertedPolygonB);
            return IntersectTVGL(minuend, invertedPolygonB, interaction, outputAsCollectionType, tolerance);
        }


        /// <summary>
        /// Returns the list of polygons that are the sub-shapes of the minuends and that are not part of the subtrahends. 
        /// Notice also that any overlap between the polygons in A or the polygons in B are ignored. Finally, all inputs must be positive.
        /// 
        /// </summary>
        /// <param name="minuends">The polygons a.</param>
        /// <param name="subtrahends">The polygons b.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="minAllowableArea">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> SubtractTVGL(this IEnumerable<Polygon> minuends, IEnumerable<Polygon> subtrahends,
            PolygonSimplify polygonSimplify = PolygonSimplify.CanSimplifyOriginal,
            PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles, double minAllowableArea = double.NaN)
        {
            minuends = minuends.CleanUpForBooleanOperations(polygonSimplify);
            subtrahends = subtrahends?.CleanUpForBooleanOperations(polygonSimplify).ToList();
            var result = new List<Polygon>();
            foreach (var minuend in minuends)
                result.AddRange(minuend.SubtractTVGL(subtrahends, PolygonSimplify.DoNotSimplify, outputAsCollectionType, minAllowableArea));
            return result;
        }



        /// <summary>
        /// Returns the list of polygons that are the sub-shapes of the minuends and that are not part of the subtrahends. 
        /// Notice also that any overlap between the polygons in A or the polygons in B are ignored. Finally, all inputs must be positive.
        /// 
        /// </summary>
        /// <param name="minuends">The polygons a.</param>
        /// <param name="subtrahends">The polygons b.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="minAllowableArea">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> Subtract(this IEnumerable<Polygon> minuends, IEnumerable<Polygon> subtrahends,
            PolygonSimplify polygonSimplify = PolygonSimplify.CanSimplifyOriginal,
            PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles, double minAllowableArea = double.NaN)
        {
            minuends = minuends.CleanUpForBooleanOperations(polygonSimplify);
            subtrahends = subtrahends?.CleanUpForBooleanOperations(polygonSimplify);
#if CLIPPER
            return BooleanViaClipper(PolyFillType.Positive, ClipType.Difference, minuends, subtrahends);
            sw.Restart();
            var pClipper = BooleanViaClipper(PolyFillType.Positive, ClipType.Difference, minuends, subtrahends);
            sw.Stop();
            var clipTime = sw.Elapsed;

            sw.Restart();
            var pClipper2B = PolygonOperationsV2.BooleanViaClipper(FillRule.Positive, ClipType2.Difference, minuends, subtrahends);
            var clipBTime = sw.Elapsed;
            if (Compare(pClipper, pClipper2B, "Difference", clipTime, clipBTime))
            {
            }
            return pClipper;
#elif !COMPARE
            return SubtractTVGL(minuends, subtrahends, polygonSimplify, outputAsCollectionType, minAllowableArea);
#else
            sw.Restart();
            var pClipper = BooleanViaClipper(PolyFillType.Positive, ClipType.Difference, minuends, subtrahends);
            sw.Stop();
            var clipTime = sw.Elapsed;
            sw.Restart();
           var minuendsList = SubtractTVGL(minuends, subtrahends, polygonSimplify, outputAsCollectionType, minAllowableArea);
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
                    TVGL.IOFunctions.IO.Save(poly, fileNameStart + "." + (i++).ToString() + "min.json");
                i = 0;
                foreach (var poly in subtrahends)
                    TVGL.IOFunctions.IO.Save(poly, fileNameStart + "." + (i++).ToString() + "sub.json");
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
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> ExclusiveOr(this Polygon polygonA, Polygon polygonB,
            PolygonSimplify polygonSimplify = PolygonSimplify.CanSimplifyOriginal,
            PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles)
        {
            polygonA = polygonA.CleanUpForBooleanOperations(polygonSimplify);
            polygonB = polygonB?.CleanUpForBooleanOperations(polygonSimplify);
#if CLIPPER
            return BooleanViaClipper(PolyFillType.Positive, ClipType.Xor, new[] { polygonA }, new[] { polygonB });
            sw.Restart();
            var pClipper = BooleanViaClipper(PolyFillType.Positive, ClipType.Xor, new[] { polygonA }, new[] { polygonB });
            sw.Stop();
            var clipTime = sw.Elapsed;

            sw.Restart();
            var pClipper2B = PolygonOperationsV2.BooleanViaClipper(FillRule.Positive, ClipType2.Xor, new[] { polygonA }, new[] { polygonB });
            var clipBTime = sw.Elapsed;
            if (Compare(pClipper, pClipper2B, "Xor", clipTime, clipBTime))
            {
            }
            return pClipper;
#elif !COMPARE
            var relationship = GetPolygonInteraction(polygonA, polygonB);
            return ExclusiveOr(polygonA, polygonB, relationship, outputAsCollectionType);
#else
            sw.Restart();
            var pClipper = BooleanViaClipper(PolyFillType.Positive, ClipType.Xor, new[] { polygonA }, new[] { polygonB });
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
                TVGL.IOFunctions.IO.Save(polygonA, fileNameStart + "." + "A.json");
                TVGL.IOFunctions.IO.Save(polygonB, fileNameStart + "." + "B.json");
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
            else if (interactionRecord.Relationship == PolygonRelationship.BInsideA &&
                !interactionRecord.CoincidentEdges && !interactionRecord.CoincidentVertices)
            {
                var polygonACopy1 = polygonA.Copy(true, false);
                polygonACopy1.AddInnerPolygon(polygonB.Copy(true, true));
                return new List<Polygon> { polygonACopy1 };
            }
            else if (interactionRecord.Relationship == PolygonRelationship.AInsideB &&
                !interactionRecord.CoincidentEdges && !interactionRecord.CoincidentVertices)
            {
                var polygonBCopy2 = polygonB.Copy(true, false);
                polygonBCopy2.AddInnerPolygon(polygonA.Copy(true, true));
                return new List<Polygon> { polygonBCopy2 };
            }
            else
            {
                var result = polygonA.SubtractTVGL(polygonB, interactionRecord, outputAsCollectionType, tolerance);
                result.AddRange(polygonB.SubtractTVGL(polygonA, interactionRecord, outputAsCollectionType, tolerance));
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
        /// <param name="tolerance">The tolerance.</param>
        /// <param name="knownWrongPoints">The known wrong points.</param>
        /// <returns>List&lt;Polygon&gt;.</returns>
        public static List<Polygon> RemoveSelfIntersections(this Polygon polygon, ResultType resultType,
            bool shapeIsOnlyNegative = false)
        {
            var intersections = polygon.GetSelfIntersections().Where(intersect => intersect.Relationship != SegmentRelationship.NoOverlap).ToList();
            if (intersections.Count == 0)
                return new List<Polygon> { polygon };
            polygonRemoveIntersections ??= new PolygonRemoveIntersections();
            return polygonRemoveIntersections.Run(polygon, intersections, resultType, shapeIsOnlyNegative);

        }

        #endregion RemoveSelfIntersections Public Method


    }
}