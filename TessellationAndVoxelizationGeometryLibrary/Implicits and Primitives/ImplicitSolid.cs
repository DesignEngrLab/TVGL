// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="ImplicitSolid.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;

namespace TVGL
{
    /// <summary>
    /// Class ImplicitSolid.
    /// Implements the <see cref="TVGL.Solid" />
    /// </summary>
    /// <seealso cref="TVGL.Solid" />
    public class ImplicitSolid : Solid
    {
        /// <summary>
        /// Gets or sets the surface level.
        /// </summary>
        /// <value>The surface level.</value>
        public double SurfaceLevel { get; set; }
        /// <summary>
        /// Gets or sets the discretization.
        /// </summary>
        /// <value>The discretization.</value>
        public double Discretization { get; set; }
        /// <summary>
        /// The operation tree
        /// </summary>
        private BooleanOperation operationTree;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImplicitSolid"/> class.
        /// </summary>
        public ImplicitSolid()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImplicitSolid"/> class.
        /// </summary>
        /// <param name="surface1">The surface1.</param>
        public ImplicitSolid(PrimitiveSurface surface1) : this()
        {
            operationTree = new LeafSurface(surface1);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="ImplicitSolid"/> class.
        /// </summary>
        /// <param name="surfaceA">The surface a.</param>
        /// <param name="surfaceB">The surface b.</param>
        /// <param name="booleanOperation">The boolean operation.</param>
        public ImplicitSolid(PrimitiveSurface surfaceA, PrimitiveSurface surfaceB, BooleanOperationType booleanOperation) : this()
        {
            operationTree = MakeBooleanOperation(new LeafSurface(surfaceA), new LeafSurface(surfaceB), booleanOperation);
        }

        /// <summary>
        /// Makes the boolean operation.
        /// </summary>
        /// <param name="boolOperA">The bool oper a.</param>
        /// <param name="boolOperB">The bool oper b.</param>
        /// <param name="booleanOperation">The boolean operation.</param>
        /// <returns>BooleanOperation.</returns>
        /// <exception cref="System.ArgumentException">Unexpected boolean operation.</exception>
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
        /// <summary>
        /// Adds the new top of tree.
        /// </summary>
        /// <param name="csgSolidA">The CSG solid a.</param>
        /// <param name="booleanOperation">The boolean operation.</param>
        public void AddNewTopOfTree(ImplicitSolid csgSolidA, BooleanOperationType booleanOperation)
        {
            var top = MakeBooleanOperation(csgSolidA.operationTree, operationTree, booleanOperation);
            operationTree = top;
        }

        /// <summary>
        /// Adds the new top of tree.
        /// </summary>
        /// <param name="booleanOperation">The boolean operation.</param>
        /// <param name="csgSolidB">The CSG solid b.</param>
        public void AddNewTopOfTree(BooleanOperationType booleanOperation, ImplicitSolid csgSolidB)
        {
            var top = MakeBooleanOperation(operationTree, csgSolidB.operationTree, booleanOperation);
            operationTree = top;
        }
        /// <summary>
        /// Adds the new top of tree.
        /// </summary>
        /// <param name="primitiveSurfaceA">The primitive surface a.</param>
        /// <param name="booleanOperation">The boolean operation.</param>
        public void AddNewTopOfTree(PrimitiveSurface primitiveSurfaceA, BooleanOperationType booleanOperation)
        {
            var top = MakeBooleanOperation(new LeafSurface(primitiveSurfaceA), operationTree, booleanOperation);
            operationTree = top;
        }

        /// <summary>
        /// Adds the new top of tree.
        /// </summary>
        /// <param name="booleanOperation">The boolean operation.</param>
        /// <param name="primitiveSurfaceB">The primitive surface b.</param>
        public void AddNewTopOfTree(BooleanOperationType booleanOperation, PrimitiveSurface primitiveSurfaceB)
        {
            var top = MakeBooleanOperation(operationTree, new LeafSurface(primitiveSurfaceB), booleanOperation);
            operationTree = top;
        }

        /// <summary>
        /// Transforms the specified transform matrix.
        /// </summary>
        /// <param name="transformMatrix">The transform matrix.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public override void Transform(Matrix4x4 transformMatrix)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets a new solid by transforming its vertices.
        /// </summary>
        /// <param name="transformationMatrix">The transformation matrix.</param>
        /// <returns>Solid.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override Solid TransformToNewSolid(Matrix4x4 transformationMatrix)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the <see cref="System.Double"/> with the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="z">The z.</param>
        /// <returns>System.Double.</returns>
        public double this[double x, double y, double z] => operationTree.Run(new Vector3(x, y, z));


        /// <summary>
        /// Converts to tessellated solid.
        /// </summary>
        /// <param name="meshSize">Size of the mesh.</param>
        /// <returns>TessellatedSolid.</returns>
        public TessellatedSolid ConvertToTessellatedSolid(double meshSize)
        {
            var marchingCubesAlgorithm = new MarchingCubesImplicit(this, meshSize);
            return marchingCubesAlgorithm.Generate();
        }

        /// <summary>
        /// Calculates the center.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        protected override void CalculateCenter()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Calculates the volume.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        protected override void CalculateVolume()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Calculates the surface area.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        protected override void CalculateSurfaceArea()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Calculates the inertia tensor.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        protected override void CalculateInertiaTensor()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Class BooleanOperation.
        /// </summary>
        abstract class BooleanOperation
        {
            /// <summary>
            /// Runs the specified point.
            /// </summary>
            /// <param name="point">The point.</param>
            /// <returns>System.Double.</returns>
            internal abstract double Run(Vector3 point);

        }
        /// <summary>
        /// Class LeafSurface.
        /// Implements the <see cref="TVGL.ImplicitSolid.BooleanOperation" />
        /// </summary>
        /// <seealso cref="TVGL.ImplicitSolid.BooleanOperation" />
        class LeafSurface : BooleanOperation
        {
            /// <summary>
            /// The surface
            /// </summary>
            private PrimitiveSurface surface;
            /// <summary>
            /// Initializes a new instance of the <see cref="LeafSurface"/> class.
            /// </summary>
            /// <param name="surface">The surface.</param>
            internal LeafSurface(PrimitiveSurface surface)
            {
                this.surface = surface;
            }
            /// <summary>
            /// Runs the specified point.
            /// </summary>
            /// <param name="point">The point.</param>
            /// <returns>System.Double.</returns>
            internal override double Run(Vector3 point)
            {
                return surface.DistanceToPoint(point);
            }
        }
        /// <summary>
        /// Class Union.
        /// Implements the <see cref="TVGL.ImplicitSolid.BooleanOperation" />
        /// </summary>
        /// <seealso cref="TVGL.ImplicitSolid.BooleanOperation" />
        class Union : BooleanOperation
        {
            /// <summary>
            /// The surface1
            /// </summary>
            BooleanOperation surface1;
            /// <summary>
            /// The surface2
            /// </summary>
            BooleanOperation surface2;

            /// <summary>
            /// Initializes a new instance of the <see cref="Union"/> class.
            /// </summary>
            /// <param name="surface1">The surface1.</param>
            /// <param name="surface2">The surface2.</param>
            internal Union(BooleanOperation surface1, BooleanOperation surface2)
            {
                this.surface1 = surface1;
                this.surface2 = surface2;
            }
            /// <summary>
            /// Runs the specified point.
            /// </summary>
            /// <param name="point">The point.</param>
            /// <returns>System.Double.</returns>
            internal override double Run(Vector3 point)
            {
                return Math.Min(surface1.Run(point), surface2.Run(point));
            }
        }
        /// <summary>
        /// Class Intersect.
        /// Implements the <see cref="TVGL.ImplicitSolid.BooleanOperation" />
        /// </summary>
        /// <seealso cref="TVGL.ImplicitSolid.BooleanOperation" />
        class Intersect : BooleanOperation
        {
            /// <summary>
            /// The surface1
            /// </summary>
            BooleanOperation surface1;
            /// <summary>
            /// The surface2
            /// </summary>
            BooleanOperation surface2;

            /// <summary>
            /// Initializes a new instance of the <see cref="Intersect"/> class.
            /// </summary>
            /// <param name="surface1">The surface1.</param>
            /// <param name="surface2">The surface2.</param>
            internal Intersect(BooleanOperation surface1, BooleanOperation surface2)
            {
                this.surface1 = surface1;
                this.surface2 = surface2;
            }
            /// <summary>
            /// Runs the specified point.
            /// </summary>
            /// <param name="point">The point.</param>
            /// <returns>System.Double.</returns>
            internal override double Run(Vector3 point)
            {
                return Math.Max(surface1.Run(point), surface2.Run(point));
            }
        }
        /// <summary>
        /// Class SubtractAB.
        /// Implements the <see cref="TVGL.ImplicitSolid.BooleanOperation" />
        /// </summary>
        /// <seealso cref="TVGL.ImplicitSolid.BooleanOperation" />
        class SubtractAB : BooleanOperation
        {
            /// <summary>
            /// The surface a
            /// </summary>
            BooleanOperation surfaceA;
            /// <summary>
            /// The surface b
            /// </summary>
            BooleanOperation surfaceB;

            /// <summary>
            /// Initializes a new instance of the <see cref="SubtractAB"/> class.
            /// </summary>
            /// <param name="surfaceA">The surface a.</param>
            /// <param name="surfaceB">The surface b.</param>
            internal SubtractAB(BooleanOperation surfaceA, BooleanOperation surfaceB)
            {
                this.surfaceA = surfaceA;
                this.surfaceB = surfaceB;
            }
            /// <summary>
            /// Runs the specified point.
            /// </summary>
            /// <param name="point">The point.</param>
            /// <returns>System.Double.</returns>
            internal override double Run(Vector3 point)
            {
                return Math.Max(surfaceA.Run(point), -surfaceB.Run(point));
            }
        }
        /// <summary>
        /// Class XOR.
        /// Implements the <see cref="TVGL.ImplicitSolid.BooleanOperation" />
        /// </summary>
        /// <seealso cref="TVGL.ImplicitSolid.BooleanOperation" />
        class XOR : BooleanOperation
        {
            /// <summary>
            /// The surface a
            /// </summary>
            BooleanOperation surfaceA;
            /// <summary>
            /// The surface b
            /// </summary>
            BooleanOperation surfaceB;

            /// <summary>
            /// Initializes a new instance of the <see cref="XOR"/> class.
            /// </summary>
            /// <param name="surfaceA">The surface a.</param>
            /// <param name="surfaceB">The surface b.</param>
            internal XOR(BooleanOperation surfaceA, BooleanOperation surfaceB)
            {
                this.surfaceA = surfaceA;
                this.surfaceB = surfaceB;
            }
            /// <summary>
            /// Runs the specified point.
            /// </summary>
            /// <param name="point">The point.</param>
            /// <returns>System.Double.</returns>
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