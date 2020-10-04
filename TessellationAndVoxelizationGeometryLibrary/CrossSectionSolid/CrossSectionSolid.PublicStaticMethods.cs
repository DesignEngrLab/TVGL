// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using System.Collections.Generic;
using System.Linq;
using TVGL.Numerics;
using TVGL.TwoDimensional;

namespace TVGL
{
    public partial class CrossSectionSolid : Solid
    {
        /// <summary>
        /// Creates the uniform cross section solid.
        /// </summary>
        /// <param name="buildDirection">The build direction.</param>
        /// <param name="distanceOfPlane">The distance of plane.</param>
        /// <param name="extrudeThickness">The extrude thickness.</param>
        /// <param name="shape">The shape.</param>
        /// <param name="sameTolerance">The same tolerance.</param>
        /// <param name="units">The units.</param>
        /// <returns>CrossSectionSolid.</returns>
        public static CrossSectionSolid CreatePrismCrossSectionSolid(Vector3 buildDirection, double distanceOfPlane, double extrudeThickness,
            IEnumerable<Polygon> shape, double sameTolerance, UnitType units)
        {
            var shapeList = shape as IList<Polygon> ?? shape.ToList();
            var stepDistances = new Dictionary<int, double> { { 0, distanceOfPlane }, { 1, distanceOfPlane + extrudeThickness } };
            var layers2D = new Dictionary<int, IList<Polygon>> { { 0, shapeList }, { 1, shapeList } };
            return new CrossSectionSolid(buildDirection, stepDistances, sameTolerance, layers2D, null, units);
        }


        /// <summary>
        /// Creates the uniform cross section solid.
        /// </summary>
        /// <param name="buildDirection">The build direction.</param>
        /// <param name="distanceOfPlane">The distance of plane.</param>
        /// <param name="extrudeThickness">The extrude thickness.</param>
        /// <param name="shape">The shape.</param>
        /// <param name="sameTolerance">The same tolerance.</param>
        /// <param name="units">The units.</param>
        /// <returns>CrossSectionSolid.</returns>
        public static CrossSectionSolid CreatePrismCrossSectionSolid(Vector3 buildDirection, double distanceOfPlane, double extrudeThickness,
            Polygon shape, double sameTolerance, UnitType units)
        {
            var stepDistances = new Dictionary<int, double> { { 0, distanceOfPlane }, { 1, distanceOfPlane + extrudeThickness } };
            var layers2D = new Dictionary<int, IList<Polygon>> { { 0, new[] { shape } }, { 1, new[] { shape } } };
            return new CrossSectionSolid(buildDirection, stepDistances, sameTolerance, layers2D, null, units);
        }

        public Vector3[][][] GetCrossSectionsAs3DLoops()
        {
            var result = new Vector3[Layer2D.Count][][];
            int k = 0;
            foreach (var layerKeyValuePair in Layer2D)
            {
                var index = layerKeyValuePair.Key;
                var zValue = StepDistances[index];
                var numLoops = layerKeyValuePair.Value.Count;
                var layer = new Vector3[numLoops][];
                result[k++] = layer;
                for (int j = 0; j < numLoops; j++)
                {
                    var loop = new Vector3[layerKeyValuePair.Value[j].Path.Count];
                    layer[j] = loop;
                    for (int i = 0; i < loop.Length; i++)
                        loop[i] = (new Vector3(layerKeyValuePair.Value[j].Path[i], zValue)).Transform(TransformMatrix);
                }
            }
            return result;
        }

    }
}