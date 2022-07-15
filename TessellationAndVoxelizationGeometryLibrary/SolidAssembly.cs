using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace TVGL
{
    [JsonObject(MemberSerialization.OptOut)]
    public class SolidAssemblyRoot
    {
        public string Name { get; set; }

        public SolidAssembly SolidAssembly { get; set; }

        //A tempory dictionary of all the distinct solids used when reading in the assembly information
        [JsonIgnore]
        internal Dictionary<TessellatedSolid, int> _distinctSolids { get; set; }

        public TessellatedSolid[] Solids { get; set; }
  
        public SolidAssemblyRoot()
        {
            SolidAssembly = new SolidAssembly(this);
            _distinctSolids = new Dictionary<TessellatedSolid, int>();
        }

        public void CompleteInitialization()
        {
            Solids = _distinctSolids.Keys.ToArray();
            //Set the index for each of the solids.
            for(var i = 0; i < Solids.Length; i++)
                Solids[i].Index = i;
            _distinctSolids.Clear();
        }

        public bool IsEmpty()
        {
            return !Solids.Any();
        }

        public void SaveTo(string filePath, bool exportAsHumanReadable = false)
        {
            var tvglFilePath = Path.ChangeExtension(filePath, ".tvgl");//Make sure the extension is .tvgl
            var jsonString = JsonConvert.SerializeObject(this, Formatting.None);
            if (exportAsHumanReadable)//This file can be read with a JSON viewer
            {
                using (StreamWriter writer = File.CreateText(tvglFilePath))
                {
                    writer.WriteLine(jsonString);
                }
            }
            else //zip the file. Can be 25% of the original size.
            {
                using (var stream = new FileStream(tvglFilePath, FileMode.Create))
                {
                    using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
                    {
                        using (StreamWriter writer = new StreamWriter(archive.CreateEntry(tvglFilePath).Open()))
                        {
                            writer.WriteLine(jsonString);
                        }
                    }
                }
            }            
        }

        public static SolidAssemblyRoot LoadFrom(string filePath)
        {
            try //Default to attempting as zip file
            {
                var serializer = new JsonSerializer();
                // string json = "";
                using (var stream = new FileStream(filePath, FileMode.Open))
                {
                    using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, true))
                    {
                        using (var reader = new JsonTextReader(new StreamReader(archive.Entries[0].Open())))
                        {
                            return serializer.Deserialize<SolidAssemblyRoot>(reader);
                        }     
                    }
                }
            }
            catch
            {
                var serializer = new JsonSerializer();
                using (var stream = new FileStream(filePath, FileMode.Open))
                {
                    using (var reader = new JsonTextReader(new StreamReader(stream)))
                    {
                        return serializer.Deserialize<SolidAssemblyRoot>(reader);
                    }       
                }
            }
        }
    }

    //A wrapper class for solids that recursively contains subassemblies and solid parts. 
    [JsonObject(MemberSerialization.OptOut)]
    public class SolidAssembly
    {
        [JsonIgnore]
        //Pointer to the global parent, where the Tessellated solids and file information are stored.
        public SolidAssemblyRoot GlobalAssemblyInfo { get; set; }

        //List of assemblies with their backtransform to the global assembly space.
        //Not a dictionary, so that items can be referenced in multiple transform locations (i.e., duplicate keys)
        public List<(SolidAssembly assembly, Matrix4x4 backtransform)> SubAssemblies { get; set; }

        //List of parts with their backtransform to the global assembly space.
        //Not a dictionary, so that parts can be referenced in multiple transform locations (i.e., duplicate keys)
        //Uses integers for easier serialize/deserialize. Get solids from GlobalAssembly.DistinctParts
        public List<(int solid, Matrix4x4 backtransform)> Solids { get; set; }

        public SolidAssembly(SolidAssemblyRoot globalAssembly)
        {
            GlobalAssemblyInfo = globalAssembly;
            SubAssemblies = new List<(SolidAssembly assembly, Matrix4x4 backtransform)>();
            Solids = new List<(int solid, Matrix4x4 backtransform)>();
        }

        public void Add(TessellatedSolid solid, Matrix4x4 backtransform)
        {
            if (!GlobalAssemblyInfo._distinctSolids.ContainsKey(solid))
                GlobalAssemblyInfo._distinctSolids.Add(solid, GlobalAssemblyInfo._distinctSolids.Count);
            Solids.Add((GlobalAssemblyInfo._distinctSolids[solid], backtransform));
        }

        public void Add(SolidAssembly subassembly, Matrix4x4 backtransform)
        {
            SubAssemblies.Add((subassembly, backtransform));
        }

        public bool IsEmpty()
        {
            return !AllParts().Any();
        }

        //Recursive call to get all the parts in the assembly. Transforms each instance of each part 
        //into the global coordinate system (GCS) by using TransformToNewSolid(). 
        //Parts that are referenced more than once are duplicated into mutliple positions within the GCS.
        public IEnumerable<TessellatedSolid> AllPartsInGlobalCoordinateSystem()
        {
            foreach (var (part, backTransform) in AllParts())
            {
                var transformed = part.TransformToNewSolid(backTransform);
                yield return (TessellatedSolid)transformed;
            }             
        }

        //Recursive call to get all the parts in the assembly. Returns the Transform to get each instance
        //of the part back into assembly space, by using TransformToNewSolid.
        //Parts that are referenced more than once are added more than once to the list.
        public List<(TessellatedSolid, Matrix4x4)> AllParts()
        {
            var allParts = new List<(TessellatedSolid, Matrix4x4)>();
            foreach (var (partIndex, backTransform) in Solids)
                allParts.Add((GlobalAssemblyInfo.Solids[partIndex], backTransform));
            foreach (var (assembly, assemblyBackTransform) in SubAssemblies)
                foreach (var (part, backTransform) in assembly.AllParts())
                    allParts.Add((part, backTransform * assemblyBackTransform));
            return allParts;
        }

        public void Transform(Matrix4x4 transformMatrix)
        {
            throw new NotImplementedException();
        }

        public SolidAssembly TransformToNewAssembly(Matrix4x4 transformationMatrix)
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
