// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using System;


namespace TVGL
{
    public class ImplicitSolid : Solid
    {
        public double SurfaceLevel { get; set; }

        public ImplicitSolid()
        {
            Bounds = new[] { new Vector3(0.0, 0.0, 0.0), new Vector3(10.0, 10.0, 10.0) };
        }


        public override void Transform(Matrix4x4 transformMatrix)
        {
            throw new NotImplementedException();
        }

        public override Solid TransformToNewSolid(Matrix4x4 transformationMatrix)
        {
            throw new NotImplementedException();
        }

        public double this[double x, double y, double z] => Evaluate(x, y, z);

        private double Evaluate(double x, double y, double z)
        {
            var center = new Vector3(5, 5, 5);
            var queriedPoint = new Vector3(x, y, z);
            var radius = 3.0;
            return (queriedPoint - center).Length() - radius;
        }

        public TessellatedSolid ConvertToTessellatedSolid()
        {
            var marchingCubesAlgorithm = new MarchingCubesImplicit(this, 0.7);
            return marchingCubesAlgorithm.Generate();
        }

        protected override void CalculateCenter()
        {
            throw new NotImplementedException();
        }

        protected override void CalculateVolume()
        {
            throw new NotImplementedException();
        }

        protected override void CalculateSurfaceArea()
        {
            throw new NotImplementedException();
        }

        protected override void CalculateInertiaTensor()
        {
            throw new NotImplementedException();
        }
    }
}