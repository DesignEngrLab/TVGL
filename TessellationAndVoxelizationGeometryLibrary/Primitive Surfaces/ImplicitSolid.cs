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
using StarMathLib;
using TVGL.IOFunctions;
using TVGL.Voxelization;

namespace TVGL.Implicit
{
    public class ImplicitSolid : Solid
    {
        public double SurfaceLevel { get; set; }
        public override Solid Copy()
        {
            throw new NotImplementedException();
        }

        public override void Transform(double[,] transformMatrix)
        {
            throw new NotImplementedException();
        }

        public override Solid TransformToNewSolid(double[,] transformationMatrix)
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
            throw new NotImplementedException();
        }

        public TessellatedSolid ConvertToTessellatedSolid()
        {
            throw new NotImplementedException();
        }
    }
}