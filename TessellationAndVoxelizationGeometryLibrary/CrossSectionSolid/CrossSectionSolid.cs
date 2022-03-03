// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TVGL.Numerics;
using TVGL.TwoDimensional;

namespace TVGL
{
    public partial class CrossSectionSolid : Solid
    {
        /// <summary>
        /// Layers are the 2D polygons for every cross section in this feature.
        /// The solid volume representation assumes that the cross sections are defined,
        /// such that the first valid loop and subsequent loops in  will be extruded 
        /// forward along the given direction, but not extruding the last cross section, which
        /// forms the end of the solid. 
        /// 
        /// A few features:
        /// 1) Layer2D can be empty layers at the start and end of the step indices. This 
        /// can be useful when joining solids of different sizes along the same direction.
        /// 2) Layer2D can be indexed in the reverse order from the step distances.
        /// This can be useful when working in a bi-directional scope. If reversed, it will
        /// extrude backward from the first valid loop in Layer3D up until the last valid loop
        /// in the list.
        /// </summary>
        [JsonIgnore]
        public IDictionary<int, IList<Polygon>> Layer2D;

        /// <summary>
        /// Optional Layer2DArea can be used when the Layer2D polygons are unnessary and only the area is needed.
        /// </summary>
        [JsonIgnore]
        public IDictionary<int, double> Layer2DArea { get; set; }

        // an alternate approach without using dictionaries could be pursued
        //public List<Polygon>[] Layer2D { get; }

        /// <summary>
        /// Step distances stores the distance along direction for each index.
        /// It can be bigger than either of the above dictionaries if, for example,
        /// you wanted to define multiple ParallelCrossSectionSolids along the same direction.
        /// <summary>
        public Dictionary<int, double> StepDistances { get; private set; }
        // an alternate approach without using dictionaries should be pursued
        //public Vector2 StepDistances { get; }
        /// <summary>
        /// This is the direction that the cross sections will be extruded along
        /// </summary>
        public Vector3 Direction => BackTransform.ZBasisVector;

        /// <summary>
        /// Gets or sets the transform matrix.
        /// </summary>
        /// <value>The transform matrix.</value>
        public Matrix4x4 TransformMatrix { get; set; }


        /// <summary>
        /// Gets or sets the transform matrix.
        /// </summary>
        /// <value>The transform matrix.</value>
        public Matrix4x4 BackTransform { get; set; }


        public int NumLayers { get; set; }


        #region Constructors
        [JsonConstructor]
        public CrossSectionSolid(Dictionary<int, double> stepDistances)
        {
            Layer2D = new Dictionary<int, IList<Polygon>>();
            StepDistances = stepDistances;
        }

        public CrossSectionSolid(Vector3 direction, Dictionary<int, double> stepDistances, double sameTolerance, Vector3[] bounds = null, UnitType units = UnitType.unspecified)
            : this(stepDistances)
        {
            TransformMatrix = direction.TransformToXYPlane(out var backTransform);
            BackTransform = backTransform;
            NumLayers = stepDistances.Count;
            if (bounds != null)
                Bounds = new[] { bounds[0].Copy(), bounds[1].Copy() };
            Units = units;
            SameTolerance = sameTolerance;
        }

        public CrossSectionSolid(Vector3 direction, Dictionary<int, double> stepDistances, double sameTolerance,
            IDictionary<int, IList<Polygon>> Layer2D, Vector3[] bounds = null, UnitType units = UnitType.unspecified)
        {
            NumLayers = stepDistances.Count;
            StepDistances = stepDistances;
            Units = units;
            SameTolerance = sameTolerance;
            TransformMatrix = direction.TransformToXYPlane(out var backTransform);
            BackTransform = backTransform;
            this.Layer2D = Layer2D;
            if (bounds == null)
            {
                var xmin = double.PositiveInfinity;
                var xmax = double.NegativeInfinity;
                var ymin = double.PositiveInfinity;
                var ymax = double.NegativeInfinity;
                foreach (var layer in Layer2D)
                    foreach (var polygon in layer.Value)
                        foreach (var point in polygon.Path)//Okay to ignore inner polygons, since this is just getting the bounds
                        {
                            if (xmin > point.X) xmin = point.X;
                            if (ymin > point.Y) ymin = point.Y;
                            if (xmax < point.X) xmax = point.X;
                            if (ymax < point.Y) ymax = point.Y;
                        }
                Bounds = new[] {
                new Vector3(xmin, ymin, StepDistances[0]),
                new Vector3(xmax, ymax, StepDistances[NumLayers-1])
                };
            }
            else Bounds = new[] { bounds[0].Copy(), bounds[1].Copy() };
        }
        #endregion

        /// <summary>
        /// Adds the specified feature2 d.
        /// </summary>
        /// <param name="feature2D">The feature2 d.</param>
        /// <param name="layer">The layer.</param>
        public void Add(Polygon feature2D, int layer)
        {
            if (!Layer2D.TryGetValue(layer, out var polygons))
                Layer2D.Add(layer, new List<Polygon>());
            polygons.Add(feature2D);
            _volume = double.NaN;
            _center = Vector3.Null;
            _inertiaTensor = Matrix3x3.Null;
            _surfaceArea = double.NaN;
        }


        /// <summary>
        /// Layer2D and 3D can be indexed in the forward or reverse order from the step distances.
        /// This can be useful when working in a bi-directional scope. In both cases, it will start 
        /// by extruding the first valid loop in Layer3D and stop before extruding the last one (the 
        /// n-1 extrusion will have ended on at the distance of the final cross section). 
        /// If reversed, it will simply extrude backward instead of forward.
        /// </summary>
        public TessellatedSolid ConvertToTessellatedExtrusions(bool extrudeBack, bool createFullVersion)
        {
            var faces = new List<PolygonalFace>();
            var facesAsTuples = ConvertToFaces(extrudeBack);
            foreach (var face in facesAsTuples)
            {
                faces.Add(new PolygonalFace(new Vertex(face.A), new Vertex(face.B), new Vertex(face.C)));
            }
            return new TessellatedSolid(faces, createFullVersion, false);
        }

        public List<(Vector3 A, Vector3 B, Vector3 C)> ConvertToFaces(bool extrudeBack)
        {
            //if (!Layer3D.Any()) SetAllVertices();
            var start = Layer2D.FirstOrDefault(p => p.Value.Count > 0).Key;
            var stop = Layer2D.LastOrDefault(p => p.Value.Count > 0).Key;
            var increment = start < stop ? 1 : -1;
            var faces = new ConcurrentBag<(Vector3 A, Vector3 B, Vector3 C)>();
            //If extruding back, then we skip the first loop, and extrude backward from the remaining loops.
            //Otherwise, extrude the first loop and all other loops forward, except the last loop.
            //Which of these extrusion options you choose depends on how the cross sections were defined.
            //But both methods, only result in material between the cross sections.
            if (extrudeBack)
            {
                start += increment;
            }
            else stop -= increment;
            //Skip gaps in layer3D, since it may actually represents more than one solid body
            Parallel.ForEach(Layer2D, layer =>
            //foreach (var layer in Layer2D.Where(p => p.Value.Any()))
            {
                var i = layer.Key;
                //Skip layers outside of the start and stop bounds. This is necessary because of the increment
                if (i * increment < start * increment || i * increment > stop * increment) return;
                var basePlaneDistance = extrudeBack ? StepDistances[i - increment] : StepDistances[i];
                var topPlaneDistance = extrudeBack ? StepDistances[i] : StepDistances[i + increment];
                //Copy polygon to avoid 
                var layerfaces = layer.Value.SelectMany(polygon => polygon.Copy(true, false).ExtrusionFaceVectorsFrom2DPolygons(BackTransform.ZBasisVector,
                    basePlaneDistance, topPlaneDistance - basePlaneDistance)).ToList();
                foreach (var face in layerfaces) faces.Add(face);
            }
            );
            return faces.ToList();
        }

        /// <summary>
        /// Returns a list of resulting polygon triangulations. Each (int A, int B, int C) represents a triangle. There is a list of 
        /// these triangles for each polgyon in the solid.
        /// </summary>
        /// <param name="extrudeBack"></param>
        /// <returns></returns>
        public IDictionary<Polygon, List<(int A, int B, int C)>> GetTriangulationByLayer()
        {
            //if (!Layer3D.Any()) SetAllVertices();
            var start = Layer2D.FirstOrDefault(p => p.Value.Count > 0).Key;
            var stop = Layer2D.LastOrDefault(p => p.Value.Count > 0).Key;
            var increment = start < stop ? 1 : -1;
            start += increment;
            var faces = new ConcurrentDictionary<Polygon, List<(int A, int B, int C)>>();
            //Skip gaps in layer3D, since it may actually represents more than one solid body
            Parallel.ForEach(Layer2D.Where(p => p.Value.Any()), layer =>
            {
                var i = layer.Key;
                //Skip layers outside of the start and stop bounds. This is necessary because of the increment
                if (i * increment < start * increment || i * increment > stop * increment) return;
                foreach (var polygon in layer.Value)
                {
                    var poly = new Polygon(polygon.Path);//Create a copy to avoid complex locking / multi-threading issues 
                    faces.TryAdd(polygon, poly.TriangulateToIndices().ToList());
                }
            });
            return faces;
        }

        public TessellatedSolid ConvertToLoftedTessellatedSolid()
        {
            var polygons = new Polygon[Layer2D.Count][];
            var faces = new List<PolygonalFace>();
            var previousLayerWasEmpty = true;
            var i = 0;
            foreach (var layer in Layer2D)
            {
                /*
                if (layer.Value == null)
                {
                    if (!previousLayerWasEmpty) //then need to triangulate the last layer facing up
                    {
                        polygons[i-1].tri
                    }
                    previousLayerWasEmpty = true;
                }
                else
                {
                    polygons[i] = layer.Value.CreateShallowPolygonTrees();
                    previousKey = layer.Key;
                    if (previousLayerWasEmpty) // then need to triangulate this layer facing down
                    {

                    }
                    previousLayerWasEmpty = false;
                }
                */
                i++;
            }

            return new TessellatedSolid(faces, false, false);
        }

        public TessellatedSolid ConvertToTessellatedSolidMarchingCubes(double gridSize)
        {
            var marchingCubesAlgorithm = new MarchingCubesCrossSectionSolid(this, gridSize);
            return marchingCubesAlgorithm.Generate();
        }
        public TessellatedSolid ConvertToTessellatedSolidMarchingCubes(int approximateNumberOfTriangles = -1)
        {
            MarchingCubesCrossSectionSolid marchingCubesAlgorithm;
            if (approximateNumberOfTriangles == -1)
                marchingCubesAlgorithm = new MarchingCubesCrossSectionSolid(this);
            else
            {
                var solidDimensions = Bounds[1] - Bounds[0];
                var bbVolume = solidDimensions.X * solidDimensions.Y * solidDimensions.Z;
                var biggestSideArea = bbVolume / Math.Min(solidDimensions.X, Math.Min(solidDimensions.Y, solidDimensions.Z));
                var areaPerTriangle = biggestSideArea / (MarchingCubesCrossSectionSolid.NumTrianglesOnSideFactor * approximateNumberOfTriangles);
                var discretization = 2 * Math.Sqrt(areaPerTriangle);
                marchingCubesAlgorithm = new MarchingCubesCrossSectionSolid(this, discretization);
            }
            return marchingCubesAlgorithm.Generate();
        }

        public CrossSectionSolid Copy()
        {
            var solid = new CrossSectionSolid(Direction, StepDistances, SameTolerance, Bounds, Units);
            //Recreate the loops, so that the lists are not linked to the original.
            foreach (var layer in Layer2D)
            {
                solid.Layer2D.TryAdd(layer.Key, new List<Polygon>(layer.Value));
            }
            solid._volume = _volume;
            solid._center = _center;
            solid._inertiaTensor = _inertiaTensor;
            solid._surfaceArea = _surfaceArea;
            return solid;
        }


        public override void Transform(Matrix4x4 transformMatrix)
        {
            //It is really easy to rotate Layer2D, just change the direction. But, it is more complicated to get the transform distances correct.
            throw new NotImplementedException();
        }

        public override Solid TransformToNewSolid(Matrix4x4 transformationMatrix)
        {
            throw new NotImplementedException();
        }

        [JsonProperty]
        private int FirstIndex { get; set; }
        [JsonProperty]
        private int LastIndex { get; set; }


        [OnSerializing]
        protected void OnSerializingMethod(StreamingContext context)
        {
            serializationData = new Dictionary<string, JToken>();
            serializationData.Add("CrossSections",
                JToken.FromObject(Layer2D.Values.Select(polygonlist => polygonlist.Select(p => p.Path.ConvertTo1DDoublesCollection()))));
            FirstIndex = Layer2D.Keys.First();
            LastIndex = Layer2D.Keys.Last();
        }

        [OnDeserialized]
        protected void OnDeserializedMethod(StreamingContext context)
        {
            JArray jArray = (JArray)serializationData["CrossSections"];
            var layerArray = jArray.ToObject<double[][][]>();
            Layer2D = new Dictionary<int, IList<Polygon>>();
            var j = 0;
            for (int i = FirstIndex; i <= LastIndex; i++)
            {
                var layer = new List<Polygon>();
                foreach (var coordinates in layerArray[j])
                    layer.Add(new Polygon(coordinates.ConvertToVector2s().ToList()));
                Layer2D.Add(i, layer);
                j++;
            }
        }

        protected override void CalculateCenter()
        {
            var xCenter = 0.0;
            var yCenter = 0.0;
            var zCenter = 0.0;
            var totalArea = 0.0;
            foreach (var stepDistanceKVP in StepDistances)  // skip the first, this is shown above.
            {
                var index = stepDistanceKVP.Key;
                var distance = stepDistanceKVP.Value;
                if (!Layer2D.TryGetValue(index, out var layer2D)) continue;
                if (layer2D == null || layer2D.Count == 0) continue;
                foreach (var polygon in layer2D)
                {
                    Vector2 c = polygon.Centroid;
                    var area = polygon.Area;
                    totalArea += area;
                    xCenter += area * c.X;
                    yCenter += area * c.Y;
                    zCenter += area * distance;
                }
            }
            _center = (new Vector3(xCenter, yCenter, zCenter) / totalArea).Multiply(BackTransform);
        }

        protected override void CalculateVolume()
        {
            _volume = 0.0;
            var index = StepDistances.Keys.First();
            var prevDistance = StepDistances.Values.First();
            Layer2D.TryGetValue(index, out var layer2D);
            var prevArea = layer2D == null || layer2D.Count == 0 ? 0.0 : layer2D.Sum(p => p.Area);
            foreach (var stepDistanceKVP in StepDistances.Skip(1))  // skip the first, this is shown above.
            {
                index = stepDistanceKVP.Key;
                var distance = stepDistanceKVP.Value;
                Layer2D.TryGetValue(index, out layer2D);
                var area = layer2D == null || layer2D.Count == 0 ? 0.0 : layer2D.Sum(p => p.Area);
                _volume += (prevArea + area) * (distance - prevDistance);
                prevArea = area;
                prevDistance = distance;
            }
            _volume *= 0.5; //actually, this is the trapezoidal volume between every layer. It's subtle, but since
            // we sum the area (5 lines above) instead of averaging them (to get the trapezoid area), we simply
            // divide by 2 at the end for simplicity/efficiency/accuracy.
            if (_volume < 0) _volume = -_volume;
        }

        protected override void CalculateSurfaceArea()
        {
            // this is probably not correct. I simply took the code for CalculateVolume and changed
            // polygon area to polygon perimeter.
            var area = 0.0;
            var index = StepDistances.Keys.First();
            var prevDistance = StepDistances.Values.First();
            Layer2D.TryGetValue(index, out var layer2D);
            var prevPerimeter = layer2D == null || layer2D.Count == 0 ? 0.0 : layer2D.Sum(p => p.Perimeter);
            foreach (var stepDistanceKVP in StepDistances.Skip(1))  // skip the first, this is shown above.
            {
                index = stepDistanceKVP.Key;
                var distance = stepDistanceKVP.Value;
                Layer2D.TryGetValue(index, out layer2D);
                var perimeter = layer2D == null || layer2D.Count == 0 ? 0.0 : layer2D.Sum(p => p.Perimeter);
                area += (prevPerimeter + perimeter) * (distance - prevDistance);
                prevPerimeter = perimeter;
                prevDistance = distance;
            }
            area *= 0.5;
            //
            if (area < 0) area = -area;
        }

        protected override void CalculateInertiaTensor()
        {
            throw new NotImplementedException();
        }

        public int GetTotalPolygonVertices()
        {
            return Layer2D
                .Sum(layer => layer.Value
                .Sum(outerP => outerP.AllPolygons
                .Sum(p => p.Vertices.Count)));
        }

        public void CheckForMissingLayers()
        {
            for (var i = Layer2D.Keys.Min(); i <= Layer2D.Keys.Max(); i++) //Including the last index
            {
                if (Layer2D.ContainsKey(i)) continue;
                else
                {
                    Layer2D[i] = Layer2D[i - 1].Select(p => p.Copy(true, false)).ToList();//make same as the prior cross section
                    //throw new Exception();
                }
            }
        }
    }
}
