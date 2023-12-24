// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="VoxelHelperClasses.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System.Collections.Generic;

namespace TVGL
{
    /// <summary>
    /// Class VoxelEnumerator.
    /// Implements the <see cref="System.Collections.Generic.IEnumerator{System.Int32[]}" />
    /// </summary>
    /// <seealso cref="System.Collections.Generic.IEnumerator{System.Int32[]}" />
    internal class VoxelEnumerator : IEnumerator<(int xIndex, int yIndex, int zIndex)>
    {
        /// <summary>
        /// The vs
        /// </summary>
        private readonly VoxelizedSolid vs;
        /// <summary>
        /// The current voxel position
        /// </summary>
        private (int xIndex, int yIndex, int zIndex) currentVoxelPosition;
        /// <summary>
        /// The x index
        /// </summary>
        private int xIndex;
        /// <summary>
        /// The y index
        /// </summary>
        private int yIndex;
        /// <summary>
        /// The z index
        /// </summary>
        private int zIndex;
        /// <summary>
        /// The x lim
        /// </summary>
        private readonly int xLim;
        /// <summary>
        /// The y lim
        /// </summary>
        private readonly int yLim;
        /// <summary>
        /// The z lim
        /// </summary>
        private readonly int zLim;

        /// <summary>
        /// Initializes a new instance of the <see cref="VoxelEnumerator"/> class.
        /// </summary>
        /// <param name="vs">The vs.</param>
        public VoxelEnumerator(VoxelizedSolid vs)
        {
            this.vs = vs;
            this.xLim = vs.numVoxelsX;
            this.yLim = vs.numVoxelsY;
            this.zLim = vs.numVoxelsZ;
        }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <value>The current.</value>
        public object Current => currentVoxelPosition;

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <value>The current.</value>
        (int xIndex, int yIndex, int zIndex) IEnumerator<(int xIndex, int yIndex, int zIndex)>.Current => currentVoxelPosition;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            //throw new NotImplementedException();
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns><see langword="true" /> if the enumerator was successfully advanced to the next element; <see langword="false" /> if the enumerator has passed the end of the collection.</returns>
        public bool MoveNext()
        {
            do
            {
                xIndex++;
                if (xIndex == xLim)
                {
                    xIndex = 0;
                    yIndex++;
                    if (yIndex == yLim)
                    {
                        yIndex = 0;
                        zIndex++;
                        if (zIndex == zLim) return false;
                    }
                }
            } while (!vs[xIndex, yIndex, zIndex]);
            currentVoxelPosition = (xIndex, yIndex, zIndex);
            return true;
        }

        /// <summary>
        /// Resets this instance.
        /// </summary>
        public void Reset()
        {
            xIndex = yIndex = zIndex = 0;
        }
    }
}