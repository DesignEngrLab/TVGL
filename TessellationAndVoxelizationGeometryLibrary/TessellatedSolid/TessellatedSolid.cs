// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-14-2023
// ***********************************************************************
// <copyright file="TessellatedSolid.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace TVGL
{
    /// <summary>
    /// Class TessellatedSolid.
    /// </summary>
    /// <tags>help</tags>
    /// <remarks>This is the currently the <strong>main</strong> class within TVGL all filetypes are read in as a TessellatedSolid,
    /// and
    /// all interesting operations work on the TessellatedSolid.</remarks>
    public partial class TessellatedSolid : Solid
    {
        #region Fields and Properties
        /// <summary>
        /// Gets or sets the tessellation error.
        /// </summary>
        /// <value>The tessellation error.</value>
        public double TessellationError { get; set; } = Constants.DefaultTessellationError;

        /// <summary>
        /// Generally we don't want to mix solid and sheet bodies when importing CAD files.
        /// </summary>
        [JsonIgnore]
        public bool SourceIsSheetBody { get; set; } = false;

        /// <summary>
        /// Used to avoid unnecessary re-checking of primitives via complex optimization methods.
        /// </summary>
        public bool PrimitivesDetermined { get; set; } = false;

        /// <summary>
        /// Gets the faces.
        /// </summary>
        /// <value>The faces.</value>
        [JsonIgnore]
        public TriangleFace[] Faces { get; set; }

        /// <summary>
        /// Gets the edges.
        /// </summary>
        /// <value>The edges.</value>
        [JsonIgnore]
        public Edge[] Edges { get; internal set; }

        /// <summary>
        /// Gets the vertices.
        /// </summary>
        /// <value>The vertices.</value>
        [JsonIgnore]
        public Vertex[] Vertices { get; set; }

        /// <summary>
        /// Gets the number of faces.
        /// </summary>
        /// <value>The number of faces.</value>
        [JsonIgnore]
        public int NumberOfFaces { get; set; }

        /// <summary>
        /// Gets the number of vertices.
        /// </summary>
        /// <value>The number of vertices.</value>
        [JsonIgnore]
        public int NumberOfVertices { get; set; }

        /// <summary>
        /// Gets the number of edges.
        /// </summary>
        /// <value>The number of edges.</value>
        [JsonIgnore]
        public int NumberOfEdges { get; internal set; }

        /// <summary>
        /// Gets the number of primitives. Must be set after completing primitive definition/combination.
        /// </summary>
        /// <value>The number of faces.</value>
        [JsonIgnore]
        public int NumberOfPrimitives { get; set; }

        /// <summary>
        /// Errors in the tesselated solid
        /// </summary>
        /// <value>The errors.</value>
        [JsonIgnore]
        public TessellationInspectAndRepair Errors { get; internal set; }

        /// <summary>
        /// Gets or sets the nonsmooth edges, which are the edges that do not exhibit C1 or C2 continuity.
        /// </summary>
        /// <value>The nonsmooth edges.</value>
        [JsonIgnore]
        public List<Edge> NonsmoothEdges { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TessellatedSolid" /> class.
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
        public TessellatedSolid(IEnumerable<(Vector3, Vector3, Vector3)> vertsPerFace, int numOfFaces, IList<Color> colors,
            TessellatedSolidBuildOptions buildOptions = null, UnitType units = UnitType.unspecified,
            string name = "", string filename = "", List<string> comments = null, string language = "")
            : base(units, name, filename, comments, language)
        {
            var vertsPerFaceList = vertsPerFace as IList<(Vector3, Vector3, Vector3)> ?? vertsPerFace.ToList();
            if (numOfFaces == -1) numOfFaces = vertsPerFace.Count();
            DefineAxisAlignedBoundingBoxAndTolerance(vertsPerFaceList.SelectMany(v => v.EnumerateThruple()));
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
                DefineAxisAlignedBoundingBoxAndTolerance(vertsPerFaceList.SelectMany(vList => vList.EnumerateThruple()
                .Select(v => scaleFactor * v)));
            }
            MakeVertices(vertsPerFaceList, scaleFactor, out var faceToVertexIndices);
            //Complete Construction with Common Functions
            MakeFaces(faceToVertexIndices, numOfFaces, colors);
            TessellationInspectAndRepair.CompleteBuildOptions(this, buildOptions, out _);
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
        public TessellatedSolid(ICollection<Vector3> vertices, ICollection<(int, int, int)> faceToVertexIndices,
            IList<Color> colors, TessellatedSolidBuildOptions buildOptions = null, UnitType units = UnitType.unspecified,
            string name = "", string filename = "", List<string> comments = null, string language = "")
            : this(vertices, vertices.Count, faceToVertexIndices, faceToVertexIndices.Count, colors, buildOptions,
                  units, name, filename, comments, language)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TessellatedSolid"/> class.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="numOfVertices">The number of vertices.</param>
        /// <param name="faceToVertexIndices">The face to vertex indices.</param>
        /// <param name="numOfFaces">The number of faces.</param>
        /// <param name="colors">The colors.</param>
        /// <param name="buildOptions">The build options.</param>
        /// <param name="units">The units.</param>
        /// <param name="name">The name.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="comments">The comments.</param>
        /// <param name="language">The language.</param>
        public TessellatedSolid(IEnumerable<Vector3> vertices, int numOfVertices,
            IEnumerable<(int, int, int)> faceToVertexIndices, int numOfFaces,
            IList<Color> colors, TessellatedSolidBuildOptions buildOptions = null, UnitType units = UnitType.unspecified,
            string name = "", string filename = "", List<string> comments = null, string language = "")
            : base(units, name, filename, comments, language)
        {
            MakeVertices(vertices, numOfVertices);
            DefineAxisAlignedBoundingBoxAndTolerance(Vertices.Select(v => v.Coordinates));
            var duplicateFaceCheck = buildOptions == null ? true : buildOptions.DuplicateFaceCheck;
            MakeFaces(faceToVertexIndices, numOfFaces, colors, true, duplicateFaceCheck);
            TessellationInspectAndRepair.CompleteBuildOptions(this, buildOptions, out _);
        }

        /// <summary>
        /// The comma
        /// </summary>
        private const string comma = ",";
        /// <summary>
        /// Stream writes the JSON structure in reverse order of how it will be read in.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="index">The index.</param>
        public void StreamWrite(JsonTextWriter writer, int index = -1)
        {
            writer.WritePropertyName("Name");
            writer.WriteValue(Name);

            writer.WritePropertyName("SourceIsSheetBody");
            writer.WriteValue(SourceIsSheetBody);

            writer.WritePropertyName(nameof(PrimitivesDetermined));
            writer.WriteValue(PrimitivesDetermined);

            if (index >= 0)
            {
                writer.WritePropertyName("ReferenceIndex");
                writer.WriteValue(index);
            }
            writer.WritePropertyName("TessellationError");
            writer.WriteValue(TessellationError);

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

            if (HasUniformColor || Faces.All(f => f.Color == null || f.Color.Equals(Faces[0].Color)))
            {
                if (SolidColor != null && !SolidColor.Equals(new Color(Constants.DefaultColor)))
                {
                    writer.WritePropertyName("Colors");
                    writer.WriteValue(SolidColor.ToString());
                }
            }
            else
            {
                // See comment in StreamWrite for the trick used here to store colors compactly.
                writer.WritePropertyName("Colors");
                var colorList = new List<string>();
                var lastColor = Faces[0].Color;
                colorList.Add(lastColor.ToString().Substring(1));
                var numRepeats = 0;
                foreach (var f in Faces.Skip(1))
                {
                    if (f.Color == null || f.Color.Equals(lastColor))
                        numRepeats++; // colorList[i] = "";
                    else
                    {
                        if (numRepeats > 0)
                        {
                            colorList.Add(numRepeats.ToString());
                            numRepeats = 0;
                        }
                        lastColor = f.Color;
                        colorList.Add(lastColor.ToString().Substring(1));
                    }
                }
                if (numRepeats > 0) colorList.Add(numRepeats.ToString());
                writer.WriteValue(string.Join(',', colorList));
            }
        }

        /// <summary>
        /// Streams the read.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="index">The index.</param>
        /// <exception cref="System.Exception">Need to add deserialize casting for primitive type: " + primitiveType</exception>
        internal void StreamRead(JsonTextReader reader, out int index, TessellatedSolidBuildOptions tsBuildOptions)
        {
            // todo: resolve this with OnDeserializedMethod. Are both needed?
            Color[] colors = null;
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
                    case "SourceIsSheetBody":
                        SourceIsSheetBody = (bool)reader.ReadAsBoolean();
                        break;
                    case "PrimitivesDetermined":
                        PrimitivesDetermined = (bool)reader.ReadAsBoolean();
                        break;
                    case "TessellationError":
                        TessellationError = (double)reader.ReadAsDouble();
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
                            var primitiveString = reader.Value.ToString().Split('_')[0];
                            var primitiveInd = int.Parse(reader.Value.ToString().Split('_')[1]);
                            var primitiveType = Type.GetType("TVGL." + primitiveString);

                            //Get the next object, which is a primitive. Cast it to the appropriate primitive type.
                            reader.Read();
                            Primitives.Add((PrimitiveSurface)jsonSerializer.Deserialize(reader, primitiveType));
                        }
                        reader.Read();//end of array },
                        break;
                    case "FaceIndices":
                        reader.Read();//start array [
                        var faceIndex = 0;
                        for (var faceRowIndex = 0; faceRowIndex < NumberOfFaces; faceRowIndex++)
                        {
                            var a = (int)reader.ReadAsInt32();
                            var b = (int)reader.ReadAsInt32();
                            var c = (int)reader.ReadAsInt32();
                            if (a == b || a == c || b == c) continue;
                            Faces[faceIndex] = new TriangleFace(Vertices[a], Vertices[b], Vertices[c]) { IndexInList = faceIndex };
                            faceIndex++;
                        }
                        if (NumberOfFaces != faceIndex)
                        {
                            var newFaces = new TriangleFace[faceIndex];
                            Array.Copy(Faces, newFaces, faceIndex);
                            Faces = newFaces;
                            NumberOfFaces = faceIndex;
                        }
                        break;
                    case "VertexCoords":
                        reader.Read();//start array [
                        for (var vertexIndex = 0; vertexIndex < NumberOfVertices; vertexIndex++)
                        {
                            var x = (double)reader.ReadAsDouble();
                            var y = (double)reader.ReadAsDouble();
                            var z = (double)reader.ReadAsDouble();
                            Vertices[vertexIndex] = new Vertex(x, y, z, vertexIndex);
                        }
                        break;
                    case "Index":
                    case "ReferenceIndex":
                        ReferenceIndex = (int)reader.ReadAsInt32();
                        index = ReferenceIndex;
                        break;
                    case "Colors":
                        // to make saving colors for faces both quick and compact, we use a little trick to 
                        // store the number of repeats of a color. If the color is the same as the last color,
                        // then the next entry is a numeral that represents the number of repeats. One could 
                        // be more extreme and store one color and all the face indices that are that color
                        // (this would require a dictionary here, plus the list of indices could be long)
                        // or we could just store a color foreach face - regardless of repeating colors.
                        // This approach is a compromise of these two. It is fast and makes fairly compact results.
                        var colorStringsArray = reader.ReadAsString().Split(',');
                        colors = new Color[Faces.Length];
                        var k = 0; //face counter
                        var lastColor = new Color(Constants.DefaultColor);
                        for (int i = 0; i < colorStringsArray.Length; i++)
                        {
                            var cStr = colorStringsArray[i];
                            // it's fast to check if the first character is a letter, so this 
                            // reduces the number of times we need to try to parse a number.
                            if (!char.IsLetter(cStr[0]) && int.TryParse(cStr, out var numRepeats))
                            {
                                for (var j = 0; j < numRepeats; j++)
                                    colors[k++] = lastColor;
                            }
                            else
                            {
                                cStr = "#" + cStr;
                                lastColor = new Color(cStr);
                                colors[k++] = lastColor;
                            }
                        }
                        break;
                }

                reader.Read();//go to next
            }
            //Lastly, assign faces and vertices to the primitives
            foreach (var prim in Primitives)
                prim.CompletePostSerialization(this);

            HasUniformColor = true;
            if (colors == null || colors.Length == 0)
                SolidColor = new Color(Constants.DefaultColor);
            else if (colors.Length == 1)
                SolidColor = colors[0];
            else
            {
                HasUniformColor = false;
                for (int i = 0; i < colors.Length; i++)
                    Faces[i].Color = colors[i];
                for (int i = colors.Length; i < Faces.Length; i++)
                    Faces[i].Color = new Color(Constants.DefaultColor);
            }

            //Lastly, define the border segments and border loops for each primitive.
            TessellationInspectAndRepair.DefineBorders(this);
            TessellationInspectAndRepair.CharacterizeBorders(this);
            //Get the max min bounds and set tolerance
            DefineAxisAlignedBoundingBoxAndTolerance();
            //DoublyConnectVerticesToFaces();
            //Build edges, convex hull, and anything else we need.
            TessellationInspectAndRepair.CompleteBuildOptions(this, tsBuildOptions, out var removedFaces);

            if (removedFaces.Count > 0)
            {
                // if the build/repair altered the faces, then we may need to check if there
                // are any faces still referenced in the primitives that need to be removed.
                var removedHash = removedFaces.ToHashSet();
                foreach (var prim in Primitives)
                {
                    prim.FaceIndices = null;
                    var needToResetOtherElements = false;
                    foreach (var face in prim.Faces)
                        if (removedHash.Contains(face))
                        {
                            prim.Faces.Remove(face);
                            needToResetOtherElements = true;
                        }
                    if (needToResetOtherElements)
                    {
                        prim.SetVerticesFromFaces();
                        prim.DefineInnerOuterEdges();
                    }
                }
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
        public TessellatedSolid(ICollection<TriangleFace> faces, ICollection<Vertex> vertices = null,
            TessellatedSolidBuildOptions buildOptions = null, IList<Color> colors = null,
            UnitType units = UnitType.unspecified, string name = "", string filename = "",
            List<string> comments = null, string language = "") : base(units, name, filename, comments, language)
        {
            if (buildOptions == null) buildOptions = TessellatedSolidBuildOptions.Minimal;
            if (colors != null && colors.Count == 1)
            {
                SolidColor = colors[0];
                HasUniformColor = true;
            }
            var manyInputColors = (colors != null && colors.Count > 1);
            if (manyInputColors) HasUniformColor = false;
            if (vertices == null)
            {
                vertices = new HashSet<Vertex>();
                foreach (var face in faces)
                    foreach (var vertex in face.Vertices)
                        ((HashSet<Vertex>)vertices).Add(vertex);
            }
            NumberOfVertices = vertices.Count;
            Vertices = new Vertex[vertices.Count];
            var oldNewVertexIndexDict = new Dictionary<int, int>();
            if (buildOptions.CopyElementsPassedToConstructor)
            {
                var i = 0;
                foreach (var vertex in vertices)
                {
                    oldNewVertexIndexDict.Add(vertex.IndexInList, i);
                    Vertices[i] = new Vertex(vertex.Coordinates, i);
                    i++;
                }
            }
            else
            {
                var i = 0;
                foreach (var vertex in vertices)
                {
                    vertex.IndexInList = i;
                    Vertices[i] = vertex;
                    i++;
                }
            }
            NumberOfFaces = faces.Count;
            Faces = new TriangleFace[faces.Count];
            if (buildOptions.CopyElementsPassedToConstructor)
            {
                var i = 0;
                foreach (var oldFace in faces)
                {
                    var newFace = Faces[i] = new TriangleFace(Vertices[oldNewVertexIndexDict[oldFace.A.IndexInList]],
                        Vertices[oldNewVertexIndexDict[oldFace.B.IndexInList]], Vertices[oldNewVertexIndexDict[oldFace.C.IndexInList]]);
                    newFace.IndexInList = i;
                    if (HasUniformColor)
                        newFace.Color = SolidColor;
                    else if (manyInputColors)
                    {
                        var j = i < colors.Count - 1 ? i : colors.Count - 1;
                        newFace.Color = colors[j];
                        if (!SolidColor.Equals(newFace.Color)) HasUniformColor = false;
                    }
                    i++;
                }
            }
            else
            {
                Faces = faces as TriangleFace[] ?? faces.ToArray();
                if (Faces[0].A.Faces == null || !Faces[0].A.Faces.Contains(Faces[0]))
                {
                    // if the vertices are not doubly linked to the faces, then do that now (this is the most common case)
                    for (int i = 0; i < Faces.Length; i++)
                    {
                        var face = Faces[i];
                        face.IndexInList = i;
                        foreach (var vertex in face.Vertices)
                            vertex.Faces.Add(face);
                    }
                }
            }
            TessellationInspectAndRepair.CompleteBuildOptions(this, buildOptions, out _);
        }

        /// <summary>
        /// Makes the edges if non existent.
        /// </summary>
        public void MakeEdgesIfNonExistent()
        {
            if (Edges != null && Edges.Length > 0) return;
            if (Errors == null) TessellationInspectAndRepair.CompleteBuildOptions(this,
                new TessellatedSolidBuildOptions
                {
                    AutomaticallyRepairHoles = false,
                    AutomaticallyRepairNegligibleTFaces = true,
                    FixEdgeDisassociations = true,
                    PredefineAllEdges = true
                }, out _);
            else Errors.MakeEdges();
        }

        #endregion

        #region Make many elements (called from constructors)
        /// <summary>
        /// Defines the axis aligned bounding box and tolerance. This is called first in the constructors
        /// because the tolerance is used in making the vertices.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        public void DefineAxisAlignedBoundingBoxAndTolerance(IEnumerable<Vector3> vertices = null)
        {
            var xMin = double.PositiveInfinity;
            var yMin = double.PositiveInfinity;
            var zMin = double.PositiveInfinity;
            var xMax = double.NegativeInfinity;
            var yMax = double.NegativeInfinity;
            var zMax = double.NegativeInfinity;
            if (vertices == null) vertices = Vertices.Select(v => v.Coordinates);
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
        internal void MakeFaces(IEnumerable<(int, int, int)> faceToVertexIndices, int numberOfFaces, IList<Color> colors,
            bool doublyLinkToVertices = true, bool duplicateFaceCheck = true)
        {
            NumberOfFaces = numberOfFaces;
            HasUniformColor = true;
            if (colors == null || !colors.Any() || colors.All(c => c == null))
                SolidColor = new Color(Constants.DefaultColor);
            else SolidColor = colors[0];
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
            var i = 0;
            foreach (var faceToVertexIndexList in faceToVertexIndices)
            {
                if (duplicateFaceCheck)
                {
                    // first check to be sure that this is a new face and not a duplicate or a degenerate
                    var orderedIndices = faceToVertexIndexList.EnumerateThruple().OrderBy(index => index).ToList();
                    while (orderedIndices.Count > checksumMultiplier.Count)
                        checksumMultiplier.Add((long)Math.Pow(NumberOfVertices, checksumMultiplier.Count));
                    var checksum = orderedIndices.Select((index, p) => index * checksumMultiplier[p]).Sum();
                    if (faceChecksums.Contains(checksum)) continue; //Duplicate face. Do not create
                    if (orderedIndices.Count < 3 || ContainsDuplicateIndices(orderedIndices)) continue;
                    // if you made it passed these to "continue" conditions, then this is a valid new face
                    faceChecksums.Add(checksum);
                }
                var color = SolidColor;
                if (colors != null && colors.Count > 0)
                {
                    if (i >= colors.Count) i = 0;
                    if (colors[i] != null) color = colors[i];
                    i++;
                    if (SolidColor == null || !SolidColor.Equals(color)) HasUniformColor = false;
                }
                var faceVertices =
                faceToVertexIndexList.EnumerateThruple().Select(vertexMatchingIndex => Vertices[vertexMatchingIndex]).ToArray();

                if (faceVertices.Length == 3)
                {
                    var face = new TriangleFace(faceVertices, doublyLinkToVertices);
                    if (!HasUniformColor) face.Color = color;
                    face.IndexInList = listOfFaces.Count;
                    listOfFaces.Add(face);
                }
                // todo: can stl have polygons greater than triangle?! if not, then simplify this code. it's unnecessaril complicated
                else
                {
                    var normal = MiscFunctions.DetermineNormalForA3DPolygon(faceVertices, faceVertices.Length, out _, Vector3.Null, out _);
                    var triangulatedList = faceVertices.Triangulate(normal);
                    var listOfFlatFaces = new List<TriangleFace>();
                    foreach (var vertexSet in triangulatedList)
                    {
                        var face = new TriangleFace(vertexSet.A, vertexSet.B, vertexSet.C, doublyLinkToVertices);
                        if (!HasUniformColor) face.Color = color;
                        listOfFaces.Add(face);
                        listOfFlatFaces.Add(face);
                        face.IndexInList = listOfFaces.Count;
                    }
                    Primitives ??= new List<PrimitiveSurface>();
                    Primitives.Add(new Plane(listOfFlatFaces));
                }
            }
            Faces = listOfFaces.ToArray();
            NumberOfFaces = Faces.Length;
        }


        /// <summary>
        /// Faces the checksum.
        /// </summary>
        /// <param name="checksumMultiplier">The checksum multiplier.</param>
        /// <param name="vertexIndices">The vertex indices.</param>
        /// <param name="orderedIndices">The ordered indices.</param>
        /// <returns>System.Int64.</returns>
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
        /// <summary>
        /// Faces the checksum.
        /// </summary>
        /// <param name="m1">The m1.</param>
        /// <param name="m2">The m2.</param>
        /// <param name="m3">The m3.</param>
        /// <param name="A">a.</param>
        /// <param name="B">The b.</param>
        /// <param name="C">The c.</param>
        /// <returns>System.Int64.</returns>
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

        /// <summary>
        /// Calculates the checksum.
        /// </summary>
        /// <param name="m1">The m1.</param>
        /// <param name="m2">The m2.</param>
        /// <param name="m3">The m3.</param>
        /// <param name="smallest">The smallest.</param>
        /// <param name="middle">The middle.</param>
        /// <param name="largest">The largest.</param>
        /// <returns>System.Int64.</returns>
        private long CalculateChecksum(long m1, long m2, long m3, int smallest, int middle, int largest)
        {
            return m1 * smallest + m2 * middle + m3 * largest;
        }

        /// <summary>
        /// Makes the vertices.
        /// </summary>
        /// <param name="vertsPerFace">The verts per face.</param>
        /// <param name="scaleFactor">The scale factor.</param>
        /// <param name="faceToVertexIndices">The face to vertex indices.</param>
        private void MakeVertices(IEnumerable<(Vector3, Vector3, Vector3)> vertsPerFace, double scaleFactor,
            out List<(int, int, int)> faceToVertexIndices)
        {
            var numDecimalPoints = 0;
            //Gets the number of decimal places
            while (Math.Round(scaleFactor * SameTolerance, numDecimalPoints) == 0.0) numDecimalPoints++;
            /* vertexMatchingIndices will be used to speed up the linking of faces and edges to vertices
             * it  preserves the order of vertsPerFace (as read in from the file), and indicates where
             * you can find each vertex in the new array of vertices. This is essentially what is built in 
             * the remainder of this method. */
            faceToVertexIndices = new List<(int, int, int)>();
            var listOfVertices = new List<Vector3>();
            var simpleCompareDict = new Dictionary<Vector3, int>();
            foreach (var t in vertsPerFace)
            {
                var locationIndices = new List<int>(); // this will become a row in faceToVertexIndices
                foreach (var coord in t.EnumerateThruple())
                {
                    /* given the low precision in files like STL, this should be a sufficient way to detect identical points. 
                     * I believe comparing these lookupStrings will be quicker than comparing two 3d points.*/
                    //First, round the vertices, then convert to a string. This will catch bidirectional tolerancing (+/-)
                    var coordinates = new Vector3(scaleFactor * Math.Round(coord.X, numDecimalPoints),
                        Math.Round(scaleFactor * coord.Y, numDecimalPoints), Math.Round(scaleFactor * coord.Z, numDecimalPoints));
                    if (simpleCompareDict.TryGetValue(coordinates, out int index))
                        /* if it's in the dictionary, simply put the location in the locationIndices */
                        locationIndices.Add(index);
                    else
                    {
                        /* else, add a new vertex to the list, and a new entry to simpleCompareDict. Also, be sure to indicate
                        * the position in the locationIndices. */
                        var newIndex = listOfVertices.Count;
                        listOfVertices.Add(coordinates);
                        simpleCompareDict.Add(coordinates, newIndex);
                        locationIndices.Add(newIndex);
                    }
                }
                faceToVertexIndices.Add((locationIndices[0], locationIndices[1], locationIndices[2]));
            }
            //Make vertices from the double arrays
            MakeVertices(listOfVertices, listOfVertices.Count);
        }

        /// <summary>
        /// Makes the vertices, and set CheckSum multiplier
        /// </summary>
        /// <param name="coordinates">The list of vertices.</param>
        private void MakeVertices(IEnumerable<Vector3> coordinates, int numberOfVertices)
        {
            NumberOfVertices = numberOfVertices;
            Vertices = new Vertex[NumberOfVertices];
            var i = 0;
            foreach (var coord in coordinates)
                Vertices[i] = new Vertex(coord, i++);
        }

        #endregion

        #region Add or Remove Items

        #region Vertices - the important thing about these is updating the IndexInList property of the vertices

        /// <summary>
        /// Adds the vertex.
        /// </summary>
        /// <param name="newVertex">The new vertex.</param>
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

        /// <summary>
        /// Adds the vertices.
        /// </summary>
        /// <param name="verticesToAdd">The vertices to add.</param>
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

        /// <summary>
        /// Replaces the vertex.
        /// </summary>
        /// <param name="removeVertex">The remove vertex.</param>
        /// <param name="newVertex">The new vertex.</param>
        /// <param name="removeReferecesToVertex">if set to <c>true</c> [remove refereces to vertex].</param>
        internal void ReplaceVertex(Vertex removeVertex, Vertex newVertex, bool removeReferecesToVertex = true)
        {
            ReplaceVertex(removeVertex.IndexInList, newVertex, removeReferecesToVertex);
        }

        /// <summary>
        /// Replaces the vertex.
        /// </summary>
        /// <param name="removeVIndex">Index of the remove v.</param>
        /// <param name="newVertex">The new vertex.</param>
        /// <param name="removeReferecesToVertex">if set to <c>true</c> [remove refereces to vertex].</param>
        internal void ReplaceVertex(int removeVIndex, Vertex newVertex, bool removeReferecesToVertex = true)
        {
            if (removeReferecesToVertex) RemoveReferencesToVertex(Vertices[removeVIndex]);
            newVertex.IndexInList = removeVIndex;
            Vertices[removeVIndex] = newVertex;
        }

        /// <summary>
        /// Removes the vertex.
        /// </summary>
        /// <param name="removeVertex">The remove vertex.</param>
        /// <param name="removeReferecesToVertex">if set to <c>true</c> [remove refereces to vertex].</param>
        internal void RemoveVertex(Vertex removeVertex, bool removeReferecesToVertex = true)
        {
            RemoveVertex(removeVertex.IndexInList, removeReferecesToVertex);
        }

        /// <summary>
        /// Removes the vertex.
        /// </summary>
        /// <param name="removeVIndex">Index of the remove v.</param>
        /// <param name="removeReferecesToVertex">if set to <c>true</c> [remove refereces to vertex].</param>
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

        /// <summary>
        /// Removes the vertices.
        /// </summary>
        /// <param name="removeVertices">The remove vertices.</param>
        internal void RemoveVertices(IEnumerable<Vertex> removeVertices)
        {
            RemoveVertices(removeVertices.Select(v => v.IndexInList).ToList());
        }

        /// <summary>
        /// Removes the vertices.
        /// </summary>
        /// <param name="removeIndices">The remove indices.</param>
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

        /// <summary>
        /// Updates all edge check sums.
        /// </summary>
        internal void UpdateAllEdgeCheckSums()
        {
            if (Edges != null)
                foreach (var edge in Edges)
                    Edge.SetAndGetEdgeChecksum(edge);
        }

        #endregion

        #region Faces

        /// <summary>
        /// Adds the face.
        /// </summary>
        /// <param name="newFace">The new face.</param>
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

        /// <summary>
        /// Adds the faces.
        /// </summary>
        /// <param name="facesToAdd">The faces to add.</param>
        public void AddFaces(IList<TriangleFace> facesToAdd)
        {
            var numToAdd = facesToAdd.Count;
            var newFaces = new TriangleFace[NumberOfFaces + numToAdd];
            Faces.CopyTo(newFaces, 0);
            //for (var i = 0; i < NumberOfFaces; i++)
            //    newFaces[i] = Faces[i];
            for (var i = 0; i < numToAdd; i++)
            {
                newFaces[NumberOfFaces + i] = facesToAdd[i];
                newFaces[NumberOfFaces + i].IndexInList = NumberOfFaces + i;
            }
            Faces = newFaces;
            NumberOfFaces += numToAdd;
        }

        /// <summary>
        /// Removes the face.
        /// </summary>
        /// <param name="removeFace">The remove face.</param>
        internal void RemoveFace(TriangleFace removeFace)
        {
            RemoveFace(removeFace.IndexInList);
        }

        /// <summary>
        /// Removes the face.
        /// </summary>
        /// <param name="removeFaceIndex">Index of the remove face.</param>
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

        /// <summary>
        /// Removes the faces.
        /// </summary>
        /// <param name="removeFaces">The remove faces.</param>
        public void RemoveFaces(IEnumerable<TriangleFace> removeFaces)
        {
            RemoveFaces(removeFaces.Select(f => f.IndexInList).ToList());
        }

        /// <summary>
        /// Removes the faces.
        /// </summary>
        /// <param name="removeIndices">The remove indices.</param>
        public void RemoveFaces(List<int> removeIndices)
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

        /// <summary>
        /// Removes the references to face.
        /// </summary>
        /// <param name="removeFaceIndex">Index of the remove face.</param>
        private void RemoveReferencesToFace(int removeFaceIndex)
        {
            var face = Faces[removeFaceIndex];
            foreach (var vertex in face.Vertices)
                vertex?.Faces.Remove(face);
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

        /// <summary>
        /// Adds the edge.
        /// </summary>
        /// <param name="newEdge">The new edge.</param>
        internal void AddEdge(Edge newEdge)
        {
            var newEdges = new Edge[NumberOfEdges + 1];
            for (var i = 0; i < NumberOfEdges; i++)
                newEdges[i] = Edges[i];
            newEdges[NumberOfEdges] = newEdge;
            if (newEdge.EdgeReference <= 0) Edge.SetAndGetEdgeChecksum(newEdge);
            newEdge.IndexInList = NumberOfEdges;
            Edges = newEdges;
            NumberOfEdges++;
        }

        /// <summary>
        /// Adds the edges.
        /// </summary>
        /// <param name="edgesToAdd">The edges to add.</param>
        internal void AddEdges(IList<Edge> edgesToAdd)
        {
            var numToAdd = edgesToAdd.Count;
            var newEdges = new Edge[NumberOfEdges + numToAdd];
            for (var i = 0; i < NumberOfEdges; i++)
                newEdges[i] = Edges[i];
            for (var i = 0; i < numToAdd; i++)
            {
                newEdges[NumberOfEdges + i] = edgesToAdd[i];
                if (newEdges[NumberOfEdges + i].EdgeReference <= 0) Edge.SetAndGetEdgeChecksum(newEdges[NumberOfEdges + i]);
                newEdges[NumberOfEdges + i].IndexInList = NumberOfEdges;
            }
            Edges = newEdges;
            NumberOfEdges += numToAdd;
        }

        /// <summary>
        /// Removes the edge.
        /// </summary>
        /// <param name="removeEdge">The remove edge.</param>
        internal void RemoveEdge(Edge removeEdge)
        {
            RemoveEdge(removeEdge.IndexInList);
        }

        /// <summary>
        /// Removes the edge.
        /// </summary>
        /// <param name="removeEdgeIndex">Index of the remove edge.</param>
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
            Edges = newEdges;
        }

        /// <summary>
        /// Removes the edges.
        /// </summary>
        /// <param name="removeEdges">The remove edges.</param>
        internal void RemoveEdges(IEnumerable<Edge> removeEdges)
        {
            RemoveEdges(removeEdges.Select(e => e.IndexInList).ToList());
        }

        /// <summary>
        /// Removes the edges.
        /// </summary>
        /// <param name="removeIndices">The remove indices.</param>
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
            Edges = newEdges;
        }

        /// <summary>
        /// Removes the references to edge.
        /// </summary>
        /// <param name="removeEdgeIndex">Index of the remove edge.</param>
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
        /// Adds the primitive.
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
        /// Copies this instance.
        /// </summary>
        /// <returns>TessellatedSolid.</returns>
        public TessellatedSolid Copy()
        {
            //Copy the solid. Do not check Edges[], rather use _edges, so that MakeEdges() does not get triggered.
            var copy = new TessellatedSolid(Faces, Vertices, new TessellatedSolidBuildOptions
            {
                AutomaticallyInvertNegativeSolids = false,
                AutomaticallyRepairNegligibleTFaces = false,
                AutomaticallyRepairHoles = false,
                CopyElementsPassedToConstructor = true,
                DefineConvexHull = ConvexHull != null,
                PredefineAllEdges = false,
                FindNonsmoothEdges = false,
            }, Faces.Select(p => p.Color).ToList(), Units, Name + "_Copy",
                FileName, Comments, Language);
            copy.TessellationError = TessellationError;
            copy.SourceIsSheetBody = SourceIsSheetBody;
            if (Primitives != null && Primitives.Any())
            {
                copy.NumberOfPrimitives = NumberOfPrimitives;
                foreach (var surface in Primitives)
                {
                    var surfCopy = surface.Copy(surface.FaceIndices.Select(fi => copy.Faces[fi]));
                    copy.AddPrimitive(surfCopy);
                }
            }
            if (Edges != null && Edges.Any())
            {
                copy.NumberOfEdges = NumberOfEdges;
                copy.Edges = new Edge[NumberOfEdges];
                for (int i = 0; i < Edges.Length; i++)
                {
                    Edge edge = Edges[i];
                    var edgeNew = new Edge(
                        copy.Vertices[edge.From.IndexInList],
                        copy.Vertices[edge.To.IndexInList],
                        copy.Faces[edge.OwnedFace.IndexInList],
                        copy.Faces[edge.OtherFace.IndexInList], true);
                    edgeNew.IndexInList = i;
                    edgeNew.Curvature = edge.Curvature;
                    edgeNew.PartOfConvexHull = edge.PartOfConvexHull;
                    copy.Edges[i] = edgeNew;
                }
            }
            if (NonsmoothEdges != null && NonsmoothEdges.Any())
            {
                copy.NonsmoothEdges = new List<Edge>();
                foreach (var edge in NonsmoothEdges)
                    copy.NonsmoothEdges.Add(copy.Edges[edge.IndexInList]);
            }
            TessellationInspectAndRepair.DefineBorders(copy);
            TessellationInspectAndRepair.CharacterizeBorders(copy);
            copy.ReferenceIndex = ReferenceIndex;
            return copy;
        }

        #endregion

        #region Reset Color Function
        /// <summary>
        /// Resets the default color.
        /// </summary>
        public void ResetDefaultColor()
        {
            var defaultColor = new Color(KnownColors.LightGray);
            foreach (var face in Faces) face.Color = defaultColor;
        }

        /// <summary>
        /// Resets the default color.
        /// </summary>
        /// <param name="primitives">The primitives.</param>
        public void ResetDefaultColor(IEnumerable<PrimitiveSurface> primitives)
        {
            var defaultColor = new Color(KnownColors.LightGray);
            foreach (var prim in primitives) prim.SetColor(defaultColor);
        }
        #endregion

        #region Transform
        /// <summary>
        /// Translates and Squares Tessellated Solid based on its oriented bounding box.
        /// The resulting Solid should be located at the origin, and only in the positive X, Y, Z octant.
        /// </summary>
        /// <param name="originalBoundingBox">The original bounding box.</param>
        /// <returns>TessellatedSolid.</returns>
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
        /// <param name="originalBoundingBox">The original bounding box.</param>
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
            foreach (var face in Faces.Concat(ConvexHull.Faces))
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
            _volume = double.NaN;
            _surfaceArea = double.NaN;
            _center = _center.Transform(transformMatrix);
            // I'm not sure this is right, but I'm just using the 3x3 rotational submatrix to rotate the inertia tensor
            var rotMatrix = new Matrix3x3(transformMatrix.M11, transformMatrix.M12, transformMatrix.M13,
                    transformMatrix.M21, transformMatrix.M22, transformMatrix.M23,
                    transformMatrix.M31, transformMatrix.M32, transformMatrix.M33);
            _inertiaTensor *= rotMatrix;
            if (Primitives != null)
                foreach (var primitive in Primitives)
                    primitive.Transform(transformMatrix);
        }

        /// <summary>
        /// Gets a new solid by transforming its vertices.
        /// </summary>
        /// <param name="transformationMatrix">The transformation matrix.</param>
        /// <returns>Solid.</returns>
        public override Solid TransformToNewSolid(Matrix4x4 transformationMatrix)
        {
            var copy = this.Copy();
            copy.Transform(transformationMatrix);
            return copy;
        }


        /// <summary>
        /// Turns the model inside out.
        /// </summary>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool TurnModelInsideOut()
        {
            _volume = -1 * _volume;
            _inertiaTensor = Matrix3x3.Null;
            //todo
            foreach (var face in Faces) face.Invert();

            if (Edges != null)
                foreach (var edge in Edges) edge.Invert();
            return true;
        }

        /// <summary>
        /// Calculates the center.
        /// </summary>
        protected override void CalculateCenter() => Faces.CalculateVolumeAndCenter(SameTolerance, out _volume, out _center);

        /// <summary>
        /// Calculates the volume.
        /// </summary>
        protected override void CalculateVolume() => Faces.CalculateVolumeAndCenter(SameTolerance, out _volume, out _center);

        /// <summary>
        /// Calculates the surface area.
        /// </summary>
        protected override void CalculateSurfaceArea() => _surfaceArea = Faces.Sum(face => face.Area);

        /// <summary>
        /// Calculates the inertia tensor.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        protected override void CalculateInertiaTensor() => _inertiaTensor = Faces.CalculateInertiaTensor(Center);

        #endregion

        /// <summary>
        /// Determines whether [contains duplicate indices] [the specified ordered indices].
        /// </summary>
        /// <param name="orderedIndices">The ordered indices.</param>
        /// <returns><c>true</c> if [contains duplicate indices] [the specified ordered indices]; otherwise, <c>false</c>.</returns>
        private static bool ContainsDuplicateIndices(List<int> orderedIndices)
        {
            for (var i = 0; i < orderedIndices.Count - 1; i++)
                if (orderedIndices[i] == orderedIndices[i + 1]) return true;
            return false;
        }

        /// <summary>
        /// Removes the references to vertex.
        /// </summary>
        /// <param name="vertex">The vertex.</param>
        private static void RemoveReferencesToVertex(Vertex vertex)
        {
            foreach (var face in vertex.Faces.ToList())
            {
                face.ReplaceVertex(vertex, null);
            }
            foreach (var edge in vertex.Edges)
            {
                if (vertex == edge.To) edge.To = null;
                if (vertex == edge.From) edge.From = null;
            }
        }

    }
}