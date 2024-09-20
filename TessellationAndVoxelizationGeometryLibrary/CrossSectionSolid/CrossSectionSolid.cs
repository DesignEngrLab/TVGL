// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-14-2023
// ***********************************************************************
// <copyright file="CrossSectionSolid.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;



namespace TVGL
{
    /// <summary>
    /// Class CrossSectionSolid.
    /// Implements the <see cref="TVGL.Solid" />
    /// </summary>
    /// <seealso cref="TVGL.Solid" />
    public partial class CrossSectionSolid : Solid
    {
        /// <summary>
        /// Layers are the 2D polygons for every cross section in this feature.
        /// The solid volume representation assumes that the cross sections are defined,
        /// such that the first valid loop and subsequent loops in  will be extruded
        /// forward along the given direction, but not extruding the last cross section, which
        /// forms the end of the solid.
        /// A few features:
        /// 1) Layer2D can be empty layers at the start and end of the step indices. This
        /// can be useful when joining solids of different sizes along the same direction.
        /// 2) Layer2D can be indexed in the reverse order from the step distances.
        /// This can be useful when working in a bi-directional scope. If reversed, it will
        /// extrude backward from the first valid loop in Layer3D up until the last valid loop
        /// in the list.
        /// </summary>
        [JsonIgnore]
        public List<Polygon>[] Layer2D;

        /// <summary>
        /// Optional Layer2DArea can be used when the Layer2D polygons are unnessary and only the area is needed.
        /// </summary>
        /// <value>The layer2 d area.</value>
        [JsonIgnore]
        public double[] Layer2DArea { get; set; }

        // an alternate approach without using dictionaries could be pursued
        //public List<Polygon>[] Layer2D { get; }

        /// <summary>
        /// Gets the step distances.
        /// </summary>
        /// <value>The step distances.</value>
        /// <font color="red">Badly formed XML comment.</font>
        public double[] StepDistances { get; private set; }

        /// <summary>
        /// This is the direction that the cross sections will be extruded along
        /// </summary>
        /// <value>The direction.</value>
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


        /// <summary>
        /// Gets or sets the number layers.
        /// </summary>
        /// <value>The number layers.</value>
        public int NumLayers { get; set; }


        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="CrossSectionSolid"/> class.
        /// </summary>
        /// <param name="stepDistances">The step distances.</param>
        [JsonConstructor]
        public CrossSectionSolid(double[] stepDistances)
        {
            Layer2D = new List<Polygon>[stepDistances.Length];
            StepDistances = (double[])stepDistances.Clone();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CrossSectionSolid"/> class.
        /// </summary>
        /// <param name="direction">The direction.</param>
        /// <param name="stepDistances">The step distances.</param>
        /// <param name="sameTolerance">The same tolerance.</param>
        /// <param name="bounds">The bounds.</param>
        /// <param name="units">The units.</param>
        public CrossSectionSolid(Vector3 direction, double[] stepDistances, double sameTolerance, Vector3[] bounds = null, UnitType units = UnitType.unspecified)
            : this(stepDistances)
        {
            TransformMatrix = direction.TransformToXYPlane(out var backTransform);
            BackTransform = backTransform;
            NumLayers = stepDistances.Length;
            if (bounds != null)
                Bounds = new[] { bounds[0].Copy(), bounds[1].Copy() };
            Units = units;
            SameTolerance = sameTolerance;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CrossSectionSolid"/> class.
        /// </summary>
        /// <param name="direction">The direction.</param>
        /// <param name="stepDistances">The step distances.</param>
        /// <param name="sameTolerance">The same tolerance.</param>
        /// <param name="Layer2D">The layer2 d.</param>
        /// <param name="bounds">The bounds.</param>
        /// <param name="units">The units.</param>
        public CrossSectionSolid(Vector3 direction, double[] stepDistances, double sameTolerance,
            IEnumerable<IEnumerable<Polygon>> layers, Vector3[] bounds = null, UnitType units = UnitType.unspecified)
        {
            NumLayers = stepDistances.Length;
            StepDistances = stepDistances;
            Units = units;
            SameTolerance = sameTolerance;
            TransformMatrix = direction.TransformToXYPlane(out var backTransform);
            BackTransform = backTransform;
            Layer2D = new List<Polygon>[NumLayers];
            var i = 0;
            foreach (var layer in layers)
                Layer2D[i++] = layer.ToList();

            if (bounds == null)
            {
                var xmin = double.PositiveInfinity;
                var xmax = double.NegativeInfinity;
                var ymin = double.PositiveInfinity;
                var ymax = double.NegativeInfinity;
                foreach (var layer in Layer2D)
                    foreach (var polygon in layer)
                        foreach (var point in polygon.Path)//Okay to ignore inner polygons, since this is just getting the bounds
                        {
                            if (xmin > point.X) xmin = point.X;
                            if (ymin > point.Y) ymin = point.Y;
                            if (xmax < point.X) xmax = point.X;
                            if (ymax < point.Y) ymax = point.Y;
                        }
                Bounds = [new Vector3(xmin, ymin, StepDistances[0]), new Vector3(xmax, ymax, StepDistances[^1])];
            }
            else Bounds = [bounds[0].Copy(), bounds[1].Copy()];
        }
        #endregion

        /// <summary>
        /// Adds the specified feature2 d.
        /// </summary>
        /// <param name="feature2D">The feature2 d.</param>
        /// <param name="layer">The layer.</param>
        public void Add(Polygon feature2D, int layer)
        {
            var polygons = Layer2D[layer];
            if (polygons == null)
                Layer2D[layer] = [feature2D];
            else
                polygons.Add(feature2D);
            _volume = double.NaN;
            _center = Vector3.Null;
            _inertiaTensor = Matrix3x3.Null;
            _surfaceArea = double.NaN;
            if (_firstIndex > layer) _firstIndex = layer;
            if (_lastIndex < layer) _lastIndex = layer;
        }


        /// <summary>
        /// Layer2D and 3D can be indexed in the forward or reverse order from the step distances.
        /// This can be useful when working in a bi-directional scope. In both cases, it will start
        /// by extruding the first valid loop in Layer3D and stop before extruding the last one (the
        /// n-1 extrusion will have ended on at the distance of the final cross section).
        /// If reversed, it will simply extrude backward instead of forward.
        /// </summary>
        /// <param name="extrudeBack">if set to <c>true</c> [extrude back].</param>
        /// <param name="createFullVersion">if set to <c>true</c> [create full version].</param>
        /// <returns>TessellatedSolid.</returns>
        public TessellatedSolid ConvertToTessellatedExtrusions(bool extrudeBack, bool createFullVersion)
        {
            return new TessellatedSolid(ConvertToFaces(extrudeBack), -1, null, TessellatedSolidBuildOptions.Minimal);
        }

        /// <summary>
        /// Converts to faces.
        /// </summary>
        /// <param name="extrudeBack">if set to <c>true</c> [extrude back].</param>
        /// <returns>List&lt;System.ValueTuple&lt;Vector3, Vector3, Vector3&gt;&gt;.</returns>
        private IEnumerable<(Vector3 A, Vector3 B, Vector3 C)> ConvertToFaces(bool extrudeBack)
        {
            var faces = new ConcurrentBag<(Vector3 A, Vector3 B, Vector3 C)>();
            //If extruding back, then we skip the first loop, and extrude backward from the remaining loops.
            //Otherwise, extrude the first loop and all other loops forward, except the last loop.
            //Which of these extrusion options you choose depends on how the cross sections were defined.
            //But both methods, only result in material between the cross sections.
            var start = extrudeBack ? FirstIndex + 1 : FirstIndex;
            var stop = extrudeBack ? LastIndex + 1 : LastIndex;
            //Skip gaps in layer3D, since it may actually represents more than one solid body
            Parallel.For(start, stop, i =>
            {
                var basePlaneDistance = extrudeBack ? StepDistances[i - 1] : StepDistances[i];
                var topPlaneDistance = extrudeBack ? StepDistances[i] : StepDistances[i + 1];
                //Copy polygon to avoid 
                var layerfaces = Layer2D[i].SelectMany(polygon => polygon.Copy(true, false).ExtrusionFaceVectorsFrom2DPolygons(BackTransform.ZBasisVector,
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
        /// <returns>IDictionary&lt;Polygon, List&lt;System.ValueTuple&lt;System.Int32, System.Int32, System.Int32&gt;&gt;&gt;.</returns>
        public IDictionary<Polygon, List<(int A, int B, int C)>> GetTriangulationByLayer()
        {
            var start = FirstIndex + 1;
            var faces = new ConcurrentDictionary<Polygon, List<(int A, int B, int C)>>();
            //Skip gaps in layer3D, since it may actually represents more than one solid body
            Parallel.For(FirstIndex, LastIndex + 1, i =>
            {
                var layer = Layer2D[i];
                foreach (var polygon in layer)
                {
                    var poly = polygon.Copy(true, false);
                    faces.TryAdd(polygon, poly.TriangulateToIndices().ToList());
                }
            });
            return faces;
        }

        /// <summary>
        /// Converts to lofted tessellated solid.
        /// </summary>
        /// <returns>TessellatedSolid.</returns>
        public TessellatedSolid ConvertToLoftedTessellatedSolid()
        {
            var polygons = new Polygon[Layer2D.Length][];
            var faces = new List<TriangleFace>();
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

            return new TessellatedSolid(faces, null, TessellatedSolidBuildOptions.Minimal);
        }

        /// <summary>
        /// Converts to tessellated solid marching cubes.
        /// </summary>
        /// <param name="gridSize">Size of the grid.</param>
        /// <returns>TessellatedSolid.</returns>
        public TessellatedSolid ConvertToTessellatedSolidMarchingCubes(double gridSize)
        {
            var marchingCubesAlgorithm = new MarchingCubesCrossSectionSolid(this, gridSize);
            return marchingCubesAlgorithm.Generate();
        }
        /// <summary>
        /// Converts to tessellated solid marching cubes.
        /// </summary>
        /// <param name="approximateNumberOfTriangles">The approximate number of triangles.</param>
        /// <returns>TessellatedSolid.</returns>
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

        /// <summary>
        /// Copies this instance.
        /// </summary>
        /// <returns>CrossSectionSolid.</returns>
        public CrossSectionSolid Copy()
        {
            var solid = new CrossSectionSolid(Direction, StepDistances, SameTolerance, Bounds, Units);
            solid._firstIndex = FirstIndex;
            solid._lastIndex = LastIndex;
            //Recreate the loops, so that the lists are not linked to the original.
            Layer2D = new List<Polygon>[NumLayers];
            for (int i = FirstIndex; i <= LastIndex; i++)
            {
                solid.Layer2D[i] = new List<Polygon>(Layer2D[i].Count);
                for (int j = 0; j < Layer2D[i].Count; j++)
                    solid.Layer2D[i].Add(Layer2D[i][j].Copy(true, false));
            }
            solid._volume = _volume;
            solid._center = _center;
            solid._inertiaTensor = _inertiaTensor;
            solid._surfaceArea = _surfaceArea;
            return solid;
        }


        /// <summary>
        /// Transforms the specified transform matrix.
        /// </summary>
        /// <param name="transformMatrix">The transform matrix.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public override void Transform(Matrix4x4 transformMatrix)
        {
            //It is really easy to rotate Layer2D, just change the direction. But, it is more complicated to get the transform distances correct.
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets a new solid by transforming its vertices.
        /// </summary>
        /// <param name="transformationMatrix">The transformation matrix.</param>
        /// <returns>Solid.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override Solid TransformToNewSolid(Matrix4x4 transformationMatrix)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets or sets the first index.
        /// </summary>
        /// <value>The first index.</value>
        [JsonProperty]
        private int FirstIndex
        {
            get
            {
                if (_firstIndex < 0)
                    _firstIndex = FindFirstIndex(Layer2D);
                return _firstIndex;
            }
        }

        /// <summary>
        /// Find the first index of the list that is not null and has at least one item.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static int FindFirstIndex<T>(IList<List<T>> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].Count > 0)
                    return i;
            }
            return -1;
        }

        private int _firstIndex = -1;
        /// <summary>
        /// Gets or sets the last index.
        /// </summary>
        /// <value>The last index.</value>
        [JsonProperty]
        private int LastIndex
        {
            get
            {
                if (_lastIndex < 0)
                    _lastIndex = FindLastIndex(Layer2D);
                return _lastIndex;
            }
        }

        /// <summary>
        /// Find the last index of the list that is not null and has at least one item.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static int FindLastIndex<T>(IList<List<T>> list)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] != null && list[i].Count > 0)
                    return i;
            }
            return -1;
        }

        private int _lastIndex = -1;

        /// <summary>
        /// Called when [serializing method].
        /// </summary>
        /// <param name="context">The context.</param>
        [OnSerializing]
        protected void OnSerializingMethod(StreamingContext context)
        {
            var layersAs1DCollections = new List<IEnumerable<IEnumerable<double>>>();
            for (int i = FirstIndex; i <= LastIndex; i++)
                layersAs1DCollections.Add(Layer2D[i].SelectMany(p => p.AllPolygons).Select(p => p.Path.ConvertTo1DDoublesCollection()));

            serializationData = new Dictionary<string, JToken>
            {
                { "CrossSections", JToken.FromObject(layersAs1DCollections) }
            };
        }

        /// <summary>
        /// Called when [deserialized method].
        /// </summary>
        /// <param name="context">The context.</param>
        [OnDeserialized]
        protected void OnDeserializedMethod(StreamingContext context)
        {
            JArray jArray = (JArray)serializationData["CrossSections"];
            var layerArray = jArray.ToObject<double[][][]>();
            Layer2D = new List<Polygon>[NumLayers];
            var j = 0;
            for (int i = FirstIndex; i <= LastIndex; i++)
            {
                var layer = new List<Polygon>();
                foreach (var coordinates in layerArray[j])
                    layer.Add(new Polygon(coordinates.ConvertToVector2s()));
                Layer2D[i] = layer.CreateShallowPolygonTrees(true);
                j++;
            }
        }

        /// <summary>
        /// Calculates the center.
        /// </summary>
        protected override void CalculateCenter()
        {
            var xCenter = 0.0;
            var yCenter = 0.0;
            var zCenter = 0.0;
            var totalArea = 0.0;
            for (int index = 0; index < StepDistances.Length; index++)
            {
                var distance = StepDistances[index];
                var layer2D = Layer2D[index];
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

        /// <summary>
        /// Calculates the volume.
        /// </summary>
        protected override void CalculateVolume()
        {
            _volume = 0.0;
            var prevDistance = StepDistances[0];
            var layer2D = Layer2D[0];
            var prevArea = layer2D == null || layer2D.Count == 0 ? 0.0 : layer2D.Sum(p => p.Area);
            for (int index = 1; index < StepDistances.Length; index++)
            {
                var distance = StepDistances[index];
                layer2D = Layer2D[index];
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

        /// <summary>
        /// Calculates the surface area.
        /// </summary>
        protected override void CalculateSurfaceArea()
        {
            // this is probably not correct. I simply took the code for CalculateVolume and changed
            // polygon area to polygon perimeter.
            var area = 0.0;
            var prevDistance = StepDistances[0];
            var layer2D = Layer2D[0];
            var prevPerimeter = layer2D == null || layer2D.Count == 0 ? 0.0 : layer2D.Sum(p => p.Perimeter);
            for (int index = 0; index < StepDistances.Length; index++)
            {
                var distance = StepDistances[index];
                layer2D = Layer2D[index];
                var perimeter = layer2D == null || layer2D.Count == 0 ? 0.0 : layer2D.Sum(p => p.Perimeter);
                area += (prevPerimeter + perimeter) * (distance - prevDistance);
                prevPerimeter = perimeter;
                prevDistance = distance;
            }
            area *= 0.5;
            //
            if (area < 0) area = -area;
        }

        /// <summary>
        /// Calculates the inertia tensor.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        protected override void CalculateInertiaTensor()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the total polygon vertices.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public int GetTotalPolygonVertices()
        {
            return Layer2D
                .Sum(layer => layer
                .Sum(outerP => outerP.AllPolygons
                .Sum(p => p.Vertices.Count)));
        }

        /// <summary>
        /// Checks for missing layers.
        /// </summary>
        public void CheckForMissingLayers()
        {
            for (var i = FirstIndex; i <= LastIndex; i++) //Including the last index
            {
                var layer = Layer2D[i];
                if (layer != null) continue;
                else
                {
                    Layer2D[i] = Layer2D[i - 1].Select(p => p.Copy(true, false)).ToList();//make same as the prior cross section
                    //throw new Exception();
                }
            }
        }
    }
}
