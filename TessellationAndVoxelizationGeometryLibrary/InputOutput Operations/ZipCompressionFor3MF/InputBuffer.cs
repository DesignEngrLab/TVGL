// Decompiled with JetBrains decompiler
// Type: System.IO.Compression.InputBuffer
// Assembly: System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
// MVID: 67F338CA-9799-462C-9779-9C54DB00C2DD
// Assembly location: C:\Windows\Microsoft.NET\Framework\v4.0.30319\System.dll

using System;

namespace TVGL.IOFunctions.Zip
{
    internal class InputBuffer
    {
        private byte[] buffer;
        private int start;
        private int end;
        private uint bitBuffer;
        private int bitsInBuffer;

        public int AvailableBits
        {
            get
            {
                return this.bitsInBuffer;
            }
        }

        public int AvailableBytes
        {
            get
            {
                return this.end - this.start + this.bitsInBuffer / 8;
            }
        }

        public bool EnsureBitsAvailable(int count)
        {
            if (this.bitsInBuffer < count)
            {
                if (this.NeedsInput())
                    return false;
                int num1 = (int)this.bitBuffer;
                byte[] numArray1 = this.buffer;
                int num2 = this.start;
                this.start = num2 + 1;
                int index1 = num2;
                int num3 = (int)numArray1[index1] << this.bitsInBuffer;
                this.bitBuffer = (uint)(num1 | num3);
                this.bitsInBuffer = this.bitsInBuffer + 8;
                if (this.bitsInBuffer < count)
                {
                    if (this.NeedsInput())
                        return false;
                    int num4 = (int)this.bitBuffer;
                    byte[] numArray2 = this.buffer;
                    int num5 = this.start;
                    this.start = num5 + 1;
                    int index2 = num5;
                    int num6 = (int)numArray2[index2] << this.bitsInBuffer;
                    this.bitBuffer = (uint)(num4 | num6);
                    this.bitsInBuffer = this.bitsInBuffer + 8;
                }
            }
            return true;
        }

        public uint TryLoad16Bits()
        {
            if (this.bitsInBuffer < 8)
            {
                if (this.start < this.end)
                {
                    int num1 = (int)this.bitBuffer;
                    byte[] numArray = this.buffer;
                    int num2 = this.start;
                    this.start = num2 + 1;
                    int index = num2;
                    int num3 = (int)numArray[index] << this.bitsInBuffer;
                    this.bitBuffer = (uint)(num1 | num3);
                    this.bitsInBuffer = this.bitsInBuffer + 8;
                }
                if (this.start < this.end)
                {
                    int num1 = (int)this.bitBuffer;
                    byte[] numArray = this.buffer;
                    int num2 = this.start;
                    this.start = num2 + 1;
                    int index = num2;
                    int num3 = (int)numArray[index] << this.bitsInBuffer;
                    this.bitBuffer = (uint)(num1 | num3);
                    this.bitsInBuffer = this.bitsInBuffer + 8;
                }
            }
            else if (this.bitsInBuffer < 16 && this.start < this.end)
            {
                int num1 = (int)this.bitBuffer;
                byte[] numArray = this.buffer;
                int num2 = this.start;
                this.start = num2 + 1;
                int index = num2;
                int num3 = (int)numArray[index] << this.bitsInBuffer;
                this.bitBuffer = (uint)(num1 | num3);
                this.bitsInBuffer = this.bitsInBuffer + 8;
            }
            return this.bitBuffer;
        }

        private uint GetBitMask(int count)
        {
            return (uint)((1 << count) - 1);
        }

        public int GetBits(int count)
        {
            if (!this.EnsureBitsAvailable(count))
                return -1;
            int num = (int)this.bitBuffer & (int)this.GetBitMask(count);
            this.bitBuffer = this.bitBuffer >> count;
            this.bitsInBuffer = this.bitsInBuffer - count;
            return num;
        }

        public int CopyTo(byte[] output, int offset, int length)
        {
            int num1 = 0;
            while (this.bitsInBuffer > 0 && length > 0)
            {
                output[offset++] = (byte)this.bitBuffer;
                this.bitBuffer = this.bitBuffer >> 8;
                this.bitsInBuffer = this.bitsInBuffer - 8;
                --length;
                ++num1;
            }
            if (length == 0)
                return num1;
            int num2 = this.end - this.start;
            if (length > num2)
                length = num2;
            Array.Copy((Array)this.buffer, this.start, (Array)output, offset, length);
            this.start = this.start + length;
            return num1 + length;
        }

        public bool NeedsInput()
        {
            return this.start == this.end;
        }

        public void SetInput(byte[] buffer, int offset, int length)
        {
            this.buffer = buffer;
            this.start = offset;
            this.end = offset + length;
        }

        public void SkipBits(int n)
        {
            this.bitBuffer = this.bitBuffer >> n;
            this.bitsInBuffer = this.bitsInBuffer - n;
        }

        public void SkipToByteBoundary()
        {
            this.bitBuffer = this.bitBuffer >> this.bitsInBuffer % 8;
            this.bitsInBuffer = this.bitsInBuffer - this.bitsInBuffer % 8;
        }
    }
}
