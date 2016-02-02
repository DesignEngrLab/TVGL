using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TVGL;

namespace PrimitiveClassificationOfTessellatedSolids
{
    internal class PlanningSurface
    {
        internal PrimitiveSurfaceType SurfaceType { get; private set; }
        internal List<FaceWithScores> Faces;

        internal PlanningSurface(PrimitiveSurfaceType SurfaceType, params FaceWithScores[] Faces)
        {
            this.SurfaceType = SurfaceType;
            this.Faces = new List<FaceWithScores>(Faces);
            foreach (var polygonalFace in Faces)
                Area += polygonalFace.Face.Area;
        }
        internal double Area { get; set; }
        internal double NegativeProbability { get; set; }

        internal double Metric
        {
            get
            {
                double TypeMultiplier;
                switch (SurfaceType)
                {
                    case PrimitiveSurfaceType.Flat:
                        TypeMultiplier = 20;
                        break;
                    case PrimitiveSurfaceType.Cylinder:
                        TypeMultiplier = 10;
                        break;
                    case PrimitiveSurfaceType.Sphere:
                        TypeMultiplier = 5;
                        break;
                    default:
                        TypeMultiplier = 1;
                        break;
                }
                return TypeMultiplier*Area * (1 - NegativeProbability);
            }
        }

        internal void Add(FaceWithScores f)
        {
            Faces.Add(f);
            Area += f.Face.Area;
            NegativeProbability *= (1 - f.FaceCat[SurfaceType]);
        }

        internal void Remove(FaceWithScores f)
        {
            Faces.Remove(f);
            Area -= f.Face.Area;
            NegativeProbability /= (1 - f.FaceCat[SurfaceType]);
        }
    }
}
