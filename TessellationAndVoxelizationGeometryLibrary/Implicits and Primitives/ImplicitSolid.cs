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
        public double Discretization { get; set; }
        private BooleanOperation operationTree;

        public ImplicitSolid()
        {
        }

        public ImplicitSolid(PrimitiveSurface surface1) : this()
        {
            operationTree = new LeafSurface(surface1);
        }
        public ImplicitSolid(PrimitiveSurface surfaceA, PrimitiveSurface surfaceB, BooleanOperationType booleanOperation) : this()
        {
            operationTree = MakeBooleanOperation(new LeafSurface(surfaceA), new LeafSurface(surfaceB), booleanOperation);
        }

        private BooleanOperation MakeBooleanOperation(BooleanOperation boolOperA, BooleanOperation boolOperB, BooleanOperationType booleanOperation)
        {
            switch (booleanOperation)
            {
                case BooleanOperationType.Union:
                    return new Union(boolOperA, boolOperB);
                case BooleanOperationType.Intersect:
                    return new Intersect(boolOperA, boolOperB);
                case BooleanOperationType.SubtractAB:
                    return new SubtractAB(boolOperA, boolOperB);
                case BooleanOperationType.SubtractBA:
                    return new SubtractAB(boolOperB, boolOperA);
                case BooleanOperationType.XOR:
                    return new XOR(boolOperA, boolOperB);
                default:
                    throw new ArgumentException("Unexpected boolean operation.");
            }
        }
        public void AddNewTopOfTree(ImplicitSolid csgSolidA, BooleanOperationType booleanOperation)
        {
            var top = MakeBooleanOperation(csgSolidA.operationTree, operationTree, booleanOperation);
            operationTree = top;
        }

        public void AddNewTopOfTree(BooleanOperationType booleanOperation, ImplicitSolid csgSolidB)
        {
            var top = MakeBooleanOperation(operationTree, csgSolidB.operationTree, booleanOperation);
            operationTree = top;
        }
        public void AddNewTopOfTree(PrimitiveSurface primitiveSurfaceA, BooleanOperationType booleanOperation)
        {
            var top = MakeBooleanOperation(new LeafSurface(primitiveSurfaceA), operationTree, booleanOperation);
            operationTree = top;
        }

        public void AddNewTopOfTree(BooleanOperationType booleanOperation, PrimitiveSurface primitiveSurfaceB)
        {
            var top = MakeBooleanOperation(operationTree, new LeafSurface(primitiveSurfaceB), booleanOperation);
            operationTree = top;
        }

        public override void Transform(Matrix4x4 transformMatrix)
        {
            throw new NotImplementedException();
        }

        public override Solid TransformToNewSolid(Matrix4x4 transformationMatrix)
        {
            throw new NotImplementedException();
        }

        public double this[double x, double y, double z] => operationTree.Run(new Vector3(x, y, z));


        public TessellatedSolid ConvertToTessellatedSolid(double meshSize)
        {
            var marchingCubesAlgorithm = new MarchingCubesImplicit(this, meshSize);
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

        abstract class BooleanOperation
        {
            internal abstract double Run(Vector3 point);

        }
        class LeafSurface : BooleanOperation
        {
            private PrimitiveSurface surface;
            internal LeafSurface(PrimitiveSurface surface)
            {
                this.surface = surface;
            }
            internal override double Run(Vector3 point)
            {
                return surface.PointMembership(point);
            }
        }
        class Union : BooleanOperation
        {
            BooleanOperation surface1;
            BooleanOperation surface2;

            internal Union(BooleanOperation surface1, BooleanOperation surface2)
            {
                this.surface1 = surface1;
                this.surface2 = surface2;
            }
            internal override double Run(Vector3 point)
            {
                return Math.Min(surface1.Run(point), surface2.Run(point));
            }
        }
        class Intersect : BooleanOperation
        {
            BooleanOperation surface1;
            BooleanOperation surface2;

            internal Intersect(BooleanOperation surface1, BooleanOperation surface2)
            {
                this.surface1 = surface1;
                this.surface2 = surface2;
            }
            internal override double Run(Vector3 point)
            {
                return Math.Max(surface1.Run(point), surface2.Run(point));
            }
        }
        class SubtractAB : BooleanOperation
        {
            BooleanOperation surfaceA;
            BooleanOperation surfaceB;

            internal SubtractAB(BooleanOperation surfaceA, BooleanOperation surfaceB)
            {
                this.surfaceA = surfaceA;
                this.surfaceB = surfaceB;
            }
            internal override double Run(Vector3 point)
            {
                return Math.Max(surfaceA.Run(point), -surfaceB.Run(point));
            }
        }
        class XOR : BooleanOperation
        {
            BooleanOperation surfaceA;
            BooleanOperation surfaceB;

            internal XOR(BooleanOperation surfaceA, BooleanOperation surfaceB)
            {
                this.surfaceA = surfaceA;
                this.surfaceB = surfaceB;
            }
            internal override double Run(Vector3 point)
            {
                var pmcA = surfaceA.Run(point);
                var pmcB = surfaceB.Run(point);
                if (pmcA < 0 && pmcB < 0)
                    return -Math.Min(pmcA, pmcB);
                if (pmcA > 0 && pmcB > 0)
                    return Math.Min(pmcA, pmcB);
                return Math.Min(pmcA, pmcB);
            }
        }
    }
}