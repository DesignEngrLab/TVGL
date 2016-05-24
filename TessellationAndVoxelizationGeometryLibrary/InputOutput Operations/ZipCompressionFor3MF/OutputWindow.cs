// Decompiled with JetBrains decompiler
// Type: System.IO.Compression.OutputWindow
// Assembly: System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
// MVID: 67F338CA-9799-462C-9779-9C54DB00C2DD
// Assembly location: C:\Windows\Microsoft.NET\Framework\v4.0.30319\System.dll

using System;

namespace TVGL.IOFunctions.Zip
{
    internal class OutputWindow
    {
        private byte[] window = new byte[32768];
        private const int WindowSize = 32768;
        private const int WindowMask = 32767;
        private int end;
        private int bytesUsed;

        public int FreeBytes
        {
            get
            {
                return 32768 - this.bytesUsed;
            }
        }

        public int AvailableBytes
        {
            get
            {
                return this.bytesUsed;
            }
        }

        public void Write(byte b)
        {
            byte[] numArray = this.window;
            int num1 = this.end;
            this.end = num1 + 1;
            int index = num1;
            int num2 = (int)b;
            numArray[index] = (byte)num2;
            this.end = this.end & (int)short.MaxValue;
            this.bytesUsed = this.bytesUsed + 1;
        }

        public void WriteLengthDistance(int length, int distance)
        {
            this.bytesUsed = this.bytesUsed + length;
            int sourceIndex = this.end - distance & (int)short.MaxValue;
            int num1 = 32768 - length;
            if (sourceIndex <= num1 && this.end < num1)
            {
                if (length <= distance)
                {
                    Array.Copy((Array)this.window, sourceIndex, (Array)this.window, this.end, length);
                    this.end = this.end + length;
                }
                else
                {
                    while (length-- > 0)
                    {
                        byte[] numArray = this.window;
                        int num2 = this.end;
                        this.end = num2 + 1;
                        int index = num2;
                        int num3 = (int)this.window[sourceIndex++];
                        numArray[index] = (byte)num3;
                    }
                }
            }
            else
            {
                while (length-- > 0)
                {
                    byte[] numArray1 = this.window;
                    int num2 = this.end;
                    this.end = num2 + 1;
                    int index1 = num2;
                    byte[] numArray2 = this.window;
                    int index2 = sourceIndex;
                    int num3 = 1;
                    int num4 = index2 + num3;
                    int num5 = (int)numArray2[index2];
                    numArray1[index1] = (byte)num5;
                    this.end = this.end & (int)short.MaxValue;
                    sourceIndex = num4 & (int)short.MaxValue;
                }
            }
        }

        public int CopyFrom(InputBuffer input, int length)
        {
            length = Math.Min(Math.Min(length, 32768 - this.bytesUsed), input.AvailableBytes);
            int length1 = 32768 - this.end;
            int num;
            if (length > length1)
            {
                num = input.CopyTo(this.window, this.end, length1);
                if (num == length1)
                    num += input.CopyTo(this.window, 0, length - length1);
            }
            else
                num = input.CopyTo(this.window, this.end, length);
            this.end = this.end + num & (int)short.MaxValue;
            this.bytesUsed = this.bytesUsed + num;
            return num;
        }

        public int CopyTo(byte[] output, int offset, int length)
        {
            int num1;
            if (length > this.bytesUsed)
            {
                num1 = this.end;
                length = this.bytesUsed;
            }
            else
                num1 = this.end - this.bytesUsed + length & (int)short.MaxValue;
            int num2 = length;
            int length1 = length - num1;
            if (length1 > 0)
            {
                Array.Copy((Array)this.window, 32768 - length1, (Array)output, offset, length1);
                offset += length1;
                length = num1;
            }
            Array.Copy((Array)this.window, num1 - length, (Array)output, offset, length);
            this.bytesUsed = this.bytesUsed - num2;
            return num2;
        }
    }
}
