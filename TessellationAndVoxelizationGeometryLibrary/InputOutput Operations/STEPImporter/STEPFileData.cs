// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Matt Campbell
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 06-05-2014
// ***********************************************************************

using AForge.Math;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TVGL;

namespace TVGL.IOFunctions.Step
{
    /// <exclude />
    public class STEPFileData : IO
    {

        private Dictionary<long, Entity> Data;
        private readonly IList<PolygonalFace> Facets;
        private readonly IList<Vector3> Vertices;
        private readonly IList<int> Triangles;



        #region Open Solids

        private static Dictionary<long, Entity> MakeEntityDictionary(IEnumerable<string> lines)
        {
            IEnumerable<Entity> entities = lines.Select(ParseHelper.ParseBodyLine);
            return entities.ToDictionary(entity => entity.Id, entity => entity);
        }


        internal static Solid[] OpenSolids(Stream s, string filename)
        {
            var now = DateTime.Now;
            var resultsList = new List<TessellatedSolid>();
#if !DEBUG
            try
            {
#endif
            var start = "HEADER;";
            var end = "ENDSEC;";
            var headerStream = ParseHelper.FindSection(s, start, end);
            start = "DATA;";
            var dataStream = ParseHelper.FindSection(s, start, end);
            var headerEntityStrings = ParseHelper.GetEntityStrings(headerStream);
            var dataEntityStrings = ParseHelper.GetEntityStrings(dataStream);

            var model = new STEPFileData
            {
                FileName = filename,
                Name = Path.GetFileNameWithoutExtension(filename),
            };
            model.Comments.AddRange(headerEntityStrings);
            model.Data = MakeEntityDictionary(dataEntityStrings);
            foreach (var entity in model.Data.Values)
                entity.Init(model.Data);
            return model.CreateSolids();
#if !DEBUG
        }
            catch
            {
                Message.output("Unable to read in STEP file.", 1);
                return null;
            }
#endif
        }


        private Solid[] CreateSolids()
        {
            var indices = new List<int>();
            var vectors = new List<Vector3>();
            //var mesh = ConverterFactory.Create().Convert(_model);
            var resultsList = new List<Solid>();
            var faceIndices = new List<int[]>();
            foreach (var solid in Get<StepSolid>())
            {
                var brepSolid = solid as BREPStepSolid;
                if (brepSolid == null) throw new NotImplementedException("Only the MANIFOLD_SOLID_BREP solids are supported.");
                foreach (var element in brepSolid.Outer)
                {
                    var offset = vectors.Count;
                    var convertable = CreateConvertable(element, this);
                    var circleVectors = convertable.Points;
                    var circleIndices = convertable.Indices;
                    vectors.AddRange(circleVectors);
                    indices.AddRange(circleIndices.Select(x => x + offset));
                    for (int i = 0; i < indices.Count; i += 3)
                        faceIndices.Add(new[] { indices[i], indices[i + 1], indices[i + 2] });
                    var mesh = new TessellatedSolid(vectors.Select(v
                        => new[] { (double)v.X, (double)v.Y, (double)v.Z }).ToList(),
                        faceIndices, null);

                    resultsList.Add(mesh);
                } 
            }
            return resultsList.ToArray();
        }

        private IEnumerable<T> Get<T>() where T : Entity
        {
            return Data.Values.Where(e => e is T).Select(e =>(T)e);
        }


        public T Get<T>(long id) where T : Entity
        {
            return (T)Data.Values.FirstOrDefault(e => e is T);
        }

        private static IConvertable CreateConvertable<T>(T element, STEPFileData model)
            where T : Entity
        {
            var type = typeof(T);
            if (type == typeof(Circle))
            {
                return new CircleConvertable(element as Circle, model);
            }
            if (type == typeof(Line))
            {
                return new LineConvertable(element as TVGL.IOFunctions.Step.Line, model);
            }
            if (type == typeof(AdvancedFace))
            {
                return new AdvancedFaceConvertable(element as AdvancedFace, model);
            }
            if (type == typeof(ClosedShell))
            {
                return new ClosedShellConveratable(element as ClosedShell, model);
            }
            throw new Exception("Not supported");
        }

        #endregion

        #region Save Solids

        private static bool SaveSolids(Stream stream, IList<TessellatedSolid> solids)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}