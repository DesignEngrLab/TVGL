// Decompiled with JetBrains decompiler
// Type: System.IO.Compression.Inflater
// Assembly: System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
// MVID: 67F338CA-9799-462C-9779-9C54DB00C2DD
// Assembly location: C:\Windows\Microsoft.NET\Framework\v4.0.30319\System.dll

using System;
using System.IO;

namespace TVGL.IOFunctions.Zip
{
    internal enum BlockType
    {
        Uncompressed,
        Static,
        Dynamic,
    }
    internal enum InflaterState
    {
        ReadingHeader = 0,
        ReadingBFinal = 2,
        ReadingBType = 3,
        ReadingNumLitCodes = 4,
        ReadingNumDistCodes = 5,
        ReadingNumCodeLengthCodes = 6,
        ReadingCodeLengthCodes = 7,
        ReadingTreeCodesBefore = 8,
        ReadingTreeCodesAfter = 9,
        DecodeTop = 10,
        HaveInitialLength = 11,
        HaveFullLength = 12,
        HaveDistCode = 13,
        UncompressedAligning = 15,
        UncompressedByte1 = 16,
        UncompressedByte2 = 17,
        UncompressedByte3 = 18,
        UncompressedByte4 = 19,
        DecodingUncompressed = 20,
        StartReadingFooter = 21,
        ReadingFooter = 22,
        VerifyingFooter = 23,
        Done = 24,
    }
    internal class Inflater
    {
        private static readonly byte[] extraLengthBits = new byte[29]
        {
      (byte) 0,
      (byte) 0,
      (byte) 0,
      (byte) 0,
      (byte) 0,
      (byte) 0,
      (byte) 0,
      (byte) 0,
      (byte) 1,
      (byte) 1,
      (byte) 1,
      (byte) 1,
      (byte) 2,
      (byte) 2,
      (byte) 2,
      (byte) 2,
      (byte) 3,
      (byte) 3,
      (byte) 3,
      (byte) 3,
      (byte) 4,
      (byte) 4,
      (byte) 4,
      (byte) 4,
      (byte) 5,
      (byte) 5,
      (byte) 5,
      (byte) 5,
      (byte) 0
        };
        private static readonly int[] lengthBase = new int[29]
        {
      3,
      4,
      5,
      6,
      7,
      8,
      9,
      10,
      11,
      13,
      15,
      17,
      19,
      23,
      27,
      31,
      35,
      43,
      51,
      59,
      67,
      83,
      99,
      115,
      131,
      163,
      195,
      227,
      258
        };
        private static readonly int[] distanceBasePosition = new int[32]
        {
      1,
      2,
      3,
      4,
      5,
      7,
      9,
      13,
      17,
      25,
      33,
      49,
      65,
      97,
      129,
      193,
      257,
      385,
      513,
      769,
      1025,
      1537,
      2049,
      3073,
      4097,
      6145,
      8193,
      12289,
      16385,
      24577,
      0,
      0
        };
        private static readonly byte[] codeOrder = new byte[19]
        {
      (byte) 16,
      (byte) 17,
      (byte) 18,
      (byte) 0,
      (byte) 8,
      (byte) 7,
      (byte) 9,
      (byte) 6,
      (byte) 10,
      (byte) 5,
      (byte) 11,
      (byte) 4,
      (byte) 12,
      (byte) 3,
      (byte) 13,
      (byte) 2,
      (byte) 14,
      (byte) 1,
      (byte) 15
        };
        private static readonly byte[] staticDistanceTreeTable = new byte[32]
        {
      (byte) 0,
      (byte) 16,
      (byte) 8,
      (byte) 24,
      (byte) 4,
      (byte) 20,
      (byte) 12,
      (byte) 28,
      (byte) 2,
      (byte) 18,
      (byte) 10,
      (byte) 26,
      (byte) 6,
      (byte) 22,
      (byte) 14,
      (byte) 30,
      (byte) 1,
      (byte) 17,
      (byte) 9,
      (byte) 25,
      (byte) 5,
      (byte) 21,
      (byte) 13,
      (byte) 29,
      (byte) 3,
      (byte) 19,
      (byte) 11,
      (byte) 27,
      (byte) 7,
      (byte) 23,
      (byte) 15,
      (byte) 31
        };
        private byte[] blockLengthBuffer = new byte[4];
        private OutputWindow output;
        private InputBuffer input;
        private HuffmanTree literalLengthTree;
        private HuffmanTree distanceTree;
        private InflaterState state;
        private bool hasFormatReader;
        private int bfinal;
        private BlockType blockType;
        private int blockLength;
        private int length;
        private int distanceCode;
        private int extraBits;
        private int loopCounter;
        private int literalLengthCodeCount;
        private int distanceCodeCount;
        private int codeLengthCodeCount;
        private int codeArraySize;
        private int lengthCode;
        private byte[] codeList;
        private byte[] codeLengthTreeCodeLength;
        private HuffmanTree codeLengthTree;
        //private IFileFormatReader formatReader;

        public int AvailableOutput
        {
            get
            {
                return this.output.AvailableBytes;
            }
        }

        public Inflater()
        {
            this.output = new OutputWindow();
            this.input = new InputBuffer();
            this.codeList = new byte[320];
            this.codeLengthTreeCodeLength = new byte[19];
            this.Reset();
        }
        /*
        internal void SetFileFormatReader(IFileFormatReader reader)
        {
            this.formatReader = reader;
            this.hasFormatReader = true;
            this.Reset();
        }*/

        private void Reset()
        {
            if (this.hasFormatReader)
                this.state = InflaterState.ReadingHeader;
            else
                this.state = InflaterState.ReadingBFinal;
        }

        public void SetInput(byte[] inputBytes, int offset, int length)
        {
            this.input.SetInput(inputBytes, offset, length);
        }

        public bool Finished()
        {
            if (this.state != InflaterState.Done)
                return this.state == InflaterState.VerifyingFooter;
            return true;
        }

        public bool NeedsInput()
        {
            return this.input.NeedsInput();
        }

        public int Inflate(byte[] bytes, int offset, int length)
        {
            int num = 0;
            do
            {
                int bytesToCopy = this.output.CopyTo(bytes, offset, length);
                if (bytesToCopy > 0)
                {
                    if (this.hasFormatReader)
                        this.formatReader.UpdateWithBytesRead(bytes, offset, bytesToCopy);
                    offset += bytesToCopy;
                    num += bytesToCopy;
                    length -= bytesToCopy;
                }
            }
            while (length != 0 && !this.Finished() && this.Decode());
            if (this.state == InflaterState.VerifyingFooter && this.output.AvailableBytes == 0)
                this.formatReader.Validate();
            return num;
        }

        private bool Decode()
        {
            bool flag1 = false;
            if (this.Finished())
                return true;
            if (this.hasFormatReader)
            {
                if (this.state == InflaterState.ReadingHeader)
                {
                    if (!this.formatReader.ReadHeader(this.input))
                        return false;
                    this.state = InflaterState.ReadingBFinal;
                }
                else if (this.state == InflaterState.StartReadingFooter || this.state == InflaterState.ReadingFooter)
                {
                    if (!this.formatReader.ReadFooter(this.input))
                        return false;
                    this.state = InflaterState.VerifyingFooter;
                    return true;
                }
            }
            if (this.state == InflaterState.ReadingBFinal)
            {
                if (!this.input.EnsureBitsAvailable(1))
                    return false;
                this.bfinal = this.input.GetBits(1);
                this.state = InflaterState.ReadingBType;
            }
            if (this.state == InflaterState.ReadingBType)
            {
                if (!this.input.EnsureBitsAvailable(2))
                {
                    this.state = InflaterState.ReadingBType;
                    return false;
                }
                this.blockType = (BlockType)this.input.GetBits(2);
                if (this.blockType == BlockType.Dynamic)
                    this.state = InflaterState.ReadingNumLitCodes;
                else if (this.blockType == BlockType.Static)
                {
                    this.literalLengthTree = HuffmanTree.StaticLiteralLengthTree;
                    this.distanceTree = HuffmanTree.StaticDistanceTree;
                    this.state = InflaterState.DecodeTop;
                }
                else
                {
                    if (this.blockType != BlockType.Uncompressed)
                        throw new InvalidOperationException(("UnknownBlockType"));
                    this.state = InflaterState.UncompressedAligning;
                }
            }
            bool flag2;
            if (this.blockType == BlockType.Dynamic)
                flag2 = this.state >= InflaterState.DecodeTop ? this.DecodeBlock(out flag1) : this.DecodeDynamicBlockHeader();
            else if (this.blockType == BlockType.Static)
            {
                flag2 = this.DecodeBlock(out flag1);
            }
            else
            {
                if (this.blockType != BlockType.Uncompressed)
                    throw new InvalidOperationException(("UnknownBlockType"));
                flag2 = this.DecodeUncompressedBlock(out flag1);
            }
            if (flag1 && this.bfinal != 0)
                this.state = !this.hasFormatReader ? InflaterState.Done : InflaterState.StartReadingFooter;
            return flag2;
        }

        private bool DecodeUncompressedBlock(out bool end_of_block)
        {
            end_of_block = false;
            while (true)
            {
                switch (this.state)
                {
                    case InflaterState.UncompressedAligning:
                        this.input.SkipToByteBoundary();
                        this.state = InflaterState.UncompressedByte1;
                        goto case InflaterState.UncompressedByte1;
                    case InflaterState.UncompressedByte1:
                    case InflaterState.UncompressedByte2:
                    case InflaterState.UncompressedByte3:
                    case InflaterState.UncompressedByte4:
                        int bits = this.input.GetBits(8);
                        if (bits >= 0)
                        {
                            this.blockLengthBuffer[(int)(this.state - 16)] = (byte)bits;
                            if (this.state == InflaterState.UncompressedByte4)
                            {
                                this.blockLength = (int)this.blockLengthBuffer[0] + (int)this.blockLengthBuffer[1] * 256;
                                if ((int)(ushort)this.blockLength != (int)(ushort)~((int)this.blockLengthBuffer[2] + (int)this.blockLengthBuffer[3] * 256))
                                    goto label_7;
                            }
                            this.state = this.state + 1;
                            continue;
                        }
                        goto label_4;
                    case InflaterState.DecodingUncompressed:
                        goto label_9;
                    default:
                        goto label_14;
                }
            }
            label_4:
            return false;
            label_7:
            throw new InvalidOperationException(("InvalidBlockLength"));
            label_9:
            this.blockLength = this.blockLength - this.output.CopyFrom(this.input, this.blockLength);
            if (this.blockLength == 0)
            {
                this.state = InflaterState.ReadingBFinal;
                end_of_block = true;
                return true;
            }
            return this.output.FreeBytes == 0;
            label_14:
            throw new InvalidOperationException(("UnknownState"));
        }

        private bool DecodeBlock(out bool end_of_block_code_seen)
        {
            end_of_block_code_seen = false;
            int freeBytes = this.output.FreeBytes;
            while (freeBytes > 258)
            {
                switch (this.state)
                {
                    case InflaterState.DecodeTop:
                        int nextSymbol = this.literalLengthTree.GetNextSymbol(this.input);
                        if (nextSymbol < 0)
                            return false;
                        if (nextSymbol < 256)
                        {
                            this.output.Write((byte)nextSymbol);
                            --freeBytes;
                            continue;
                        }
                        if (nextSymbol == 256)
                        {
                            end_of_block_code_seen = true;
                            this.state = InflaterState.ReadingBFinal;
                            return true;
                        }
                        int index = nextSymbol - 257;
                        if (index < 8)
                        {
                            index += 3;
                            this.extraBits = 0;
                        }
                        else if (index == 28)
                        {
                            index = 258;
                            this.extraBits = 0;
                        }
                        else
                        {
                            if (index < 0 || index >= Inflater.extraLengthBits.Length)
                                throw new InvalidOperationException(("GenericInvalidData"));
                            this.extraBits = (int)Inflater.extraLengthBits[index];
                        }
                        this.length = index;
                        goto case InflaterState.HaveInitialLength;
                    case InflaterState.HaveInitialLength:
                        if (this.extraBits > 0)
                        {
                            this.state = InflaterState.HaveInitialLength;
                            int bits = this.input.GetBits(this.extraBits);
                            if (bits < 0)
                                return false;
                            if (this.length < 0 || this.length >= Inflater.lengthBase.Length)
                                throw new InvalidOperationException(("GenericInvalidData"));
                            this.length = Inflater.lengthBase[this.length] + bits;
                        }
                        this.state = InflaterState.HaveFullLength;
                        goto case InflaterState.HaveFullLength;
                    case InflaterState.HaveFullLength:
                        if (this.blockType == BlockType.Dynamic)
                        {
                            this.distanceCode = this.distanceTree.GetNextSymbol(this.input);
                        }
                        else
                        {
                            this.distanceCode = this.input.GetBits(5);
                            if (this.distanceCode >= 0)
                                this.distanceCode = (int)Inflater.staticDistanceTreeTable[this.distanceCode];
                        }
                        if (this.distanceCode < 0)
                            return false;
                        this.state = InflaterState.HaveDistCode;
                        goto case InflaterState.HaveDistCode;
                    case InflaterState.HaveDistCode:
                        int distance;
                        if (this.distanceCode > 3)
                        {
                            this.extraBits = this.distanceCode - 2 >> 1;
                            int bits = this.input.GetBits(this.extraBits);
                            if (bits < 0)
                                return false;
                            distance = Inflater.distanceBasePosition[this.distanceCode] + bits;
                        }
                        else
                            distance = this.distanceCode + 1;
                        this.output.WriteLengthDistance(this.length, distance);
                        freeBytes -= this.length;
                        this.state = InflaterState.DecodeTop;
                        continue;
                    default:
                        throw new InvalidOperationException(("UnknownState"));
                }
            }
            return true;
        }

        private bool DecodeDynamicBlockHeader()
        {
            switch (this.state)
            {
                case InflaterState.ReadingNumLitCodes:
                    this.literalLengthCodeCount = this.input.GetBits(5);
                    if (this.literalLengthCodeCount < 0)
                        return false;
                    this.literalLengthCodeCount = this.literalLengthCodeCount + 257;
                    this.state = InflaterState.ReadingNumDistCodes;
                    goto case InflaterState.ReadingNumDistCodes;
                case InflaterState.ReadingNumDistCodes:
                    this.distanceCodeCount = this.input.GetBits(5);
                    if (this.distanceCodeCount < 0)
                        return false;
                    this.distanceCodeCount = this.distanceCodeCount + 1;
                    this.state = InflaterState.ReadingNumCodeLengthCodes;
                    goto case InflaterState.ReadingNumCodeLengthCodes;
                case InflaterState.ReadingNumCodeLengthCodes:
                    this.codeLengthCodeCount = this.input.GetBits(4);
                    if (this.codeLengthCodeCount < 0)
                        return false;
                    this.codeLengthCodeCount = this.codeLengthCodeCount + 4;
                    this.loopCounter = 0;
                    this.state = InflaterState.ReadingCodeLengthCodes;
                    goto case InflaterState.ReadingCodeLengthCodes;
                case InflaterState.ReadingCodeLengthCodes:
                    for (; this.loopCounter < this.codeLengthCodeCount; this.loopCounter = this.loopCounter + 1)
                    {
                        int bits = this.input.GetBits(3);
                        if (bits < 0)
                            return false;
                        this.codeLengthTreeCodeLength[(int)Inflater.codeOrder[this.loopCounter]] = (byte)bits;
                    }
                    for (int index = this.codeLengthCodeCount; index < Inflater.codeOrder.Length; ++index)
                        this.codeLengthTreeCodeLength[(int)Inflater.codeOrder[index]] = (byte)0;
                    this.codeLengthTree = new HuffmanTree(this.codeLengthTreeCodeLength);
                    this.codeArraySize = this.literalLengthCodeCount + this.distanceCodeCount;
                    this.loopCounter = 0;
                    this.state = InflaterState.ReadingTreeCodesBefore;
                    goto case InflaterState.ReadingTreeCodesBefore;
                case InflaterState.ReadingTreeCodesBefore:
                case InflaterState.ReadingTreeCodesAfter:
                    while (this.loopCounter < this.codeArraySize)
                    {
                        if (this.state == InflaterState.ReadingTreeCodesBefore && (this.lengthCode = this.codeLengthTree.GetNextSymbol(this.input)) < 0)
                            return false;
                        if (this.lengthCode <= 15)
                        {
                            byte[] numArray = this.codeList;
                            int num1 = this.loopCounter;
                            this.loopCounter = num1 + 1;
                            int index = num1;
                            int num2 = (int)(byte)this.lengthCode;
                            numArray[index] = (byte)num2;
                        }
                        else
                        {
                            if (!this.input.EnsureBitsAvailable(7))
                            {
                                this.state = InflaterState.ReadingTreeCodesAfter;
                                return false;
                            }
                            if (this.lengthCode == 16)
                            {
                                if (this.loopCounter == 0)
                                    throw new InvalidOperationException();
                                byte num1 = this.codeList[this.loopCounter - 1];
                                int num2 = this.input.GetBits(2) + 3;
                                if (this.loopCounter + num2 > this.codeArraySize)
                                    throw new InvalidOperationException();
                                for (int index1 = 0; index1 < num2; ++index1)
                                {
                                    byte[] numArray = this.codeList;
                                    int num3 = this.loopCounter;
                                    this.loopCounter = num3 + 1;
                                    int index2 = num3;
                                    int num4 = (int)num1;
                                    numArray[index2] = (byte)num4;
                                }
                            }
                            else if (this.lengthCode == 17)
                            {
                                int num1 = this.input.GetBits(3) + 3;
                                if (this.loopCounter + num1 > this.codeArraySize)
                                    throw new InvalidOperationException();
                                for (int index1 = 0; index1 < num1; ++index1)
                                {
                                    byte[] numArray = this.codeList;
                                    int num2 = this.loopCounter;
                                    this.loopCounter = num2 + 1;
                                    int index2 = num2;
                                    int num3 = 0;
                                    numArray[index2] = (byte)num3;
                                }
                            }
                            else
                            {
                                int num1 = this.input.GetBits(7) + 11;
                                if (this.loopCounter + num1 > this.codeArraySize)
                                    throw new InvalidOperationException();
                                for (int index1 = 0; index1 < num1; ++index1)
                                {
                                    byte[] numArray = this.codeList;
                                    int num2 = this.loopCounter;
                                    this.loopCounter = num2 + 1;
                                    int index2 = num2;
                                    int num3 = 0;
                                    numArray[index2] = (byte)num3;
                                }
                            }
                        }
                        this.state = InflaterState.ReadingTreeCodesBefore;
                    }
                    byte[] codeLengths1 = new byte[288];
                    byte[] codeLengths2 = new byte[32];
                    Array.Copy((Array)this.codeList, (Array)codeLengths1, this.literalLengthCodeCount);
                    Array.Copy((Array)this.codeList, this.literalLengthCodeCount, (Array)codeLengths2, 0, this.distanceCodeCount);
                    if ((int)codeLengths1[256] == 0)
                        throw new InvalidOperationException();
                    this.literalLengthTree = new HuffmanTree(codeLengths1);
                    this.distanceTree = new HuffmanTree(codeLengths2);
                    this.state = InflaterState.DecodeTop;
                    return true;
                default:
                    throw new InvalidOperationException(("UnknownState"));
            }
        }
    }
}
