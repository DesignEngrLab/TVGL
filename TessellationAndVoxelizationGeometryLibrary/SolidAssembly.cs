// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-07-2023
// ***********************************************************************
// <copyright file="SolidAssembly.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace TVGL
{
    /// <summary>
    /// Class SolidAssembly.
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class SolidAssembly
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        public int NumberOfSolidBodies
        {
            get
            {
                if (numberOfSolidBodies < 0)
                    numberOfSolidBodies = Solids.Count(t => t is TessellatedSolid ts && !ts.SourceIsSheetBody);
                return numberOfSolidBodies;
            }
        }

        //If a CAD model was healed, the converter may still list the body as a sheet.
        //Otherwise, if there are a mix of sheet and solid bodies, there may be an issue
        //in the original conversion.
        public int NumberOfSheetBodies
        {
            get
            {
                if (numberOfSheetBodies < 0)
                    numberOfSheetBodies = Solids.Count(t => t is TessellatedSolid ts && ts.SourceIsSheetBody);
                return numberOfSheetBodies;
            }
        }
        /// <summary>
        /// Gets or sets the root assembly.
        /// </summary>
        /// <value>The root assembly.</value>
        public SubAssembly RootAssembly { get; set; }

        //A tempory dictionary of all the distinct solids used when reading in the assembly information
        /// <summary>
        /// Gets or sets the distinct solids.
        /// </summary>
        /// <value>The distinct solids.</value>
        [JsonIgnore]
        internal Dictionary<Solid, int> _distinctSolids { get; set; }

        /// <summary>
        /// Gets or sets the solids.
        /// </summary>
        /// <value>The solids.</value>
        [JsonIgnore]
        public Solid[] Solids { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SolidAssembly"/> class.
        /// </summary>
        public SolidAssembly()
        { //Empty Constructor for JSON
            RootAssembly = new SubAssembly(this);
            _distinctSolids = new Dictionary<Solid, int>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SolidAssembly"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public SolidAssembly(string name):this()
        {
            Name = name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SolidAssembly"/> class.
        /// </summary>
        /// <param name="solids">The solids.</param>
        /// <param name="fileName">Name of the file.</param>
        public SolidAssembly(IEnumerable<Solid> solids, string fileName = ""):this()
        {
            if (fileName.Length > 0)
                Name = fileName;
            else
            {
                Name = solids.First().Name;
                if (Name.Length == 0)
                    Name = "SolidAssembly";
            }

            foreach (var solid in solids)
                RootAssembly.Add(solid, Matrix4x4.Identity);

            Solids = solids.ToArray();
        }

        /// <summary>
        /// Completes the initialization.
        /// </summary>
        public void CompleteInitialization()
        {
            Solids = _distinctSolids.Keys.ToArray();
            //Set the index for each of the solids.
            for (var i = 0; i < Solids.Length; i++)
                Solids[i].ReferenceIndex = i;
            _distinctSolids.Clear();
        }

        /// <summary>
        /// Gets the TessellatedSolids from the SolidAssembly. Optional argument to return one type (recommended).
        /// Use case is when a model has both solid bodies and surface bodies. In this case, you usually just want
        /// the solid bodies. Otherwise, you could try stitching them together.
        /// </summary>
        /// <param name="containsBothTypes"></param>
        /// <param name="onlyReturnOneBodyType"></param>
        /// <returns></returns>
        public void GetTessellatedSolids(out IEnumerable<TessellatedSolid> solids, out IEnumerable<TessellatedSolid> sheets)
        {
            solids = Solids.Where(t => t is TessellatedSolid ts && !ts.SourceIsSheetBody).Select(t => (TessellatedSolid)t);
            sheets = Solids.Where(t => t is TessellatedSolid ts && ts.SourceIsSheetBody).Select(t => (TessellatedSolid)t);
        }

        public bool ContainsBothBodyTypes() => NumberOfSheetBodies > 0 && NumberOfSolidBodies > 0;

        /// <summary>
        /// Determines whether this instance is empty.
        /// </summary>
        /// <returns><c>true</c> if this instance is empty; otherwise, <c>false</c>.</returns>
        public bool IsEmpty()
        {
            return !Solids.Any();
        }

        /// <summary>
        /// Streams the write.
        /// </summary>
        /// <param name="writer">The writer.</param>
        public void StreamWrite(JsonTextWriter writer)
        {
            writer.WriteStartObject();
            {
                writer.WritePropertyName("SolidAssembly");
                useOnSerialization = false;//Don't serialize the TessellatedSolids directly. We are doing to do this with a stream.
                var jsonString = ((string)JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.None));//ignore last character so that the object is not closed
                //JSON uses a key value structure. We already wrote the key, now we write the value to end this object. This adds its own comma.
                writer.WriteRawValue(jsonString);

                //Write each solid as it's own object on this level rather than in an array. This will allow us more control about the solid, and
                //will make it easier to read in, since there is one fewer global levels in the JSON structure. Pointers from the assembly refer 
                //to the solid index and/or the order of the solids.
                var i = 0;
                foreach (var solid in Solids.Where(p => p is TessellatedSolid))
                {
                    writer.WritePropertyName("TessellatedSolid" + i);
                    var ts = (TessellatedSolid)solid;
                    writer.WriteStartObject();
                    {
                        ts.StreamWrite(writer, i++);
                    }
                    writer.WriteEndObject();
                }
            }
            writer.WriteEndObject();
        }

        /// <summary>
        /// Streams the read.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="assembly">The assembly.</param>
        internal static void StreamRead(JsonTextReader reader, out SolidAssembly assembly, TessellatedSolidBuildOptions tsBuildOptions)
        {
            var solids = new Dictionary<int, TessellatedSolid>();
            assembly = new SolidAssembly();
            //useOnSerialization = false;//Don't serialize the TessellatedSolids directly. We are doing to do this with a stream.
            var jsonSerializer = new JsonSerializer();
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.PropertyName && reader.Value.ToString() == "SolidAssembly")
                {
                    reader.Read();//Skip to object
                    assembly = jsonSerializer.Deserialize<SolidAssembly>(reader);
                }
                else if (reader.TokenType == JsonToken.PropertyName && reader.Value.ToString().Contains("TessellatedSolid"))
                {
                    var solid = new TessellatedSolid();
                    solid.StreamRead(reader, out var index, tsBuildOptions);
                    solids.Add(index, solid);
                }
                /* what if assembly could store other types of solids (e.g. VoxelizedSolids)
                else if (reader.TokenType == JsonToken.PropertyName)
                {
                    var typeString = reader.Value.ToString();
                    var splitIndex = typeString.FindIndex(char.IsNumber);
                    var index = int.Parse(typeString.Substring(splitIndex));
                    typeString.Substring(0, splitIndex);
                    var type = Assembly.GetExecutingAssembly().GetType(typeString);
                    JObject obj = JObject.Load(reader);
                    if (type != null && obj != null)
                    {
                        var solidInner = obj.ToObject(type) as Solid;
                        if (solidInner != null)
                            solids.Add(index, solidInner);
                        else
                            Debug.WriteLine("Unknown type: " + typeString);
                    }
                */
            }
            assembly.Solids = solids.Values.ToArray();
            assembly.RootAssembly.SetGlobalAssembly(assembly);
        }

        /// <summary>
        /// The use on serialization
        /// </summary>
        private bool useOnSerialization = false;

        /// <summary>
        /// Called when [serializing method].
        /// </summary>
        /// <param name="context">The context.</param>
        [OnSerializing]
        protected void OnSerializingMethod(StreamingContext context)
        {
            if (!useOnSerialization) return;
            serializationData = new Dictionary<string, JToken>();
            serializationData.Add("TessellatedSolids", JToken.FromObject(Solids.Where(p => p is TessellatedSolid)));
            serializationData.Add("CrossSectionSolids", JToken.FromObject(Solids.Where(p => p is CrossSectionSolid)));
            serializationData.Add("VoxelizedSolids", JToken.FromObject(Solids.Where(p => p is VoxelizedSolid)));
        }

        /// <summary>
        /// Called when [deserialized method].
        /// </summary>
        /// <param name="context">The context.</param>
        [OnDeserialized]
        protected void OnDeserializedMethod(StreamingContext context)
        {
            if (!useOnSerialization) return;
            JArray jArray = (JArray)serializationData["TessellatedSolids"];
            Solids = jArray.ToObject<TessellatedSolid[]>();
            RootAssembly.SetGlobalAssembly(this);
        }

        // everything else gets stored here
        /// <summary>
        /// The serialization data
        /// </summary>
        [JsonExtensionData]
        protected IDictionary<string, JToken> serializationData;
        private int numberOfSolidBodies = -1;
        private int numberOfSheetBodies = -1;
    }

    //A wrapper class for solids that recursively contains subassemblies and solid parts. 
    /// <summary>
    /// Class SubAssembly.
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class SubAssembly
    {
        /// <summary>
        /// Gets or sets the solid assembly global information.
        /// </summary>
        /// <value>The solid assembly global information.</value>
        [JsonIgnore]
        //Pointer to the global parent, where the Tessellated solids and file information are stored.
        private SolidAssembly SolidAssemblyGlobalInfo { get; set; }

        //Recursively set GlobalAssembly. Used on deserialization.
        /// <summary>
        /// Sets the global assembly.
        /// </summary>
        /// <param name="global">The global.</param>
        public void SetGlobalAssembly(SolidAssembly global)
        {
            SolidAssemblyGlobalInfo = global;
            foreach (var (subAssembly, _) in SubAssemblies)
                subAssembly.SetGlobalAssembly(global);
        }

        //List of assemblies with their backtransform to the global assembly space.
        //Not a dictionary, so that items can be referenced in multiple transform locations (i.e., duplicate keys)
        /// <summary>
        /// Gets or sets the sub assemblies.
        /// </summary>
        /// <value>The sub assemblies.</value>
        public List<(SubAssembly assembly, Matrix4x4 backtransform)> SubAssemblies { get; set; }

        //List of parts with their backtransform to the global assembly space.
        //Not a dictionary, so that parts can be referenced in multiple transform locations (i.e., duplicate keys)
        //Uses integers for easier serialize/deserialize. Get solids from GlobalAssembly.DistinctParts
        /// <summary>
        /// Gets or sets the solids.
        /// </summary>
        /// <value>The solids.</value>
        public List<(int solid, Matrix4x4 backtransform)> Solids { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubAssembly"/> class.
        /// </summary>
        /// <param name="globalAssembly">The global assembly.</param>
        public SubAssembly(SolidAssembly globalAssembly)
        {
            SolidAssemblyGlobalInfo = globalAssembly;
            SubAssemblies = new List<(SubAssembly assembly, Matrix4x4 backtransform)>();
            Solids = new List<(int solid, Matrix4x4 backtransform)>();
        }

        /// <summary>
        /// Adds the specified solid.
        /// </summary>
        /// <param name="solid">The solid.</param>
        /// <param name="backtransform">The backtransform.</param>
        public void Add(Solid solid, Matrix4x4 backtransform)
        {
            if (!SolidAssemblyGlobalInfo._distinctSolids.ContainsKey(solid))
                SolidAssemblyGlobalInfo._distinctSolids.Add(solid, SolidAssemblyGlobalInfo._distinctSolids.Count);
            Solids.Add((SolidAssemblyGlobalInfo._distinctSolids[solid], backtransform));
        }

        /// <summary>
        /// Adds the specified subassembly.
        /// </summary>
        /// <param name="subassembly">The subassembly.</param>
        /// <param name="backtransform">The backtransform.</param>
        public void Add(SubAssembly subassembly, Matrix4x4 backtransform)
        {
            SubAssemblies.Add((subassembly, backtransform));
        }

        /// <summary>
        /// Determines whether this instance is empty.
        /// </summary>
        /// <returns><c>true</c> if this instance is empty; otherwise, <c>false</c>.</returns>
        public bool IsEmpty()
        {
            return !AllParts().Any();
        }

        /// <summary>
        /// Alls the tessellated solids in global coordinate system.
        /// </summary>
        /// <returns>TessellatedSolid[].</returns>
        public TessellatedSolid[] AllTessellatedSolidsInGlobalCoordinateSystem()
        {
            return AllPartsInGlobalCoordinateSystem.Where(s => s.Item1 is TessellatedSolid).Select(s => s.Item1).Cast<TessellatedSolid>().ToArray();
        }

        /// <summary>
        /// Alls the tessellated solids with global coordinate system transform.
        /// </summary>
        /// <returns>IEnumerable&lt;System.ValueTuple&lt;TessellatedSolid, Matrix4x4&gt;&gt;.</returns>
        public IEnumerable<(TessellatedSolid, Matrix4x4)> AllTessellatedSolidsWithGlobalCoordinateSystemTransform()
        {
            var returnList = new List<(TessellatedSolid, Matrix4x4)>();
            var allParts = AllPartsInGlobalCoordinateSystem.Where(s => s.Item1 is TessellatedSolid);
            foreach (var part in allParts)
                returnList.Add(((TessellatedSolid)part.Item1, part.Item2));
            return returnList;
        }

        //Recursive call to get all the parts in the assembly. Transforms each instance of each part 
        //into the global coordinate system (GCS) by using TransformToNewSolid(). 
        //Parts that are referenced more than once are duplicated into mutliple positions within the GCS.
        /// <summary>
        /// All parts in global coordinate system
        /// </summary>
        private IEnumerable<(Solid, Matrix4x4)> _allPartsInGlobalCoordinateSystem;
        /// <summary>
        /// Gets all parts in global coordinate system.
        /// </summary>
        /// <value>All parts in global coordinate system.</value>
        [JsonIgnore]
        public IEnumerable<(Solid, Matrix4x4)> AllPartsInGlobalCoordinateSystem
        {
            get
            {
                if (_allPartsInGlobalCoordinateSystem == null)
                    _allPartsInGlobalCoordinateSystem = GetAllPartsInGlobalCoordinateSystem();
                return _allPartsInGlobalCoordinateSystem;
            }
        }

        /// <summary>
        /// Gets all parts in global coordinate system.
        /// </summary>
        /// <returns>IEnumerable&lt;System.ValueTuple&lt;Solid, Matrix4x4&gt;&gt;.</returns>
        private IEnumerable<(Solid, Matrix4x4)> GetAllPartsInGlobalCoordinateSystem()
        {
            foreach (var (part, backTransform) in AllParts())
            {
                var transformed = part.TransformToNewSolid(backTransform);
                yield return ((TessellatedSolid)transformed, backTransform);
            }
        }

        //Recursive call to get all the parts in the assembly. Returns the Transform to get each instance
        //of the part back into assembly space, by using TransformToNewSolid.
        //Parts that are referenced more than once are added more than once to the list.
        /// <summary>
        /// Alls the parts.
        /// </summary>
        /// <returns>List&lt;System.ValueTuple&lt;Solid, Matrix4x4&gt;&gt;.</returns>
        public List<(Solid, Matrix4x4)> AllParts()
        {
            var allParts = new List<(Solid, Matrix4x4)>();
            foreach (var (partIndex, backTransform) in Solids)
                allParts.Add((SolidAssemblyGlobalInfo.Solids[partIndex], backTransform));
            foreach (var (assembly, assemblyBackTransform) in SubAssemblies)
                foreach (var (part, backTransform) in assembly.AllParts())
                    allParts.Add((part, backTransform * assemblyBackTransform));
            return allParts;
        }

        /// <summary>
        /// Transforms the specified transform matrix.
        /// </summary>
        /// <param name="transformMatrix">The transform matrix.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void Transform(Matrix4x4 transformMatrix)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Transforms to new assembly.
        /// </summary>
        /// <param name="transformationMatrix">The transformation matrix.</param>
        /// <returns>SubAssembly.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public SubAssembly TransformToNewAssembly(Matrix4x4 transformationMatrix)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Calculates the center.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        protected void CalculateCenter()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Calculates the inertia tensor.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        protected void CalculateInertiaTensor()
        {
            throw new NotImplementedException();
        }
    }
}
