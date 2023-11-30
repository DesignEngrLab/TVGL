// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="IVoxelRow.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
namespace TVGL
{
    /// <summary>
    /// Interface IVoxelRow
    /// </summary>
    /// <font color="red">Badly formed XML comment.</font>
    public interface IVoxelRow
    {
        /// <summary>
        /// The length of the row. This is the same as the number of voxels in x (numVoxelsX)
        /// for the participating solid.
        /// </summary>
        /// <value>The maximum number of voxels.</value>
        ushort maxNumberOfVoxels { get; }

        /// <summary>
        /// Gets or sets the <see cref="System.Boolean" /> at the specified index.
        /// </summary>
        /// <param name="xCoord">The x coordinate.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        bool this[int xCoord] { get; set; }
        /// <summary>
        /// Gets the number of voxels in this row.
        /// </summary>
        /// <value>The count.</value>
        int Count { get; }
        /// <summary>
        /// Turns all the voxels within the range to on/true.
        /// </summary>
        /// <param name="lo">The lo.</param>
        /// <param name="hi">The hi.</param>
        void TurnOnRange(ushort lo, ushort hi);
        /// <summary>
        /// Turns all the voxels within the range to off/false.
        /// </summary>
        /// <param name="lo">The lo.</param>
        /// <param name="hi">The hi.</param>
        void TurnOffRange(ushort lo, ushort hi);

        /// <summary>
        /// Gets the lower-x neighbor and the upper-x neighbor for the one at xCoord.
        /// </summary>
        /// <param name="xCoord">The x coord.</param>
        /// <returns>System.ValueTuple&lt;System.Boolean, System.Boolean&gt;.</returns>
        (bool, bool) GetNeighbors(int xCoord);
        /// <summary>
        /// Unions the specified other rows with this row.
        /// </summary>
        /// <param name="others">The others.</param>
        /// <param name="offset">The offset.</param>
        void Union(IVoxelRow[] others, int offset = 0);
        /// <summary>
        /// Intersects the specified other rows with this row.
        /// </summary>
        /// <param name="others">The others.</param>
        /// <param name="offset">The offset.</param>
        void Intersect(IVoxelRow[] others, int offset = 0);
        /// <summary>
        /// Subtracts the specified subtrahend rows from this row.
        /// </summary>
        /// <param name="subtrahends">The subtrahends.</param>
        /// <param name="offset">The offset.</param>
        void Subtract(IVoxelRow[] subtrahends, int offset = 0);

        /// <summary>
        /// Inverts this row - making all on voxels off and vice-versa.
        /// </summary>
        void Invert();

        /// <summary>
        /// Clears this row of all on voxels.
        /// </summary>
        void Clear();

        /// <summary>
        /// Averages the positions of the on voxels. This is used in finding center of mass.
        /// </summary>
        /// <returns>System.Int32.</returns>
        int TotalXPosition();
    }
}
