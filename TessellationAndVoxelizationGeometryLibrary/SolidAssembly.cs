using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    //A wrapper class for solids that recursively contains subassemblies and solid parts. 
    public class SolidAssembly
    {
        //List of assemblies with their backtransform to the global assembly space.
        //Not a dictionary, so that items can be referenced in multiple transform locations (i.e., duplicate keys)
        public List<(SolidAssembly assembly, Matrix4x4 backtransform)> SubAssemblies { get; set; }
        public List<(TessellatedSolid solid, Matrix4x4 backtransform)> Parts { get; set; }

        public SolidAssembly()
        {
            SubAssemblies = new List<(SolidAssembly assembly, Matrix4x4 backtransform)>();
            Parts = new List<(TessellatedSolid solid, Matrix4x4 backtransform)>();
        }

        public void Add(TessellatedSolid solid, Matrix4x4 backtransform)
        {
            Parts.Add((solid, backtransform));
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
            var allParts = new List<(TessellatedSolid, Matrix4x4)>(Parts);
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
