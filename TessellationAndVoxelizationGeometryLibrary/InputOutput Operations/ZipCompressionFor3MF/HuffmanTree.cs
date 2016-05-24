// Decompiled with JetBrains decompiler
// Type: System.IO.Compression.HuffmanTree
// Assembly: System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
// MVID: 67F338CA-9799-462C-9779-9C54DB00C2DD
// Assembly location: C:\Windows\Microsoft.NET\Framework\v4.0.30319\System.dll

using System;
using System.IO;

namespace TVGL.IOFunctions.Zip
{
    internal class HuffmanTree
    {
        private static HuffmanTree staticLiteralLengthTree = new HuffmanTree(HuffmanTree.GetStaticLiteralTreeLength());
        private static HuffmanTree staticDistanceTree = new HuffmanTree(HuffmanTree.GetStaticDistanceTreeLength());
        internal const int MaxLiteralTreeElements = 288;
        internal const int MaxDistTreeElements = 32;
        internal const int EndOfBlockCode = 256;
        internal const int NumberOfCodeLengthTreeElements = 19;
        private int tableBits;
        private short[] table;
        private short[] left;
        private short[] right;
        private byte[] codeLengthArray;
        private int tableMask;

        public static HuffmanTree StaticLiteralLengthTree
        {
            get
            {
                return HuffmanTree.staticLiteralLengthTree;
            }
        }

        public static HuffmanTree StaticDistanceTree
        {
            get
            {
                return HuffmanTree.staticDistanceTree;
            }
        }

        public HuffmanTree(byte[] codeLengths)
        {
            this.codeLengthArray = codeLengths;
            this.tableBits = this.codeLengthArray.Length != 288 ? 7 : 9;
            this.tableMask = (1 << this.tableBits) - 1;
            this.CreateTable();
        }

        private static byte[] GetStaticLiteralTreeLength()
        {
            byte[] numArray = new byte[288];
            for (int index = 0; index <= 143; ++index)
                numArray[index] = (byte)8;
            for (int index = 144; index <= (int)byte.MaxValue; ++index)
                numArray[index] = (byte)9;
            for (int index = 256; index <= 279; ++index)
                numArray[index] = (byte)7;
            for (int index = 280; index <= 287; ++index)
                numArray[index] = (byte)8;
            return numArray;
        }

        private static byte[] GetStaticDistanceTreeLength()
        {
            byte[] numArray = new byte[32];
            for (int index = 0; index < 32; ++index)
                numArray[index] = (byte)5;
            return numArray;
        }

        private uint[] CalculateHuffmanCode()
        {
            uint[] numArray1 = new uint[17];
            foreach (int index in this.codeLengthArray)
                ++numArray1[index];
            numArray1[0] = 0U;
            uint[] numArray2 = new uint[17];
            uint num = 0;
            for (int index = 1; index <= 16; ++index)
            {
                num = (uint)((int)num + (int)numArray1[index - 1] << 1);
                numArray2[index] = num;
            }
            uint[] numArray3 = new uint[288];
            for (int index = 0; index < this.codeLengthArray.Length; ++index)
            {
                int length = (int)this.codeLengthArray[index];
                if (length > 0)
                {
                    numArray3[index] = FastEncoderStatics.BitReverse(numArray2[length], length);
                    ++numArray2[length];
                }
            }
            return numArray3;
        }

        private void CreateTable()
        {
            uint[] numArray1 = this.CalculateHuffmanCode();
            this.table = new short[1 << this.tableBits];
            this.left = new short[2 * this.codeLengthArray.Length];
            this.right = new short[2 * this.codeLengthArray.Length];
            short num1 = (short)this.codeLengthArray.Length;
            for (int index1 = 0; index1 < this.codeLengthArray.Length; ++index1)
            {
                int num2 = (int)this.codeLengthArray[index1];
                if (num2 > 0)
                {
                    int index2 = (int)numArray1[index1];
                    if (num2 <= this.tableBits)
                    {
                        int num3 = 1 << num2;
                        if (index2 >= num3)
                            throw new InvalidOperationException(("InvalidHuffmanData"));
                        int num4 = 1 << this.tableBits - num2;
                        for (int index3 = 0; index3 < num4; ++index3)
                        {
                            this.table[index2] = (short)index1;
                            index2 += num3;
                        }
                    }
                    else
                    {
                        int num3 = num2 - this.tableBits;
                        int num4 = 1 << this.tableBits;
                        int index3 = index2 & (1 << this.tableBits) - 1;
                        short[] numArray2 = this.table;
                        do
                        {
                            short num5 = numArray2[index3];
                            if ((int)num5 == 0)
                            {
                                numArray2[index3] = -num1;
                                num5 = -num1;
                                ++num1;
                            }
                            if ((int)num5 > 0)
                                throw new InvalidOperationException(("InvalidHuffmanData"));
                            numArray2 = (index2 & num4) != 0 ? this.right : this.left;
                            index3 = (int)-num5;
                            num4 <<= 1;
                            --num3;
                        }
                        while (num3 != 0);
                        numArray2[index3] = (short)index1;
                    }
                }
            }
        }

        public int GetNextSymbol(InputBuffer input)
        {
            uint num1 = input.TryLoad16Bits();
            if (input.AvailableBits == 0)
                return -1;
            int index1 = (int)this.table[(long)num1 & (long)this.tableMask];
            if (index1 < 0)
            {
                uint num2 = (uint)(1 << this.tableBits);
                do
                {
                    int index2 = -index1;
                    index1 = ((int)num1 & (int)num2) != 0 ? (int)this.right[index2] : (int)this.left[index2];
                    num2 <<= 1;
                }
                while (index1 < 0);
            }
            int n = (int)this.codeLengthArray[index1];
            if (n <= 0)
                throw new InvalidOperationException(("InvalidHuffmanData"));
            if (n > input.AvailableBits)
                return -1;
            input.SkipBits(n);
            return index1;
        }
    }
}
