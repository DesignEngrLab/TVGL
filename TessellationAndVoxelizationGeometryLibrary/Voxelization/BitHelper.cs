using System;

namespace TVGL.Voxelization
{
    internal class BitHelper
    {
        private int* bitArrayPtr;
        private int intArrayLength;

        public BitHelper(int* bitArrayPtr, int intArrayLength)
        {
            this.bitArrayPtr = bitArrayPtr;
            this.intArrayLength = intArrayLength;
        }

        internal static int ToIntArrayLength(int originalLastIndex)
        {
            throw new NotImplementedException();
        }

        internal void MarkBit(int index)
        {
            throw new NotImplementedException();
        }

        internal bool IsMarked(int i)
        {
            throw new NotImplementedException();
        }
    }
}