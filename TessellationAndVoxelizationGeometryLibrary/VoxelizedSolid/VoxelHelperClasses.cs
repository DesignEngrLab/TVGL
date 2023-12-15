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
    /// Class SameCoordinates.
    /// Implements the <see cref="System.Collections.Generic.EqualityComparer{System.Int32[]}" />
    /// </summary>
    /// <seealso cref="System.Collections.Generic.EqualityComparer{System.Int32[]}" />
    internal class SameCoordinates : EqualityComparer<int[]>
    {
        /// <summary>
        /// Equalses the specified a1.
        /// </summary>
        /// <param name="a1">The a1.</param>
        /// <param name="a2">The a2.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public override bool Equals(int[] a1, int[] a2)
        {
            if (a1 == null && a2 == null)
                return true;
            if (a1 == null || a2 == null)
                return false;
            return (a1[0] == a2[0] &&
                    a1[1] == a2[1] &&
                    a1[2] == a2[2]);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <param name="ax">The ax.</param>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode(int[] ax)
        {
            if (ax is null) return 0;
            var hCode = ax[0] + (ax[1] << 10) + (ax[2] << 20);
            return hCode.GetHashCode();
        }
    }

    /// <summary>
    /// Class VoxelEnumerator.
    /// Implements the <see cref="System.Collections.Generic.IEnumerator{System.Int32[]}" />
    /// </summary>
    /// <seealso cref="System.Collections.Generic.IEnumerator{System.Int32[]}" />
    internal class VoxelEnumerator : IEnumerator<(int, int, int)>
    {
        /// <summary>
        /// The vs
        /// </summary>
        private readonly VoxelizedSolid vs;
        /// <summary>
        /// The current voxel position
        /// </summary>
        private (int, int, int) currentVoxelPosition;
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
            this.xLim = vs.VoxelsPerSide[0];
            this.yLim = vs.VoxelsPerSide[1];
            this.zLim = vs.VoxelsPerSide[2];
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
        (int, int, int) IEnumerator<(int, int, int)>.Current => currentVoxelPosition;

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