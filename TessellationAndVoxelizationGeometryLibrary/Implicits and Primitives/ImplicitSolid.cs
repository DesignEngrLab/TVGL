// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 03-07-2015
// ***********************************************************************
// <copyright file="TessellatedSolid.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using MIConvexHull;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using TVGL.IOFunctions;
using TVGL.Numerics;
using TVGL.Voxelization;

namespace TVGL
{
    public class ImplicitSolid : Solid
    {
        public double SurfaceLevel { get; set; }

        public ImplicitSolid()
        {
            Bounds = new[] { new[] {0.0,0.0,0.0},
            new[]{10.0,10.0,10.0}};
        }
        public override Solid Copy()
        {
            throw new NotImplementedException();
        }

        public override void Transform(Matrix4x4 transformMatrix)
        {
            throw new NotImplementedException();
        }

        public override Solid TransformToNewSolid(Matrix4x4 transformationMatrix)
        {
            throw new NotImplementedException();
        }
        public double this[double x, double y, double z]
        {
            get
            {
                return Evaluate(x, y, z);
            }
        }

        private double Evaluate(double x, double y, double z)
        {
            var center = new Vector3(5, 5, 5 );
            var queriedPoint = new Vector3(x, y, z);
            var radius = 3.0;
            return (queriedPoint-center).Length() - radius;
        }

        public TessellatedSolid ConvertToTessellatedSolid()
        {
            var marchingCubesAlgorithm = new MarchingCubesImplicit(this, 0.7);
            return marchingCubesAlgorithm.Generate();
        }
    }
}