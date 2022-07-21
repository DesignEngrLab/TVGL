using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

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

        public Solid[] Solids { get; set; }
  
        public SolidAssembly()
        {
            RootAssembly = new SubAssembly(this);
            _distinctSolids = new Dictionary<Solid, int>();
        }

        public SolidAssembly(IEnumerable<Solid> solids)
        {
            Name = solids.First().Name;
            if (Name.Length == 0)
                Name = "SolidAssembly";
            RootAssembly = new SubAssembly(this);
            foreach (var solid in solids)
                RootAssembly.Add(solid, Matrix4x4.Identity);
            _distinctSolids = new Dictionary<Solid, int>();
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
    }

    //A wrapper class for solids that recursively contains subassemblies and solid parts. 
    [JsonObject(MemberSerialization.OptOut)]
    public class SubAssembly
    {
        [JsonIgnore]
        //Pointer to the global parent, where the Tessellated solids and file information are stored.
        public SolidAssembly SolidAssemblyGlobalInfo { get; set; }

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


        public IEnumerable<(TessellatedSolid, Matrix4x4)> AllTessellatedSolidsInGlobalCoordinateSystem()
        {
            return AllPartsInGlobalCoordinateSystem.Where(s => s.Item1 is TessellatedSolid).Cast<(TessellatedSolid, Matrix4x4)> ();
        }

        //Recursive call to get all the parts in the assembly. Transforms each instance of each part 
        //into the global coordinate system (GCS) by using TransformToNewSolid(). 
        //Parts that are referenced more than once are duplicated into mutliple positions within the GCS.
        private IEnumerable<(Solid, Matrix4x4)> _allPartsInGlobalCoordinateSystem;
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
