using System;
using System.Collections.Generic;
using System.Linq;
using AForge.Math;
using STPLoader;
using STPLoader.Implementation.Model;
using STPLoader.Implementation.Model.Entity;
using TVGL.IOFunctions;

namespace TVGL.IOFunctions.Step
{
    /// <summary>
    /// 
    /// </summary>
    public class ClosedShellConveratable : IConvertable
    {
        private readonly ClosedShell _closedShell;
        private readonly STEPFileData _model;

        public ClosedShellConveratable(ClosedShell closedShell, STEPFileData model)
        {
            _closedShell = closedShell;
            _model = model;
            Init();
        }

        private void Init()
        {
            var faces = _closedShell.PointIds.Select(_model.Get<AdvancedFace>);
            // create convertable for all faces and merge points and indices
            var convertables = faces.Select(face => new AdvancedFaceConvertable(face, _model)).Select(c => Wuple.New(c.Points, c.Indices));

            Points = convertables.Select(c => c.First).SelectMany(p => p).ToList();
            Indices = convertables.Aggregate(Wuple.New(0, new List<int>()), Wuple.AggregateIndices).Second;
        }

        public IList<Vector3> Points { get; private set; }
        public IList<int> Indices { get; private set; }
    }
}
