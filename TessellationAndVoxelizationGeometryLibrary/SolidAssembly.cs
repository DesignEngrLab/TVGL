using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace TVGL
{
    [JsonObject(MemberSerialization.OptOut)]
    public class SolidAssembly
    {
        public string Name { get; set; }

        public SubAssembly RootAssembly { get; set; }

        //A tempory dictionary of all the distinct solids used when reading in the assembly information
        [JsonIgnore]
        internal Dictionary<Solid, int> _distinctSolids { get; set; }

        [JsonIgnore]
        public Solid[] Solids { get; set; }

        public SolidAssembly() 
        { //Empty Constructor for JSON
        }

        public SolidAssembly(string name)
        {
            Name = name;
            RootAssembly = new SubAssembly(this);
            _distinctSolids = new Dictionary<Solid, int>();
        }

        public SolidAssembly(IEnumerable<Solid> solids, string fileName = "")
        {
            if(fileName.Length > 0)
                Name = fileName;
            else
            {
                Name = solids.First().Name;
                if (Name.Length == 0)
                    Name = "SolidAssembly";
            }

            RootAssembly = new SubAssembly(this);
            foreach (var solid in solids)
                RootAssembly.Add(solid, Matrix4x4.Identity);

            _distinctSolids = new Dictionary<Solid, int>();
            Solids = solids.ToArray();
        }

        public void CompleteInitialization()
        {
            Solids = _distinctSolids.Keys.ToArray();
            //Set the index for each of the solids.
            for(var i = 0; i < Solids.Length; i++)
                Solids[i].ReferenceIndex = i;
            _distinctSolids.Clear();
        }

        public bool IsEmpty()
        {
            return !Solids.Any();
        }

        [OnSerializing]
        protected void OnSerializingMethod(StreamingContext context)
        {
            serializationData = new Dictionary<string, JToken>();            
            serializationData.Add("TessellatedSolids", JToken.FromObject(Solids.Where(p => p is TessellatedSolid)));
            serializationData.Add("CrossSectionSolids", JToken.FromObject(Solids.Where(p => p is CrossSectionSolid)));
            serializationData.Add("VolizedSolids", JToken.FromObject(Solids.Where(p => p is VoxelizedSolid)));
        }

        [OnDeserialized]
        protected void OnDeserializedMethod(StreamingContext context)
        {
            JArray jArray = (JArray)serializationData["TessellatedSolids"];
            Solids = jArray.ToObject<TessellatedSolid[]>();
            RootAssembly.SetGlobalAssembly(this);
        }

        // everything else gets stored here
        [JsonExtensionData]
        protected IDictionary<string, JToken> serializationData;
    }

    //A wrapper class for solids that recursively contains subassemblies and solid parts. 
    [JsonObject(MemberSerialization.OptOut)]
    public class SubAssembly
    {
        [JsonIgnore]
        //Pointer to the global parent, where the Tessellated solids and file information are stored.
        private SolidAssembly SolidAssemblyGlobalInfo { get; set; }

        //Recursively set GlobalAssembly. Used on deserialization.
        public void SetGlobalAssembly(SolidAssembly global)
        {
            SolidAssemblyGlobalInfo = global;
            foreach (var (subAssembly, _) in SubAssemblies)
                subAssembly.SetGlobalAssembly(global);
        }

        //List of assemblies with their backtransform to the global assembly space.
        //Not a dictionary, so that items can be referenced in multiple transform locations (i.e., duplicate keys)
        public List<(SubAssembly assembly, Matrix4x4 backtransform)> SubAssemblies { get; set; }

        //List of parts with their backtransform to the global assembly space.
        //Not a dictionary, so that parts can be referenced in multiple transform locations (i.e., duplicate keys)
        //Uses integers for easier serialize/deserialize. Get solids from GlobalAssembly.DistinctParts
        public List<(int solid, Matrix4x4 backtransform)> Solids { get; set; }

        public SubAssembly(SolidAssembly globalAssembly)
        {
            SolidAssemblyGlobalInfo = globalAssembly;
            SubAssemblies = new List<(SubAssembly assembly, Matrix4x4 backtransform)>();
            Solids = new List<(int solid, Matrix4x4 backtransform)>();
        }

        public void Add(Solid solid, Matrix4x4 backtransform)
        {
            if (!SolidAssemblyGlobalInfo._distinctSolids.ContainsKey(solid))
                SolidAssemblyGlobalInfo._distinctSolids.Add(solid, SolidAssemblyGlobalInfo._distinctSolids.Count);
            Solids.Add((SolidAssemblyGlobalInfo._distinctSolids[solid], backtransform));
        }

        public void Add(SubAssembly subassembly, Matrix4x4 backtransform)
        {
            SubAssemblies.Add((subassembly, backtransform));
        }

        public bool IsEmpty()
        {
            return !AllParts().Any();
        }

        public TessellatedSolid[] AllTessellatedSolidsInGlobalCoordinateSystem()
        {
            return AllPartsInGlobalCoordinateSystem.Where(s => s.Item1 is TessellatedSolid).Select(s => s.Item1).Cast<TessellatedSolid>().ToArray();
        }

        public IEnumerable<(TessellatedSolid, Matrix4x4)> AllTessellatedSolidsWithGlobalCoordinateSystemTransform()
        {
            var returnList = new List<(TessellatedSolid, Matrix4x4)>();
            var allParts = AllPartsInGlobalCoordinateSystem.Where(s => s.Item1 is TessellatedSolid);
            foreach(var part in allParts)
                returnList.Add(((TessellatedSolid)part.Item1, part.Item2));
            return returnList;
        }

        //Recursive call to get all the parts in the assembly. Transforms each instance of each part 
        //into the global coordinate system (GCS) by using TransformToNewSolid(). 
        //Parts that are referenced more than once are duplicated into mutliple positions within the GCS.
        private IEnumerable<(Solid, Matrix4x4)> _allPartsInGlobalCoordinateSystem;
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

        public void Transform(Matrix4x4 transformMatrix)
        {
            throw new NotImplementedException();
        }

        public SubAssembly TransformToNewAssembly(Matrix4x4 transformationMatrix)
        {
            throw new NotImplementedException();
        }

        protected void CalculateCenter()
        {
            throw new NotImplementedException();
        }

        protected void CalculateInertiaTensor()
        {
            throw new NotImplementedException();
        }
    }
}
