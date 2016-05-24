// Decompiled with JetBrains decompiler
// Type: System.IO.Compression.DeflaterManaged
// Assembly: System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
// MVID: 67F338CA-9799-462C-9779-9C54DB00C2DD
// Assembly location: C:\Windows\Microsoft.NET\Framework\v4.0.30319\System.dll

using System;

namespace TVGL.IOFunctions.Zip
{
    internal class DeflaterManaged : IDeflater, IDisposable
    {
        private const int MinBlockSize = 256;
        private const int MaxHeaderFooterGoo = 120;
        private const int CleanCopySize = 8072;
        private const double BadCompressionThreshold = 1.0;
        private FastEncoder deflateEncoder;
        private CopyEncoder copyEncoder;
        private DeflateInput input;
        private OutputBuffer output;
        private DeflaterManaged.DeflaterState processingState;
        private DeflateInput inputFromHistory;

        internal DeflaterManaged()
        {
            this.deflateEncoder = new FastEncoder();
            this.copyEncoder = new CopyEncoder();
            this.input = new DeflateInput();
            this.output = new OutputBuffer();
            this.processingState = DeflaterManaged.DeflaterState.NotStarted;
        }

        private bool NeedsInput()
        {
            return ((IDeflater)this).NeedsInput();
        }

        bool IDeflater.NeedsInput()
        {
            if (this.input.Count == 0)
                return this.deflateEncoder.BytesInHistory == 0;
            return false;
        }

        void IDeflater.SetInput(byte[] inputBuffer, int startIndex, int count)
        {
            this.input.Buffer = inputBuffer;
            this.input.Count = count;
            this.input.StartIndex = startIndex;
            if (count <= 0 || count >= 256)
                return;
            switch (this.processingState)
            {
                case DeflaterManaged.DeflaterState.NotStarted:
                case DeflaterManaged.DeflaterState.CheckingForIncompressible:
                    this.processingState = DeflaterManaged.DeflaterState.StartingSmallData;
                    break;
                case DeflaterManaged.DeflaterState.CompressThenCheck:
                    this.processingState = DeflaterManaged.DeflaterState.HandlingSmallData;
                    break;
            }
        }

        int IDeflater.GetDeflateOutput(byte[] outputBuffer)
        {
            this.output.UpdateBuffer(outputBuffer);
            switch (this.processingState)
            {
                case DeflaterManaged.DeflaterState.NotStarted:
                    DeflateInput.InputState state1 = this.input.DumpState();
                    OutputBuffer.BufferState state2 = this.output.DumpState();
                    this.deflateEncoder.GetBlockHeader(this.output);
                    this.deflateEncoder.GetCompressedData(this.input, this.output);
                    if (!this.UseCompressed(this.deflateEncoder.LastCompressionRatio))
                    {
                        this.input.RestoreState(state1);
                        this.output.RestoreState(state2);
                        this.copyEncoder.GetBlock(this.input, this.output, false);
                        this.FlushInputWindows();
                        this.processingState = DeflaterManaged.DeflaterState.CheckingForIncompressible;
                        break;
                    }
                    this.processingState = DeflaterManaged.DeflaterState.CompressThenCheck;
                    break;
                case DeflaterManaged.DeflaterState.SlowDownForIncompressible1:
                    this.deflateEncoder.GetBlockFooter(this.output);
                    this.processingState = DeflaterManaged.DeflaterState.SlowDownForIncompressible2;
                    goto case DeflaterManaged.DeflaterState.SlowDownForIncompressible2;
                case DeflaterManaged.DeflaterState.SlowDownForIncompressible2:
                    if (this.inputFromHistory.Count > 0)
                        this.copyEncoder.GetBlock(this.inputFromHistory, this.output, false);
                    if (this.inputFromHistory.Count == 0)
                    {
                        this.deflateEncoder.FlushInput();
                        this.processingState = DeflaterManaged.DeflaterState.CheckingForIncompressible;
                        break;
                    }
                    break;
                case DeflaterManaged.DeflaterState.StartingSmallData:
                    this.deflateEncoder.GetBlockHeader(this.output);
                    this.processingState = DeflaterManaged.DeflaterState.HandlingSmallData;
                    goto case DeflaterManaged.DeflaterState.HandlingSmallData;
                case DeflaterManaged.DeflaterState.CompressThenCheck:
                    this.deflateEncoder.GetCompressedData(this.input, this.output);
                    if (!this.UseCompressed(this.deflateEncoder.LastCompressionRatio))
                    {
                        this.processingState = DeflaterManaged.DeflaterState.SlowDownForIncompressible1;
                        this.inputFromHistory = this.deflateEncoder.UnprocessedInput;
                        break;
                    }
                    break;
                case DeflaterManaged.DeflaterState.CheckingForIncompressible:
                    DeflateInput.InputState state3 = this.input.DumpState();
                    OutputBuffer.BufferState state4 = this.output.DumpState();
                    this.deflateEncoder.GetBlock(this.input, this.output, 8072);
                    if (!this.UseCompressed(this.deflateEncoder.LastCompressionRatio))
                    {
                        this.input.RestoreState(state3);
                        this.output.RestoreState(state4);
                        this.copyEncoder.GetBlock(this.input, this.output, false);
                        this.FlushInputWindows();
                        break;
                    }
                    break;
                case DeflaterManaged.DeflaterState.HandlingSmallData:
                    this.deflateEncoder.GetCompressedData(this.input, this.output);
                    break;
            }
            return this.output.BytesWritten;
        }

        bool IDeflater.Finish(byte[] outputBuffer, out int bytesRead)
        {
            if (this.processingState == DeflaterManaged.DeflaterState.NotStarted)
            {
                bytesRead = 0;
                return true;
            }
            this.output.UpdateBuffer(outputBuffer);
            if (this.processingState == DeflaterManaged.DeflaterState.CompressThenCheck || this.processingState == DeflaterManaged.DeflaterState.HandlingSmallData || this.processingState == DeflaterManaged.DeflaterState.SlowDownForIncompressible1)
                this.deflateEncoder.GetBlockFooter(this.output);
            this.WriteFinal();
            bytesRead = this.output.BytesWritten;
            return true;
        }

        void IDisposable.Dispose()
        {
        }

        protected void Dispose(bool disposing)
        {
        }

        private bool UseCompressed(double ratio)
        {
            return ratio <= 1.0;
        }

        private void FlushInputWindows()
        {
            this.deflateEncoder.FlushInput();
        }

        private void WriteFinal()
        {
            this.copyEncoder.GetBlock((DeflateInput)null, this.output, true);
        }

        private enum DeflaterState
        {
            NotStarted,
            SlowDownForIncompressible1,
            SlowDownForIncompressible2,
            StartingSmallData,
            CompressThenCheck,
            CheckingForIncompressible,
            HandlingSmallData,
        }
    }
}
