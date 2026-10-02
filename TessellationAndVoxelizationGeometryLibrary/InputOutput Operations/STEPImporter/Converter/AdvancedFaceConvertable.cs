using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using AForge.Math;
using TVGL.IOFunctions;

namespace TVGL.IOFunctions.Step
{
    /// <summary>
    /// 
    /// </summary>
    public class AdvancedFaceConvertable : IConvertable
    {
        private readonly AdvancedFace _face;
        private readonly STEPFileData _model;

        public AdvancedFaceConvertable(AdvancedFace face, STEPFileData model)
        {
            _face = face;
            _model = model;
            Init();
        }
        
        private void Init()
        {
            var bounds = _face.BoundIds.Select(_model.Get<Bound>);
            var surface = _model.Get<Surface>(_face.SurfaceId);
            var surfaceConvertable = new SurfaceConvertable(surface, _model);
            // create convertable for all faces and merge points and indices
            var convertables = bounds.Select(bound => new BoundConvertable(bound, _model)).Select(c => Wuple.New(c.Points, c.Indices)).ToList();
            convertables.Add(Wuple.New(surfaceConvertable.Points, surfaceConvertable.Indices));

            Points = convertables.Select(c => c.First).SelectMany(p => p).ToList();
            Indices = convertables.Aggregate(Wuple.New(0, new List<int>()), Wuple.AggregateIndices).Second;
        }

        public IList<Vector3> Points { get; private set; }
        public IList<int> Indices { get; private set; }
    }
}
