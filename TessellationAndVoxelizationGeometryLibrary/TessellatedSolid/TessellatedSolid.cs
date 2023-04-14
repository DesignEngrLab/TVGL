// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace TVGL
{
    /// <summary>
    ///     Class TessellatedSolid.
    /// </summary>
    /// <tags>help</tags>
    /// <remarks>
    ///     This is the currently the <strong>main</strong> class within TVGL all filetypes are read in as a TessellatedSolid,
    ///     and
    ///     all interesting operations work on the TessellatedSolid.
    /// </remarks>
    public partial class TessellatedSolid : Solid
    {
        #region Fields and Properties
        public double TessellationError { get; set; } = Constants.DefaultTessellationError;

        public double TessellationMaxAngleError { get; set; } = Constants.DefaultTessellationMaxAngleError;

        /// <summary>
        ///     Gets the faces.
        /// </summary>
        /// <value>The faces.</value>
        [JsonIgnore]
        public TriangleFace[] Faces { get; set; }

        /// <summary>
        ///     Gets the edges.
        /// </summary>
        /// <value>The edges.</value>
        [JsonIgnore]
        public Edge[] Edges
        {
            get
            {
                if (_edges == null) MakeEdges();
                return _edges;
            }
        }
        private Edge[] _edges;

        /// <summary>
        ///     Gets the vertices.
        /// </summary>
        /// <value>The vertices.</value>
        [JsonIgnore]
        public Vertex[] Vertices { get; set; }

        /// <summary>
        ///     Gets the number of faces.
        /// </summary>
        /// <value>The number of faces.</value>
        [JsonIgnore]
        public int NumberOfFaces { get; set; }

        /// <summary>
        ///     Gets the number of vertices.
        /// </summary>
        /// <value>The number of vertices.</value>
        [JsonIgnore]
        public int NumberOfVertices { get; set; }

        /// <summary>
        ///     Gets the number of edges.
        /// </summary>
        /// <value>The number of edges.</value>
        [JsonIgnore]
        public int NumberOfEdges { get; private set; }

        /// <summary>
        ///     Gets the number of primitives. Must be set after completing primitive definition/combination.
        /// </summary>
        /// <value>The number of faces.</value>
        [JsonIgnore]
        public int NumberOfPrimitives { get; set; }

        /// <summary>
        ///     Errors in the tesselated solid
        /// </summary>
        [JsonIgnore]
        public TessellationError Errors { get; internal set; }

        /// <summary>
        /// Gets or sets the nonsmooth edges, which are the edges that do not exhibit C1 or C2 continuity.
        /// </summary>
        /// <value>The nonsmooth edges.</value>
        [JsonIgnore]
        public List<EdgePath> NonsmoothEdges { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TessellatedSolid"/> class.
        /// </summary>
        public TessellatedSolid() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TessellatedSolid" /> class. This is the one that
        /// matches with the STL format.
        /// </summary>
        /// <param name="vertsPerFace">The verts per face.</param>
        /// <param name="createFullVersion">if set to <c>true</c> [make edges].</param>
        /// <param name="colors">The colors.</param>
        /// <param name="units">The units.</param>
        /// <param name="name">The name.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="comments">The comments.</param>
        /// <param name="language">The language.</param>
        public TessellatedSolid(IEnumerable<List<Vector3>> vertsPerFace, bool createFullVersion, IList<Color> colors,
            UnitType units = UnitType.unspecified, string name = "", string filename = "", List<string> comments = null,
            string language = "")
            : base(units, name, filename, comments, language)
        {
            var vertsPerFaceList = vertsPerFace as IList<List<Vector3>> ?? vertsPerFace.ToList();
            DefineAxisAlignedBoundingBoxAndTolerance(vertsPerFaceList.SelectMany(v => v));
            var scaleFactor = 1.0;
            if ((Bounds[1] - Bounds[0]).Length() < 0.1)
            {
                if (units == UnitType.unspecified || units == UnitType.meter)
                {
                    units = UnitType.millimeter;
                    scaleFactor = 1000;
                }
                else if (units == UnitType.foot)
                {
                    units = UnitType.inch;
                    scaleFactor = 12;
                }
                DefineAxisAlignedBoundingBoxAndTolerance(vertsPerFaceList.SelectMany(vList => vList.Select(v => scaleFactor * v)));
            }
            MakeVertices(vertsPerFaceList, scaleFactor, out List<int[]> faceToVertexIndices);
            //Complete Construction with Common Functions
            MakeFaces(faceToVertexIndices, colors);
            if (createFullVersion) CompleteInitiation(true);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TessellatedSolid" /> class. This matches with formats
        /// that use indices to the vertices (almost everything except STL).
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="faceToVertexIndices">The face to vertex indices.</param>
        /// <param name="createFullVersion">if set to <c>true</c> [make edges].</param>
        /// <param name="colors">The colors.</param>
        /// <param name="units">The units.</param>
        /// <param name="name">The name.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="comments">The comments.</param>
        /// <param name="language">The language.</param>
        public TessellatedSolid(IList<Vector3> vertices, IList<int[]> faceToVertexIndices, bool createFullVersion,
            IList<Color> colors, UnitType units = UnitType.unspecified, string name = "", string filename = "",
            List<string> comments = null, string language = "") : base(units, name, filename, comments, language)
        {
            DefineAxisAlignedBoundingBoxAndTolerance(vertices);
            MakeVertices(vertices);
            //Complete Construction with Common Functions
            MakeFaces(faceToVertexIndices, colors);
            if (createFullVersion) CompleteInitiation();
        }

        public TessellatedSolid(IList<Vector3> vertices, IList<int[]> faceToVertexIndices, bool createFullVersion,
          IList<PrimitiveSurface> primitives, IList<Color> colors, UnitType units = UnitType.unspecified, string name = "", string filename = "",
          List<string> comments = null, string language = "") : base(units, name, filename, comments, language)
        {
            DefineAxisAlignedBoundingBoxAndTolerance(vertices);
            MakeVertices(vertices);
            //Complete Construction with Common Functions
            MakeFaces(faceToVertexIndices, primitives, colors);
            if (createFullVersion)
            {
                CompleteInitiation();
                //Create edges and then update primitives with links to Faces, Vertices, and Edges
                foreach (var prim in Primitives)
                    prim.CompletePostSerialization(this);
            }
            else CalculateVolume();
        }

        public TessellatedSolid(Vertex[] vertices, List<PrimitiveSurface> primitives, UnitType units = UnitType.unspecified, string name = "", string filename = "",
            List<string> comments = null, string language = "") : base(units, name, filename, comments, language)
        {
            //Set the list of vertices and primitives directly to reduce garbage
            Vertices = vertices;
            Primitives = primitives;

            //Make the faces from the primitives.
            MakeFacesFromPrimitives();

            //Set the volume and convex hull vertices
            CalculateVolume();
            ConvexHull = new TVGLConvexHull(Vertices, SameTolerance);
        }

        [OnSerializing]
        protected void OnSerializingMethod(StreamingContext context)
        {
            //if (serializationData == null)
            serializationData = new Dictionary<string, JToken>();
            //Don't bother storing the convex hull, so we can keep the size down as much as possible. CVXHull is fast to recalculate.
            serializationData.Add("FaceIndices",
                JToken.FromObject(Faces.SelectMany(face => face.Vertices.Select(v => v.IndexInList)).ToArray()));
            serializationData.Add("VertexCoords",
               JToken.FromObject(Vertices.ConvertTo1DDoublesCollection()));
            if (HasUniformColor || Faces.All(f => f.Color == null || f.Color.Equals(Faces[0].Color)))
                serializationData.Add("Colors", SolidColor.ToString());
            else
            {
                var colorList = new List<string>();
                var lastColor = new Color(KnownColors.LightGray).ToString();
                foreach (var f in Faces)
                {
                    if (f.Color != null) lastColor = f.Color.ToString();
                    colorList.Add(lastColor);
                }
                serializationData.Add("Colors", JToken.FromObject(colorList));
            }
        }

        private const string comma = ",";
        /// <summary>
        /// Stream writes the JSON structure in reverse order of how it will be read in.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="index"></param>
        public void StreamWrite(JsonTextWriter writer, int index)
        {
            writer.WritePropertyName("Name");
            writer.WriteValue(Name);

            writer.WritePropertyName("Index");
            writer.WriteValue(index);

            writer.WritePropertyName("Volume");
            writer.WriteValue(Volume);

            writer.WritePropertyName("SurfaceArea");
            writer.WriteValue(SurfaceArea);

            writer.WritePropertyName("NumberOfVertices");
            writer.WriteValue(NumberOfVertices);

            writer.WritePropertyName("NumberOfFaces");
            writer.WriteValue(NumberOfFaces);

            writer.WritePropertyName("NumberOfPrimitives");
            writer.WriteValue(NumberOfPrimitives);

            writer.WritePropertyName("VertexCoords");
            writer.WriteStartArray();
            {
                foreach (var vertex in Vertices)
                {
                    writer.WriteValue(vertex.X);
                    writer.WriteValue(vertex.Y);
                    writer.WriteValue(vertex.Z);
                }
            }
            writer.WriteEndArray();

            writer.WritePropertyName("FaceIndices");
            writer.WriteStartArray();//[
            {
                foreach (var face in Faces)
                    foreach (var vertex in face.Vertices)
                        writer.WriteValue(vertex.IndexInList);
            }
            writer.WriteEndArray();//]

            var i = 0;
            writer.WritePropertyName("Primitives");
            //Write the primitives as one large object with sub-objects.
            //This will be easier to read in than an array, because the primitives
            //don't serialize a type automatically and so we know how to cast it when reading
            //before actually reading it.
            writer.WriteStartObject();//{
            {
                foreach (var primitive in Primitives)
                {
                    //Name the primitive as its type plus an index. This will make it unique, which is 
                    //required for JSON.
                    var type = primitive.GetType().ToString().Substring(5);//plane
                    writer.WritePropertyName(type + "_" + i++);
                    writer.WriteRawValue(JsonConvert.SerializeObject(primitive, Formatting.None));
                }
            }
            writer.WriteEndObject();//}
        }

        public void StreamRead(JsonTextReader reader, out int index)
        {
            index = -1;
            var jsonSerializer = new Newtonsoft.Json.JsonSerializer();
            reader.Read();
            while (reader.TokenType != JsonToken.EndObject)
            {
                if (reader.TokenType != JsonToken.PropertyName)
                {
                    reader.Read();
                    continue;
                }

                var propertyName = reader.Value.ToString();
                switch (propertyName)
                {
                    case "Name":
                        Name = reader.ReadAsString();
                        break;
                    case "Index":
                        index = (int)reader.ReadAsInt32();
                        break;
                    case "SurfaceArea":
                        _surfaceArea = (double)reader.ReadAsDouble();
                        break;
                    case "Volume":
                        Volume = (double)reader.ReadAsDouble();
                        break;
                    case "NumberOfVertices":
                        NumberOfVertices = (int)reader.ReadAsInt32();
                        Vertices = new Vertex[NumberOfVertices];
                        break;
                    case "NumberOfFaces":
                        NumberOfFaces = (int)reader.ReadAsInt32();
                        Faces = new TriangleFace[NumberOfFaces];
                        break;
                    case "NumberOfPrimitives":
                        NumberOfPrimitives = (int)reader.ReadAsInt32();
                        Primitives = new List<PrimitiveSurface>(NumberOfPrimitives);
                        break;
                    case "Primitives":
                        //Start reading primitives                          
                        reader.Read();//skip object container "{"
                        for (var primitiveIndex = 0; primitiveIndex < NumberOfPrimitives; primitiveIndex++)
                        {
                            //Get the property name, which in this case, is the name of the primitive plus an index.
                            reader.Read();
                            var primitiveType = reader.Value.ToString().Split('_')[0];

                            //Get the next object, which is a primitive. Cast it to the appropriate primitive type.
                            reader.Read();
                            switch (primitiveType)
                            {
                                case "Plane":
                                    Primitives.Add(jsonSerializer.Deserialize<Plane>(reader));
                                    break;
                                case "Cylinder":
                                    Primitives.Add(jsonSerializer.Deserialize<Cylinder>(reader));
                                    break;
                                case "Cone":
                                    Primitives.Add(jsonSerializer.Deserialize<Cone>(reader));
                                    break;
                                case "Sphere":
                                    Primitives.Add(jsonSerializer.Deserialize<Sphere>(reader));
                                    break;
                                case "Torus":
                                    Primitives.Add(jsonSerializer.Deserialize<Torus>(reader));
                                    break;
                                case "Capsule":
                                    Primitives.Add(jsonSerializer.Deserialize<Capsule>(reader));
                                    break;
                                case "UnknownRegion":
                                    Primitives.Add(jsonSerializer.Deserialize<UnknownRegion>(reader));
                                    break;
                                default:
                                    throw new Exception("Need to add deserialize casting for primitive type: " + primitiveType);
                            }
                        }
                        break;
                    case "FaceIndices":
                        reader.Read();//start array [
                        for (var faceIndex = 0; faceIndex < NumberOfFaces; faceIndex++)
                        {
                            var a = (int)reader.ReadAsInt32();
                            var b = (int)reader.ReadAsInt32();
                            var c = (int)reader.ReadAsInt32();
                            Faces[faceIndex] = new TriangleFace(Vertices[a], Vertices[b], Vertices[c], true) { IndexInList = faceIndex };
                        }
                        break;
                    case "VertexCoords":
                        reader.Read();//start array [
                        for (var vertexIndex = 0; vertexIndex < NumberOfVertices; vertexIndex++)
                        {
                            var x = (double)reader.ReadAsDouble();
                            var y = (double)reader.ReadAsDouble();
                            var z = (double)reader.ReadAsDouble();
                            Vertices[vertexIndex] = new Vertex(new Vector3(x, y, z), vertexIndex);
                        }
                        break;
                }

                reader.Read();//go to next
            }

            //Build edges, convex hull, and anything else we need.
            CompleteInitiation();

            //Lastly, assign faces and vertices to the primitives
            foreach (var prim in Primitives)
                prim.CompletePostSerialization(this);
        }


        [OnDeserialized]
        protected void OnDeserializedMethod(StreamingContext context)
        {
            JArray jArray = (JArray)serializationData["VertexCoords"];
            var vertexArray = jArray.ToObject<double[]>();
            var coords = new Vector3[vertexArray.Length / 3];
            for (int i = 0; i < vertexArray.Length / 3; i++)
                coords[i] = new Vector3(vertexArray[3 * i], vertexArray[3 * i + 1], vertexArray[3 * i + 2]);

            jArray = (JArray)serializationData["FaceIndices"];
            var faceIndicesArray = jArray.ToObject<int[]>();
            var faceIndices = new int[faceIndicesArray.Length / 3][];
            for (int i = 0; i < faceIndicesArray.Length / 3; i++)
                faceIndices[i] = new[] { faceIndicesArray[3 * i], faceIndicesArray[3 * i + 1], faceIndicesArray[3 * i + 2] };

            jArray = serializationData["Colors"] as JArray;
            Color[] colors;
            if (jArray == null)
            { colors = new[] { new Color(serializationData["Colors"].ToString()) }; }
            else
            {
                var colorStringsArray = jArray.ToObject<string[]>();
                colors = new Color[colorStringsArray.Length];
                for (int i = 0; i < colorStringsArray.Length; i++)
                    colors[i] = new Color(colorStringsArray[i]);
            }
            DefineAxisAlignedBoundingBoxAndTolerance(coords);
            MakeVertices(coords);
            MakeFaces(faceIndices, colors);
            MakeEdges();

            ConvexHull = new TVGLConvexHull(this);
            foreach (var cvxHullPt in ConvexHull.Vertices)
                cvxHullPt.PartOfConvexHull = true;
            foreach (var face in Faces.Where(face => face.Vertices.All(v => v.PartOfConvexHull)))
            {
                face.PartOfConvexHull = true;
                foreach (var e in face.Edges)
                    if (e != null) e.PartOfConvexHull = true;
            }
            if (Primitives != null && Primitives.Any())
            {
                foreach (var surface in Primitives)
                    surface.CompletePostSerialization(this);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TessellatedSolid" /> class. This constructor is
        /// for cases in which the faces and vertices are already defined.
        /// </summary>
        /// <param name="faces">The faces.</param>
        /// <param name="createFullVersion">if set to <c>true</c> [make edges].</param>
        /// <param name="copyElements">if set to <c>true</c> [copy elements].</param>
        /// <param name="vertices">The vertices.</param>
        /// <param name="colors">The colors.</param>
        /// <param name="units">The units.</param>
        /// <param name="name">The name.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="comments">The comments.</param>
        /// <param name="language">The language.</param>
        public TessellatedSolid(IEnumerable<TriangleFace> faces, bool createFullVersion, bool copyElements,
            IEnumerable<Vertex> vertices = null, IList<Color> colors = null, UnitType units = UnitType.unspecified, string name = "", string filename = "",
            List<string> comments = null, string language = "") : base(units, name, filename, comments, language)
        {
            if (colors != null && colors.Count == 1)
            {
                SolidColor = colors[0];
                HasUniformColor = true;
            }
            var manyInputColors = (colors != null && colors.Count > 1);
            if (manyInputColors) HasUniformColor = false;
            Faces = faces.ToArray();
            NumberOfFaces = Faces.Length;
            if (vertices == null)
            {
                vertices = new HashSet<Vertex>();
                foreach (var face in Faces)
                    foreach (var vertex in face.Vertices)
                        if (!vertices.Contains(vertex))
                            ((HashSet<Vertex>)vertices).Add(vertex);
            }
            Vertices = vertices.ToArray();
            NumberOfVertices = Vertices.Length;
            var simpleCompareDict = new Dictionary<Vertex, Vertex>();
            if (copyElements)
            {
                for (var i = 0; i < NumberOfVertices; i++)
                {
                    var origVertex = Vertices[i];
                    var vertex = origVertex.Copy();
                    vertex.IndexInList = i;
                    vertex.PartOfConvexHull = false; //We will find the convex hull vertices during CompleteInitiation
                    Vertices[i] = vertex;
                    simpleCompareDict.Add(origVertex, vertex);
                }
            }
            else
            {
                for (var i = 0; i < NumberOfVertices; i++)
                {
                    var vertex = Vertices[i];
                    vertex.IndexInList = i;
                    vertex.PartOfConvexHull = false; //We will find the convex hull vertices during CompleteInitiation
                }
            }

            if (createFullVersion)
            {
                DefineAxisAlignedBoundingBoxAndTolerance(Vertices.Select(v => v.Coordinates));
                if (copyElements)
                {
                    var i = 0;
                    foreach (var origFace in Faces)
                    {
                        //Keep "CreatedInFunction" to help with debug
                        var face = origFace.Copy();
                        face.PartOfConvexHull = false;
                        face.IndexInList = i;
                        var newVertex = simpleCompareDict[origFace.A];
                        face.A = newVertex;
                        newVertex.Faces.Add(face);
                        newVertex = simpleCompareDict[origFace.B];
                        face.B = newVertex;
                        newVertex.Faces.Add(face);
                        newVertex = simpleCompareDict[origFace.C];
                        face.C = newVertex;
                        newVertex.Faces.Add(face);
                        if (HasUniformColor)
                            face.Color = SolidColor;
                        else if (manyInputColors)
                        {
                            var j = i < colors.Count - 1 ? i : colors.Count - 1;
                            face.Color = colors[j];
                            if (!SolidColor.Equals(face.Color)) HasUniformColor = false;
                        }
                        Faces[i] = face;
                        i++;
                    }
                }
                else
                {
                    NumberOfFaces = Faces.Length;
                    for (var i = 0; i < NumberOfFaces; i++)
                    {
                        var face = Faces[i];
                        face.IndexInList = i;
                        face.PartOfConvexHull = false; //We will find the convex hull vertices during CompleteInitiation
                        if (HasUniformColor)
                            face.Color = SolidColor;
                        else if (manyInputColors)
                        {
                            var j = i < colors.Count - 1 ? i : colors.Count - 1;
                            face.Color = colors[j];
                            if (!SolidColor.Equals(face.Color)) HasUniformColor = false;
                        }
                    }
                }
                CompleteInitiation();
            }
            else
            {
                if (copyElements)
                {
                    var i = 0;
                    foreach (var origFace in Faces)
                    {
                        //Keep "CreatedInFunction" to help with debug
                        var face = origFace.Copy();
                        face.PartOfConvexHull = false;
                        face.IndexInList = i;
                        var newVertex = simpleCompareDict[origFace.A];
                        face.A = newVertex;
                        newVertex.Faces.Add(face);
                        newVertex = simpleCompareDict[origFace.B];
                        face.B = newVertex;
                        newVertex.Faces.Add(face);
                        newVertex = simpleCompareDict[origFace.C];
                        face.C = newVertex;
                        newVertex.Faces.Add(face);
                        if (HasUniformColor)
                            face.Color = SolidColor;
                        else if (manyInputColors)
                        {
                            var j = i < colors.Count - 1 ? i : colors.Count - 1;
                            face.Color = colors[j];
                            if (!SolidColor.Equals(face.Color)) HasUniformColor = false;
                        }
                        Faces[i] = face;
                        i++;
                    }
                }
                else
                {
                    for (var i = 0; i < NumberOfFaces; i++)
                    {
                        var face = Faces[i];
                        face.IndexInList = i;
                        if (HasUniformColor)
                            face.Color = SolidColor;
                        else if (manyInputColors)
                        {
                            var j = i < colors.Count - 1 ? i : colors.Count - 1;
                            face.Color = colors[j];
                            if (!SolidColor.Equals(face.Color)) HasUniformColor = false;
                        }
                    }
                }
            }
        }

        public void MakeEdgesIfNonExistent()
        {
            if (_edges != null && _edges.Length > 0) return;
            CompleteInitiation();
        }

        public void DoublyConnectVerticesToFaces()
        {
            foreach (var face in Faces)
            {
                foreach (var vertex in face.Vertices)
                {
                    vertex.Faces.Add(face);
                }
            }
        }

        internal void CompleteInitiation(bool fromSTL = false)
        {
            if (Vertices[0].Faces == null || !Vertices[0].Faces.Any())
                DoublyConnectVerticesToFaces();

            try
            {
                MakeEdges(fromSTL);
            }
            catch
            {
                //Continue
            }
            CalculateVolume();
            try
            {
                this.CheckModelIntegrity();
            }
            catch
            {
                //Continue
            }

            //If the volume is zero, creating the convex hull may cause a null exception
            if (this.Volume.IsNegligible()) return;

            //Otherwise, create the convex hull and connect the vertices and faces that belong to the hull.
            ConvexHull = new TVGLConvexHull(this);
            if (ConvexHull.Vertices != null)
                foreach (var cvxHullPt in ConvexHull.Vertices)
                    cvxHullPt.PartOfConvexHull = true;
            foreach (var face in Faces.Where(face => face.Vertices.All(v => v.PartOfConvexHull)))
            {
                face.PartOfConvexHull = true;
                foreach (var e in face.Edges)
                    if (e != null) e.PartOfConvexHull = true;
            }
        }

        #endregion

        #region Make many elements (called from constructors)

        /// <summary>
        ///     Defines the axis aligned bounding box and tolerance. This is called first in the constructors
        ///     because the tolerance is used in making the vertices.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        private void DefineAxisAlignedBoundingBoxAndTolerance(IEnumerable<Vector3> vertices)
        {
            var xMin = double.PositiveInfinity;
            var yMin = double.PositiveInfinity;
            var zMin = double.PositiveInfinity;
            var xMax = double.NegativeInfinity;
            var yMax = double.NegativeInfinity;
            var zMax = double.NegativeInfinity;
            foreach (var v in vertices)
            {
                if (xMin > v.X) xMin = v.X;
                if (yMin > v.Y) yMin = v.Y;
                if (zMin > v.Z) zMin = v.Z;
                if (xMax < v.X) xMax = v.X;
                if (yMax < v.Y) yMax = v.Y;
                if (zMax < v.Z) zMax = v.Z;
            }
            Bounds = new[] { new Vector3(xMin, yMin, zMin), new Vector3(xMax, yMax, zMax) };
            var averageDimension = 0.333 * ((XMax - XMin) + (YMax - YMin) + (ZMax - ZMin));
            SameTolerance = averageDimension * Constants.BaseTolerance;
        }

        /// <summary>
        /// Makes the faces, avoiding duplicates.
        /// </summary>
        /// <param name="faceToVertexIndices">The face to vertex indices.</param>
        /// <param name="colors">The colors.</param>
        /// <param name="doublyLinkToVertices">if set to <c>true</c> [doubly link to vertices].</param>
        /// 
        internal void MakeFaces(IEnumerable<List<Vector3>> vertsPerFace, IList<Color> colors)
        {
            IList<List<Vector3>> vertexLocations = vertsPerFace as IList<List<Vector3>> ?? vertsPerFace.ToArray();
            HasUniformColor = true;
            if (colors == null || !colors.Any() || colors.All(c => c == null))
                SolidColor = new Color(Constants.DefaultColor);
            else SolidColor = colors[0];
            NumberOfFaces = vertexLocations.Count;
            Faces = new TriangleFace[NumberOfFaces];
            for (var i = 0; i < NumberOfFaces; i++)
            {
                var color = SolidColor;
                if (colors != null)
                {
                    var j = i < colors.Count - 1 ? i : colors.Count - 1;
                    if (colors[j] != null) color = colors[j];
                    if (!SolidColor.Equals(color)) HasUniformColor = false;
                }
                Faces[i] = new TriangleFace(vertexLocations[i].Select(v => new Vertex(v)));
                Faces[i].IndexInList = i;
            }
        }


        /// <summary>
        /// Makes the faces, avoiding duplicates.
        /// </summary>
        /// <param name="faceToVertexIndices">The face to vertex indices.</param>
        /// <param name="colors">The colors.</param>
        /// <param name="doublyLinkToVertices">if set to <c>true</c> [doubly link to vertices].</param>
        /// 
        internal void MakeFaces(IList<int[]> faceToVertexIndices, IList<Color> colors,
            bool doublyLinkToVertices = true)
        {
            var duplicateFaceCheck = true;
            HasUniformColor = true;
            if (colors == null || !colors.Any() || colors.All(c => c == null))
                SolidColor = new Color(Constants.DefaultColor);
            else SolidColor = colors[0];
            NumberOfFaces = faceToVertexIndices.Count;
            var listOfFaces = new List<TriangleFace>(NumberOfFaces);
            var faceChecksums = new HashSet<long>();
            if (NumberOfVertices > Constants.CubeRootOfLongMaxValue)
            {
                Message.output("Repeat Face check is disabled since the number of vertices exceeds "
                               + Constants.CubeRootOfLongMaxValue);
                duplicateFaceCheck = false;
            }
            var checksumMultiplier = duplicateFaceCheck
                ? new List<long> { 1, NumberOfVertices, NumberOfVertices * NumberOfVertices }
                : null;
            for (var i = 0; i < NumberOfFaces; i++)
            {
                var faceToVertexIndexList = faceToVertexIndices[i];
                if (duplicateFaceCheck)
                {
                    // first check to be sure that this is a new face and not a duplicate or a degenerate
                    var orderedIndices =
                        new List<int>(faceToVertexIndexList.Select(index => Vertices[index].IndexInList));
                    orderedIndices.Sort();
                    while (orderedIndices.Count > checksumMultiplier.Count)
                        checksumMultiplier.Add((long)Math.Pow(NumberOfVertices, checksumMultiplier.Count));
                    var checksum = orderedIndices.Select((index, p) => index * checksumMultiplier[p]).Sum();
                    if (faceChecksums.Contains(checksum)) continue; //Duplicate face. Do not create
                    if (orderedIndices.Count < 3 || ContainsDuplicateIndices(orderedIndices)) continue;
                    // if you made it passed these to "continue" conditions, then this is a valid new face
                    faceChecksums.Add(checksum);
                }
                var faceVertices =
                    faceToVertexIndexList.Select(vertexMatchingIndex => Vertices[vertexMatchingIndex]).ToArray();

                var color = SolidColor;
                if (colors != null)
                {
                    var j = i < colors.Count - 1 ? i : colors.Count - 1;
                    if (colors[j] != null) color = colors[j];
                    if (SolidColor == null || !SolidColor.Equals(color)) HasUniformColor = false;
                }
                if (faceVertices.Length == 3)
                {
                    var face = new TriangleFace(faceVertices, doublyLinkToVertices);
                    if (!HasUniformColor) face.Color = color;
                    listOfFaces.Add(face);
                }
                else
                {
                    var normal = MiscFunctions.DetermineNormalForA3DPolygon(faceVertices, faceVertices.Length, out _, Vector3.Null, out _);
                    var triangulatedList = faceVertices.Triangulate(normal);
                    var listOfFlatFaces = new List<TriangleFace>();
                    foreach (var vertexSet in triangulatedList)
                    {
                        var face = new TriangleFace(vertexSet, normal, doublyLinkToVertices);
                        if (!HasUniformColor) face.Color = color;
                        listOfFaces.Add(face);
                        listOfFlatFaces.Add(face);
                    }
                    Primitives ??= new List<PrimitiveSurface>();
                    Primitives.Add(new Plane(listOfFlatFaces));
                }
            }
            Faces = listOfFaces.ToArray();
            NumberOfFaces = Faces.GetLength(0);
            for (var i = 0; i < NumberOfFaces; i++)
                Faces[i].IndexInList = i;
        }

        public struct faceInfo
        {
            public int A;
            public int B;
            public int C;
            public int Index;
        }

        /// <summary>
        /// Makes the faces
        /// </summary>
        /// <param name="faceToVertexIndices">The face to vertex indices.</param>
        /// <param name="colors">The colors.</param>
        /// <param name="doublyLinkToVertices">if set to <c>true</c> [doubly link to vertices].</param>
        /// 
        public void MakeFacesFromPrimitives()
        {
            HasUniformColor = true;
            Faces = new TriangleFace[NumberOfFaces];
            var faceIndex = 0;
            foreach (var primitive in Primitives)
                foreach (var face in primitive.Faces)
                    Faces[faceIndex++] = face;
        }

        /// <summary>
        /// Makes the faces, avoiding duplicates.
        /// </summary>
        /// <param name="faceToVertexIndices">The face to vertex indices.</param>
        /// <param name="colors">The colors.</param>
        /// <param name="doublyLinkToVertices">if set to <c>true</c> [doubly link to vertices].</param>
        /// 
        internal void MakeFaces(IList<int[]> faceToVertexIndices, IList<PrimitiveSurface> primitives, IList<Color> colors,
            bool doublyLinkToVertices = true)
        {
            var duplicateFaceCheck = true;
            HasUniformColor = true;
            if (colors == null || !colors.Any() || colors.All(c => c == null))
                SolidColor = new Color(Constants.DefaultColor);
            else SolidColor = colors[0];
            NumberOfFaces = faceToVertexIndices.Count;
            var tempFaceIndices = new Dictionary<int, List<TriangleFace>>(NumberOfFaces);
            var faceChecksums = new Dictionary<long, int>();
            if (NumberOfVertices > Constants.CubeRootOfLongMaxValue)
            {
                Message.output("Repeat Face check is disabled since the number of vertices exceeds "
                               + Constants.CubeRootOfLongMaxValue);
                duplicateFaceCheck = false;
            }

            var m1 = 1;
            var m2 = NumberOfVertices;
            var m3 = NumberOfVertices * NumberOfVertices;
            var checksumMultiplier = duplicateFaceCheck
                ? new List<long> { 1, NumberOfVertices, NumberOfVertices * NumberOfVertices }
                : null;
            for (var i = 0; i < NumberOfFaces; i++)
            {
                var faceToVertexIndexList = faceToVertexIndices[i];
                if (duplicateFaceCheck)
                {
                    // first check to be sure that this is a new face and not a duplicate or a degenerate
                    var checksum = FaceChecksum(checksumMultiplier, faceToVertexIndexList, out var orderedIndices);
                    if (faceChecksums.ContainsKey(checksum)) continue; //Duplicate face. Do not create
                    if (orderedIndices.Count < 3 || ContainsDuplicateIndices(orderedIndices)) continue;
                    // if you made it passed these to "continue" conditions, then this is a valid new face
                    faceChecksums.Add(checksum, i);
                }
                var faceVertices =
                    faceToVertexIndexList.Select(vertexMatchingIndex => Vertices[vertexMatchingIndex]).ToArray();

                var color = SolidColor;
                if (colors != null)
                {
                    var j = i < colors.Count - 1 ? i : colors.Count - 1;
                    if (colors[j] != null) color = colors[j];
                    if (SolidColor == null || !SolidColor.Equals(color)) HasUniformColor = false;
                }
                if (faceVertices.Length == 3)
                {
                    var face = new TriangleFace(faceVertices, doublyLinkToVertices);
                    if (!HasUniformColor) face.Color = color;
                    tempFaceIndices.Add(i, new List<TriangleFace> { face });
                }
                else
                {
                    var normal = MiscFunctions.DetermineNormalForA3DPolygon(faceVertices, faceVertices.Length, out _, Vector3.Null, out _);
                    var triangulatedList = faceVertices.Triangulate(normal);
                    tempFaceIndices[i] = new List<TriangleFace>();
                    foreach (var vertexSet in triangulatedList)
                    {
                        var face = new TriangleFace(vertexSet, normal, doublyLinkToVertices);
                        if (!HasUniformColor) face.Color = color;
                        tempFaceIndices[i].Add(face);
                    }
                }
            }
            //Set the faces and their indices
            Faces = tempFaceIndices.Values.SelectMany(p => p).ToArray();
            NumberOfFaces = Faces.GetLength(0);
            for (var i = 0; i < NumberOfFaces; i++)
                Faces[i].IndexInList = i;

            //Connect the primitives to their faces through the vertex triangle[] and checksums
            Primitives ??= new List<PrimitiveSurface>();
            var addNonSmoothEdges = NonsmoothEdges == null;
            if (addNonSmoothEdges)
                NonsmoothEdges = new List<EdgePath>();
            if (primitives != null)
            {
                foreach (var primitive in primitives)
                {
                    //Get all the faces 
                    //var faceIndi
                    var faceIndices = new List<int>();
                    foreach (var face in primitive.TriangleVertexIndices)
                    {
                        var checksum = FaceChecksum(m1, m2, m3, face.Item1, face.Item2, face.Item3);
                        if (!faceChecksums.TryGetValue(checksum, out var faceIndex)) continue;//This may be an invalid face - such as duplicated vertices.
                        faceIndices.AddRange(tempFaceIndices[faceIndex].Select(p => p.IndexInList));
                    }
                    primitive.FaceIndices = faceIndices.ToArray();
                    primitive.TriangleVertexIndices = null;//Don't need these anymore
                    Primitives.Add(primitive);
                }
            }
        }

        private long FaceChecksum(List<long> checksumMultiplier, IEnumerable<int> vertexIndices, out List<int> orderedIndices)
        {
            orderedIndices =
                   new List<int>(vertexIndices.Select(index => Vertices[index].IndexInList));
            orderedIndices.Sort();
            while (orderedIndices.Count > checksumMultiplier.Count)
                checksumMultiplier.Add((long)Math.Pow(NumberOfVertices, checksumMultiplier.Count));
            return orderedIndices.Select((index, p) => index * checksumMultiplier[p]).Sum();
        }

        //Get the face checksum from three vertex indices on a face without creating needless lists in memory or using the Sort function.
        private long FaceChecksum(long m1, long m2, long m3, int A, int B, int C)
        {
            if (A < B)
            {
                if (B < C)
                    return CalculateChecksum(m1, m2, m3, A, B, C);
                else if (A > C)
                    return CalculateChecksum(m1, m2, m3, C, A, B);
                else
                    return CalculateChecksum(m1, m2, m3, A, C, B);
            }
            else
            {
                if (B > C)
                    return CalculateChecksum(m1, m2, m3, C, B, A);
                else if (C > A)
                    return CalculateChecksum(m1, m2, m3, B, A, C);
                else
                    return CalculateChecksum(m1, m2, m3, B, C, A);
            }
        }

        private long CalculateChecksum(long m1, long m2, long m3, int smallest, int middle, int largest)
        {
            return m1 * smallest + m2 * middle + m3 * largest;
        }

        /// <summary>
        ///     Makes the vertices.
        /// </summary>
        /// <param name="vertsPerFace">The verts per face.</param>
        /// <param name="faceToVertexIndices">The face to vertex indices.</param>
        private void MakeVertices(IEnumerable<List<Vector3>> vertsPerFace, double scaleFactor, out List<int[]> faceToVertexIndices)
        {
            var numDecimalPoints = 0;
            //Gets the number of decimal places
            while (Math.Round(scaleFactor * SameTolerance, numDecimalPoints) == 0.0) numDecimalPoints++;
            /* vertexMatchingIndices will be used to speed up the linking of faces and edges to vertices
             * it  preserves the order of vertsPerFace (as read in from the file), and indicates where
             * you can find each vertex in the new array of vertices. This is essentially what is built in 
             * the remainder of this method. */
            faceToVertexIndices = new List<int[]>();
            var listOfVertices = new List<Vector3>();
            var simpleCompareDict = new Dictionary<Vector3, int>();
            //in order to reduce compare times we use a string comparer and dictionary
            foreach (var t in vertsPerFace)
            {
                var locationIndices = new List<int>(); // this will become a row in faceToVertexIndices
                for (int i = 0; i < t.Count; i++)
                {
                    /* given the low precision in files like STL, this should be a sufficient way to detect identical points. 
                     * I believe comparing these lookupStrings will be quicker than comparing two 3d points.*/
                    //First, round the vertices, then convert to a string. This will catch bidirectional tolerancing (+/-)
                    var coordinates = t[i] = new Vector3(scaleFactor * Math.Round(t[i].X, numDecimalPoints),
                        Math.Round(scaleFactor * t[i].Y, numDecimalPoints), Math.Round(scaleFactor * t[i].Z, numDecimalPoints));
                    if (simpleCompareDict.TryGetValue(coordinates, out int index))
                        /* if it's in the dictionary, simply put the location in the locationIndices */
                        locationIndices.Add(index);
                    else
                    {
                        /* else, add a new vertex to the list, and a new entry to simpleCompareDict. Also, be sure to indicate
                        * the position in the locationIndices. */
                        var newIndex = listOfVertices.Count;
                        listOfVertices.Add(t[i]);
                        simpleCompareDict.Add(coordinates, newIndex);
                        locationIndices.Add(newIndex);
                    }
                }
                faceToVertexIndices.Add(locationIndices.ToArray());
            }
            //Make vertices from the double arrays
            MakeVertices(listOfVertices);
        }

        /// <summary>
        ///     Makes the vertices, and set CheckSum multiplier
        /// </summary>
        /// <param name="listOfVertices">The list of vertices.</param>
        private void MakeVertices(IList<Vector3> listOfVertices)
        {
            NumberOfVertices = listOfVertices.Count;
            Vertices = new Vertex[NumberOfVertices];
            for (var i = 0; i < NumberOfVertices; i++)
                Vertices[i] = new Vertex(listOfVertices[i], i);
            //Set the checksum
        }

        #endregion

        #region Add or Remove Items

        #region Vertices - the important thing about these is updating the IndexInList property of the vertices

        internal void AddVertex(Vertex newVertex)
        {
            var newVertices = new Vertex[NumberOfVertices + 1];
            for (var i = 0; i < NumberOfVertices; i++)
                newVertices[i] = Vertices[i];
            newVertices[NumberOfVertices] = newVertex;
            newVertex.IndexInList = NumberOfVertices;
            Vertices = newVertices;
            NumberOfVertices++;
        }

        internal void AddVertices(IList<Vertex> verticesToAdd)
        {
            var numToAdd = verticesToAdd.Count;
            var newVertices = new Vertex[NumberOfVertices + numToAdd];
            for (var i = 0; i < NumberOfVertices; i++)
                newVertices[i] = Vertices[i];
            for (var i = 0; i < numToAdd; i++)
            {
                var newVertex = verticesToAdd[i];
                newVertices[NumberOfVertices + i] = newVertex;
                newVertex.IndexInList = NumberOfVertices + i;
            }
            Vertices = newVertices;
            NumberOfVertices += numToAdd;
        }

        internal void ReplaceVertex(Vertex removeVertex, Vertex newVertex, bool removeReferecesToVertex = true)
        {
            ReplaceVertex(Vertices.FindIndex(removeVertex), newVertex, removeReferecesToVertex);
        }

        internal void ReplaceVertex(int removeVIndex, Vertex newVertex, bool removeReferecesToVertex = true)
        {
            if (removeReferecesToVertex) RemoveReferencesToVertex(Vertices[removeVIndex]);
            newVertex.IndexInList = removeVIndex;
            Vertices[removeVIndex] = newVertex;
        }

        internal void RemoveVertex(Vertex removeVertex, bool removeReferecesToVertex = true)
        {
            RemoveVertex(Vertices.FindIndex(removeVertex), removeReferecesToVertex);
        }

        internal void RemoveVertex(int removeVIndex, bool removeReferecesToVertex = true)
        {
            if (removeReferecesToVertex) RemoveReferencesToVertex(Vertices[removeVIndex]);
            NumberOfVertices--;
            var newVertices = new Vertex[NumberOfVertices];
            for (var i = 0; i < removeVIndex; i++)
                newVertices[i] = Vertices[i];
            for (var i = removeVIndex; i < NumberOfVertices; i++)
            {
                var v = Vertices[i + 1];
                v.IndexInList--;
                newVertices[i] = v;
            }
            Vertices = newVertices;
            UpdateAllEdgeCheckSums();
        }

        internal void RemoveVertices(IEnumerable<Vertex> removeVertices)
        {
            RemoveVertices(removeVertices.Select(Vertices.FindIndex).ToList());
        }

        internal void RemoveVertices(List<int> removeIndices)
        {
            var numToRemove = removeIndices.Count;
            if (numToRemove == 0) return;
            var offset = 0;
            foreach (var vertexIndex in removeIndices)
            {
                RemoveReferencesToVertex(Vertices[vertexIndex]);
            }
            removeIndices.Sort();
            NumberOfVertices -= numToRemove;
            var newVertices = new Vertex[NumberOfVertices];
            for (var i = 0; i < NumberOfVertices; i++)
            {
                while (offset < numToRemove && i + offset == removeIndices[offset])
                    offset++;
                var v = Vertices[i + offset];
                v.IndexInList = i;
                newVertices[i] = v;
            }
            Vertices = newVertices;
            UpdateAllEdgeCheckSums();
        }

        internal void UpdateAllEdgeCheckSums()
        {
            foreach (var edge in Edges)
                SetAndGetEdgeChecksum(edge);
        }

        #endregion

        #region Faces

        internal void AddFace(TriangleFace newFace)
        {
            var newFaces = new TriangleFace[NumberOfFaces + 1];
            for (var i = 0; i < NumberOfFaces; i++)
                newFaces[i] = Faces[i];
            newFaces[NumberOfFaces] = newFace;
            newFace.IndexInList = NumberOfFaces;
            Faces = newFaces;
            NumberOfFaces++;
        }

        internal void AddFaces(IList<TriangleFace> facesToAdd)
        {
            var numToAdd = facesToAdd.Count;
            var newFaces = new TriangleFace[NumberOfFaces + numToAdd];
            for (var i = 0; i < NumberOfFaces; i++)
                newFaces[i] = Faces[i];
            for (var i = 0; i < numToAdd; i++)
            {
                newFaces[NumberOfFaces + i] = facesToAdd[i];
                newFaces[NumberOfFaces + i].IndexInList = NumberOfFaces + i;
            }
            Faces = newFaces;
            NumberOfFaces += numToAdd;
        }

        internal void RemoveFace(TriangleFace removeFace)
        {
            RemoveFace(Faces.FindIndex(removeFace));
        }

        internal void RemoveFace(int removeFaceIndex)
        {
            //First. Remove all the references to each edge and vertex.
            RemoveReferencesToFace(removeFaceIndex);
            NumberOfFaces--;
            var newFaces = new TriangleFace[NumberOfFaces];
            for (var i = 0; i < removeFaceIndex; i++)
                newFaces[i] = Faces[i];
            for (var i = removeFaceIndex; i < NumberOfFaces; i++)
            {
                newFaces[i] = Faces[i + 1];
                newFaces[i].IndexInList = i;
            }
            Faces = newFaces;
        }

        internal void RemoveFaces(IEnumerable<TriangleFace> removeFaces)
        {
            RemoveFaces(removeFaces.Select(Faces.FindIndex).ToList());
        }

        internal void RemoveFaces(List<int> removeIndices)
        {
            //First. Remove all the references to each edge and vertex.
            foreach (var faceIndex in removeIndices)
            {
                RemoveReferencesToFace(faceIndex);
            }
            var offset = 0;
            var numToRemove = removeIndices.Count;
            removeIndices.Sort();
            NumberOfFaces -= numToRemove;
            var newFaces = new TriangleFace[NumberOfFaces];
            for (var i = 0; i < NumberOfFaces; i++)
            {
                while (offset < numToRemove && i + offset == removeIndices[offset])
                    offset++;
                newFaces[i] = Faces[i + offset];
                newFaces[i].IndexInList = i;
            }
            Faces = newFaces;
        }

        private void RemoveReferencesToFace(int removeFaceIndex)
        {
            var face = Faces[removeFaceIndex];
            foreach (var vertex in face.Vertices)
                vertex.Faces.Remove(face);
            foreach (var edge in face.Edges)
            {
                if (edge == null) continue;
                if (face == edge.OwnedFace) edge.OwnedFace = null;
                if (face == edge.OtherFace) edge.OtherFace = null;
            }
            //Face adjacency is a method call, not an object reference. So it updates automatically.
        }

        #endregion

        #region Edges

        internal void AddEdge(Edge newEdge)
        {
            var newEdges = new Edge[NumberOfEdges + 1];
            for (var i = 0; i < NumberOfEdges; i++)
                newEdges[i] = _edges[i];
            newEdges[NumberOfEdges] = newEdge;
            if (newEdge.EdgeReference <= 0) SetAndGetEdgeChecksum(newEdge);
            newEdge.IndexInList = NumberOfEdges;
            _edges = newEdges;
            NumberOfEdges++;
        }

        internal void AddEdges(IList<Edge> edgesToAdd)
        {
            var numToAdd = edgesToAdd.Count;
            var newEdges = new Edge[NumberOfEdges + numToAdd];
            for (var i = 0; i < NumberOfEdges; i++)
                newEdges[i] = Edges[i];
            for (var i = 0; i < numToAdd; i++)
            {
                newEdges[NumberOfEdges + i] = edgesToAdd[i];
                if (newEdges[NumberOfEdges + i].EdgeReference <= 0) SetAndGetEdgeChecksum(newEdges[NumberOfEdges + i]);
                newEdges[NumberOfEdges + i].IndexInList = NumberOfEdges;
            }
            _edges = newEdges;
            NumberOfEdges += numToAdd;
        }

        internal void RemoveEdge(Edge removeEdge)
        {
            RemoveEdge(Edges.FindIndex(removeEdge));
        }

        internal void RemoveEdge(int removeEdgeIndex)
        {
            RemoveReferencesToEdge(removeEdgeIndex);
            NumberOfEdges--;
            var newEdges = new Edge[NumberOfEdges];
            for (var i = 0; i < removeEdgeIndex; i++)
                newEdges[i] = Edges[i];
            for (var i = removeEdgeIndex; i < NumberOfEdges; i++)
            {
                newEdges[i] = Edges[i + 1];
                newEdges[i].IndexInList = i;
            }
            _edges = newEdges;
        }

        internal void RemoveEdges(IEnumerable<Edge> removeEdges)
        {
            RemoveEdges(removeEdges.Select(Edges.FindIndex).ToList());
        }

        internal void RemoveEdges(List<int> removeIndices)
        {
            //First. Remove all the references to each edge and vertex.
            foreach (var edgeIndex in removeIndices)
            {
                RemoveReferencesToEdge(edgeIndex);
            }
            var offset = 0;
            var numToRemove = removeIndices.Count;
            removeIndices.Sort();
            NumberOfEdges -= numToRemove;
            var newEdges = new Edge[NumberOfEdges];
            for (var i = 0; i < NumberOfEdges; i++)
            {
                while (offset < numToRemove && i + offset == removeIndices[offset])
                    offset++;
                newEdges[i] = Edges[i + offset];
                newEdges[i].IndexInList = i;
            }
            _edges = newEdges;
        }

        private void RemoveReferencesToEdge(int removeEdgeIndex)
        {
            var edge = Edges[removeEdgeIndex];
            int index;
            if (edge.To != null)
            {
                index = edge.To.Edges.IndexOf(edge);
                if (index >= 0) edge.To.Edges.RemoveAt(index);
            }
            if (edge.From != null)
            {
                index = edge.From.Edges.IndexOf(edge);
                if (index >= 0) edge.From.Edges.RemoveAt(index);
            }
            if (edge.OwnedFace != null)
                edge.OwnedFace.ReplaceEdge(edge, null);
            if (edge.OtherFace != null)
                edge.OtherFace.ReplaceEdge(edge, null);
        }

        #endregion

        /// <summary>
        ///     Adds the primitive.
        /// </summary>
        /// <param name="p">The p.</param>
        public void AddPrimitive(PrimitiveSurface p)
        {
            Primitives ??= new List<PrimitiveSurface>();
            Primitives.Add(p);
            if (p.Faces == null)
                return;
            foreach (var face in p.Faces)
                face.BelongsToPrimitive = p;
        }

        #endregion

        #region Copy Function

        /// <summary>
        ///     Copies this instance.
        /// </summary>
        /// <returns>TessellatedSolid.</returns>
        public TessellatedSolid Copy()
        {
            //Copy the solid. Do not check Edges[], rather use _edges, so that MakeEdges() does not get triggered.
            var copy = new TessellatedSolid(Faces, _edges != null, true, Vertices, Faces.Select(p => p.Color).ToList(), Units, Name + "_Copy",
                FileName, Comments, Language);
            if (Primitives != null && Primitives.Any())
            {
                foreach (var surface in Primitives)
                {
                    var surfType = surface.GetType();
                    var surfConstructor = surfType.GetConstructor(new[] { surfType, typeof(TessellatedSolid) });
                    copy.AddPrimitive((PrimitiveSurface)surfConstructor.Invoke(new object[] { surface, copy }));
                }

            }
            if (NonsmoothEdges != null && NonsmoothEdges.Any())
            {
                copy.NonsmoothEdges = new List<EdgePath>();
                foreach (var nonSmoothEdgePath in NonsmoothEdges)
                {
                    var copiedPath = new EdgePath();
                    foreach (var item in nonSmoothEdgePath)
                        copiedPath.AddEnd(copy.Edges[item.edge.IndexInList], item.dir);
                    copy.NonsmoothEdges.Add(copiedPath);
                }
            }
            copy.ReferenceIndex = ReferenceIndex;
            return copy;
        }

        #endregion

        #region Reset Color Function
        public void ResetDefaultColor()
        {
            var defaultColor = new Color(KnownColors.LightGray);
            foreach (var face in Faces) face.Color = defaultColor;
        }

        public void ResetDefaultColor(IEnumerable<PrimitiveSurface> primitives)
        {
            var defaultColor = new Color(KnownColors.LightGray);
            foreach (var prim in primitives) prim.SetColor(defaultColor);
        }
        #endregion

        #region Transform
        /// <summary>
        /// Translates and Squares Tesselated Solid based on its oriented bounding box. 
        /// The resulting Solid should be located at the origin, and only in the positive X, Y, Z octant.
        /// </summary>
        /// <returns></returns>
        public TessellatedSolid SetToOriginAndSquareToNewSolid(out BoundingBox originalBoundingBox)
        {
            originalBoundingBox = this.OrientedBoundingBox();
            Matrix4x4.Invert(originalBoundingBox.Transform, out var transform);
            return (TessellatedSolid)TransformToNewSolid(transform);
        }
        /// <summary>
        /// Translates and Squares Tesselated Solid based on its oriented bounding box. 
        /// The resulting Solid should be located at the origin, and only in the positive X, Y, Z octant.
        /// </summary>
        /// <returns></returns>
        public void SetToOriginAndSquare(out BoundingBox originalBoundingBox)
        {
            originalBoundingBox = this.OrientedBoundingBox();
            Matrix4x4.Invert(originalBoundingBox.Transform, out var transform);
            Transform(transform);
        }

        /// <summary>
        /// Transforms the specified transform matrix.
        /// </summary>
        /// <param name="transformMatrix">The transform matrix.</param>
        public override void Transform(Matrix4x4 transformMatrix)
        {
            var xMin = double.PositiveInfinity;
            var yMin = double.PositiveInfinity;
            var zMin = double.PositiveInfinity;
            var xMax = double.NegativeInfinity;
            var yMax = double.NegativeInfinity;
            var zMax = double.NegativeInfinity;
            foreach (var v in Vertices)
            {
                v.Coordinates = v.Coordinates.Transform(transformMatrix);
                if (xMin > v.Coordinates.X) xMin = v.Coordinates.X;
                if (yMin > v.Coordinates.Y) yMin = v.Coordinates.Y;
                if (zMin > v.Coordinates.Z) zMin = v.Coordinates.Z;
                if (xMax < v.Coordinates.X) xMax = v.Coordinates.X;
                if (yMax < v.Coordinates.Y) yMax = v.Coordinates.Y;
                if (zMax < v.Coordinates.Z) zMax = v.Coordinates.Z;
            }
            Bounds = new[] { new Vector3(xMin, yMin, zMin), new Vector3(xMax, yMax, zMax) };

            //Update the faces
            foreach (var face in Faces)
            {
                face.Update();// Transform(transformMatrix);
            }
            //Update the edges
            if (NumberOfEdges > 1)
            {
                foreach (var edge in Edges)
                {
                    edge.Update(true);
                }
            }
            _center = _center.Transform(transformMatrix);
            // I'm not sure this is right, but I'm just using the 3x3 rotational submatrix to rotate the inertia tensor
            var rotMatrix = new Matrix3x3(transformMatrix.M11, transformMatrix.M12, transformMatrix.M13,
                    transformMatrix.M21, transformMatrix.M22, transformMatrix.M23,
                    transformMatrix.M31, transformMatrix.M32, transformMatrix.M33);
            _inertiaTensor *= rotMatrix;
            if (Primitives != null)
                foreach (var primitive in Primitives)
                    primitive.Transform(transformMatrix);
            this.SetNegligibleAreaFaceNormals(true);
        }

        /// <summary>
        /// Gets a new solid by transforming its vertices.
        /// </summary>
        /// <param name="transformationMatrix"></param>
        /// <returns></returns>
        public override Solid TransformToNewSolid(Matrix4x4 transformationMatrix)
        {
            var copy = this.Copy();
            try
            {
                copy.Transform(transformationMatrix);
            }
            catch
            {
                copy = new TessellatedSolid(copy.Faces, false, true);
                copy.Transform(transformationMatrix);
            }
            return copy;
        }


        internal bool TurnModelInsideOut()
        {
            _volume = -1 * _volume;
            _inertiaTensor = Matrix3x3.Null;
            foreach (var face in Faces) face.Invert();

            if (_edges != null)
                foreach (var edge in Edges) edge.Invert();
            return true;
        }

        protected override void CalculateCenter()
        {
            CalculateVolumeAndCenter(Faces, SameTolerance, out _volume, out _center);
        }

        protected override void CalculateVolume()
        {
            CalculateVolumeAndCenter(Faces, SameTolerance, out _volume, out _center);
        }

        const double oneThird = 1.0 / 3.0;
        const double oneTwelth = 1.0 / 12.0;
        public static void CalculateVolumeAndCenter(IEnumerable<TriangleFace> faces, double tolerance, out double volume, out Vector3 center)
        {
            center = new Vector3();
            volume = 0.0;
            double currentVolumeTerm;
            double xCenter = 0, yCenter = 0, zCenter = 0;
            if (faces == null) return;
            foreach (var face in faces)
            {
                if (face.Area.IsNegligible(tolerance)) continue; //Ignore faces with zero area, since their Normals are not set.
                // this is the volume of a tetrahedron from defined by the face and the origin {0,0,0}. The origin would be part of the second term
                // in the dotproduct, "face.Normal.Dot(face.A.Position - ORIGIN))", but clearly there is no need to subtract
                // {0,0,0}. Note that the volume of the tetrahedron could be negative. This is fine as it ensures that the origin has no influence
                // on the volume.
                var a = face.A; var b = face.B; var c = face.C;// get once, so we don't have as many gets from an array.
                //The actual tetrehedron volume should be divided by three, but we can just process that at the end.
                volume += currentVolumeTerm = face.Area * face.Normal.Dot(a.Coordinates);
                xCenter += (a.X + b.X + c.X) * currentVolumeTerm;
                yCenter += (a.Y + b.Y + c.Y) * currentVolumeTerm;
                zCenter += (a.Z + b.Z + c.Z) * currentVolumeTerm;
                // center is found by a weighted sum of the centers of each tetrahedron. The weighted sum coordinate are collected here.
            }

            //Divide the volume by 3 and the center by 4. Since center is also mutliplied by the currentVolume, it is actually divided by 3 * 4 = 12;                
            volume *= oneThird;
            center = new Vector3(xCenter * oneTwelth, yCenter * oneTwelth, zCenter * oneTwelth) / volume;
        }

        //ToDo: Remove this function if there is no need for it. Why does this function repeat the volume & center calculation? Does this improve accuracy somehow? 
        public static void CalculateVolumeAndCenter_Old(IEnumerable<TriangleFace> faces, double tolerance, out double volume, out Vector3 center)
        {
            center = new Vector3();
            volume = 0.0;
            double oldVolume;
            var iterations = 0;
            Vector3 oldCenter1 = center;
            if (faces == null) return;
            var facesList = faces as IList<TriangleFace> ?? faces.ToList();
            do
            {
                oldVolume = volume;
                var oldCenter2 = oldCenter1;
                oldCenter1 = center;
                volume = 0;
                center = Vector3.Zero;
                foreach (var face in facesList)
                {
                    if (face.Area.IsNegligible(tolerance)) continue; //Ignore faces with zero area, since their Normals are not set.
                    var tetrahedronVolume = face.Area * face.Normal.Dot(face.A.Coordinates - oldCenter1) / 3;
                    // this is the volume of a tetrahedron from defined by the face and the origin {0,0,0}. The origin would be part of the second term
                    // in the dotproduct, "face.Normal.Dot(face.A.Position - ORIGIN))", but clearly there is no need to subtract
                    // {0,0,0}. Note that the volume of the tetrahedron could be negative. This is fine as it ensures that the origin has no influence
                    // on the volume.
                    volume += tetrahedronVolume;
                    center += new Vector3(
                        (oldCenter1[0] + face.A.X + face.B.X + face.C.X) * tetrahedronVolume / 4,
                        (oldCenter1[1] + face.A.Y + face.B.Y + face.C.Y) * tetrahedronVolume / 4,
                        (oldCenter1[2] + face.A.Z + face.B.Z + face.C.Z) * tetrahedronVolume / 4);
                    // center is found by a weighted sum of the centers of each tetrahedron. The weighted sum coordinate are collected here.
                }
                if (iterations > 10 || volume < 0) center = 0.5 * (oldCenter1 + oldCenter2);
                else center = center / volume;
                iterations++;
            } while (Math.Abs(oldVolume - volume) > tolerance && iterations <= 20);
        }

        protected override void CalculateSurfaceArea()
        {
            _surfaceArea = Faces.Sum(face => face.Area);
        }

        const double oneSixtieth = 1.0 / 60.0;

        protected override void CalculateInertiaTensor()
        {
            //var matrixA = new double[3, 3];
            var matrixCtotal = new Matrix3x3();
            var canonicalMatrix = new Matrix3x3(oneSixtieth, 0.5 * oneSixtieth, 0.5 * oneSixtieth,
                0.5 * oneSixtieth, oneSixtieth, 0.5 * oneSixtieth,
                0.5 * oneSixtieth, 0.5 * oneSixtieth, oneSixtieth);
            foreach (var face in Faces)
            {
                var matrixA = new Matrix3x3(
                   face.A.Coordinates[0] - Center[0],
                   face.A.Coordinates[1] - Center[1],
                   face.A.Coordinates[2] - Center[2],

                   face.B.Coordinates[0] - Center[0],
                   face.B.Coordinates[1] - Center[1],
                   face.B.Coordinates[2] - Center[2],

                   face.C.Coordinates[0] - Center[0],
                   face.C.Coordinates[1] - Center[1],
                   face.C.Coordinates[2] - Center[2]);

                var matrixC = matrixA.Transpose() * canonicalMatrix;
                matrixC = matrixC * matrixA * matrixA.GetDeterminant();
                matrixCtotal = matrixCtotal + matrixC;
            }
            // todo fix this calculation
            //var translateMatrix = new double[,] { { 0 }, { 0 }, { 0 } };
            ////what is this crazy equations?
            //var matrixCprime =
            //    (translateMatrix * -1)
            //         * (translateMatrix.Transpose())
            //         + (translateMatrix * ((translateMatrix * -1).transpose()))
            //         + ((translateMatrix * -1) * ((translateMatrix * -1).transpose())
            //         * Volume);
            //matrixCprime = matrixCprime + matrixCtotal;
            //var result = Matrix4x4.Identity * (matrixCprime[0, 0] + matrixCprime[1, 1] + matrixCprime[2, 2]);
            //return result.Subtract(matrixCprime);
            throw new NotImplementedException();
        }
        #endregion
    }
}